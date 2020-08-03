// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* Licensed to the .NET Foundation under one or more agreements.
 * The .NET Foundation licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information. */

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MessagePack
{
    internal ref partial struct SequenceReader<T>
        where T : unmanaged, IEquatable<T>
    {
        /// <summary>
        /// A value indicating whether we're using <see cref="sequence"/> (as opposed to <see cref="memory"/>.
        /// </summary>
        private bool usingSequence;

        /// <summary>
        /// Backing for the entire sequence when we're not using <see cref="memory"/>.
        /// </summary>
        private ReadOnlySequence<T> sequence;

        /// <summary>
        /// The position at the start of the <see cref="CurrentSpan"/>.
        /// </summary>
        private SequencePosition currentPosition;

        /// <summary>
        /// The position at the end of the <see cref="CurrentSpan"/>.
        /// </summary>
        private SequencePosition nextPosition;

        /// <summary>
        /// Backing for the entire sequence when we're not using <see cref="sequence"/>.
        /// </summary>
        private ReadOnlyMemory<T> memory;

        /// <summary>
        /// A value indicating whether there is unread data remaining.
        /// </summary>
        private bool moreData;

        /// <summary>
        /// The total number of elements in the sequence.
        /// </summary>
        private long length;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceReader{T}"/> struct
        /// over the given <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SequenceReader(in ReadOnlySequence<T> sequence)
        {
            this.usingSequence = true;
            this.CurrentSpanIndex = 0;
            this.Consumed = 0;
            this.sequence = sequence;
            this.memory = default;
            this.currentPosition = sequence.Start;
            this.length = -1;

            ReadOnlySpan<T> first = sequence.First.Span;
            this.nextPosition = sequence.GetPosition(first.Length);
            this.CurrentSpan = first;
            this.moreData = first.Length > 0;

            if (!this.moreData && !sequence.IsSingleSegment)
            {
                this.moreData = true;
                this.GetNextSpan();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceReader{T}"/> struct
        /// over the given <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SequenceReader(ReadOnlyMemory<T> memory)
        {
            this.usingSequence = false;
            this.CurrentSpanIndex = 0;
            this.Consumed = 0;
            this.memory = memory;
            this.CurrentSpan = memory.Span;
            this.length = memory.Length;
            this.moreData = memory.Length > 0;

            this.currentPosition = default;
            this.nextPosition = default;
            this.sequence = default;
        }

        /// <summary>
        /// Gets a value indicating whether there is no more data in the <see cref="Sequence"/>.
        /// </summary>
        public bool End => !this.moreData;

        /// <summary>
        /// Gets the underlying <see cref="ReadOnlySequence{T}"/> for the reader.
        /// </summary>
        public ReadOnlySequence<T> Sequence
        {
            get
            {
                if (this.sequence.IsEmpty && !this.memory.IsEmpty)
                {
                    // We're in memory mode (instead of sequence mode).
                    // Lazily fill in the sequence data.
                    this.sequence = new ReadOnlySequence<T>(this.memory);
                    this.currentPosition = this.sequence.Start;
                    this.nextPosition = this.sequence.End;
                }

                return this.sequence;
            }
        }

        /// <summary>
        /// Gets the current position in the <see cref="Sequence"/>.
        /// </summary>
        public SequencePosition Position
            => this.Sequence.GetPosition(this.CurrentSpanIndex, this.currentPosition);

        /// <summary>
        /// Gets the current segment in the <see cref="Sequence"/> as a span.
        /// </summary>
        public ReadOnlySpan<T> CurrentSpan { get; private set; }

        /// <summary>
        /// Gets the index in the <see cref="CurrentSpan"/>.
        /// </summary>
        public int CurrentSpanIndex { get; private set; }

        /// <summary>
        /// Gets the unread portion of the <see cref="CurrentSpan"/>.
        /// </summary>
        public ReadOnlySpan<T> UnreadSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.CurrentSpan.Slice(this.CurrentSpanIndex);
        }

        /// <summary>
        /// Gets the total number of <typeparamref name="T"/>'s processed by the reader.
        /// </summary>
        public long Consumed { get; private set; }

        /// <summary>
        /// Gets remaining <typeparamref name="T"/>'s in the reader's <see cref="Sequence"/>.
        /// </summary>
        public long Remaining => this.Length - this.Consumed;

        /// <summary>
        /// Gets count of <typeparamref name="T"/> in the reader's <see cref="Sequence"/>.
        /// </summary>
        public long Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (this.length < 0)
                {
                    // Cache the length
                    this.length = this.Sequence.Length;
                }

                return this.length;
            }
        }

        /// <summary>
        /// Peeks at the next value without advancing the reader.
        /// </summary>
        /// <param name="value">The next value or default if at the end.</param>
        /// <returns>False if at the end of the reader.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out T value)
        {
            if (this.moreData)
            {
                value = this.CurrentSpan[this.CurrentSpanIndex];
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Read the next value and advance the reader.
        /// </summary>
        /// <param name="value">The next value or default if at the end.</param>
        /// <returns>False if at the end of the reader.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out T value)
        {
            if (this.End)
            {
                value = default;
                return false;
            }

            value = this.CurrentSpan[this.CurrentSpanIndex];
            this.CurrentSpanIndex++;
            this.Consumed++;

            if (this.CurrentSpanIndex >= this.CurrentSpan.Length)
            {
                if (this.usingSequence)
                {
                    this.GetNextSpan();
                }
                else
                {
                    this.moreData = false;
                }
            }

            return true;
        }

        /// <summary>
        /// Move the reader back the specified number of items.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rewind(long count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            this.Consumed -= count;

            if (this.CurrentSpanIndex >= count)
            {
                this.CurrentSpanIndex -= (int)count;
                this.moreData = true;
            }
            else if (this.usingSequence)
            {
                // Current segment doesn't have enough data, scan backward through segments
                this.RetreatToPreviousSpan(this.Consumed);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Rewind went past the start of the memory.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RetreatToPreviousSpan(long consumed)
        {
            Debug.Assert(this.usingSequence, "usingSequence");
            this.ResetReader();
            this.Advance(consumed);
        }

        private void ResetReader()
        {
            Debug.Assert(this.usingSequence, "usingSequence");
            this.CurrentSpanIndex = 0;
            this.Consumed = 0;
            this.currentPosition = this.Sequence.Start;
            this.nextPosition = this.currentPosition;

            if (this.Sequence.TryGet(ref this.nextPosition, out ReadOnlyMemory<T> memory, advance: true))
            {
                this.moreData = true;

                if (memory.Length == 0)
                {
                    this.CurrentSpan = default;

                    // No data in the first span, move to one with data
                    this.GetNextSpan();
                }
                else
                {
                    this.CurrentSpan = memory.Span;
                }
            }
            else
            {
                // No data in any spans and at end of sequence
                this.moreData = false;
                this.CurrentSpan = default;
            }
        }

        /// <summary>
        /// Get the next segment with available data, if any.
        /// </summary>
        private void GetNextSpan()
        {
            Debug.Assert(this.usingSequence, "usingSequence");
            if (!this.Sequence.IsSingleSegment)
            {
                SequencePosition previousNextPosition = this.nextPosition;
                while (this.Sequence.TryGet(ref this.nextPosition, out ReadOnlyMemory<T> memory, advance: true))
                {
                    this.currentPosition = previousNextPosition;
                    if (memory.Length > 0)
                    {
                        this.CurrentSpan = memory.Span;
                        this.CurrentSpanIndex = 0;
                        return;
                    }
                    else
                    {
                        this.CurrentSpan = default;
                        this.CurrentSpanIndex = 0;
                        previousNextPosition = this.nextPosition;
                    }
                }
            }

            this.moreData = false;
        }

        /// <summary>
        /// Move the reader ahead the specified number of items.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(long count)
        {
            const long TooBigOrNegative = unchecked((long)0xFFFFFFFF80000000);
            if ((count & TooBigOrNegative) == 0 && this.CurrentSpan.Length - this.CurrentSpanIndex > (int)count)
            {
                this.CurrentSpanIndex += (int)count;
                this.Consumed += count;
            }
            else if (this.usingSequence)
            {
                // Can't satisfy from the current span
                this.AdvanceToNextSpan(count);
            }
            else if (this.CurrentSpan.Length - this.CurrentSpanIndex == (int)count)
            {
                this.CurrentSpanIndex += (int)count;
                this.Consumed += count;
                this.moreData = false;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        /// <summary>
        /// Unchecked helper to avoid unnecessary checks where you know count is valid.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AdvanceCurrentSpan(long count)
        {
            Debug.Assert(count >= 0, "count >= 0");

            this.Consumed += count;
            this.CurrentSpanIndex += (int)count;
            if (this.usingSequence && this.CurrentSpanIndex >= this.CurrentSpan.Length)
            {
                this.GetNextSpan();
            }
        }

        /// <summary>
        /// Only call this helper if you know that you are advancing in the current span
        /// with valid count and there is no need to fetch the next one.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AdvanceWithinSpan(long count)
        {
            Debug.Assert(count >= 0, "count >= 0");

            this.Consumed += count;
            this.CurrentSpanIndex += (int)count;

            Debug.Assert(this.CurrentSpanIndex < this.CurrentSpan.Length, "this.CurrentSpanIndex < this.CurrentSpan.Length");
        }

        /// <summary>
        /// Move the reader ahead the specified number of items
        /// if there are enough elements remaining in the sequence.
        /// </summary>
        /// <returns><c>true</c> if there were enough elements to advance; otherwise <c>false</c>.</returns>
        internal bool TryAdvance(long count)
        {
            if (this.Remaining < count)
            {
                return false;
            }

            this.Advance(count);
            return true;
        }

        private void AdvanceToNextSpan(long count)
        {
            Debug.Assert(this.usingSequence, "usingSequence");
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            this.Consumed += count;
            while (this.moreData)
            {
                int remaining = this.CurrentSpan.Length - this.CurrentSpanIndex;

                if (remaining > count)
                {
                    this.CurrentSpanIndex += (int)count;
                    count = 0;
                    break;
                }

                // As there may not be any further segments we need to
                // push the current index to the end of the span.
                this.CurrentSpanIndex += remaining;
                count -= remaining;
                Debug.Assert(count >= 0, "count >= 0");

                this.GetNextSpan();

                if (count == 0)
                {
                    break;
                }
            }

            if (count != 0)
            {
                // Not enough data left- adjust for where we actually ended and throw
                this.Consumed -= count;
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        /// <summary>
        /// Copies data from the current <see cref="Position"/> to the given <paramref name="destination"/> span.
        /// </summary>
        /// <param name="destination">Destination to copy to.</param>
        /// <returns>True if there is enough data to copy to the <paramref name="destination"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<T> destination)
        {
            ReadOnlySpan<T> firstSpan = this.UnreadSpan;
            if (firstSpan.Length >= destination.Length)
            {
                firstSpan.Slice(0, destination.Length).CopyTo(destination);
                return true;
            }

            return this.TryCopyMultisegment(destination);
        }

        internal bool TryCopyMultisegment(Span<T> destination)
        {
            if (this.Remaining < destination.Length)
            {
                return false;
            }

            ReadOnlySpan<T> firstSpan = this.UnreadSpan;
            Debug.Assert(firstSpan.Length < destination.Length, "firstSpan.Length < destination.Length");
            firstSpan.CopyTo(destination);
            int copied = firstSpan.Length;

            SequencePosition next = this.nextPosition;
            while (this.Sequence.TryGet(ref next, out ReadOnlyMemory<T> nextSegment, true))
            {
                if (nextSegment.Length > 0)
                {
                    ReadOnlySpan<T> nextSpan = nextSegment.Span;
                    int toCopy = Math.Min(nextSpan.Length, destination.Length - copied);
                    nextSpan.Slice(0, toCopy).CopyTo(destination.Slice(copied));
                    copied += toCopy;
                    if (copied >= destination.Length)
                    {
                        break;
                    }
                }
            }

            return true;
        }
    }
}

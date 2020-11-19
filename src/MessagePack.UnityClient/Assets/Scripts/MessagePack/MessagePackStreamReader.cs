// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nerdbank.Streams;

namespace MessagePack
{
    /// <summary>
    /// Reads one or more messagepack data structures from a <see cref="Stream"/>.
    /// </summary>
    /// <remarks>
    /// This class is *not* thread-safe. Do not call more than one member at once and be sure any call completes (including asynchronous tasks)
    /// before calling the next one.
    /// </remarks>
    public partial class MessagePackStreamReader : IDisposable
    {
        private readonly Stream stream;
        private readonly bool leaveOpen;
        private SequencePool.Rental sequenceRental;
        private SequencePosition? endOfLastMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackStreamReader"/> class.
        /// </summary>
        /// <param name="stream">The stream to read from. This stream will be disposed of when this <see cref="MessagePackStreamReader"/> is disposed.</param>
        public MessagePackStreamReader(Stream stream)
            : this(stream, leaveOpen: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackStreamReader"/> class.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="leaveOpen">If true, leaves the stream open after this <see cref="MessagePackStreamReader"/> is disposed; otherwise, false.</param>
        public MessagePackStreamReader(Stream stream, bool leaveOpen)
            : this(stream, leaveOpen, SequencePool.Shared)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackStreamReader"/> class.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="leaveOpen">If true, leaves the stream open after this <see cref="MessagePackStreamReader"/> is disposed; otherwise, false.</param>
        /// <param name="sequencePool">The pool to rent a <see cref="Sequence{T}"/> object from.</param>
        public MessagePackStreamReader(Stream stream, bool leaveOpen, SequencePool sequencePool)
        {
            if (sequencePool == null)
            {
                throw new ArgumentNullException(nameof(sequencePool));
            }

            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.leaveOpen = leaveOpen;
            this.sequenceRental = sequencePool.Rent();
        }

        /// <summary>
        /// Gets any bytes that have been read since the last complete message returned from <see cref="ReadAsync(CancellationToken)"/>.
        /// </summary>
        public ReadOnlySequence<byte> RemainingBytes => this.endOfLastMessage.HasValue ? this.ReadData.AsReadOnlySequence.Slice(this.endOfLastMessage.Value) : this.ReadData.AsReadOnlySequence;

        /// <summary>
        /// Gets the sequence that we read data from the <see cref="stream"/> into.
        /// </summary>
        private Sequence<byte> ReadData => this.sequenceRental.Value;

        /// <summary>
        /// Reads the next whole (top-level) messagepack data structure.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// A task whose result is the next whole data structure from the stream, or <c>null</c> if the stream ends.
        /// The returned sequence is valid until this <see cref="MessagePackStreamReader"/> is disposed or
        /// until this method is called again, whichever comes first.
        /// </returns>
        /// <remarks>
        /// When <c>null</c> is the result of the returned task,
        /// any extra bytes read (between the last complete message and the end of the stream) will be available via the <see cref="RemainingBytes"/> property.
        /// </remarks>
        public async ValueTask<ReadOnlySequence<byte>?> ReadAsync(CancellationToken cancellationToken)
        {
            this.RecycleLastMessage();

            while (true)
            {
                // Check if we have a complete message and return it if we have it.
                // We do this before reading anything since a previous read may have brought in several messages.
                cancellationToken.ThrowIfCancellationRequested();
                if (this.TryReadNextMessage(out ReadOnlySequence<byte> completeMessage))
                {
                    return completeMessage;
                }

                if (!await this.TryReadMoreDataAsync(cancellationToken).ConfigureAwait(false))
                {
                    // We've reached the end of the stream.
                    // We already checked for a complete message with what we already had, so evidently it's not a complete message.
                    return null;
                }
            }
        }

        /// <summary>
        /// Arranges for the next read operation to start by reading from the underlying <see cref="Stream"/>
        /// instead of any data buffered from a previous read.
        /// </summary>
        /// <remarks>
        /// This is appropriate if the underlying <see cref="Stream"/> has been repositioned such that
        /// any previously buffered data is no longer applicable to what the caller wants to read.
        /// </remarks>
        public void DiscardBufferedData()
        {
            this.sequenceRental.Value.Reset();
            this.endOfLastMessage = default;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if this instance is being disposed; <see langword="false"/> if it is being finalized.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.leaveOpen)
                {
                    this.stream.Dispose();
                }

                this.sequenceRental.Dispose();
                this.sequenceRental = default;
            }
        }

        /// <summary>
        /// Recycle memory from a previously returned message.
        /// </summary>
        private void RecycleLastMessage()
        {
            if (this.endOfLastMessage.HasValue)
            {
                // A previously returned message can now be safely recycled since the caller wants more.
                this.ReadData.AdvanceTo(this.endOfLastMessage.Value);
                this.endOfLastMessage = null;
            }
        }

        /// <summary>
        /// Read more data from the stream into the <see cref="ReadData"/> buffer.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns><c>true</c> if more data was read; <c>false</c> if the end of the stream had already been reached.</returns>
        private async Task<bool> TryReadMoreDataAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Memory<byte> buffer = this.ReadData.GetMemory(sizeHint: 0);
            int bytesRead = 0;
            try
            {
                bytesRead = await this.stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                return bytesRead > 0;
            }
            finally
            {
                // Keep our state clean in case the caller wants to call us again.
                this.ReadData.Advance(bytesRead);
            }
        }

        /// <summary>
        /// Checks whether the content in <see cref="ReadData"/> include a complete messagepack structure.
        /// </summary>
        /// <param name="completeMessage">Receives the sequence of the first complete data structure found, if any.</param>
        /// <returns><c>true</c> if a complete data structure was found; <c>false</c> otherwise.</returns>
        private bool TryReadNextMessage(out ReadOnlySequence<byte> completeMessage)
        {
            if (this.ReadData.Length > 0)
            {
                var reader = new MessagePackReader(this.ReadData);

                // Perf opportunity: instead of skipping from the start each time, we could incrementally skip across tries
                // possibly as easy as simply keeping a count of how many tokens still need to be skipped (that we know about).
                if (reader.TrySkip())
                {
                    this.endOfLastMessage = reader.Position;
                    completeMessage = reader.Sequence.Slice(0, reader.Position);
                    return true;
                }
            }

            completeMessage = default;
            return false;
        }
    }
}

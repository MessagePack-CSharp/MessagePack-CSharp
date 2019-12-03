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
    public class MessagePackStreamReader : IDisposable
    {
        private readonly Stream stream;
        private SequencePool.Rental sequenceRental = SequencePool.Shared.Rent();
        private SequencePosition? endOfLastMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackStreamReader"/> class.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        public MessagePackStreamReader(Stream stream)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
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
            if (this.endOfLastMessage.HasValue)
            {
                // A previously returned message can now be safely recycled since the caller wants more.
                this.ReadData.AdvanceTo(this.endOfLastMessage.Value);
                this.endOfLastMessage = null;
            }

            while (true)
            {
                // Check if we have a complete message and return it if we have it.
                // We do this before reading anything since a previous read may have brought in several messages.
                cancellationToken.ThrowIfCancellationRequested();
                if (this.TryReadNextMessage(out ReadOnlySequence<byte> completeMessage))
                {
                    return completeMessage;
                }

                cancellationToken.ThrowIfCancellationRequested();
                Memory<byte> buffer = this.ReadData.GetMemory(sizeHint: 0);
                int bytesRead = 0;
                try
                {
                    bytesRead = await this.stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        // We've reached the end of the stream.
                        // We already checked for a complete message with what we already had, so evidently it's not a complete message.
                        return null;
                    }
                }
                finally
                {
                    // Keep our state clean in case the caller wants to call us again.
                    this.ReadData.Advance(bytesRead);
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.stream.Dispose();
            this.sequenceRental.Dispose();
            this.sequenceRental = default;
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

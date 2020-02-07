// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Nerdbank.Streams;

namespace MessagePack
{
    public partial class MessagePackStreamReader : IDisposable
    {
        /// <summary>
        /// Reads the next messagepack array and produces each element individually.
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
        public async IAsyncEnumerable<ReadOnlySequence<byte>> ReadArrayAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            this.RecycleLastMessage();

            cancellationToken.ThrowIfCancellationRequested();
            int length;
            while (!this.TryReadArrayHeader(out length))
            {
                if (!await this.TryReadMoreDataAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new EndOfStreamException("The stream ended before an array header could be found.");
                }
            }

            for (int i = 0; i < length; i++)
            {
                var element = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
                if (element.HasValue)
                {
                    yield return element.Value;
                }
                else
                {
                    throw new EndOfStreamException("Stream ended before all elements were read.");
                }
            }
        }

        /// <summary>
        /// Reads an array header from <see cref="ReadData"/> if there are enough bytes to do so.
        /// </summary>
        /// <param name="length">Receives the length of the array, if its header could be read.</param>
        /// <returns><c>true</c> if the array header was found and complete; <c>false</c> if there were insufficient bytes to read the header.</returns>
        /// <exception cref="MessagePackSerializationException">Thrown if the next msgpack structure is not an array header.</exception>
        private bool TryReadArrayHeader(out int length)
        {
            if (this.ReadData.Length > 0)
            {
                var reader = new MessagePackReader(this.ReadData);
                if (reader.TryReadArrayHeader(out length))
                {
                    this.endOfLastMessage = reader.Position;
                    return true;
                }
            }

            length = 0;
            return false;
        }
    }
}

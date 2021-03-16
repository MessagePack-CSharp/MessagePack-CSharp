// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePack
{
    public partial class MessagePackStreamReader : IDisposable
    {
        /// <summary>
        /// Reads the next messagepack map header.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// A task whose result is the size of the next map from the stream.
        /// </returns>
        /// <remarks>
        /// Any extra bytes read (between the last complete message and the end of the stream) will be available via the <see cref="RemainingBytes"/> property.
        /// </remarks>
        public async ValueTask<int> ReadMapHeaderAsync(CancellationToken cancellationToken)
        {
            this.RecycleLastMessage();

            cancellationToken.ThrowIfCancellationRequested();
            int count;
            while (!this.TryReadMapHeader(out count))
            {
                if (!await this.TryReadMoreDataAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new EndOfStreamException("The stream ended before a map header could be found.");
                }
            }

            return count;
        }

        /// <summary>
        /// Reads a map header from <see cref="ReadData"/> if there are enough bytes to do so.
        /// </summary>
        /// <param name="count">Receives the size of the map, if its header could be read.</param>
        /// <returns><c>true</c> if the map header was found and complete; <c>false</c> if there were insufficient bytes to read the header.</returns>
        /// <exception cref="MessagePackSerializationException">Thrown if the next msgpack structure is not a map header.</exception>
        private bool TryReadMapHeader(out int count)
        {
            if (this.ReadData.Length > 0)
            {
                var reader = new MessagePackReader(this.ReadData);
                if (reader.TryReadMapHeader(out count))
                {
                    this.endOfLastMessage = reader.Position;
                    return true;
                }
            }

            count = 0;
            return false;
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePack.Tests
{
    internal class OneByteAtATimeStream : Stream
    {
        private readonly ReadOnlySequence<byte> source;

        private SequencePosition position;

        internal OneByteAtATimeStream(ReadOnlySequence<byte> source)
        {
            this.source = source;
            this.position = source.Start;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (this.position.Equals(this.source.End))
            {
                return Task.FromResult(0);
            }

            buffer[offset] = this.source.Slice(this.position, 1).First.Span[0];
            this.position = this.source.GetPosition(1, this.position);
            return Task.FromResult(1);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests
{
    public class MessagePackWriterTests
    {
        /// <summary>
        /// Verifies that <see cref="MessagePackWriter.WriteRaw(in ReadOnlySpan{byte})"/>
        /// accepts a span that came from stackalloc.
        /// </summary>
        [Fact]
        public void WriteRaw_StackAllocatedSpan()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);

            Span<byte> bytes = stackalloc byte[8];
            bytes[0] = 1;
            bytes[7] = 2;
            writer.WriteRaw(bytes);
            writer.Flush();
            Assert.Equal(1, sequence.AsReadOnlySequence.First.Span[0]);
            Assert.Equal(2, sequence.AsReadOnlySequence.First.Span[7]);
        }
    }
}

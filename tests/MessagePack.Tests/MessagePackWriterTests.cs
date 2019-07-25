// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests
{
    public class MessagePackWriterTests
    {
        /// <summary>
        /// Verifies that <see cref="MessagePackWriter.WriteRaw(ReadOnlySpan{byte})"/>
        /// accepts a span that came from stackalloc.
        /// </summary>
        [Fact]
        public unsafe void WriteRaw_StackAllocatedSpan()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);

            Span<byte> bytes = stackalloc byte[8];
            bytes[0] = 1;
            bytes[7] = 2;
            fixed (byte* pBytes = bytes)
            {
                var flexSpan = new Span<byte>(pBytes, bytes.Length);
                writer.WriteRaw(flexSpan);
            }

            writer.Flush();
            var written = sequence.AsReadOnlySequence.ToArray();
            Assert.Equal(1, written[0]);
            Assert.Equal(2, written[7]);
        }

        [Fact]
        public void WriteExtensionFormatHeader_NegativeExtension()
        {
            var sequence = new Sequence<byte>();
            var writer = new MessagePackWriter(sequence);

            var header = new ExtensionHeader(-1, 10);
            writer.WriteExtensionFormatHeader(header);
            writer.Flush();

            var written = sequence.AsReadOnlySequence;
            var reader = new MessagePackReader(written);
            var readHeader = reader.ReadExtensionFormatHeader();

            Assert.Equal(header.TypeCode, readHeader.TypeCode);
            Assert.Equal(header.Length, readHeader.Length);
        }
    }
}

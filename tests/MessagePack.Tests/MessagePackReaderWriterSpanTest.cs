// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace MessagePack.Tests
{
    public class MessagePackReaderWriterSpanTest
    {
        [Fact]
        public void WriterTest()
        {
            Span<byte> span = stackalloc byte[100];

            var writer = new MessagePackWriter(span);

            writer.WriteArrayHeader(5);
            writer.Write(1);
            writer.Write(10);
            writer.Write(100);
            writer.Write(1000);
            writer.Write(10000);

            span = span.Slice(0, (int)writer.WrittenCount);

            var bin = MessagePackSerializer.Deserialize<int[]>(span.ToArray());

            bin.Is(1, 10, 100, 1000, 1000);
        }

        [Fact]
        public void WriterThrow()
        {
            Span<byte> span = stackalloc byte[5];

            var writer = new MessagePackWriter(span);

            writer.WriteArrayHeader(5); // +1
            writer.Write(1); // +1
            writer.Write(10); // +1
            writer.Write(100); // +1

            try
            {
                writer.Write(1000); // +3, can't grow
                Assert.Fail("should throw exceptions.");
            }
            catch (Exception ex)
            {
                if (ex is not InvalidOperationException)
                {
                    throw;
                }
            }
        }

        [Fact]
        public void ReaderTest()
        {
            var bin = MessagePackSerializer.Serialize(new int[] { 1, 10, 100, 1000, 10000 });
            var span = bin.AsSpan();

            var reader = new MessagePackReader(span);

            var len = reader.ReadArrayHeader();
            var array = new int[len];
            array[0] = reader.ReadInt32();
            array[1] = reader.ReadInt32();
            array[2] = reader.ReadInt32();
            array[3] = reader.ReadInt32();
            array[4] = reader.ReadInt32();

            array.Length.Is(5);
            array.Is(1, 10, 100, 1000, 10000);
        }
    }
}

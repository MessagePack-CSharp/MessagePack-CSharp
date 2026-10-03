// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Linq;
using System.Reflection;
using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests
{
    public class LZ4MalformedInputTests
    {
        private const int OverflowExtensionCount = 8_421_505;

        private unsafe delegate int Decoder(byte* input, int inputLength, byte* output, int outputLength);

        [Theory]
        [InlineData(32, false)]
        [InlineData(32, true)]
        [InlineData(64, false)]
        [InlineData(64, true)]
        [Trait("CWE", "190")]
        public void Decode_OverflowingLength_RejectsBeforeOverflow(int bits, bool match)
        {
            Assert.True(15L + (255L * OverflowExtensionCount) > int.MaxValue);
            byte[] input = CreateLengthRun(match, OverflowExtensionCount, terminate: true);

            int consumed = Decode(bits, input, 4096, out _);

            Assert.True(consumed < 0);
            Assert.True(-consumed < 32, "Lengths must be rejected as soon as they exceed the output capacity.");
        }

        [Theory]
        [InlineData(MessagePackCompression.Lz4Block, false)]
        [InlineData(MessagePackCompression.Lz4Block, true)]
        [InlineData(MessagePackCompression.Lz4BlockArray, false)]
        [InlineData(MessagePackCompression.Lz4BlockArray, true)]
        [Trait("CWE", "190")]
        public void Deserialize_OverflowingLength_ThrowsSerializationException(MessagePackCompression compression, bool match)
        {
            byte[] input = CreateLengthRun(match, OverflowExtensionCount, terminate: true);
            byte[] payload = Wrap(input, 4096, compression);
            var options = MessagePackSerializerOptions.Standard
                .WithCompression(compression)
                .WithSecurity(MessagePackSecurity.UntrustedData);

            MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(
                () => MessagePackSerializer.Deserialize<object>(payload, options));

            Assert.Contains("LZ4 block is corrupted", FlattenMessages(ex));
        }

        [Theory]
        [InlineData(32, false)]
        [InlineData(32, true)]
        [InlineData(64, false)]
        [InlineData(64, true)]
        public void Decode_TruncatedLengthExtension_RejectsInput(int bits, bool match)
        {
            byte[] input = CreateLengthRun(match, 1, terminate: false);

            Assert.True(Decode(bits, input, 4096, out _) < 0);
        }

        [Theory]
        [InlineData(32)]
        [InlineData(64)]
        public void Decode_LiteralExceedingOutput_RejectsInput(int bits)
        {
            byte[] input = new byte[] { 0xF0, 0x00 }.Concat(new byte[15]).ToArray();

            Assert.True(Decode(bits, input, 14, out _) < 0);
        }

        [Theory]
        [InlineData(32, 0)]
        [InlineData(32, 1)]
        [InlineData(64, 0)]
        [InlineData(64, 1)]
        public void Decode_ValidExtendedLiteral_FillsOutputExactly(int bits, int extensions)
        {
            int length = 15 + (255 * extensions);
            byte[] expected = Enumerable.Repeat((byte)0x41, length).ToArray();
            byte[] input = CreateLengthRun(false, extensions, terminate: true).Concat(expected).ToArray();

            Assert.Equal(input.Length, Decode(bits, input, length, out byte[] actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(32, 0)]
        [InlineData(32, 1)]
        [InlineData(64, 0)]
        [InlineData(64, 1)]
        public void Decode_ValidExtendedMatch_ReservesLastLiterals(int bits, int extensions)
        {
            byte[] lastLiterals = { 0x42, 0x43, 0x44, 0x45, 0x46 };
            byte[] input = CreateLengthRun(true, extensions, terminate: true)
                .Concat(new byte[] { 0x50 }).Concat(lastLiterals).ToArray();
            byte[] expected = Enumerable.Repeat((byte)0x41, 1 + 15 + (255 * extensions) + 4)
                .Concat(lastLiterals).ToArray();

            Assert.True(Decode(bits, input, expected.Length - 1, out _) < 0);
            Assert.Equal(input.Length, Decode(bits, input, expected.Length, out byte[] actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(32)]
        [InlineData(64)]
        public void Decode_MatchWithoutExtensionExceedingOutput_RejectsInput(int bits)
        {
            byte[] input = { 0x1E, 0x41, 0x01, 0x00, 0x50, 0x42, 0x43, 0x44, 0x45, 0x46 };

            Assert.True(Decode(bits, input, 22, out _) < 0);
        }

        private static byte[] CreateLengthRun(bool match, int extensions, bool terminate)
        {
            int prefixLength = match ? 4 : 1;
            byte[] input = new byte[prefixLength + extensions + (terminate ? 1 : 0)];
            input[0] = match ? (byte)0x1F : (byte)0xF0;
            if (match)
            {
                input[1] = 0x41;
                input[2] = 0x01;
            }

            for (int i = prefixLength; i < prefixLength + extensions; i++)
            {
                input[i] = 0xFF;
            }

            return input;
        }

        private static unsafe int Decode(int bits, byte[] input, int outputLength, out byte[] output)
        {
            Type codec = typeof(MessagePackSerializer).Assembly.GetType("MessagePack.LZ4.LZ4Codec", throwOnError: true)!;
            var method = codec.GetMethod("LZ4_uncompress_" + bits, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            var decoder = (Decoder)method!.CreateDelegate(typeof(Decoder));

            // Padding accommodates the decoder's intentional wide reads; canaries detect output overruns.
            byte[] paddedInput = new byte[input.Length + 8];
            Array.Copy(input, paddedInput, input.Length);
            byte[] guardedOutput = Enumerable.Repeat((byte)0xCC, outputLength + 16).ToArray();
            int consumed;
            fixed (byte* inputPointer = paddedInput)
            fixed (byte* outputPointer = guardedOutput)
            {
                consumed = decoder(inputPointer, input.Length, outputPointer + 8, outputLength);
            }

            Assert.All(guardedOutput.Take(8), value => Assert.Equal((byte)0xCC, value));
            Assert.All(guardedOutput.Skip(8 + outputLength), value => Assert.Equal((byte)0xCC, value));
            output = guardedOutput.Skip(8).Take(outputLength).ToArray();
            return consumed;
        }

        private static byte[] Wrap(byte[] input, int outputLength, MessagePackCompression compression)
        {
            byte[] sizeBuffer = MessagePackSerializer.Serialize(outputLength, MessagePackSerializerOptions.Standard);
            using var buffer = new Sequence<byte>();
            var writer = new MessagePackWriter(buffer);
            if (compression == MessagePackCompression.Lz4Block)
            {
                writer.WriteExtensionFormatHeader(new ExtensionHeader(99, sizeBuffer.Length + input.Length));
                writer.WriteRaw(sizeBuffer);
                writer.WriteRaw(input);
            }
            else
            {
                writer.WriteArrayHeader(2);
                writer.WriteExtensionFormatHeader(new ExtensionHeader(98, sizeBuffer.Length));
                writer.WriteRaw(sizeBuffer);
                writer.WriteBinHeader(input.Length);
                writer.WriteRaw(input);
            }

            writer.Flush();
            return ((ReadOnlySequence<byte>)buffer).ToArray();
        }

        private static string FlattenMessages(Exception ex)
        {
            return ex.InnerException is null ? ex.Message : ex.Message + " " + FlattenMessages(ex.InnerException);
        }
    }
}

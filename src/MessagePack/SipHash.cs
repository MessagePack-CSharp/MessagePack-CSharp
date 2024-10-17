// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//// Originally from: https://github.com/paya-cz/siphash
//// Author:          Pavel Werl
//// License:         Public Domain
//// SipHash website: https://131002.net/siphash/

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace MessagePack
{
    /// <summary>
    /// Implements the <see href="https://en.wikipedia.org/wiki/SipHash">SipHash pseudo-random function</see>.
    /// </summary>
    /// <remarks>
    /// This class is immutable and thread-safe.
    /// </remarks>
    internal class SipHash
    {
        /// <summary>
        /// Part of the initial 256-bit internal state.
        /// </summary>
        private readonly ulong initialState0;

        /// <summary>
        /// Part of the initial 256-bit internal state.
        /// </summary>
        private readonly ulong initialState1;

        /// <summary>Initializes a new instance of the <see cref="SipHash"/> class using a random key.</summary>
        public SipHash()
        {
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
#if SPAN_BUILTIN
            Span<byte> key = stackalloc byte[16];
            rng.GetBytes(key);
#else
            byte[] buffer = ArrayPool<byte>.Shared.Rent(16);
            rng.GetBytes(buffer, 0, 16);
            Span<byte> key = buffer;
#endif

            this.initialState0 = 0x736f6d6570736575UL ^ BinaryPrimitives.ReadUInt64LittleEndian(key);
            this.initialState1 = 0x646f72616e646f6dUL ^ BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(sizeof(ulong)));

#if !SPAN_BUILTIN
            ArrayPool<byte>.Shared.Return(buffer);
#endif
        }

        /// <summary>Initializes a new instance of the <see cref="SipHash"/> class using the specified 128-bit key.</summary>
        /// <param name="key">Key for the SipHash pseudo-random function. Must be exactly 16 bytes long.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is not exactly 16 bytes long (128 bits).</exception>
        public SipHash(ReadOnlySpan<byte> key)
        {
            if (key.Length != 16)
            {
                throw new ArgumentException("SipHash key must be exactly 128-bit long (16 bytes).", nameof(key));
            }

            this.initialState0 = 0x736f6d6570736575UL ^ BinaryPrimitives.ReadUInt64LittleEndian(key);
            this.initialState1 = 0x646f72616e646f6dUL ^ BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(sizeof(ulong)));
        }

        /// <summary>
        /// Gets a 128-bit SipHash key.
        /// </summary>
        /// <param name="key">The 16-byte buffer that receives the key originally provided to the constructor.</param>
        public void GetKey(Span<byte> key)
        {
            if (key.Length != 16)
            {
                throw new ArgumentException("SipHash key must be exactly 128-bit long (16 bytes).", nameof(key));
            }

            BinaryPrimitives.WriteUInt64LittleEndian(key, this.initialState0 ^ 0x736f6d6570736575UL);
            BinaryPrimitives.WriteUInt64LittleEndian(key.Slice(sizeof(ulong)), this.initialState1 ^ 0x646f72616e646f6dUL);
        }

        /// <summary>Computes 64-bit SipHash tag for the specified message.</summary>
        /// <param name="data">The byte array for which to computer SipHash tag.</param>
        /// <returns>Returns 64-bit (8 bytes) SipHash tag.</returns>
        public long Compute(ReadOnlySpan<byte> data)
        {
            static T Read<T>(in byte start)
                where T : unmanaged => Unsafe.ReadUnaligned<T>(ref Unsafe.AsRef(in start));

            unchecked
            {
                // SipHash internal state
                ulong v0 = this.initialState0;
                ulong v1 = this.initialState1;

                // It is faster to load the initialStateX fields from memory again than to reference v0 and v1:
                ulong v2 = 0x1F160A001E161714UL ^ this.initialState0;
                ulong v3 = 0x100A160317100A1EUL ^ this.initialState1;

                // We process data in 64-bit blocks
                ulong block;

                // The last 64-bit block of data
                int finalBlockPosition = data.Length & ~7;

                // Process the input data in blocks of 64 bits
                for (int blockPosition = 0; blockPosition < finalBlockPosition; blockPosition += sizeof(ulong))
                {
                    block = Read<ulong>(data[blockPosition]);

                    v3 ^= block;

                    // Round 1
                    v0 += v1;
                    v2 += v3;
                    v1 = v1 << 13 | v1 >> 51;
                    v3 = v3 << 16 | v3 >> 48;
                    v1 ^= v0;
                    v3 ^= v2;
                    v0 = v0 << 32 | v0 >> 32;
                    v2 += v1;
                    v0 += v3;
                    v1 = v1 << 17 | v1 >> 47;
                    v3 = v3 << 21 | v3 >> 43;
                    v1 ^= v2;
                    v3 ^= v0;
                    v2 = v2 << 32 | v2 >> 32;

                    // Round 2
                    v0 += v1;
                    v2 += v3;
                    v1 = v1 << 13 | v1 >> 51;
                    v3 = v3 << 16 | v3 >> 48;
                    v1 ^= v0;
                    v3 ^= v2;
                    v0 = v0 << 32 | v0 >> 32;
                    v2 += v1;
                    v0 += v3;
                    v1 = v1 << 17 | v1 >> 47;
                    v3 = v3 << 21 | v3 >> 43;
                    v1 ^= v2;
                    v3 ^= v0;
                    v2 = v2 << 32 | v2 >> 32;

                    v0 ^= block;
                }

                // Load the remaining bytes
                block = (ulong)data.Length << 56;
                switch (data.Length & 7)
                {
                    case 7:
                        block |= Read<uint>(data[finalBlockPosition]) | (ulong)Read<ushort>(data[finalBlockPosition + 4]) << 32 | (ulong)data[finalBlockPosition + 6] << 48;
                        break;
                    case 6:
                        block |= Read<uint>(data[finalBlockPosition]) | (ulong)Read<ushort>(data[finalBlockPosition + 4]) << 32;
                        break;
                    case 5:
                        block |= Read<uint>(data[finalBlockPosition]) | (ulong)data[finalBlockPosition + 4] << 32;
                        break;
                    case 4:
                        block |= Read<uint>(data[finalBlockPosition]);
                        break;
                    case 3:
                        block |= Read<ushort>(data[finalBlockPosition]) | (ulong)data[finalBlockPosition + 2] << 16;
                        break;
                    case 2:
                        block |= Read<ushort>(data[finalBlockPosition]);
                        break;
                    case 1:
                        block |= data[finalBlockPosition];
                        break;
                }

                // Process the final block
                {
                    v3 ^= block;

                    // Round 1
                    v0 += v1;
                    v2 += v3;
                    v1 = v1 << 13 | v1 >> 51;
                    v3 = v3 << 16 | v3 >> 48;
                    v1 ^= v0;
                    v3 ^= v2;
                    v0 = v0 << 32 | v0 >> 32;
                    v2 += v1;
                    v0 += v3;
                    v1 = v1 << 17 | v1 >> 47;
                    v3 = v3 << 21 | v3 >> 43;
                    v1 ^= v2;
                    v3 ^= v0;
                    v2 = v2 << 32 | v2 >> 32;

                    // Round 2
                    v0 += v1;
                    v2 += v3;
                    v1 = v1 << 13 | v1 >> 51;
                    v3 = v3 << 16 | v3 >> 48;
                    v1 ^= v0;
                    v3 ^= v2;
                    v0 = v0 << 32 | v0 >> 32;
                    v2 += v1;
                    v0 += v3;
                    v1 = v1 << 17 | v1 >> 47;
                    v3 = v3 << 21 | v3 >> 43;
                    v1 ^= v2;
                    v3 ^= v0;
                    v2 = v2 << 32 | v2 >> 32;

                    v0 ^= block;
                    v2 ^= 0xff;
                }

                // 4 finalization rounds
                {
                    // Round 1
                    v0 += v1;
                    v2 += v3;
                    v1 = v1 << 13 | v1 >> 51;
                    v3 = v3 << 16 | v3 >> 48;
                    v1 ^= v0;
                    v3 ^= v2;
                    v0 = v0 << 32 | v0 >> 32;
                    v2 += v1;
                    v0 += v3;
                    v1 = v1 << 17 | v1 >> 47;
                    v3 = v3 << 21 | v3 >> 43;
                    v1 ^= v2;
                    v3 ^= v0;
                    v2 = v2 << 32 | v2 >> 32;

                    // Round 2
                    v0 += v1;
                    v2 += v3;
                    v1 = v1 << 13 | v1 >> 51;
                    v3 = v3 << 16 | v3 >> 48;
                    v1 ^= v0;
                    v3 ^= v2;
                    v0 = v0 << 32 | v0 >> 32;
                    v2 += v1;
                    v0 += v3;
                    v1 = v1 << 17 | v1 >> 47;
                    v3 = v3 << 21 | v3 >> 43;
                    v1 ^= v2;
                    v3 ^= v0;
                    v2 = v2 << 32 | v2 >> 32;

                    // Round 3
                    v0 += v1;
                    v2 += v3;
                    v1 = v1 << 13 | v1 >> 51;
                    v3 = v3 << 16 | v3 >> 48;
                    v1 ^= v0;
                    v3 ^= v2;
                    v0 = v0 << 32 | v0 >> 32;
                    v2 += v1;
                    v0 += v3;
                    v1 = v1 << 17 | v1 >> 47;
                    v3 = v3 << 21 | v3 >> 43;
                    v1 ^= v2;
                    v3 ^= v0;
                    v2 = v2 << 32 | v2 >> 32;

                    // Round 4
                    v0 += v1;
                    v2 += v3;
                    v1 = v1 << 13 | v1 >> 51;
                    v3 = v3 << 16 | v3 >> 48;
                    v1 ^= v0;
                    v3 ^= v2;
                    v0 = v0 << 32 | v0 >> 32;
                    v2 += v1;
                    v0 += v3;
                    v1 = v1 << 17 | v1 >> 47;
                    v3 = v3 << 21 | v3 >> 43;
                    v1 ^= v2;
                    v3 ^= v0;
                    v2 = v2 << 32 | v2 >> 32;
                }

                return (long)((v0 ^ v1) ^ (v2 ^ v3));
            }
        }
    }
}

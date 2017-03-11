#if ENABLE_UNSAFE_MSGPACK

#region license

/*
Copyright (c) 2013, Milosz Krajewski
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided
that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions
  and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of conditions
  and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN
IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

#endregion

using System;

namespace MessagePack.LZ4
{
    /// <summary>Unsafe LZ4 codec.</summary>
    public static partial class LZ4Codec
    {
        /// <summary>Copies block of memory.</summary>
        /// <param name="src">The source.</param>
        /// <param name="dst">The destination.</param>
        /// <param name="len">The length (in bytes).</param>
        private static unsafe void BlockCopy(byte* src, byte* dst, int len)
        {
            while (len >= 8)
            {
                *(ulong*)dst = *(ulong*)src;
                dst += 8;
                src += 8;
                len -= 8;
            }
            if (len >= 4)
            {
                *(uint*)dst = *(uint*)src;
                dst += 4;
                src += 4;
                len -= 4;
            }
            if (len >= 2)
            {
                *(ushort*)dst = *(ushort*)src;
                dst += 2;
                src += 2;
                len -= 2;
            }
            if (len >= 1)
            {
                *dst = *src; /* d++; s++; l--; */
            }
        }

        /// <summary>Encodes the specified input.</summary>
        /// <param name="input">The input.</param>
        /// <param name="inputOffset">The input offset.</param>
        /// <param name="inputLength">Length of the input.</param>
        /// <param name="output">The output.</param>
        /// <param name="outputOffset">The output offset.</param>
        /// <param name="outputLength">Length of the output.</param>
        /// <returns>Number of bytes written.</returns>
        public static unsafe int Encode32Unsafe(
            byte[] input,
            int inputOffset,
            int inputLength,
            byte[] output,
            int outputOffset,
            int outputLength)
        {
            CheckArguments(input, inputOffset, inputLength, output, outputOffset, outputLength);

            if (outputLength == 0) return 0;

            fixed (byte* inputPtr = &input[inputOffset])
            fixed (byte* outputPtr = &output[outputOffset])
            {
                if (inputLength < LZ4_64KLIMIT)
                {
                    var uHashTable = HashTablePool.GetUShortHashTablePool();
                    fixed (ushort* hash1 = &uHashTable[0])
                    {
                        return LZ4_compress64kCtx_32(hash1, inputPtr, outputPtr, inputLength, outputLength);
                    }
                }
                else
                {
                    var bHashTable = HashTablePool.GetUIntHashTablePool();
                    fixed (uint* hash2 = &bHashTable[0])
                    {
                        return LZ4_compressCtx_32(hash2, inputPtr, outputPtr, inputLength, outputLength);
                    }
                }
            }
        }

        /// <summary>Decodes the specified input.</summary>
        /// <param name="input">The input.</param>
        /// <param name="inputOffset">The input offset.</param>
        /// <param name="inputLength">Length of the input.</param>
        /// <param name="output">The output.</param>
        /// <param name="outputOffset">The output offset.</param>
        /// <param name="outputLength">Length of the output.</param>
        /// <returns>Number of bytes written.</returns>
        public static unsafe int Decode32Unsafe(
            byte[] input,
            int inputOffset,
            int inputLength,
            byte[] output,
            int outputOffset,
            int outputLength)
        {
            CheckArguments(
                input, inputOffset, inputLength,
                output, outputOffset, outputLength);

            if (outputLength == 0) return 0;

            fixed (byte* inputPtr = &input[inputOffset])
            fixed (byte* outputPtr = &output[outputOffset])
            {
                var length = LZ4_uncompress_32(inputPtr, outputPtr, outputLength);
                if (length != inputLength)
                    throw new ArgumentException("LZ4 block is corrupted, or invalid length has been given.");
                return outputLength;
            }
        }

        /// <summary>Encodes the specified input.</summary>
        /// <param name="input">The input.</param>
        /// <param name="inputOffset">The input offset.</param>
        /// <param name="inputLength">Length of the input.</param>
        /// <param name="output">The output.</param>
        /// <param name="outputOffset">The output offset.</param>
        /// <param name="outputLength">Length of the output.</param>
        /// <returns>Number of bytes written.</returns>
        public static unsafe int Encode64Unsafe(
            byte[] input,
            int inputOffset,
            int inputLength,
            byte[] output,
            int outputOffset,
            int outputLength)
        {
            CheckArguments(
                input, inputOffset, inputLength,
                output, outputOffset, outputLength);

            if (outputLength == 0) return 0;

            fixed (byte* inputPtr = &input[inputOffset])
            fixed (byte* outputPtr = &output[outputOffset])
            {
                if (inputLength < LZ4_64KLIMIT)
                {
                    var uHashTable = HashTablePool.GetUShortHashTablePool();
                    fixed (ushort* h1 = &uHashTable[0])
                    {
                        return LZ4_compress64kCtx_64(h1, inputPtr, outputPtr, inputLength, outputLength);
                    }
                }
                else
                {
                    var uiHashTable = HashTablePool.GetUIntHashTablePool();
                    fixed (uint* h2 = &uiHashTable[0])
                    {
                        return LZ4_compressCtx_64(h2, inputPtr, outputPtr, inputLength, outputLength);
                    }
                }
            }
        }

        /// <summary>Decode64s the specified input.</summary>
        /// <param name="input">The input.</param>
        /// <param name="inputOffset">The input offset.</param>
        /// <param name="inputLength">Length of the input.</param>
        /// <param name="output">The output.</param>
        /// <param name="outputOffset">The output offset.</param>
        /// <param name="outputLength">Length of the output.</param>
        /// <returns>Number of bytes written.</returns>
        public static unsafe int Decode64Unsafe(
            byte[] input,
            int inputOffset,
            int inputLength,
            byte[] output,
            int outputOffset,
            int outputLength)
        {
            CheckArguments(
                input, inputOffset, inputLength,
                output, outputOffset, outputLength);

            if (outputLength == 0) return 0;

            fixed (byte* inputPtr = &input[inputOffset])
            fixed (byte* outputPtr = &output[outputOffset])
            {
                var length = LZ4_uncompress_64(inputPtr, outputPtr, outputLength);
                if (length != inputLength)
                    throw new ArgumentException("LZ4 block is corrupted, or invalid length has been given.");
                return outputLength;
            }
        }
    }
}

#endif
// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    internal partial class LZ4Codec
    {
        /// <summary>Encodes the specified input.</summary>
        /// <param name="input">The input.</param>
        /// <param name="output">The output.</param>
        /// <returns>Number of bytes written.</returns>
        public static unsafe int Encode(ReadOnlySpan<byte> input, Span<byte> output)
        {
            if (output.Length == 0)
            {
                throw new MessagePackSerializationException("Output is empty.");
            }

            fixed (byte* inputPtr = input)
            fixed (byte* outputPtr = output)
            {
                if (input.Length < LZ4_64KLIMIT)
                {
                    var uHashTable = HashTablePool.GetUShortHashTablePool();
                    fixed (ushort* hash1 = &uHashTable[0])
                    {
                        if (IntPtr.Size == 4)
                        {
                            return LZ4_compress64kCtx_32(hash1, inputPtr, outputPtr, input.Length, output.Length);
                        }
                        else
                        {
                            return LZ4_compress64kCtx_64(hash1, inputPtr, outputPtr, input.Length, output.Length);
                        }
                    }
                }
                else
                {
                    var bHashTable = HashTablePool.GetUIntHashTablePool();
                    fixed (uint* hash2 = &bHashTable[0])
                    {
                        if (IntPtr.Size == 4)
                        {
                            return LZ4_compressCtx_32(hash2, inputPtr, outputPtr, input.Length, output.Length);
                        }
                        else
                        {
                            return LZ4_compressCtx_64(hash2, inputPtr, outputPtr, input.Length, output.Length);
                        }
                    }
                }
            }
        }

        /// <summary>Decodes the specified input.</summary>
        /// <param name="input">The input.</param>
        /// <param name="output">The output.</param>
        /// <returns>Number of bytes written.</returns>
        public static unsafe int Decode(ReadOnlySpan<byte> input, Span<byte> output)
        {
            if (output.Length == 0)
            {
                throw new MessagePackSerializationException("Output is empty.");
            }

            fixed (byte* inputPtr = input)
            fixed (byte* outputPtr = output)
            {
                int length;
                if (IntPtr.Size == 4)
                {
                    length = LZ4_uncompress_32(inputPtr, outputPtr, output.Length);
                }
                else
                {
                    length = LZ4_uncompress_64(inputPtr, outputPtr, output.Length);
                }

                if (length != input.Length)
                {
                    throw new MessagePackSerializationException("LZ4 block is corrupted, or invalid length has been given.");
                }

                return output.Length;
            }
        }
    }
}

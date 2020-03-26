// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace MessagePackCompiler.Generator
{
    public static class EmbedStringHelper
    {
        public static readonly Encoding Utf8 = new UTF8Encoding(false);

        public static byte[] GetEncodedStringBytes(string value)
        {
            var byteCount = Utf8.GetByteCount(value);
            if (byteCount <= 31)
            {
                var bytes = new byte[byteCount + 1];
                bytes[0] = (byte)(0xa0 | byteCount);
                Utf8.GetBytes(value, 0, value.Length, bytes, 1);
                return bytes;
            }

            if (byteCount <= byte.MaxValue)
            {
                var bytes = new byte[byteCount + 2];
                bytes[0] = 0xd9;
                bytes[1] = unchecked((byte)byteCount);
                Utf8.GetBytes(value, 0, value.Length, bytes, 2);
                return bytes;
            }

            if (byteCount <= ushort.MaxValue)
            {
                var bytes = new byte[byteCount + 3];
                bytes[0] = 0xda;
                bytes[1] = unchecked((byte)(byteCount >> 8));
                bytes[2] = unchecked((byte)byteCount);
                Utf8.GetBytes(value, 0, value.Length, bytes, 3);
                return bytes;
            }
            else
            {
                var bytes = new byte[byteCount + 5];
                bytes[0] = 0xdb;
                bytes[1] = unchecked((byte)(byteCount >> 24));
                bytes[2] = unchecked((byte)(byteCount >> 16));
                bytes[3] = unchecked((byte)(byteCount >> 8));
                bytes[4] = unchecked((byte)byteCount);
                Utf8.GetBytes(value, 0, value.Length, bytes, 5);
                return bytes;
            }
        }
    }
}

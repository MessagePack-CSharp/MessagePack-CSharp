namespace MessagePack.Internal
{
    using System;
    using System.Runtime.CompilerServices;
    /// <summary>
    /// unicode manupilation utilities
    /// </summary>
    public static class UnicodeUtility
    {
        [ThreadStatic]
        const char UnicodeInvalidChar = (char)0xfffd;
        static char[] Utf8Buffer;
        /// <summary>
        /// convert utf8 bytearray to string instance
        /// </summary>
        /// <param name="data">UTF-8 byte array(without BOM)</param>
        /// <param name="offset">start position in data</param>
        /// <param name="count">number of byte length</param>
        /// <remarks>when the data contains only under U+7F and has long length(around 4k-8k), this is slower than Encoding.UTF8.GetBytes(need more optimization)</remarks>
        /// <returns>converted string instance</returns>
        public static unsafe string GetStringFromUtf8(byte[] data, int offset, int count)
        {
            if (data.Length < offset + count)
            {
                throw new IndexOutOfRangeException("offset + count exceeds on byte array length");
            }
            else if (count == 0)
            {
                return string.Empty;
            }
            fixed (byte* databeginptr = &data[offset])
            {
                byte* dataptr = databeginptr;
                var endptr = dataptr + count;
                if (count < 256)
                {
                    var buf = stackalloc char[count];
                    char* iterptr = buf;
                    while (UpdateCharUnsafe(ref dataptr, ref endptr, ref iterptr)) ;
                    return new string(buf, 0, (int)(iterptr - buf));
                }
                else
                {
                    Utf8Buffer = Utf8Buffer != null && Utf8Buffer.Length >= count ? Utf8Buffer : new char[count];
                    fixed (char* bufferptr = &Utf8Buffer[0])
                    {
                        char* iterptr = bufferptr;
                        char* beginptr = iterptr;
                        while (UpdateCharUnsafe(ref dataptr, ref endptr, ref iterptr)) ;
                        return new string(beginptr, 0, (int)(iterptr - beginptr));
                    }
                }
            }
        }
        static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool UpdateCharUnsafe(ref byte* data, ref byte* endptr, ref char* outbuf)
        {
            if (data >= endptr)
            {
                return false;
            }
            if (*data < 0x80)
            {
                unchecked
                {
                    byte* stopptr = endptr - 8;
                    byte* outbyteptr = IsLittleEndian ? (byte*)outbuf : (byte*)outbuf + 1;
                    while (data < stopptr)
                    {
                        if ((*(ulong*)data & unchecked(0x8080808080808080UL)) == 0)
                        {
                            outbyteptr[0] = data[0];
                            outbyteptr[2] = data[1];
                            outbyteptr[4] = data[2];
                            outbyteptr[6] = data[3];
                            outbyteptr[8] = data[4];
                            outbyteptr[10] = data[5];
                            outbyteptr[12] = data[6];
                            outbyteptr[14] = data[7];
                            outbyteptr += 16;
                            data += 8;
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (data < endptr && *data < 0x80)
                    {
                        *outbyteptr = *data;
                        outbyteptr += 2;
                        data++;
                    }
                    outbuf = (char*)(outbyteptr - (IsLittleEndian ? 0 : 1));
                    return true;
                }
            }
            if ((*data & 0xf0) == 0xf0)
            {
                if (data + 4 > endptr)
                {
                    *outbuf = UnicodeInvalidChar;
                    outbuf++;
                    data = endptr;
                    return true;
                }
                // between U+110000 and U+1FFFFF should be retrieved as invalid unicode point
                if (((*data & 0x07) | (*(data + 1) & 0x30)) == 0 || (((*data & 0x03) | (*(data + 1) & 0x30)) != 0))
                {
                    outbuf[0] = UnicodeInvalidChar;
                    outbuf++;
                    data++;
                    return true;
                }
                else if ((*(data + 1) & 0xc0) != 0x80)
                {
                    outbuf[0] = UnicodeInvalidChar;
                    outbuf++;
                    data++;
                    return true;
                }
                else if ((*(data + 2) & 0xc0) != 0x80)
                {
                    outbuf[0] = UnicodeInvalidChar;
                    outbuf++;
                    data+=2;
                    return true;
                }
                else if((*(data + 3) & 0xc0) != 0x80)
                {
                    outbuf[0] = UnicodeInvalidChar;
                    outbuf++;
                    data += 3;
                    return true;
                }
                // U+10FFFF(surrogate pair)
                var w = ((((((*data) & 0x7) << 2)
                    | ((*(data + 1) & 0x30) >> 4))
                    - 1) & 0x0f) << 6;
                outbuf[0] = (char)(
                    // 6bit
                    0xd800
                    // 4bit
                    | w
                    // 4bit
                    | ((*(data + 1) & 0x0f) << 2)
                    // 2bit
                    | ((*(data + 2) & 0x30) >> 4)
                );
                outbuf++;
                *outbuf = (char)(
                    // 6bit
                    0xdc00 |
                    // 4bit
                    ((*(data + 2) & 0xf) << 6) |
                    // 6bit
                    (*(data + 3) & 0x3f)
                );
                data += 4;
                outbuf++;
            }
            else if ((*data & 0xe0) == 0xe0)
            {
                if (data + 3 > endptr)
                {
                    *outbuf = UnicodeInvalidChar;
                    outbuf++;
                    data = endptr;
                    return true;
                }
                if ((((*data & 0xf) | (*(data + 1) & 0x20)) == 0))
                {
                    *outbuf = UnicodeInvalidChar;
                    data++;
                    outbuf++;
                    return true;
                }
                else if ((*(data + 1) & 0xc0) != 0x80)
                {
                    *outbuf = UnicodeInvalidChar;
                    outbuf++;
                    data++;
                    return true;
                }
                else if ((*(data + 2) & 0xc0) != 0x80)
                {
                    *outbuf = UnicodeInvalidChar;
                    outbuf++;
                    data += 2;
                    return true;
                }
                // U+FFFF
                // 4 + 6 + 6 = 16
                outbuf[0] = (char)((*data & 0x0f) << 12
                    | (*(data + 1) & 0x3f) << 6
                    | (*(data + 2) & 0x3f));
                data += 3;
                outbuf++;
            }
            else if ((*data & 0xc0) == 0xc0)
            {
                if (data + 2 > endptr)
                {
                    *outbuf = UnicodeInvalidChar;
                    outbuf++;
                    data = endptr;
                    return true;
                }
                // U+07FF
                if ((*data & 0x1e) == 0 || ((*(data + 1) & 0xc0) != 0x80))
                {
                    outbuf[0] = UnicodeInvalidChar;
                    outbuf++;
                    data++;
                    return true;
                }
                outbuf[0] = (char)(((*data & 0x1f) << 6) | ((*(data + 1)) & 0x3f));
                data += 2;
                outbuf++;
            }
            else
            {
                *outbuf = UnicodeInvalidChar;
                data++;
                outbuf++;
                //throw new InvalidOperationException("unknown byte data");
            }
            return true;
        }
    }
}
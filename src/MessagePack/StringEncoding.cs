using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MessagePack
{
    internal static class StringEncoding
    {
        internal static readonly Encoding UTF8 = new UTF8Encoding(false);

        internal static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            if (chars.Length == 0)
            {
                return 0;
            }

            fixed (char* pChars = &chars[0])
            fixed (byte* pBytes = &bytes[0])
            {
                return encoding.GetBytes(pChars, chars.Length, pBytes, bytes.Length);
            }
        }

        internal static unsafe string GetString(this Encoding encoding, ReadOnlyMemory<byte> bytes)
        {
            if (bytes.Length == 0)
            {
                return string.Empty;
            }

            if (MemoryMarshal.TryGetArray(bytes, out var segment))
            {
                return encoding.GetString(segment.Array, segment.Offset, segment.Count);
            }
            else
            {
                var tmp = new byte[bytes.Length];
                bytes.CopyTo(tmp);
                return encoding.GetString(tmp);
            }
        }

        internal static unsafe int GetBytes(this Encoding encoding, string chars, Span<byte> bytes) => GetBytes(encoding, chars.AsSpan(), bytes);
    }
}

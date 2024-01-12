// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.NET.StringTools;

namespace MessagePack.Formatters
{
    /// <summary>
    /// A <see cref="string" /> formatter that interns strings on deserialization.
    /// </summary>
    public sealed class StringInterningFormatter : IMessagePackFormatter<string?>
    {
        /// <inheritdoc/>
        public string? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            MessagePackReader retryReader = reader;
            if (reader.TryReadStringSpan(out ReadOnlySpan<byte> bytes))
            {
                if (bytes.Length < 4096)
                {
                    if (bytes.Length == 0)
                    {
                        return string.Empty;
                    }

                    Span<char> chars = stackalloc char[bytes.Length];
                    int charLength;
#if SPAN_BUILTIN || UNITY_2021_2_OR_NEWER
                    charLength = StringEncoding.UTF8.GetChars(bytes, chars);
#else
                    unsafe
                    {
                        fixed (byte* pBytes = bytes)
                        fixed (char* pChars = chars)
                        {
                            charLength = StringEncoding.UTF8.GetChars(pBytes, bytes.Length, pChars, chars.Length);
                        }
                    }
#endif
                    return Strings.WeakIntern(chars.Slice(0, charLength));
                }
                else
                {
                    // Rewind the reader to the start of the string because we're taking the slow path.
                    reader = retryReader;
                }
            }

            return reader.ReadString() is string s ? Strings.WeakIntern(s) : null;
        }

        /// <inheritdoc/>
        public void Serialize(ref MessagePackWriter writer, string? value, MessagePackSerializerOptions options) => writer.Write(value);
    }
}

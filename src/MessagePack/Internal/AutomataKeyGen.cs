// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1509 // Opening braces should not be preceded by blank line

namespace MessagePack.Internal
{
    /// <remarks>
    /// This is used by dynamically generated code. It can be made internal after we enable our dynamic assemblies to access internals.
    /// But that trick may require net46, so maybe we should leave this as public.
    /// </remarks>
    public static class AutomataKeyGen
    {
        public static readonly MethodInfo GetKeyMethod = typeof(AutomataKeyGen).GetRuntimeMethod(nameof(GetKey), new[] { typeof(ReadOnlySpan<byte>).MakeByRefType() }) ?? throw new Exception("Unable to find our own APIs.");

        public static ulong GetKey(ref ReadOnlySpan<byte> span)
        {
            ulong key;

            unchecked
            {
                if (span.Length >= 8)
                {
                    key = SafeBitConverter.ToUInt64(span);
                    span = span.Slice(8);
                }
                else
                {
                    switch (span.Length)
                    {
                        case 1:
                            {
                                key = span[0];
                                span = span.Slice(1);
                                break;
                            }

                        case 2:
                            {
                                key = SafeBitConverter.ToUInt16(span);
                                span = span.Slice(2);
                                break;
                            }

                        case 3:
                            {
                                var a = span[0];
                                var b = SafeBitConverter.ToUInt16(span.Slice(1));
                                key = a | (ulong)b << 8;
                                span = span.Slice(3);
                                break;
                            }

                        case 4:
                            {
                                key = SafeBitConverter.ToUInt32(span);
                                span = span.Slice(4);
                                break;
                            }

                        case 5:
                            {
                                var a = span[0];
                                var b = SafeBitConverter.ToUInt32(span.Slice(1));
                                key = a | (ulong)b << 8;
                                span = span.Slice(5);
                                break;
                            }

                        case 6:
                            {
                                ulong a = SafeBitConverter.ToUInt16(span);
                                ulong b = SafeBitConverter.ToUInt32(span.Slice(2));
                                key = a | (b << 16);
                                span = span.Slice(6);
                                break;
                            }

                        case 7:
                            {
                                var a = span[0];
                                var b = SafeBitConverter.ToUInt16(span.Slice(1));
                                var c = SafeBitConverter.ToUInt32(span.Slice(3));
                                key = a | (ulong)b << 8 | (ulong)c << 24;
                                span = span.Slice(7);
                                break;
                            }

                        default:
                            throw new MessagePackSerializationException("Not Supported Length");
                    }
                }

                return key;
            }
        }
    }
}

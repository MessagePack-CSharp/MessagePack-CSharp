// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack
{
    /// <summary>
    /// https://github.com/msgpack/msgpack/blob/master/spec.md#serialization-type-to-format-conversion.
    /// </summary>
#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    enum MessagePackType : byte
    {
        Unknown = 0,

        Integer = 1,
        Nil = 2,
        Boolean = 3,
        Float = 4,
        String = 5,
        Binary = 6,
        Array = 7,
        Map = 8,
        Extension = 9,
    }

    /// <summary>
    /// The core type codes as defined by msgpack.
    /// </summary>
    /// <seealso href="https://github.com/msgpack/msgpack/blob/master/spec.md#overview" />
#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    static class MessagePackCode
    {
        public const byte MinFixInt = 0x00; // 0
        public const byte MaxFixInt = 0x7f; // 127
        public const byte MinFixMap = 0x80; // 128
        public const byte MaxFixMap = 0x8f; // 143
        public const byte MinFixArray = 0x90; // 144
        public const byte MaxFixArray = 0x9f; // 159
        public const byte MinFixStr = 0xa0; // 160
        public const byte MaxFixStr = 0xbf; // 191
        public const byte Nil = 0xc0;
        public const byte NeverUsed = 0xc1;
        public const byte False = 0xc2;
        public const byte True = 0xc3;
        public const byte Bin8 = 0xc4;
        public const byte Bin16 = 0xc5;
        public const byte Bin32 = 0xc6;
        public const byte Ext8 = 0xc7;
        public const byte Ext16 = 0xc8;
        public const byte Ext32 = 0xc9;
        public const byte Float32 = 0xca;
        public const byte Float64 = 0xcb;
        public const byte UInt8 = 0xcc;
        public const byte UInt16 = 0xcd;
        public const byte UInt32 = 0xce;
        public const byte UInt64 = 0xcf;
        public const byte Int8 = 0xd0;
        public const byte Int16 = 0xd1;
        public const byte Int32 = 0xd2;
        public const byte Int64 = 0xd3;
        public const byte FixExt1 = 0xd4;
        public const byte FixExt2 = 0xd5;
        public const byte FixExt4 = 0xd6;
        public const byte FixExt8 = 0xd7;
        public const byte FixExt16 = 0xd8;
        public const byte Str8 = 0xd9;
        public const byte Str16 = 0xda;
        public const byte Str32 = 0xdb;
        public const byte Array16 = 0xdc;
        public const byte Array32 = 0xdd;
        public const byte Map16 = 0xde;
        public const byte Map32 = 0xdf;
        public const byte MinNegativeFixInt = 0xe0; // 224
        public const byte MaxNegativeFixInt = 0xff; // 255

        private static readonly MessagePackType[] TypeLookupTable = new MessagePackType[256];
        private static readonly string[] FormatNameTable = new string[256];

        static MessagePackCode()
        {
            // Init Lookup Table
            for (int i = MinFixInt; i <= MaxFixInt; i++)
            {
                TypeLookupTable[i] = MessagePackType.Integer;
                FormatNameTable[i] = "positive fixint";
            }

            for (int i = MinFixMap; i <= MaxFixMap; i++)
            {
                TypeLookupTable[i] = MessagePackType.Map;
                FormatNameTable[i] = "fixmap";
            }

            for (int i = MinFixArray; i <= MaxFixArray; i++)
            {
                TypeLookupTable[i] = MessagePackType.Array;
                FormatNameTable[i] = "fixarray";
            }

            for (int i = MinFixStr; i <= MaxFixStr; i++)
            {
                TypeLookupTable[i] = MessagePackType.String;
                FormatNameTable[i] = "fixstr";
            }

            TypeLookupTable[Nil] = MessagePackType.Nil;
            TypeLookupTable[NeverUsed] = MessagePackType.Unknown;
            TypeLookupTable[False] = MessagePackType.Boolean;
            TypeLookupTable[True] = MessagePackType.Boolean;
            TypeLookupTable[Bin8] = MessagePackType.Binary;
            TypeLookupTable[Bin16] = MessagePackType.Binary;
            TypeLookupTable[Bin32] = MessagePackType.Binary;
            TypeLookupTable[Ext8] = MessagePackType.Extension;
            TypeLookupTable[Ext16] = MessagePackType.Extension;
            TypeLookupTable[Ext32] = MessagePackType.Extension;
            TypeLookupTable[Float32] = MessagePackType.Float;
            TypeLookupTable[Float64] = MessagePackType.Float;
            TypeLookupTable[UInt8] = MessagePackType.Integer;
            TypeLookupTable[UInt16] = MessagePackType.Integer;
            TypeLookupTable[UInt32] = MessagePackType.Integer;
            TypeLookupTable[UInt64] = MessagePackType.Integer;
            TypeLookupTable[Int8] = MessagePackType.Integer;
            TypeLookupTable[Int16] = MessagePackType.Integer;
            TypeLookupTable[Int32] = MessagePackType.Integer;
            TypeLookupTable[Int64] = MessagePackType.Integer;
            TypeLookupTable[FixExt1] = MessagePackType.Extension;
            TypeLookupTable[FixExt2] = MessagePackType.Extension;
            TypeLookupTable[FixExt4] = MessagePackType.Extension;
            TypeLookupTable[FixExt8] = MessagePackType.Extension;
            TypeLookupTable[FixExt16] = MessagePackType.Extension;
            TypeLookupTable[Str8] = MessagePackType.String;
            TypeLookupTable[Str16] = MessagePackType.String;
            TypeLookupTable[Str32] = MessagePackType.String;
            TypeLookupTable[Array16] = MessagePackType.Array;
            TypeLookupTable[Array32] = MessagePackType.Array;
            TypeLookupTable[Map16] = MessagePackType.Map;
            TypeLookupTable[Map32] = MessagePackType.Map;

            FormatNameTable[Nil] = "nil";
            FormatNameTable[NeverUsed] = "(never used)";
            FormatNameTable[False] = "false";
            FormatNameTable[True] = "true";
            FormatNameTable[Bin8] = "bin 8";
            FormatNameTable[Bin16] = "bin 16";
            FormatNameTable[Bin32] = "bin 32";
            FormatNameTable[Ext8] = "ext 8";
            FormatNameTable[Ext16] = "ext 16";
            FormatNameTable[Ext32] = "ext 32";
            FormatNameTable[Float32] = "float 32";
            FormatNameTable[Float64] = "float 64";
            FormatNameTable[UInt8] = "uint 8";
            FormatNameTable[UInt16] = "uint 16";
            FormatNameTable[UInt32] = "uint 32";
            FormatNameTable[UInt64] = "uint 64";
            FormatNameTable[Int8] = "int 8";
            FormatNameTable[Int16] = "int 16";
            FormatNameTable[Int32] = "int 32";
            FormatNameTable[Int64] = "int 64";
            FormatNameTable[FixExt1] = "fixext 1";
            FormatNameTable[FixExt2] = "fixext 2";
            FormatNameTable[FixExt4] = "fixext 4";
            FormatNameTable[FixExt8] = "fixext 8";
            FormatNameTable[FixExt16] = "fixext 16";
            FormatNameTable[Str8] = "str 8";
            FormatNameTable[Str16] = "str 16";
            FormatNameTable[Str32] = "str 32";
            FormatNameTable[Array16] = "array 16";
            FormatNameTable[Array32] = "array 32";
            FormatNameTable[Map16] = "map 16";
            FormatNameTable[Map32] = "map 32";

            for (int i = MinNegativeFixInt; i <= MaxNegativeFixInt; i++)
            {
                TypeLookupTable[i] = MessagePackType.Integer;
                FormatNameTable[i] = "negative fixint";
            }
        }

        public static MessagePackType ToMessagePackType(byte code)
        {
            return TypeLookupTable[code];
        }

        public static string ToFormatName(byte code)
        {
            return FormatNameTable[code];
        }

        /// <summary>
        /// Checks whether a given messagepack code represents an integer that might include a sign (i.e. might be a negative number).
        /// </summary>
        /// <param name="code">The messagepack code.</param>
        /// <returns>A boolean value.</returns>
        internal static bool IsSignedInteger(byte code)
        {
            switch (code)
            {
                case Int8:
                case Int16:
                case Int32:
                case Int64:
                    return true;
                default:
                    return code >= MinNegativeFixInt && code <= MaxNegativeFixInt;
            }
        }
    }

    /// <summary>
    /// The officially defined messagepack extension type codes.
    /// </summary>
#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    static class ReservedMessagePackExtensionTypeCode
    {
        public const sbyte DateTime = -1;
    }

#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    static class MessagePackRange
    {
        public const int MinFixNegativeInt = -32;
        public const int MaxFixNegativeInt = -1;
        public const int MaxFixPositiveInt = 127;
        public const int MinFixStringLength = 0;
        public const int MaxFixStringLength = 31;
        public const int MaxFixMapCount = 15;
        public const int MaxFixArrayCount = 15;
    }
}

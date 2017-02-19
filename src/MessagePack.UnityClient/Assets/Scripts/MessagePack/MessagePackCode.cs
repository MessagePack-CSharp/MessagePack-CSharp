using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack
{
    /// <summary>
    /// https://github.com/msgpack/msgpack/blob/master/spec.md#serialization-type-to-format-conversion
    /// </summary>
    public enum MessagePackType : byte
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
    /// https://github.com/msgpack/msgpack/blob/master/spec.md#overview
    /// </summary>
    public static class MessagePackCode
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

        static readonly MessagePackType[] typeLookupTable = new MessagePackType[256];
        static readonly string[] formatNameTable = new string[256];

        static MessagePackCode()
        {
            // Init Lookup Table
            for (int i = MinFixInt; i <= MaxFixInt; i++)
            {
                typeLookupTable[i] = MessagePackType.Integer;
                formatNameTable[i] = "positive fixint";
            }
            for (int i = MinFixMap; i <= MaxFixMap; i++)
            {
                typeLookupTable[i] = MessagePackType.Map;
                formatNameTable[i] = "fixmap";
            }
            for (int i = MinFixArray; i <= MaxFixArray; i++)
            {
                typeLookupTable[i] = MessagePackType.Array;
                formatNameTable[i] = "fixarray";
            }
            for (int i = MinFixStr; i <= MaxFixStr; i++)
            {
                typeLookupTable[i] = MessagePackType.String;
                formatNameTable[i] = "fixstr";
            }

            typeLookupTable[Nil] = MessagePackType.Nil;
            typeLookupTable[NeverUsed] = MessagePackType.Unknown;
            typeLookupTable[False] = MessagePackType.Boolean;
            typeLookupTable[True] = MessagePackType.Boolean;
            typeLookupTable[Bin8] = MessagePackType.Binary;
            typeLookupTable[Bin16] = MessagePackType.Binary;
            typeLookupTable[Bin32] = MessagePackType.Binary;
            typeLookupTable[Ext8] = MessagePackType.Extension;
            typeLookupTable[Ext16] = MessagePackType.Extension;
            typeLookupTable[Ext32] = MessagePackType.Extension;
            typeLookupTable[Float32] = MessagePackType.Float;
            typeLookupTable[Float64] = MessagePackType.Float;
            typeLookupTable[UInt8] = MessagePackType.Integer;
            typeLookupTable[UInt16] = MessagePackType.Integer;
            typeLookupTable[UInt32] = MessagePackType.Integer;
            typeLookupTable[UInt64] = MessagePackType.Integer;
            typeLookupTable[Int8] = MessagePackType.Integer;
            typeLookupTable[Int16] = MessagePackType.Integer;
            typeLookupTable[Int32] = MessagePackType.Integer;
            typeLookupTable[Int64] = MessagePackType.Integer;
            typeLookupTable[FixExt1] = MessagePackType.Extension;
            typeLookupTable[FixExt2] = MessagePackType.Extension;
            typeLookupTable[FixExt4] = MessagePackType.Extension;
            typeLookupTable[FixExt8] = MessagePackType.Extension;
            typeLookupTable[FixExt16] = MessagePackType.Extension;
            typeLookupTable[Str8] = MessagePackType.String;
            typeLookupTable[Str16] = MessagePackType.String;
            typeLookupTable[Str32] = MessagePackType.String;
            typeLookupTable[Array16] = MessagePackType.Array;
            typeLookupTable[Array32] = MessagePackType.Array;
            typeLookupTable[Map16] = MessagePackType.Map;
            typeLookupTable[Map32] = MessagePackType.Map;

            formatNameTable[Nil] = "nil";
            formatNameTable[NeverUsed] = "(never used)";
            formatNameTable[False] = "false";
            formatNameTable[True] = "true";
            formatNameTable[Bin8] = "bin 8";
            formatNameTable[Bin16] = "bin 16";
            formatNameTable[Bin32] = "bin 32";
            formatNameTable[Ext8] = "ext 8";
            formatNameTable[Ext16] = "ext 16";
            formatNameTable[Ext32] = "ext 32";
            formatNameTable[Float32] = "float 32";
            formatNameTable[Float64] = "float 64";
            formatNameTable[UInt8] = "uint 8";
            formatNameTable[UInt16] = "uint 16";
            formatNameTable[UInt32] = "uint 32";
            formatNameTable[UInt64] = "uint 64";
            formatNameTable[Int8] = "int 8";
            formatNameTable[Int16] = "int 16";
            formatNameTable[Int32] = "int 32";
            formatNameTable[Int64] = "int 64";
            formatNameTable[FixExt1] = "fixext 1";
            formatNameTable[FixExt2] = "fixext 2";
            formatNameTable[FixExt4] = "fixext 4";
            formatNameTable[FixExt8] = "fixext 8";
            formatNameTable[FixExt16] = "fixext 16";
            formatNameTable[Str8] = "str 8";
            formatNameTable[Str16] = "str 16";
            formatNameTable[Str32] = "str 32";
            formatNameTable[Array16] = "array 16";
            formatNameTable[Array32] = "array 32";
            formatNameTable[Map16] = "map 16";
            formatNameTable[Map32] = "map 32";

            for (int i = MinNegativeFixInt; i <= MaxNegativeFixInt; i++)
            {
                typeLookupTable[i] = MessagePackType.Integer;
                formatNameTable[i] = "negative fixint";
            }
        }

        public static MessagePackType ToMessagePackType(byte code)
        {
            return typeLookupTable[code];
        }

        public static string ToFormatName(byte code)
        {
            return formatNameTable[code];
        }
    }

    public static class ReservedMessagePackExtensionTypeCode
    {
        public const sbyte DateTime = -1;
    }

    public static class MessagePackRange
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

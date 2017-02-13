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
        Integer = 0,
        Nil = 1,
        Boolean = 2,
        Float = 3,
        String = 4,
        Binary = 5,
        Array = 6,
        Map = 7,
        Extension = 8,

        Unknown = 255
    }

    /// <summary>
    /// https://github.com/msgpack/msgpack/blob/master/spec.md#overview
    /// </summary>
    public static class MessagePackCode
    {
        public const byte MinFixInt = 0x00;
        public const byte MaxFixInt = 0x7f;
        public const byte MinFixMap = 0x80;
        public const byte MaxFixMap = 0x8f;
        public const byte MinFixArray = 0x90;
        public const byte MaxFixArray = 0x9f;
        public const byte MinFixStr = 0xa0;
        public const byte MaxFixStr = 0xbf;
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
        public const byte MinNegativeFixInt = 0xe0;
        public const byte MaxNegativeFixInt = 0xff;

        static readonly MessagePackType[] typeLookupTable = new MessagePackType[255];

        static MessagePackCode()
        {
            // Init Lookup Table
            for (int i = MinFixInt; i <= MaxFixInt; i++)
            {
                typeLookupTable[i] = MessagePackType.Integer;
            }
            for (int i = MinFixMap; i <= MaxFixMap; i++)
            {
                typeLookupTable[i] = MessagePackType.Map;
            }
            for (int i = MinFixArray; i <= MaxFixArray; i++)
            {
                typeLookupTable[i] = MessagePackType.Array;
            }
            for (int i = MinFixStr; i <= MaxFixStr; i++)
            {
                typeLookupTable[i] = MessagePackType.String;
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
            for (int i = MinNegativeFixInt; i <= MaxNegativeFixInt; i++)
            {
                typeLookupTable[i] = MessagePackType.Integer;
            }
        }

        public static MessagePackType ToMessagePackType(byte code)
        {
            return typeLookupTable[code];
        }
    }
}

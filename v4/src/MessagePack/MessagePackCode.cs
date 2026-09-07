// The msgpack spec and MessagePack-CSharp both name these codes after the wire types (Int32, Float64, ...).
#pragma warning disable CA1720 // Identifier contains type name

using System;

namespace MessagePack;

/// <summary>
/// The core type codes as defined by msgpack.
/// </summary>
/// <seealso href="https://github.com/msgpack/msgpack/blob/master/spec.md#overview" />
public static class MessagePackCode
{
    // fixint code ranges (reference for formatter authors; the hot paths use folded forms of these bounds,
    // e.g. (uint)(value + 32) <= 159 and (byte)(code + 32) <= 159)
    public const byte MinFixInt = 0x00; // positive fixint: value 0..127
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
    public const byte MinNegativeFixInt = 0xe0;     // negative fixint: value -32..-1
    public const byte MaxNegativeFixInt = 0xff;

    /// <summary>Classifies a leading byte into the msgpack family it starts.</summary>
    public static MessagePackType ToMessagePackType(byte code) => (MessagePackType)TypeTable[code];

    /// <summary>The spec's format name for a leading byte, for diagnostics.</summary>
    public static string ToFormatName(byte code) => code switch
    {
        <= MaxFixInt => "positive fixint",
        <= MaxFixMap => "fixmap",
        <= MaxFixArray => "fixarray",
        <= MaxFixStr => "fixstr",
        Nil => "nil",
        NeverUsed => "(never used)",
        False => "false",
        True => "true",
        Bin8 => "bin 8",
        Bin16 => "bin 16",
        Bin32 => "bin 32",
        Ext8 => "ext 8",
        Ext16 => "ext 16",
        Ext32 => "ext 32",
        Float32 => "float 32",
        Float64 => "float 64",
        UInt8 => "uint 8",
        UInt16 => "uint 16",
        UInt32 => "uint 32",
        UInt64 => "uint 64",
        Int8 => "int 8",
        Int16 => "int 16",
        Int32 => "int 32",
        Int64 => "int 64",
        FixExt1 => "fixext 1",
        FixExt2 => "fixext 2",
        FixExt4 => "fixext 4",
        FixExt8 => "fixext 8",
        FixExt16 => "fixext 16",
        Str8 => "str 8",
        Str16 => "str 16",
        Str32 => "str 32",
        Array16 => "array 16",
        Array32 => "array 32",
        Map16 => "map 16",
        Map32 => "map 32",
        _ => "negative fixint",
    };

    // one MessagePackType per leading byte; a constant u8 span, so the lookup is a single load from the assembly's data section
    static ReadOnlySpan<byte> TypeTable =>
    [
        // 0x00..0x7f positive fixint
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
        // 0x80..0x8f fixmap, 0x90..0x9f fixarray
        8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        // 0xa0..0xbf fixstr
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        // 0xc0 nil, 0xc1 never used, 0xc2/0xc3 bool, 0xc4..0xc6 bin, 0xc7..0xc9 ext, 0xca/0xcb float, 0xcc..0xd3 int
        2, 0, 3, 3, 6, 6, 6, 9, 9, 9, 4, 4, 1, 1, 1, 1, 1, 1, 1, 1,
        // 0xd4..0xd8 fixext, 0xd9..0xdb str, 0xdc/0xdd array, 0xde/0xdf map
        9, 9, 9, 9, 9, 5, 5, 5, 7, 7, 8, 8,
        // 0xe0..0xff negative fixint
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
    ];
}

/// <summary>
/// The msgpack families a leading byte can start (the classification behind
/// <see cref="MessagePackCode.ToMessagePackType(byte)"/>).
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
/// The value ranges of the msgpack fixed-size forms.
/// </summary>
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

/// <summary>
/// The ext type codes reserved by the msgpack spec.
/// </summary>
public static class ReservedMessagePackExtensionTypeCode
{
    public const sbyte DateTime = -1;
}

/// <summary>
/// The ext type codes assigned by MessagePack-CSharp.
/// </summary>
public static class ThisLibraryExtensionTypeCodes
{
    public const sbyte Lz4BlockArray = 98;
    public const sbyte Lz4Block = 99;
    public const sbyte TypelessFormatter = 100;

    /// <summary>Circular-reference back-reference; payload = raw big-endian id in the smallest of 1/2/4 bytes.</summary>
    public const sbyte CircularReference = 97;
}

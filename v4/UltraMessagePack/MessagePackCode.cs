namespace UltraMessagePack;

public static class MessagePackCode
{
    public const byte Nil = 192;
    public const byte False = 194;
    public const byte True = 195;
    public const byte Bin8 = 196;
    public const byte Bin16 = 197;
    public const byte Bin32 = 198;
    public const byte Float32 = 202;
    public const byte Float64 = 203;
    public const byte UInt8 = 204;
    public const byte UInt16 = 205;
    public const byte UInt32 = 206;
    public const byte UInt64 = 207;
    public const byte Int8 = 208;
    public const byte Int16 = 209;
    public const byte Int32 = 210;
    public const byte Int64 = 211;
    public const byte Ext8 = 199;
    public const byte Ext16 = 200;
    public const byte Ext32 = 201;
    public const byte FixExt1 = 212;
    public const byte FixExt2 = 213;
    public const byte FixExt4 = 214;
    public const byte FixExt8 = 215;
    public const byte FixExt16 = 216;
    public const byte Str8 = 217;
    public const byte Str16 = 218;
    public const byte Str32 = 219;
    public const byte Array16 = 220;
    public const byte Array32 = 221;
    public const byte Map16 = 222;
    public const byte Map32 = 223;
    public const byte MinFixStr = 160;
    public const byte MinFixArray = 144;
    public const byte MinFixMap = 128;

    // fixint code ranges (reference for formatter authors; the hot paths use folded
    // forms of these bounds, e.g. (uint)(value + 32) <= 159 and (byte)(code + 32) <= 159)
    public const byte MinFixInt = 0;            // positive fixint: value 0..127
    public const byte MaxFixInt = 127;
    public const byte MinNegativeFixInt = 224;  // negative fixint: value -32..-1
    public const byte MaxNegativeFixInt = 255;

    // fix-form capacity limits
    public const int MaxFixArrayCount = 15;
    public const int MaxFixMapCount = 15;
    public const int MaxFixStringLength = 31;

    // reserved ext type codes (msgpack spec)
    public const sbyte TimestampExtensionTypeCode = -1;
}

using Xunit;

namespace MessagePack.Tests;

public class MessagePackCodeTest
{
    [Fact]
    public void ToMessagePackType_CoversEveryLeadingByte()
    {
        for (var i = 0; i < 256; i++)
        {
            var code = (byte)i;
            var expected = code switch
            {
                <= MessagePackCode.MaxFixInt => MessagePackType.Integer,
                <= MessagePackCode.MaxFixMap => MessagePackType.Map,
                <= MessagePackCode.MaxFixArray => MessagePackType.Array,
                <= MessagePackCode.MaxFixStr => MessagePackType.String,
                MessagePackCode.Nil => MessagePackType.Nil,
                MessagePackCode.NeverUsed => MessagePackType.Unknown,
                MessagePackCode.False or MessagePackCode.True => MessagePackType.Boolean,
                >= MessagePackCode.Bin8 and <= MessagePackCode.Bin32 => MessagePackType.Binary,
                >= MessagePackCode.Ext8 and <= MessagePackCode.Ext32 => MessagePackType.Extension,
                MessagePackCode.Float32 or MessagePackCode.Float64 => MessagePackType.Float,
                >= MessagePackCode.UInt8 and <= MessagePackCode.Int64 => MessagePackType.Integer,
                >= MessagePackCode.FixExt1 and <= MessagePackCode.FixExt16 => MessagePackType.Extension,
                >= MessagePackCode.Str8 and <= MessagePackCode.Str32 => MessagePackType.String,
                MessagePackCode.Array16 or MessagePackCode.Array32 => MessagePackType.Array,
                MessagePackCode.Map16 or MessagePackCode.Map32 => MessagePackType.Map,
                _ => MessagePackType.Integer, // negative fixint
            };
            Assert.Equal(expected, MessagePackCode.ToMessagePackType(code));
        }
    }

    [Theory]
    [InlineData(0x00, "positive fixint")]
    [InlineData(0x7f, "positive fixint")]
    [InlineData(0x8f, "fixmap")]
    [InlineData(0x90, "fixarray")]
    [InlineData(0xbf, "fixstr")]
    [InlineData(0xc0, "nil")]
    [InlineData(0xc1, "(never used)")]
    [InlineData(0xc9, "ext 32")]
    [InlineData(0xd3, "int 64")]
    [InlineData(0xd8, "fixext 16")]
    [InlineData(0xdf, "map 32")]
    [InlineData(0xe0, "negative fixint")]
    [InlineData(0xff, "negative fixint")]
    public void ToFormatName_MatchesSpecNames(byte code, string expected)
    {
        Assert.Equal(expected, MessagePackCode.ToFormatName(code));
    }

    [Fact]
    public void FixRangesAgreeAcrossCodeAndRange()
    {
        Assert.Equal(MessagePackRange.MaxFixMapCount, MessagePackCode.MaxFixMap - MessagePackCode.MinFixMap);
        Assert.Equal(MessagePackRange.MaxFixArrayCount, MessagePackCode.MaxFixArray - MessagePackCode.MinFixArray);
        Assert.Equal(MessagePackRange.MaxFixStringLength, MessagePackCode.MaxFixStr - MessagePackCode.MinFixStr);
        Assert.Equal(MessagePackRange.MaxFixPositiveInt, MessagePackCode.MaxFixInt);
        Assert.Equal(MessagePackRange.MinFixNegativeInt, unchecked((sbyte)MessagePackCode.MinNegativeFixInt));
        Assert.Equal(MessagePackRange.MaxFixNegativeInt, unchecked((sbyte)MessagePackCode.MaxNegativeFixInt));
    }
}

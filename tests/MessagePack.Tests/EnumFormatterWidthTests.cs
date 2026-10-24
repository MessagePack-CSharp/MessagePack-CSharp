using System.Reflection;
using MessagePack.Formatters;
using SerializerFoundation;
using static MessagePack.Tests.EnumCollectionFormatterTests;

namespace MessagePack.Tests;

// EnumFormatters.cs: the scalar enum formatters reinterpret T as their underlying integer with Unsafe.As, which moves
// sizeof(underlying) bytes. They are public, so a hand-built formatter with the wrong T (EnumInt64Formatter over a
// byte-backed enum) would read past the value on Serialize, or past the local on Deserialize. The constructor rejects
// every T whose underlying type differs, the same rule as the enum collection formatters (equal width alone is not
// enough: a uint-backed enum through EnumInt32Formatter would change the wire's signedness).
public class EnumFormatterWidthTests
{
    // index-aligned: Formatters[i] is the formatter for Enums[i]'s underlying type
    static readonly Type[] Formatters =
    [
        typeof(EnumByteFormatter<,,>), typeof(EnumSByteFormatter<,,>), typeof(EnumInt16Formatter<,,>), typeof(EnumUInt16Formatter<,,>),
        typeof(EnumInt32Formatter<,,>), typeof(EnumUInt32Formatter<,,>), typeof(EnumInt64Formatter<,,>), typeof(EnumUInt64Formatter<,,>),
    ];

    static readonly Type[] Enums =
    [
        typeof(ByteEnum), typeof(SByteEnum), typeof(ShortEnum), typeof(UShortEnum),
        typeof(IntEnum), typeof(UIntEnum), typeof(LongEnum), typeof(ULongEnum),
    ];

    [Fact]
    public void OnlyTheMatchingUnderlyingTypeConstructs()
    {
        for (int f = 0; f < Formatters.Length; f++)
        {
            for (int e = 0; e < Enums.Length; e++)
            {
                var closed = Formatters[f].MakeGenericType(typeof(ArrayPoolListWriteBuffer), typeof(ReadOnlySpanReadBuffer), Enums[e]);
                if (f == e)
                {
                    Assert.NotNull(Activator.CreateInstance(closed));
                }
                else
                {
                    var thrown = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(closed));
                    Assert.IsType<InvalidOperationException>(thrown.InnerException);
                }
            }
        }
    }

    [Fact]
    public void WidthMismatch_IsRejectedAtConstruction()
    {
        // the two out-of-bounds shapes: a wider formatter reads past the value on Serialize,
        // a narrower one reads past its local on Deserialize
        Assert.Throws<InvalidOperationException>(() => new EnumInt64Formatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, ByteEnum>());
        Assert.Throws<InvalidOperationException>(() => new EnumByteFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, LongEnum>());
        // same width, other signedness
        Assert.Throws<InvalidOperationException>(() => new EnumInt32Formatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, UIntEnum>());
    }

    [Fact]
    public void ResolverPath_StillServesEveryWidth()
    {
        Assert.Equal(ByteEnum.One, RoundTrip(ByteEnum.One));
        Assert.Equal(SByteEnum.MinusOne, RoundTrip(SByteEnum.MinusOne));
        Assert.Equal(ShortEnum.One, RoundTrip(ShortEnum.One));
        Assert.Equal(UShortEnum.One, RoundTrip(UShortEnum.One));
        Assert.Equal(IntEnum.One, RoundTrip(IntEnum.One));
        Assert.Equal(UIntEnum.One, RoundTrip(UIntEnum.One));
        Assert.Equal(LongEnum.One, RoundTrip(LongEnum.One));
        Assert.Equal(ULongEnum.One, RoundTrip(ULongEnum.One));

        static T RoundTrip<T>(T value) => MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
    }
}

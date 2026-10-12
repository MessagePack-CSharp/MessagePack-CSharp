using MessagePack.Formatters;
using Xunit;

namespace MessagePack.Tests;

// DefaultAot has no GenericFormatterFactory, so a root Nullable<T> of a built-in scalar (a SignalR hub argument `int?`,
// a bare Serialize<DateTime?>) had no formatter there: "Type 'System.Nullable`1[System.Int32]' is not found". The
// generator harvests nullable members of annotated types, not roots. BuiltInFormatterFactory now closes
// NullableFormatter over its scalar set itself, with the same wire as the generic tier.
public class AotNullableScalarTests
{
    static readonly MessagePackSerializerOptions Aot = MessagePackSerializerOptions.DefaultAot;

    static void AssertRoundtrip<T>(T? value) where T : struct
    {
        var aotBytes = MessagePackSerializer.Serialize(value, Aot);
        Assert.Equal(MessagePackSerializer.Serialize(value), aotBytes); // same wire as the Default chain
        Assert.Equal(value, MessagePackSerializer.Deserialize<T?>(aotBytes, Aot));
        Assert.Null(MessagePackSerializer.Deserialize<T?>(MessagePackSerializer.Serialize<T?>(null, Aot), Aot));
    }

    [Fact]
    public void DefaultAot_ServesNullableBuiltInScalars()
    {
        AssertRoundtrip<int>(42);
        AssertRoundtrip<long>(-1);
        AssertRoundtrip<bool>(true);
        AssertRoundtrip<double>(1.5);
        AssertRoundtrip<char>('x');
        AssertRoundtrip<decimal>(1.25m);
        AssertRoundtrip<Guid>(Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10"));
        AssertRoundtrip<TimeSpan>(TimeSpan.FromMinutes(3));
        AssertRoundtrip<DateTime>(new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc));
        AssertRoundtrip<DateTimeOffset>(new DateTimeOffset(2026, 10, 12, 0, 0, 0, TimeSpan.FromHours(9)));
        AssertRoundtrip<System.Numerics.BigInteger>(System.Numerics.BigInteger.Pow(2, 100));
        AssertRoundtrip<Half>((Half)1.5f);
        AssertRoundtrip<DateOnly>(new DateOnly(2026, 10, 12));
        AssertRoundtrip<Index>(^2);
    }

    [Fact]
    public void BuiltIn_ServesTheSameFormatterTypeAsTheGenericTier()
    {
        var formatter = Aot.Resolver.GetFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, int?>();
        Assert.IsType<NullableFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, int>>(formatter);
        Assert.IsType<NullableFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, int>>(
            MessagePackSerializerOptions.Default.Resolver.GetFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, int?>());
    }
}

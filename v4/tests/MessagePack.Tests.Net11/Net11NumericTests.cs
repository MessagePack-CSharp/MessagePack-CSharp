using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MessagePack;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests.Net11;

// the .NET 11 numeric additions: BFloat16 rides float32 (the Half treatment), the IEEE
// decimal trio rides bin(4/8/16) of its interchange bits (the Int128 treatment —
// cohort-exact, unlike any string form), Complex<T> rides [real, imaginary] matching the
// non-generic Complex byte for byte, and NFloat rides float64. nint/nuint stay
// deliberately unserved: they usually carry handles, and a loud miss beats a silent
// pointer on the wire.
public class Net11NumericTests
{
    [Fact]
    public void BFloat16_RidesFloat32()
    {
        var bytes = V4.Serialize((BFloat16)1.5f);
        Assert.Equal(new byte[] { 0xCA, 0x3F, 0xC0, 0x00, 0x00 }, bytes);
        Assert.Equal((BFloat16)1.5f, V4.Deserialize<BFloat16>(bytes));

        Assert.True(BFloat16.IsNaN(V4.Deserialize<BFloat16>(V4.Serialize(BFloat16.NaN))));
        Assert.Equal(BFloat16.PositiveInfinity, V4.Deserialize<BFloat16>(V4.Serialize(BFloat16.PositiveInfinity)));
    }

    [Fact]
    public void Decimal64_RidesInterchangeBits_CohortExact()
    {
        // "1.50" and "1.5" are distinct IEEE decimal values (different cohort members);
        // the bit image keeps them apart where any compact string form would not
        var withTrailingZero = Decimal64.Parse("1.50", CultureInfo.InvariantCulture);
        var bytes = V4.Serialize(withTrailingZero);
        Assert.Equal(2 + 8, bytes.Length);
        Assert.Equal(0xC4, bytes[0]); // bin8
        Assert.Equal(8, bytes[1]);

        var back = V4.Deserialize<Decimal64>(bytes);
        Assert.Equal("1.50", back.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(
            Unsafe.BitCast<Decimal64, ulong>(withTrailingZero),
            Unsafe.BitCast<Decimal64, ulong>(back));
    }

    [Fact]
    public void DecimalTrio_ExtremesRoundtrip()
    {
        var d32 = Decimal32.Parse("-9.999999E+96", CultureInfo.InvariantCulture);
        Assert.Equal(d32, V4.Deserialize<Decimal32>(V4.Serialize(d32)));
        Assert.Equal(2 + 4, V4.Serialize(d32).Length);

        var d128 = Decimal128.Parse("-9.999999999999999999999999999999999E+6144", CultureInfo.InvariantCulture);
        Assert.Equal(d128, V4.Deserialize<Decimal128>(V4.Serialize(d128)));
        Assert.Equal(2 + 16, V4.Serialize(d128).Length);

        Assert.True(Decimal32.IsNaN(V4.Deserialize<Decimal32>(V4.Serialize(Decimal32.NaN))));
        Assert.Equal(Decimal64.NegativeInfinity, V4.Deserialize<Decimal64>(V4.Serialize(Decimal64.NegativeInfinity)));
    }

    [Fact]
    public void ComplexOfDouble_ByteIdenticalToNonGenericComplex()
    {
        var generic = V4.Serialize(new Complex<double>(1.5, -2.5));
        var nonGeneric = V4.Serialize(new Complex(1.5, -2.5));
        Assert.Equal(nonGeneric, generic);
        var back = V4.Deserialize<Complex<double>>(generic);
        Assert.Equal(1.5, back.Real);
        Assert.Equal(-2.5, back.Imaginary);
    }

    [Fact]
    public void ComplexOfFloatAndHalf_Roundtrip()
    {
        var single = V4.Deserialize<Complex<float>>(V4.Serialize(new Complex<float>(1.5f, -2.5f)));
        Assert.Equal(1.5f, single.Real);
        Assert.Equal(-2.5f, single.Imaginary);

        var half = V4.Deserialize<Complex<Half>>(V4.Serialize(new Complex<Half>((Half)0.5f, (Half)(-0.25f))));
        Assert.Equal((Half)0.5f, half.Real);
        Assert.Equal((Half)(-0.25f), half.Imaginary);
    }

    [Fact]
    public void ComplexAsMember_ResolvesThroughTheHarvestedConstruction()
    {
        var holder = new SignalSample { Amplitude = new Complex<float>(3f, 4f), MaybeGain = (BFloat16)2f, Precise = Decimal32.Parse("0.5", CultureInfo.InvariantCulture) };
        var back = V4.Deserialize<SignalSample>(V4.Serialize(holder))!;
        Assert.Equal(3f, back.Amplitude.Real);
        Assert.Equal(4f, back.Amplitude.Imaginary);
        Assert.Equal((BFloat16)2f, back.MaybeGain);
        Assert.Equal(Decimal32.Parse("0.5", CultureInfo.InvariantCulture), back.Precise);
    }

    [Fact]
    public void NFloat_RidesFloat64()
    {
        var bytes = V4.Serialize((NFloat)3.25);
        Assert.Equal(0xCB, bytes[0]); // float64
        Assert.Equal((NFloat)3.25, V4.Deserialize<NFloat>(bytes));
    }

    [Fact]
#pragma warning disable MsgPack108 // the loud miss IS the assertion
    public void NativeInt_StaysUnserved()
    {
        Assert.Contains("IntPtr", Assert.Throws<InvalidOperationException>(() => V4.Serialize((nint)1)).Message);
        Assert.Contains("UIntPtr", Assert.Throws<InvalidOperationException>(() => V4.Serialize((nuint)1)).Message);
    }
#pragma warning restore MsgPack108
}

[MessagePackObject]
public class SignalSample
{
    [Key(0)] public Complex<float> Amplitude { get; set; }
    [Key(1)] public BFloat16 MaybeGain { get; set; }
    [Key(2)] public Decimal32 Precise { get; set; }
}

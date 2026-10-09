using MessagePack;

namespace MessagePack.Tests.NetFx;

// Runs the compression processors on their netstandard2.0 assets under .NET Framework. The round trips are the
// smoke; the point is that the assemblies load at all: MessagePack.LZ4 and MessagePack.Zstandard are strong-named,
// and .NET Framework refuses a strong-named assembly whose reference (NativeCompressions, signed since 1.0.1) is not.
public class DownlevelCompressionTests
{
    static int[] BigCompressible()
    {
        var data = new int[100_000];
        for (int i = 0; i < data.Length; i++) data[i] = i % 100;
        return data;
    }

    static void AssertRoundtrip<T>(T value, MessagePackSerializerOptions options)
    {
        Assert.Equal(value, MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, options), options));
    }

    [Fact]
    public void Lz4Frame_Roundtrips()
    {
        var options = MessagePackSerializerOptions.Default.WithLz4Frame();
        AssertRoundtrip(BigCompressible(), options);
        AssertRoundtrip(new[] { 1, 2, 3 }, options);
        AssertRoundtrip("downlevel", options);
    }

    [Fact]
    public void Lz4V3Envelopes_Roundtrip()
    {
#pragma warning disable CS0618 // the v3 wire formats stay supported; this exercises them on purpose
        AssertRoundtrip(BigCompressible(), MessagePackSerializerOptions.Default.WithLz4Block());
        AssertRoundtrip(BigCompressible(), MessagePackSerializerOptions.Default.WithLz4BlockArray());
#pragma warning restore CS0618
    }

    [Fact]
    public void ZstandardFrame_Roundtrips()
    {
        var options = MessagePackSerializerOptions.Default.WithZstandardFrame();
        AssertRoundtrip(BigCompressible(), options);
        AssertRoundtrip(new[] { 1, 2, 3 }, options);
        AssertRoundtrip("downlevel", options);
    }
}

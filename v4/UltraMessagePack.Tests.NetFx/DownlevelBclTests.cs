using System.Collections;
using System.Text;
using UltraMessagePack;
using UltraMessagePack.Formatters;

namespace UltraMessagePack.Tests.NetFx;

// Runs the BclFormatters.cs scalars on the netstandard2.0 asset, where decimal/Guid go
// through System.Memory's Utf8Formatter/Utf8Parser instead of the in-box ones. Wire
// parity with v3 is asserted by the main test project; this is a roundtrip smoke over
// the downlevel binary.
public class DownlevelBclTests
{
    static void AssertRoundtrip<T>(T value)
    {
        Assert.Equal(value, MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value)));
    }

    [Fact]
    public void ScalarRoundtrips()
    {
        AssertRoundtrip(123.456m);
        AssertRoundtrip(decimal.MinValue);
        AssertRoundtrip(TimeSpan.FromTicks(123456789));
        AssertRoundtrip(Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"));
        AssertRoundtrip(new Version(1, 2, 3, 4));
        // ns2.0 BigInteger has no GetByteCount/TryWriteBytes: exercises the ToByteArray route
        AssertRoundtrip(System.Numerics.BigInteger.Parse("123456789012345678901234567890"));
        AssertRoundtrip(System.Numerics.BigInteger.Pow(2, 3000));

        // Type is opt-in (disabled in the default chain for security); suppressing
        // CS0618 is the intended risk acknowledgement
#pragma warning disable CS0618
        var typeOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([new TypeFormatterFactory(), MessagePackFormatterFactory.Default]));
#pragma warning restore CS0618
        var typeBytes = MessagePackSerializer.Serialize(typeof(List<Dictionary<string, int[]>>), typeOptions);
        Assert.Equal(typeof(List<Dictionary<string, int[]>>), MessagePackSerializer.Deserialize<Type>(typeBytes, typeOptions));

        var dto = new DateTimeOffset(2026, 8, 7, 12, 34, 56, TimeSpan.FromHours(9));
        Assert.True(dto.EqualsExact(MessagePackSerializer.Deserialize<DateTimeOffset>(MessagePackSerializer.Serialize(dto))));

        var uri = new Uri("https://example.com/path?query=1");
        Assert.Equal(uri.OriginalString, MessagePackSerializer.Deserialize<Uri>(MessagePackSerializer.Serialize(uri))!.OriginalString);

        var sb = new StringBuilder("hello, 日本語");
        Assert.Equal(sb.ToString(), MessagePackSerializer.Deserialize<StringBuilder>(MessagePackSerializer.Serialize(sb))!.ToString());

        var bits = new BitArray(33);
        bits[0] = bits[32] = true;
        Assert.Equal(bits.Cast<bool>(), MessagePackSerializer.Deserialize<BitArray>(MessagePackSerializer.Serialize(bits))!.Cast<bool>());
    }
}

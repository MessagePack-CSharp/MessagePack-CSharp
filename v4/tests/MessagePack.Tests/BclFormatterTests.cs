extern alias V3;
using System.Collections;
using System.Text;
using MessagePack;
using MessagePack.Formatters;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// BclFormatters.cs scalars: every case is wire-compared against MessagePack v3 AND
// roundtripped through our own reader.
public class BclFormatterTests
{
    static byte[] AssertOracle<T>(T value)
    {
        var ours = MessagePackSerializer.Serialize(value);
        Assert.Equal(Oracle.Serialize(value), ours);
        return ours;
    }

    static void AssertOracleAndRoundtrip<T>(T value)
    {
        var ours = AssertOracle(value);
        Assert.Equal(value, MessagePackSerializer.Deserialize<T>(ours));
    }

    [Fact]
    public void Decimal_MatchOracleAndRoundtrip()
    {
        decimal[] values =
        [
            0m, 1m, -1m, 123.456m, -0.000000001m,
            1.10m, // scale is part of the value: must roundtrip as "1.10", not "1.1"
            decimal.MaxValue, decimal.MinValue,
            -7922816251426433759.3543950335m, // sign + 29 digits + point = 31 chars (fixstr ceiling)
        ];
        foreach (var value in values)
        {
            var ours = AssertOracleAndRoundtripReturning(value);
            Assert.True(ours[0] >= 0xa0 && ours[0] <= 0xbf); // always fixstr
        }

        // scale survives the wire (decimal equality ignores it, so assert via ToString)
        Assert.Equal("1.10", MessagePackSerializer.Deserialize<decimal>(MessagePackSerializer.Serialize(1.10m)).ToString(System.Globalization.CultureInfo.InvariantCulture));

        // nullable rides the generic Nullable tier
        AssertOracleAndRoundtrip((decimal?)123.456m);
        AssertOracleAndRoundtrip((decimal?)null);

        // nil into non-nullable decimal is malformed data
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<decimal>(new byte[] { 0xc0 }));

        static byte[] AssertOracleAndRoundtripReturning(decimal value)
        {
            var ours = AssertOracle(value);
            Assert.Equal(value, MessagePackSerializer.Deserialize<decimal>(ours));
            return ours;
        }
    }

    [Fact]
    public void TimeSpan_MatchOracleAndRoundtrip()
    {
        TimeSpan[] values = [TimeSpan.Zero, TimeSpan.MinValue, TimeSpan.MaxValue, TimeSpan.FromTicks(123456789), TimeSpan.FromMinutes(-90)];
        foreach (var value in values)
        {
            AssertOracleAndRoundtrip(value);
        }
        AssertOracleAndRoundtrip((TimeSpan?)TimeSpan.FromHours(1));
        AssertOracleAndRoundtrip((TimeSpan?)null);
    }

    [Fact]
    public void DateTimeOffset_MatchOracleAndRoundtrip()
    {
        DateTimeOffset[] values =
        [
            new DateTimeOffset(2026, 8, 7, 12, 34, 56, 789, TimeSpan.FromHours(9)),
            new DateTimeOffset(1999, 12, 31, 23, 59, 59, TimeSpan.FromMinutes(-330)), // -05:30
            new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(0001, 1, 1, 0, 0, 0, TimeSpan.Zero), // MinValue
        ];
        foreach (var value in values)
        {
            var ours = AssertOracle(value);
            // == compares the instant only; the wire preserves ticks AND offset exactly
            Assert.True(value.EqualsExact(MessagePackSerializer.Deserialize<DateTimeOffset>(ours)));
        }
    }

    [Fact]
    public void Guid_MatchOracleAndRoundtrip()
    {
        Guid[] values =
        [
            Guid.Empty,
            Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
            Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        ];
        foreach (var value in values)
        {
            var ours = AssertOracle(value);
            Assert.Equal(0xd9, ours[0]); // 36 chars = str8
            Assert.Equal(36, ours[1]);
            Assert.Equal(value, MessagePackSerializer.Deserialize<Guid>(ours));
        }
        AssertOracleAndRoundtrip((Guid?)Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"));
        AssertOracleAndRoundtrip((Guid?)null);

        // a str whose length is not 36 is malformed
        var wrongLength = MessagePackSerializer.Serialize("0123456789abcdef");
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Guid>(wrongLength));
    }

    [Fact]
    public void Uri_MatchOracleAndRoundtrip()
    {
        Uri?[] values =
        [
            new Uri("https://example.com/path?query=1"),
            new Uri("HTTPS://EXAMPLE.com/Casing"), // OriginalString is what travels
            new Uri("foo/bar", UriKind.Relative),
            null,
        ];
        foreach (var value in values)
        {
            var ours = AssertOracle(value);
            var back = MessagePackSerializer.Deserialize<Uri?>(ours);
            Assert.Equal(value?.OriginalString, back?.OriginalString);
        }
    }

    [Fact]
    public void Version_MatchOracleAndRoundtrip()
    {
        Version?[] values =
        [
            new Version(1, 2), new Version(1, 2, 3), new Version(1, 2, 3, 4), null,
            // 43 chars: crosses the fixstr->str8 boundary, exercising the speculative
            // header's payload-shift path in Serialize
            new Version(1000000000, 1000000000, 1000000000, 1000000000),
        ];
        foreach (var value in values)
        {
            AssertOracleAndRoundtrip(value);
        }
    }

    [Fact]
    public void StringBuilder_MatchOracleAndRoundtrip()
    {
        StringBuilder?[] values = [new StringBuilder(), new StringBuilder("hello, world"), new StringBuilder("日本語もOK"), null];
        foreach (var value in values)
        {
            var ours = AssertOracle(value);
            var back = MessagePackSerializer.Deserialize<StringBuilder?>(ours);
            Assert.Equal(value?.ToString(), back?.ToString());
        }

        // a lone high surrogate cannot survive UTF-8: it must encode as U+FFFD exactly
        // like the contiguous (ToString-based) encoder, wire-identical to v3
        var loneSurrogate = AssertOracle(new StringBuilder("a\uD800"));
        Assert.Equal("a�", MessagePackSerializer.Deserialize<StringBuilder>(loneSurrogate)!.ToString());
    }

    [Fact]
    public void BitArray_MatchOracleAndRoundtrip()
    {
        BitArray?[] values =
        [
            new BitArray(0),
            new BitArray(new[] { true, false, true, true }),
            new BitArray(33), // crosses the backing-int boundary
            null,
        ];
        values[2]![32] = true;
        foreach (var value in values)
        {
            var ours = AssertOracle(value);
            var back = MessagePackSerializer.Deserialize<BitArray?>(ours);
            Assert.Equal(value?.Cast<bool>(), back?.Cast<bool>());
        }
    }

    [Fact]
    public void Type_DisabledByDefault_OptInMatchesOracleAndRoundtrips()
    {
        // default chain: resolving Type throws the instructive error naming the opt-in,
        // not the generic missing-formatter message (v4 break from v3, security stance)
        var ex = Assert.Throws<InvalidOperationException>(() => MessagePackSerializer.Serialize(typeof(int)));
        Assert.Contains("TypeFormatterFactory", ex.Message);
        Assert.Throws<InvalidOperationException>(
            () => MessagePackSerializer.Deserialize<Type?>(MessagePackSerializer.Serialize("System.Int32")));

        // opt-in: factory composed before the default chain; wire stays v3-compatible.
        // the pragma is the intended consumption model — suppressing CS0618 IS the
        // "I accept the Type.GetType risk" acknowledgement
#pragma warning disable CS0618
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver([new TypeFormatterFactory(), MessagePackFormatterFactory.Default]));
#pragma warning restore CS0618
        Type?[] values = [typeof(int), typeof(List<Dictionary<string, int[]>>), null];
        foreach (var value in values)
        {
            var ours = MessagePackSerializer.Serialize(value, options);
            Assert.Equal(Oracle.Serialize(value), ours);
            Assert.Equal(value, MessagePackSerializer.Deserialize<Type?>(ours, options));
        }

        // an unresolvable name is a load error, not silently null
        var bogus = MessagePackSerializer.Serialize("Not.A.Real.Type, Not.A.Real.Assembly");
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Type?>(bogus, options));
    }

    [Fact]
    public void ModernScalars_MatchOracleAndRoundtrip()
    {
        foreach (var half in new[] { Half.Zero, Half.One, Half.MinValue, Half.MaxValue, Half.Epsilon, (Half)(-1.5f) })
        {
            AssertOracleAndRoundtrip(half);
        }
        // NaN compares unequal to itself: wire vs oracle + IsNaN roundtrip
        var nan = AssertOracle(Half.NaN);
        Assert.True(Half.IsNaN(MessagePackSerializer.Deserialize<Half>(nan)));

        foreach (var rune in new[] { new Rune('A'), new Rune('あ'), new Rune(0x10FFFF) })
        {
            AssertOracleAndRoundtrip(rune);
        }

        foreach (var date in new[] { DateOnly.MinValue, DateOnly.MaxValue, new DateOnly(2026, 8, 7) })
        {
            AssertOracleAndRoundtrip(date);
        }

        foreach (var time in new[] { TimeOnly.MinValue, TimeOnly.MaxValue, new TimeOnly(12, 34, 56, 789) })
        {
            AssertOracleAndRoundtrip(time);
        }

        foreach (var i128 in new[] { (Int128)0, Int128.One, -Int128.One, Int128.MaxValue, Int128.MinValue, (Int128)long.MaxValue + 1 })
        {
            AssertOracleAndRoundtrip(i128);
        }
        foreach (var u128 in new[] { (UInt128)0, UInt128.One, UInt128.MaxValue, (UInt128)ulong.MaxValue + 1 })
        {
            AssertOracleAndRoundtrip(u128);
        }

        // nullables ride the generic Nullable tier
        AssertOracleAndRoundtrip((Int128?)Int128.MaxValue);
        AssertOracleAndRoundtrip((Int128?)null);
        AssertOracleAndRoundtrip((DateOnly?)new DateOnly(2026, 8, 7));
    }

    [Fact]
    public void ModernScalars_MalformedData_SurfacesAsSerializationException()
    {
        // a surrogate scalar fits the int but is not a valid Rune
        var surrogate = MessagePackSerializer.Serialize(0xD800);
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Rune>(surrogate));

        // day number beyond DateOnly.MaxValue
        var hugeDay = MessagePackSerializer.Serialize(int.MaxValue);
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<DateOnly>(hugeDay));

        // negative ticks / a full day of ticks are both out of TimeOnly's range
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<TimeOnly>(MessagePackSerializer.Serialize(-1L)));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<TimeOnly>(MessagePackSerializer.Serialize(TimeSpan.TicksPerDay)));

        // Int128/UInt128 must be exactly a 16-byte bin
        var shortBin = MessagePackSerializer.Serialize(new byte[15]);
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Int128>(shortBin));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<UInt128>(shortBin));
    }

    [Fact]
    public void MalformedData_SurfacesAsSerializationException()
    {
        // data-driven parse failures must not leak BCL exception types
        // (UriFormatException / FormatException / ArgumentOutOfRangeException)

        // scheme prefix with a broken authority fails even UriKind.RelativeOrAbsolute
        var badUri = MessagePackSerializer.Serialize("http://");
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Uri?>(badUri));

        var badVersion = MessagePackSerializer.Serialize("not-a-version");
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Version?>(badVersion));

        // [timestamp(epoch), int16 32767]: an offset of 32767 minutes (~546h) is far
        // outside DateTimeOffset's +-14h — must throw as a data error, not from the ctor
        var badOffset = new byte[] { 0x92, 0xD6, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xCD, 0x7F, 0xFF };
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<DateTimeOffset>(badOffset));
    }
}

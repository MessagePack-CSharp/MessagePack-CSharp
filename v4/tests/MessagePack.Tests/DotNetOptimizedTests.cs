extern alias V3;
using System.Collections;
using MessagePack;
using MessagePack.Formatters;
using Xunit;
using Oracle = V3::MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// MessagePackFormatterFactory.DotNetOptimized: alternate .NET-to-.NET wire formats.
// Guid/decimal blits are wire-compared against v3's NativeGuid/NativeDecimalFormatter
// (the formats this tier ports); BitArray's packed form is new, so its shape is asserted
// directly plus cross-preset dual-reads.
public class DotNetOptimizedTests
{
    static readonly MessagePackSerializerOptions options =
        new MessagePackSerializerOptions(new MessagePackFormatterResolver([MessagePackFormatterFactory.DotNetOptimized]));

    static V3::MessagePack.MessagePackSerializerOptions OracleOptions { get; } =
        V3::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(
            V3::MessagePack.Resolvers.CompositeResolver.Create(
                [
                    V3::MessagePack.Formatters.NativeGuidFormatter.Instance,
                    V3::MessagePack.Formatters.NativeDecimalFormatter.Instance,
                    V3::MessagePack.Formatters.NativeDateTimeFormatter.Instance,
                ],
                [V3::MessagePack.Resolvers.StandardResolver.Instance]));

    [Fact]
    public void Guid_BlitWire_MatchesV3NativeAndRoundtrips()
    {
        Guid[] values = [Guid.Empty, Guid.Parse("01234567-89ab-cdef-0123-456789abcdef")];
        foreach (var value in values)
        {
            var ours = MessagePackSerializer.Serialize(value, options);
            Assert.Equal(Oracle.Serialize(value, OracleOptions), ours);
            Assert.Equal(0xC4, ours[0]); // bin8(16), 18 bytes total vs the default's 38-byte str
            Assert.Equal(18, ours.Length);
            Assert.Equal(value, MessagePackSerializer.Deserialize<Guid>(ours, options));
        }

        // a 15-byte bin is malformed
        var shortBin = MessagePackSerializer.Serialize(new byte[15]);
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<Guid>(shortBin, options));
    }

    [Fact]
    public void Decimal_BlitWire_MatchesV3NativeAndRoundtripsWithScale()
    {
        decimal[] values = [0m, 1.10m, decimal.MaxValue, decimal.MinValue, -0.000000001m];
        foreach (var value in values)
        {
            var ours = MessagePackSerializer.Serialize(value, options);
            Assert.Equal(Oracle.Serialize(value, OracleOptions), ours);
            Assert.Equal(18, ours.Length);
            Assert.Equal(value, MessagePackSerializer.Deserialize<decimal>(ours, options));
        }

        // the binary image preserves scale bits exactly: 1.10 stays "1.10"
        var back = MessagePackSerializer.Deserialize<decimal>(MessagePackSerializer.Serialize(1.10m, options), options);
        Assert.Equal("1.10", back.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Decimal_ForgedFlagBits_AreDataErrors()
    {
        // this is what separates DotNetOptimizedDecimalFormatter from the obsolete
        // Native blit: invalid scale/flag bits are rejected instead of materialized

        // scale = 29 (valid bit positions, invalid value)
        var forgedScale = new byte[] { 0xC4, 16, 0x00, 0x00, 0x1D, 0x00, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 };
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<decimal>(forgedScale, options));

        // garbage in the must-be-zero low bits of flags
        var forgedLow = new byte[] { 0xC4, 16, 0x01, 0x00, 0x00, 0x00, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 };
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<decimal>(forgedLow, options));
    }

    [Fact]
    public void DateTime_ToBinary_PreservesKind()
    {
        DateTime[] values =
        [
            new DateTime(2026, 8, 7, 1, 2, 3, DateTimeKind.Utc),
            new DateTime(2026, 8, 7, 1, 2, 3, DateTimeKind.Local),
            new DateTime(2026, 8, 7, 1, 2, 3, DateTimeKind.Unspecified),
        ];
        foreach (var value in values)
        {
            // constant shape: forced int64, 9 bytes flat (v3 native wrote smallest-format;
            // byte-identity was traded for the fixed shape — cross-reads must still hold)
            var ours = MessagePackSerializer.Serialize(value, options);
            Assert.Equal(9, ours.Length);
            Assert.Equal(0xD3, ours[0]);
            var back = MessagePackSerializer.Deserialize<DateTime>(ours, options);
            Assert.Equal(value, back);
            Assert.Equal(value.Kind, back.Kind); // the default timestamp ext loses this

            // v3 native readers accept our forced width; we accept v3's smallest-format
            var v3Back = Oracle.Deserialize<DateTime>(ours, OracleOptions);
            Assert.Equal(value, v3Back);
            Assert.Equal(value.Kind, v3Back.Kind);
            var v3Bytes = Oracle.Serialize(value, OracleOptions);
            var fromV3 = MessagePackSerializer.Deserialize<DateTime>(v3Bytes, options);
            Assert.Equal(value, fromV3);
            Assert.Equal(value.Kind, fromV3.Kind);
        }
    }

    [Fact]
    public void DateTimeOffset_ConstantShape_PreservesTicksAndOffset()
    {
        // reinstated after measurement (write 5x / read 2.9x vs the default's
        // three-token composite, see DateTimeOffsetFormatBenchmark)
        DateTimeOffset[] values =
        [
            new DateTimeOffset(2026, 8, 7, 12, 34, 56, 789, TimeSpan.FromHours(9)),
            new DateTimeOffset(1999, 12, 31, 23, 59, 59, TimeSpan.FromMinutes(-330)),
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.MinValue,
            DateTimeOffset.MaxValue,
        ];
        foreach (var value in values)
        {
            var ours = MessagePackSerializer.Serialize(value, options);
            // constant shape: fixarray(2) + forced int64 + forced int16 = 13 bytes flat
            Assert.Equal(13, ours.Length);
            Assert.Equal(0x92, ours[0]);
            Assert.Equal(0xD3, ours[1]);
            Assert.Equal(0xD1, ours[10]);
            Assert.True(value.EqualsExact(MessagePackSerializer.Deserialize<DateTimeOffset>(ours, options)));
        }

        // the fallback reader accepts smallest-format encodings: [0, 0] = MinValue at UTC
        var compact = new byte[] { 0x92, 0x00, 0x00 };
        Assert.True(DateTimeOffset.MinValue.EqualsExact(MessagePackSerializer.Deserialize<DateTimeOffset>(compact, options)));

        // forged offset (32767 minutes) is a data error
        var badOffset = MessagePackSerializer.Serialize((0L, 32767));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<DateTimeOffset>(badOffset, options));
    }

    [Fact]
    public void DateTime_BitDirectDecode_MatchesFromBinary()
    {
        // the read fast path replaces DateTime.FromBinary with bit-direct decode for
        // Utc/Unspecified; Local still routes through FromBinary (timezone reconversion)
        DateTime[] values =
        [
            new DateTime(2026, 8, 7, 1, 2, 3, DateTimeKind.Utc),
            new DateTime(2026, 8, 7, 1, 2, 3, DateTimeKind.Unspecified),
            new DateTime(2026, 8, 7, 1, 2, 3, DateTimeKind.Local),
            DateTime.MinValue, // Unspecified, ticks 0
            DateTime.MaxValue,
        ];
        foreach (var value in values)
        {
            var back = MessagePackSerializer.Deserialize<DateTime>(MessagePackSerializer.Serialize(value, options), options);
            Assert.Equal(DateTime.FromBinary(value.ToBinary()), back);
            Assert.Equal(value.Kind, back.Kind);
        }

        // forged out-of-range ticks (kind bits clear) are data errors
        var forged = MessagePackSerializer.Serialize(0x3FFF_FFFF_FFFF_FFFFL);
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<DateTime>(forged, options));
    }

    [Fact]
    public void BitArray_Packed_ShapeAndRoundtrip()
    {
        foreach (var bitLength in new[] { 0, 1, 8, 9, 33, 1000 })
        {
            var bits = new BitArray(bitLength);
            for (int i = 0; i < bitLength; i += 3)
            {
                bits[i] = true;
            }

            var packed = MessagePackSerializer.Serialize(bits, options);
            Assert.Equal(0x92, packed[0]); // [bitLength, bin]
            var back = MessagePackSerializer.Deserialize<BitArray>(packed, options)!;
            Assert.Equal(bits.Cast<bool>(), back.Cast<bool>());
        }

        // 1000 bits: packed ~= 130 bytes vs the default form's 1003
        var thousand = new BitArray(1000);
        Assert.True(MessagePackSerializer.Serialize(thousand, options).Length < 140);
        Assert.Equal(1003, MessagePackSerializer.Serialize(thousand).Length);

        Assert.Null(MessagePackSerializer.Deserialize<BitArray?>(MessagePackSerializer.Serialize<BitArray?>(null, options), options));
    }

    [Fact]
    public void BitArray_DualRead_CrossesPresets()
    {
        var bits = new BitArray(new[] { true, false, true, true, false, true, false, false, true });

        // default reader accepts the packed form
        var packed = MessagePackSerializer.Serialize(bits, options);
        Assert.Equal(bits.Cast<bool>(), MessagePackSerializer.Deserialize<BitArray>(packed)!.Cast<bool>());

        // optimized reader accepts the legacy bool-array form
        var legacy = MessagePackSerializer.Serialize(bits);
        Assert.Equal(bits.Cast<bool>(), MessagePackSerializer.Deserialize<BitArray>(legacy, options)!.Cast<bool>());
    }

    [Fact]
    public void OptimizedReaders_AcceptDefaultForms()
    {
        // one-directional migration compat: upgrade every reader to this preset first
        // (they still read default-form data), then flip writers. Default readers stay
        // strict so the standard tier keeps its cross-language contract.
        var guid = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        Assert.Equal(guid, MessagePackSerializer.Deserialize<Guid>(MessagePackSerializer.Serialize(guid), options));

        Assert.Equal(1.10m, MessagePackSerializer.Deserialize<decimal>(MessagePackSerializer.Serialize(1.10m), options));

        // the timestamp form reads as Utc — the default reader's own contract
        var utc = new DateTime(2026, 8, 7, 1, 2, 3, DateTimeKind.Utc);
        Assert.Equal(utc, MessagePackSerializer.Deserialize<DateTime>(MessagePackSerializer.Serialize(utc), options));
        var subSecond = utc.AddTicks(1234567); // ts64 fixext8 class (whole seconds take ts32 fixext4)
        Assert.Equal(subSecond, MessagePackSerializer.Deserialize<DateTime>(MessagePackSerializer.Serialize(subSecond), options));

        var dto = new DateTimeOffset(2026, 8, 7, 12, 34, 56, TimeSpan.FromHours(9));
        Assert.True(dto.EqualsExact(MessagePackSerializer.Deserialize<DateTimeOffset>(MessagePackSerializer.Serialize(dto), options)));

        // default readers remain strict: optimized wire is rejected
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<Guid>(MessagePackSerializer.Serialize(guid, options)));
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<decimal>(MessagePackSerializer.Serialize(1.10m, options)));
    }

    [Fact]
    public void OtherTypes_FallThroughToDefaultChain()
    {
        // anything the tier does not serve resolves exactly as Default does
        Assert.Equal(MessagePackSerializer.Serialize(42), MessagePackSerializer.Serialize(42, options));
        Assert.Equal(MessagePackSerializer.Serialize("text"), MessagePackSerializer.Serialize("text", options));
        Assert.Equal(
            MessagePackSerializer.Serialize(new List<int> { 1, 2, 3 }),
            MessagePackSerializer.Serialize(new List<int> { 1, 2, 3 }, options));
    }

    [Fact]
    public void GeneratedFormatter_DateTimeMember_HonorsTheConfiguredChain()
    {
        // regression: the generator used to classify DateTime as a direct write
        // (UnsafeWriteTimestamp / ReadTimestamp inlined into the generated formatter),
        // which pinned the default encoding and made this whole tier a no-op for every
        // DateTime member of a [MessagePackObject] type — Kind silently dropped
        var value = new DotNetOptimizedStampPoco
        {
            Id = 7,
            Utc = new DateTime(2026, 8, 7, 1, 2, 3, DateTimeKind.Utc),
            Local = new DateTime(2026, 8, 7, 1, 2, 3, DateTimeKind.Local),
            Unspecified = new DateTime(2026, 8, 7, 1, 2, 3, DateTimeKind.Unspecified),
        };

        var optimized = MessagePackSerializer.Serialize(value, options);
        var back = MessagePackSerializer.Deserialize<DotNetOptimizedStampPoco>(optimized, options)!;
        Assert.Equal(value.Utc, back.Utc);
        Assert.Equal(DateTimeKind.Utc, back.Utc.Kind);
        Assert.Equal(value.Local, back.Local);
        Assert.Equal(DateTimeKind.Local, back.Local.Kind);
        Assert.Equal(value.Unspecified, back.Unspecified);
        Assert.Equal(DateTimeKind.Unspecified, back.Unspecified.Kind);

        // the members really are on the optimized wire: fixarray(4), int, then three
        // forced int64 tokens (the default emits timestamp ext here)
        Assert.Equal(0x94, optimized[0]);
        Assert.Equal(0x07, optimized[1]);
        Assert.Equal(0xD3, optimized[2]);
        Assert.Equal(2 + (3 * 9), optimized.Length);

        // the default chain is untouched: still byte-identical to the v3 oracle
        Assert.Equal(Oracle.Serialize(value), MessagePackSerializer.Serialize(value));
    }
}

[V3::MessagePack.MessagePackObject]
public class DotNetOptimizedStampPoco
{
    [V3::MessagePack.Key(0)] public int Id { get; set; }
    [V3::MessagePack.Key(1)] public DateTime Utc { get; set; }
    [V3::MessagePack.Key(2)] public DateTime Local { get; set; }
    [V3::MessagePack.Key(3)] public DateTime Unspecified { get; set; }
}

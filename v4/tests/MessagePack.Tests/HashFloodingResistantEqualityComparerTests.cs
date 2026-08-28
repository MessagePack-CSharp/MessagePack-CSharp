using MessagePack;
using Xunit;

namespace MessagePack.Tests;

public class HashFloodingResistantEqualityComparerTests
{
    enum ByteEnum : byte { A = 1, B = 2 }
    enum LongEnum : long { A = 1, B = long.MaxValue }

    struct CustomKey : IEquatable<CustomKey>
    {
        public int Value;
        public bool Equals(CustomKey other) => Value == other.Value;
    }

    static void AssertConsistent<T>(T a, T sameAsA, T different) where T : notnull
    {
        var comparer = HashFloodingResistantEqualityComparer.Get<T>();
        Assert.NotNull(comparer);
        Assert.True(comparer.Equals(a, sameAsA));
        Assert.Equal(comparer.GetHashCode(a), comparer.GetHashCode(sameAsA));
        Assert.False(comparer.Equals(a, different));
        // SipHash makes accidental 32-bit collision astronomically unlikely
        Assert.NotEqual(comparer.GetHashCode(a), comparer.GetHashCode(different));
    }

    [Fact]
    public void WhitelistTypes_EqualValuesHashEqual()
    {
        AssertConsistent(true, true, false);
        AssertConsistent('あ', 'あ', 'い');
        AssertConsistent((sbyte)-3, (sbyte)-3, (sbyte)3);
        AssertConsistent((byte)200, (byte)200, (byte)1);
        AssertConsistent((short)-30000, (short)-30000, (short)5);
        AssertConsistent((ushort)60000, (ushort)60000, (ushort)5);
        AssertConsistent(int.MinValue, int.MinValue, 42);
        AssertConsistent(uint.MaxValue, uint.MaxValue, 42u);
        AssertConsistent(long.MinValue, long.MinValue, 42L);
        AssertConsistent(ulong.MaxValue, ulong.MaxValue, 42ul);
        var g = Guid.NewGuid();
        AssertConsistent(g, g, Guid.NewGuid());
        AssertConsistent(1.5f, 1.5f, 2.5f);
        AssertConsistent(1.5, 1.5, 2.5);
        var now = DateTime.UtcNow;
        AssertConsistent(now, now, now.AddTicks(1));
        var dto = DateTimeOffset.UtcNow;
        AssertConsistent(dto, dto, dto.AddTicks(1));
        AssertConsistent(ByteEnum.A, ByteEnum.A, ByteEnum.B);
        AssertConsistent(LongEnum.A, LongEnum.A, LongEnum.B);
    }

    // On modern .NET string passes through (null): Dictionary's own adaptive randomized
    // hashing already covers HashDoS, and pass-through measured 2.1x faster per insert.
    // (Downlevel targets still get StringHashComparer; this test project runs net10.0.)
    [Fact]
    public void String_PassesThroughOnModernNet()
    {
        Assert.Null(HashFloodingResistantEqualityComparer.Get<string>());
    }

    // StringHashComparer remains in use by the object-fallback comparer on every target
    [Fact]
    public void String_HashesContentNotReference()
    {
        var comparer = StringHashComparer.Instance;
        var a = new string('x', 100);
        var b = new string('x', 100);
        Assert.NotSame(a, b);
        Assert.True(comparer.Equals(a, b));
        Assert.Equal(comparer.GetHashCode(a), comparer.GetHashCode(b));
    }

    [Fact]
    public void Float_NegativeZeroAndNaN_Canonicalized()
    {
        var f = HashFloodingResistantEqualityComparer.Get<float>()!;
        Assert.True(f.Equals(0f, -0f));
        Assert.Equal(f.GetHashCode(0f), f.GetHashCode(-0f));
        var payloadNaN = BitConverter.Int32BitsToSingle(unchecked((int)0xFFC0_0001));
        Assert.True(f.Equals(float.NaN, payloadNaN));
        Assert.Equal(f.GetHashCode(float.NaN), f.GetHashCode(payloadNaN));

        var d = HashFloodingResistantEqualityComparer.Get<double>()!;
        Assert.True(d.Equals(0d, -0d));
        Assert.Equal(d.GetHashCode(0d), d.GetHashCode(-0d));
        var payloadNaNd = BitConverter.Int64BitsToDouble(unchecked((long)0xFFF8_0000_0000_0001));
        Assert.True(d.Equals(double.NaN, payloadNaNd));
        Assert.Equal(d.GetHashCode(double.NaN), d.GetHashCode(payloadNaNd));
    }

    [Fact]
    public void DateTime_KindIgnored_LikeDefaultEquality()
    {
        var comparer = HashFloodingResistantEqualityComparer.Get<DateTime>()!;
        var ticks = DateTime.UtcNow.Ticks;
        var utc = new DateTime(ticks, DateTimeKind.Utc);
        var local = new DateTime(ticks, DateTimeKind.Local);
        Assert.True(comparer.Equals(utc, local)); // matches ==
        Assert.Equal(comparer.GetHashCode(utc), comparer.GetHashCode(local));
    }

    [Fact]
    public void DateTimeOffset_SameInstantDifferentOffset_HashEqual()
    {
        var comparer = HashFloodingResistantEqualityComparer.Get<DateTimeOffset>()!;
        var a = new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);
        var b = a.ToOffset(TimeSpan.FromHours(9));
        Assert.True(comparer.Equals(a, b));
        Assert.Equal(comparer.GetHashCode(a), comparer.GetHashCode(b));
    }

    [Fact]
    public void ObjectKeys_DispatchOnRuntimeType()
    {
        var comparer = (IEqualityComparer<object>)HashFloodingResistantEqualityComparer.Get<object>()!;
        Assert.True(comparer.Equals(42, 42));
        Assert.Equal(comparer.GetHashCode(42), comparer.GetHashCode(42));
        Assert.False(comparer.Equals(42, 42L)); // object.Equals across types

        // string via object must agree with StringHashComparer (same process key + path);
        // object keys never pass through — the runtime type is attacker-chosen
        Assert.Equal(StringHashComparer.Instance.GetHashCode("abc"), comparer.GetHashCode("abc"));

        // unknown reference types fall through to their own GetHashCode
        var array = new byte[] { 1, 2, 3 };
        Assert.Equal(array.GetHashCode(), comparer.GetHashCode(array));
    }

    [Fact]
    public void UnlistedTypes_FallThroughAsNull()
    {
        Assert.Null(HashFloodingResistantEqualityComparer.Get<CustomKey>());
        Assert.Null(HashFloodingResistantEqualityComparer.Get<byte[]>());
        Assert.Null(HashFloodingResistantEqualityComparer.Get<decimal>());
        Assert.Null(HashFloodingResistantEqualityComparer.Get<Uri>());
    }

    // the ≤8-byte Hash64Fixed fast path must produce exactly what the span path (Hash64
    // over the value's LE bytes under the process key) produces
    [Fact]
    public void FixedSizeFastPath_MatchesSpanPath()
    {
        var k0 = HashFloodingResistantEqualityComparer.sipHashKey0;
        var k1 = HashFloodingResistantEqualityComparer.sipHashKey1;

        static int Fold(ulong h) => (int)(h ^ (h >> 32));

        void AssertMatches<T>(T value, byte[] leBytes) where T : notnull
        {
            var comparer = HashFloodingResistantEqualityComparer.Get<T>()!;
            Assert.Equal(Fold(SipHash.Hash64(leBytes, k0, k1)), comparer.GetHashCode(value));
        }

        AssertMatches(true, [1]);
        AssertMatches('あ', BitConverter.GetBytes('あ'));
        AssertMatches((short)-12345, BitConverter.GetBytes((short)-12345));
        AssertMatches(0x12345678, BitConverter.GetBytes(0x12345678));
        AssertMatches(long.MinValue, BitConverter.GetBytes(long.MinValue));
        AssertMatches(ulong.MaxValue - 42, BitConverter.GetBytes(ulong.MaxValue - 42));
        AssertMatches(ByteEnum.B, [2]);
        AssertMatches(LongEnum.B, BitConverter.GetBytes(long.MaxValue));
        // 16-byte Guid takes the two-block Hash64Fixed16 path — must still equal the
        // span path over its memory bytes (which ToByteArray reproduces on LE)
        var g = Guid.NewGuid();
        AssertMatches(g, g.ToByteArray());
    }

    [Fact]
    public void Get_ReturnsSingletonPerType()
    {
        Assert.Same(
            HashFloodingResistantEqualityComparer.Get<long>(),
            HashFloodingResistantEqualityComparer.Get<long>());
        Assert.Same(
            HashFloodingResistantEqualityComparer.Get<int>(),
            HashFloodingResistantEqualityComparer.Get<int>());
    }

    [Fact]
    public void WorksAsDictionaryComparer()
    {
        var dict = new Dictionary<string, int>(StringHashComparer.Instance);
        for (var i = 0; i < 1000; i++)
        {
            dict["key" + i] = i;
        }
        Assert.Equal(1000, dict.Count);
        for (var i = 0; i < 1000; i++)
        {
            Assert.Equal(i, dict["key" + i]);
        }
        Assert.False(dict.ContainsKey("key1000"));

        var intSet = new HashSet<long>(HashFloodingResistantEqualityComparer.Get<long>());
        for (var i = 0L; i < 1000; i++)
        {
            Assert.True(intSet.Add(i * 65536)); // bucket-aligned values under identity hashing
        }
        Assert.Equal(1000, intSet.Count);
        Assert.Contains(999 * 65536L, intSet);
    }
}

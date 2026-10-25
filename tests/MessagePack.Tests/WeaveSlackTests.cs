using SerializerFoundation;
using MessagePack.Formatters;

namespace MessagePack.Tests;

// PrimitiveCollectionFormatters.ElementCodecs.cs WeaveSlack: the int32/uint32 AVX2 and int64/uint64 VBMI wide weaves
// store past their token run, and each region reservation carries that overhang as slack. Every all-wide run here is
// written into a span of exactly the worst case plus WeaveSlack, over an array with a canary tail: a store reaching
// past the reservation (a weave offset moved without the slack following it) changes the canary instead of silently
// writing into whatever follows. The counts end a region on a full weave block, the one place the overhang lands past
// the worst case. Which tier runs is the machine's (VBMI, AVX2, SSSE3 or scalar); the wire is checked against
// per-element serialization either way. On an AVX-512 machine int32/uint32 take the overhang-free VBMI weave, so run
// once with DOTNET_EnableAVX512=0 (.NET 10's knob; DOTNET_EnableAVX512F no longer has an effect) to put them on AVX2.
public class WeaveSlackTests
{
    const byte Canary = 0xA5;
    const int CanaryLength = 128;

    [Fact]
    public void Int32() => Check<int, Int32ElementCodec>(
        i => 100_000 + i, MessagePackPrimitives.MaxInt32Length, WeaveSlack.Token5Avx2, block: 16, Int32ElementCodec.SerializeRegionElements);

    [Fact]
    public void UInt32() => Check<uint, UInt32ElementCodec>(
        i => 100_000u + (uint)i, MessagePackPrimitives.MaxUInt32Length, WeaveSlack.Token5Avx2, block: 16, UInt32ElementCodec.SerializeRegionElements);

    [Fact]
    public void Int64() => Check<long, Int64ElementCodec>(
        i => (1L << 40) + i, MessagePackPrimitives.MaxInt64Length, WeaveSlack.Token9Vbmi512, block: 8, Int64ElementCodec.SerializeRegionElements);

    [Fact]
    public void UInt64() => Check<ulong, UInt64ElementCodec>(
        i => (1UL << 40) + (ulong)i, MessagePackPrimitives.MaxUInt64Length, WeaveSlack.Token9Vbmi512, block: 8, UInt64ElementCodec.SerializeRegionElements);

    static void Check<T, TCodec>(Func<int, T> wide, int maxLength, int slack, int block, int region)
        where T : unmanaged
        where TCodec : struct, IElementCodec<T>
    {
        foreach (var count in new[] { block, 2 * block, region, region + block })
        {
            var values = Enumerable.Range(0, count).Select(wide).ToArray();
            var expected = values.SelectMany(v => MessagePackSerializer.Serialize(v)).ToArray();
            Assert.Equal(count * maxLength, expected.Length); // every element really takes the widest token

            // all-wide: each region consumes exactly n * maxLength, so the last region's reservation is what is left
            var reserved = count * maxLength + slack;
            var backing = new byte[reserved + CanaryLength];
            backing.AsSpan(reserved).Fill(Canary);

            var buffer = new SpanWriteBuffer(backing.AsSpan(0, reserved));
            default(TCodec).WriteElements(ref buffer, values);

            var name = $"{typeof(T).Name}[{count}]";
            Assert.True(buffer.BytesWritten == expected.Length, $"{name}: wrote {buffer.BytesWritten}, expected {expected.Length}");
            Assert.True(backing.AsSpan(0, expected.Length).SequenceEqual(expected), $"{name}: wire differs from per-element serialization");
            Assert.True(backing.AsSpan(reserved).IndexOfAnyExcept(Canary) < 0, $"{name}: a store reached past the reservation");
        }
    }
}

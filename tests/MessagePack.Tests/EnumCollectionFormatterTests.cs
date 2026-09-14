extern alias V3;
using System.Runtime.CompilerServices;
using MessagePack.Formatters;
using SerializerFoundation;
using Oracle = V3::MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// EnumCollectionFormatters.cs: TEnum[] / List<TEnum> reinterpreted as the underlying integer
// collection and pushed through the primitive element codecs. Every underlying width is
// wire-compared against v3 (which serializes an enum element as its integer) AND against our
// own serialization of the cast integer collection, at SIMD chunk-boundary counts with value
// pools crossing every token width; then the resolver dispatch itself is pinned, because a
// formatter nothing routes to is a formatter nothing tests.
public class EnumCollectionFormatterTests
{
    static readonly int[] Counts = [0, 1, 15, 16, 17, 33, 64, 65, 100, 1000];

    public enum ByteEnum : byte { Zero = 0, One = 1 }
    public enum SByteEnum : sbyte { Zero = 0, MinusOne = -1 }
    public enum ShortEnum : short { Zero = 0, One = 1 }
    public enum UShortEnum : ushort { Zero = 0, One = 1 }
    public enum IntEnum { Zero = 0, One = 1 }
    public enum UIntEnum : uint { Zero = 0, One = 1 }
    public enum LongEnum : long { Zero = 0, One = 1 }
    public enum ULongEnum : ulong { Zero = 0, One = 1 }

    // the format-class edges of each width, as in PrimitiveCollectionShapeTests; values
    // outside the declared members are deliberate (an enum carries any underlying value)
    [Fact] public void Byte() => CheckAllCounts<ByteEnum, byte>([0, 1, 127, 128, 200, 255]);
    [Fact] public void SByte() => CheckAllCounts<SByteEnum, sbyte>([0, 1, -1, -32, -33, 127, -128, 100, -100]);
    [Fact] public void Int16() => CheckAllCounts<ShortEnum, short>([0, 1, -1, -32, 127, -33, 128, 255, 256, -128, -129, short.MinValue, short.MaxValue]);
    [Fact] public void UInt16() => CheckAllCounts<UShortEnum, ushort>([0, 1, 127, 128, 255, 256, ushort.MaxValue]);
    [Fact] public void Int32() => CheckAllCounts<IntEnum, int>([0, 1, -1, -32, -33, 127, 128, 255, 256, 65535, 65536, -32768, -32769, int.MaxValue, int.MinValue]);
    [Fact] public void UInt32() => CheckAllCounts<UIntEnum, uint>([0, 1, 127, 128, 255, 256, 65535, 65536, uint.MaxValue]);
    [Fact] public void Int64() => CheckAllCounts<LongEnum, long>([0, 1, -1, -32, 127, int.MaxValue, int.MinValue, (long)int.MaxValue + 1, (long)int.MinValue - 1, long.MaxValue, long.MinValue]);
    [Fact] public void UInt64() => CheckAllCounts<ULongEnum, ulong>([0, 1, 127, 255, 65535, uint.MaxValue, (ulong)uint.MaxValue + 1, ulong.MaxValue]);

    static void CheckAllCounts<TEnum, TUnderlying>(TUnderlying[] pool)
        where TEnum : struct, Enum
        where TUnderlying : unmanaged
    {
        var rand = new Random(42);
        foreach (var count in Counts)
        {
            var underlying = new TUnderlying[count];
            var values = new TEnum[count];
            for (int i = 0; i < count; i++)
            {
                underlying[i] = pool[rand.Next(pool.Length)];
                values[i] = Unsafe.As<TUnderlying, TEnum>(ref underlying[i]);
            }
            Check(values, underlying);
        }

        // a single-class run (all positive fixint) keeps the vector fast arm on the whole
        // way; the mixed pools above make it re-probe after every scalar chunk
        var uniform = new TEnum[1000];
        var uniformUnderlying = new TUnderlying[1000];
        for (int i = 0; i < uniform.Length; i++)
        {
            uniformUnderlying[i] = pool[1];
            uniform[i] = Unsafe.As<TUnderlying, TEnum>(ref uniformUnderlying[i]);
        }
        Check(uniform, uniformUnderlying);
    }

    static void Check<TEnum, TUnderlying>(TEnum[] values, TUnderlying[] underlying)
        where TEnum : struct, Enum
        where TUnderlying : unmanaged
    {
        var name = $"{typeof(TEnum).Name}[{values.Length}]";

        var arrayBytes = MessagePackSerializer.Serialize(values);
        Assert.True(arrayBytes.AsSpan().SequenceEqual(Oracle.Serialize(values)), $"{name}: bytes differ from v3");
        // byte[] itself is bin-format, so the byte width is referenced through int[] instead
        // (0..255 takes the identical positive-fixint / uint8 tokens from the int32 writer)
        var reference = typeof(TUnderlying) == typeof(byte)
            ? MessagePackSerializer.Serialize(underlying.Select(x => Convert.ToInt32(x)).ToArray())
            : MessagePackSerializer.Serialize(underlying);
        Assert.True(arrayBytes.AsSpan().SequenceEqual(reference), $"{name}: bytes differ from the underlying {typeof(TUnderlying).Name}[]");
        Assert.Equal(values, MessagePackSerializer.Deserialize<TEnum[]>(arrayBytes));

        var list = new List<TEnum>(values);
        var listBytes = MessagePackSerializer.Serialize(list);
        Assert.True(listBytes.AsSpan().SequenceEqual(arrayBytes), $"List<{name}>: bytes differ from the array");
        Assert.True(listBytes.AsSpan().SequenceEqual(Oracle.Serialize(list)), $"List<{name}>: bytes differ from v3");
        Assert.Equal(list, MessagePackSerializer.Deserialize<List<TEnum>>(listBytes));

        // v3-written bytes read back as the same enums (v3 = the oracle's own writer)
        Assert.Equal(values, MessagePackSerializer.Deserialize<TEnum[]>(Oracle.Serialize(values)));
    }

    // ---------------------------------------------------------------- dispatch

    [Fact]
    public void DefaultChain_RoutesEnumArrayAndListToTheCodecFormatters()
    {
        var resolver = MessagePackSerializerOptions.Default.Resolver;
        Assert.IsType<EnumArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, ByteEnum, byte>>(resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, ByteEnum[]>());
        Assert.IsType<EnumArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, LongEnum, long>>(resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, LongEnum[]>());
        Assert.IsType<EnumListFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, IntEnum, int>>(resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, List<IntEnum>>());
        Assert.IsType<EnumListFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, UShortEnum, ushort>>(resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, List<UShortEnum>>());

        // jagged and multi-dimensional enum arrays keep the generic formatters
        Assert.IsType<ArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, IntEnum[]>>(resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, IntEnum[][]>());
        Assert.IsType<TwoDimensionalArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, IntEnum>>(resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, IntEnum[,]>());
    }

    [Fact]
    public void AotChain_ResolvesGeneratedMembersToTheCodecFormatters()
    {
        // SweepRoot (ChunkBoundarySweepTests) carries SweepColor[] (byte) and SweepLevel[] /
        // List<SweepLevel> (int) members: the generator must have harvested the enum
        // collection constructions, or the Native AOT chain (no GenericFormatterFactory)
        // would fail to resolve them
        var resolver = new MessagePackFormatterResolver([MessagePackFormatterFactory.DefaultAot]);
        Assert.IsType<EnumArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, SweepColor, byte>>(resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, SweepColor[]>());
        Assert.IsType<EnumArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, SweepLevel, int>>(resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, SweepLevel[]>());
        Assert.IsType<EnumListFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, SweepLevel, int>>(resolver.GetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, List<SweepLevel>>());
    }

    [Fact]
    public void WidthMismatch_IsRejectedAtConstruction()
    {
        Assert.Throws<InvalidOperationException>(() => new EnumArrayFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, ByteEnum, int>());
        Assert.Throws<InvalidOperationException>(() => new EnumListFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, IntEnum, uint>());
    }

    // ---------------------------------------------------------------- contract parity with ArrayFormatter / ListFormatter

    [Fact]
    public void Nil_And_Empty()
    {
        Assert.Equal(new byte[] { 0xc0 }, MessagePackSerializer.Serialize<IntEnum[]?>(null));
        Assert.Null(MessagePackSerializer.Deserialize<IntEnum[]?>(new byte[] { 0xc0 }));
        Assert.Equal(new byte[] { 0x90 }, MessagePackSerializer.Serialize(Array.Empty<IntEnum>()));
        Assert.Empty(MessagePackSerializer.Deserialize<IntEnum[]>(new byte[] { 0x90 })!);

        Assert.Equal(new byte[] { 0xc0 }, MessagePackSerializer.Serialize<List<IntEnum>?>(null));
        Assert.Null(MessagePackSerializer.Deserialize<List<IntEnum>?>(new byte[] { 0xc0 }));
        Assert.Empty(MessagePackSerializer.Deserialize<List<IntEnum>>(new byte[] { 0x90 })!);
    }

    [Fact]
    public void Populate_ReusesExactLengthArray_AndExistingList()
    {
        var bytes = MessagePackSerializer.Serialize(new[] { IntEnum.One, IntEnum.Zero, (IntEnum)70000 });

        var sameLength = new IntEnum[3];
        var target = sameLength;
        MessagePackSerializer.Deserialize(bytes, ref target);
        Assert.Same(sameLength, target);
        Assert.Equal(new[] { IntEnum.One, IntEnum.Zero, (IntEnum)70000 }, target);

        var otherLength = new IntEnum[2];
        target = otherLength;
        MessagePackSerializer.Deserialize(bytes, ref target);
        Assert.NotSame(otherLength, target);
        Assert.Equal(3, target!.Length);

        var list = new List<IntEnum> { (IntEnum)9, (IntEnum)9, (IntEnum)9, (IntEnum)9, (IntEnum)9 };
        var listTarget = list;
        MessagePackSerializer.Deserialize(bytes, ref listTarget);
        Assert.Same(list, listTarget);
        Assert.Equal(new[] { IntEnum.One, IntEnum.Zero, (IntEnum)70000 }, listTarget);
    }

    [Fact]
    public void ReadsAnyIntegerEncodingOfTheUnderlyingWidth()
    {
        // [uint8 1, uint16 127, positive fixint 5]: a hand-widened stream (never what our
        // writer emits) must read into the byte enum exactly as ReadByte accepts it
        var widened = new byte[] { 0x93, 0xcc, 0x01, 0xcd, 0x00, 0x7f, 0x05 };
        Assert.Equal(new[] { (ByteEnum)1, (ByteEnum)127, (ByteEnum)5 }, MessagePackSerializer.Deserialize<ByteEnum[]>(widened));

        // and a value that does not fit the width is rejected, not truncated
        var overflow = new byte[] { 0x91, 0xcd, 0x01, 0x00 }; // uint16 256 into a byte enum
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<ByteEnum[]>(overflow));
    }

    // ---------------------------------------------------------------- the element formatter still owns the wire

    public enum Named { Alpha, Beta, Gamma }

    [Fact]
    public void EnumAsString_FallsBackToPerElement()
    {
        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [new GenericEnumAsStringFormatterFactory(), MessagePackFormatterFactory.Default]));

        var values = new[] { Named.Gamma, Named.Alpha, Named.Beta };
        var expected = MessagePackSerializer.Serialize(new[] { "Gamma", "Alpha", "Beta" });

        var arrayBytes = MessagePackSerializer.Serialize(values, options);
        Assert.Equal(expected, arrayBytes);
        Assert.Equal(values, MessagePackSerializer.Deserialize<Named[]>(arrayBytes, options));

        var listBytes = MessagePackSerializer.Serialize(new List<Named>(values), options);
        Assert.Equal(expected, listBytes);
        Assert.Equal(values, MessagePackSerializer.Deserialize<List<Named>>(listBytes, options));

        // and the integer wire is untouched under the default options
        Assert.Equal(new byte[] { 0x93, 0x02, 0x00, 0x01 }, MessagePackSerializer.Serialize(values));
    }
}

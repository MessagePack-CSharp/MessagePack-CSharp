using System.Buffers;
using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// reflection tier: two parameters binding the same member (differing by case) must not be accepted as a match, or
// the second overwrites the first's binding and the member's value is lost
[MessagePackObject(true, SuppressSourceGeneration = true)]
public class DoubleBoundConstructor
{
    public DoubleBoundConstructor()
    {
    }

    public DoubleBoundConstructor(int value, int VALUE)
    {
        Value = value + VALUE * 1000;
    }

    public int Value { get; set; }
}

// a factory with F(int) and F(long): the literal 10 binds F(int) exactly, no MsgPack013 (compiling is the assertion)
public sealed partial class OverloadedScaleFactory : MessagePackFormatterFactory
{
    readonly long scale;

    public OverloadedScaleFactory(int scale) => this.scale = scale;

    public OverloadedScaleFactory(long scale) => this.scale = -scale; // the wrong overload would negate

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        => type == typeof(int) ? new ScaledIntFormatter<TWriteBuffer, TReadBuffer>((int)scale, false) : null;
}

[MessagePackObject]
public class OverloadedScaleModel
{
    [Key(0)]
    [MessagePackFormatter(typeof(OverloadedScaleFactory), 10)]
    public int Value { get; set; }
}

// F(long) and F(double), neither exact for the literal 10: long is the better conversion target
public sealed partial class WideningScaleFactory : MessagePackFormatterFactory
{
    readonly long scale;

    public WideningScaleFactory(long scale) => this.scale = scale;

    public WideningScaleFactory(double scale) => this.scale = -(long)scale; // the wrong overload would negate

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        => type == typeof(int) ? new ScaledIntFormatter<TWriteBuffer, TReadBuffer>((int)scale, false) : null;
}

[MessagePackObject]
public class WideningScaleModel
{
    [Key(0)]
    [MessagePackFormatter(typeof(WideningScaleFactory), 10)]
    public int Value { get; set; }
}

// F(int, bool = false) and F(long): the exact int parameter wins although it fills a default (C#'s order)
public sealed partial class DefaultedScaleFactory : MessagePackFormatterFactory
{
    readonly long scale;

    public DefaultedScaleFactory(int scale, bool option = false) => this.scale = option ? 0 : scale;

    public DefaultedScaleFactory(long scale) => this.scale = -scale; // the wrong overload would negate

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        => type == typeof(int) ? new ScaledIntFormatter<TWriteBuffer, TReadBuffer>((int)scale, false) : null;
}

[MessagePackObject]
public class DefaultedScaleModel
{
    [Key(0)]
    [MessagePackFormatter(typeof(DefaultedScaleFactory), 10)]
    public int Value { get; set; }
}

// F(short) and F(long) for the int 40000: only long converts implicitly (short would overflow)
public sealed partial class NarrowingScaleFactory : MessagePackFormatterFactory
{
    readonly long scale;

    public NarrowingScaleFactory(short scale) => this.scale = -scale; // the wrong overload would negate (and overflow)

    public NarrowingScaleFactory(long scale) => this.scale = scale;

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        => type == typeof(long) ? new ScaledLongFormatter<TWriteBuffer, TReadBuffer>(scale) : null;
}

public sealed partial class ScaledLongFormatter<TWriteBuffer, TReadBuffer>(long scale) : IMessagePackFormatter<TWriteBuffer, TReadBuffer, long>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, long value) => buffer.WriteInt64(value * scale);

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref long value) => value = buffer.ReadInt64() / scale;
}

// the reflection tier must take the same constructor as the generated code for every one of these factories
[MessagePackObject(SuppressSourceGeneration = true)]
public class ReflectionScaleModels
{
    [Key(3)]
    [MessagePackFormatter(typeof(NarrowingScaleFactory), 40000)]
    public long Narrowing { get; set; }

    [Key(0)]
    [MessagePackFormatter(typeof(OverloadedScaleFactory), 10)]
    public int Exact { get; set; }

    [Key(1)]
    [MessagePackFormatter(typeof(WideningScaleFactory), 10)]
    public int Widening { get; set; }

    [Key(2)]
    [MessagePackFormatter(typeof(DefaultedScaleFactory), 10)]
    public int Defaulted { get; set; }
}

// reflection, explicit keys: a derived override's [IgnoreMember] wins over the base declaration's [Key]
public class IgnoredOverrideBase
{
    [Key(0)] public virtual int X { get; set; }

    [Key(1)] public int Y { get; set; }
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class IgnoredOverrideReflection : IgnoredOverrideBase
{
    [IgnoreMember] public override int X { get; set; }
}

[MessagePackObject]
public class IgnoredOverrideGenerated : IgnoredOverrideBase
{
    [IgnoreMember] public override int X { get; set; }
}

// a factory nested in a partial interface: the bridge is emitted into `partial interface`, not `partial class`
public partial interface IBridgeHost
{
    public sealed partial class Factory : MessagePackFormatterFactory
    {
        public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type) => null;
    }
}

public class ReviewRound9RegressionTests
{
    sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory, RunningIndex + Memory.Length);
            Next = next;
            return next;
        }
    }

    static ReadOnlySequence<byte> Segmented(params byte[][] parts)
    {
        var first = new Segment(parts[0], 0);
        var last = first;
        for (int i = 1; i < parts.Length; i++)
        {
            last = last.Append(parts[i]);
        }
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    // a bin8 header whose length byte sits in the next segment, and the value it opens is EMPTY: the stitched token
    // ends the message, and the scanner must stop there instead of walking into the next message
    [Fact]
    public void BoundaryScanner_StitchedHeaderThatEndsTheValue_StopsThere()
    {
        var scanner = new MessagePackBoundaryScanner();
        var buffer = Segmented([0xC4], [0x00, 0x01]); // bin8(len 0) | fixint 1 (the next message)
        Assert.True(scanner.TryFindEnd(buffer));
        Assert.Equal(2, scanner.Consumed);

        // the same with an empty str8 and an empty array16 split before their last length byte
        scanner = new MessagePackBoundaryScanner();
        Assert.True(scanner.TryFindEnd(Segmented([0xD9], [0x00, 0xA1, (byte)'x'])));
        Assert.Equal(2, scanner.Consumed);
        scanner = new MessagePackBoundaryScanner();
        Assert.True(scanner.TryFindEnd(Segmented([0xDC, 0x00], [0x00, 0x01])));
        Assert.Equal(3, scanner.Consumed);

        // and a non-empty one still continues into its payload
        scanner = new MessagePackBoundaryScanner();
        Assert.True(scanner.TryFindEnd(Segmented([0xC4], [0x02, 0xAA, 0xBB, 0x01])));
        Assert.Equal(4, scanner.Consumed);
    }

    [Fact]
    public void Reflection_ConstructorClaimingAMemberTwice_IsNotSelected()
    {
        var back = V4.Deserialize<DoubleBoundConstructor>(V4.Serialize(new DoubleBoundConstructor { Value = 7 }))!;
        Assert.Equal(7, back.Value);
    }

    [Fact]
    public void FormatterAttribute_ExactOverloadWins()
    {
        var bytes = V4.Serialize(new OverloadedScaleModel { Value = 4 });
        Assert.Equal(new byte[] { 0x91, 0x28 }, bytes); // [40]: F(int) with 10, not F(long) with -10
        Assert.Equal(4, V4.Deserialize<OverloadedScaleModel>(bytes)!.Value);

        var widened = V4.Serialize(new WideningScaleModel { Value = 4 });
        Assert.Equal(new byte[] { 0x91, 0x28 }, widened); // F(long), not F(double)

        var defaulted = V4.Serialize(new DefaultedScaleModel { Value = 4 });
        Assert.Equal(new byte[] { 0x91, 0x28 }, defaulted); // F(int, bool = false), not F(long)

        // and the reflection tier agrees on all of them (40000 * 1 = 40000 = 0x9C40 as uint16)
        Assert.Equal(new byte[] { 0x94, 0x28, 0x28, 0x28, 0xCD, 0x9C, 0x40 }, V4.Serialize(new ReflectionScaleModels { Exact = 4, Widening = 4, Defaulted = 4, Narrowing = 1 }));
    }

    [Fact]
    public void Reflection_OverrideIgnoreMember_WinsOverBaseKey()
    {
        var reflected = V4.Serialize(new IgnoredOverrideReflection { X = 5, Y = 6 });
        var generated = V4.Serialize(new IgnoredOverrideGenerated { X = 5, Y = 6 });
        Assert.Equal(generated, reflected);
        Assert.Equal(new byte[] { 0x92, 0xC0, 0x06 }, reflected); // [nil (key 0 retired), 6]: X is not written
        Assert.Equal(0, V4.Deserialize<IgnoredOverrideReflection>(reflected)!.X);
    }

    [Fact]
    public void ObjectKeyedComparer_CoversBoxedEnums()
    {
        var comparer = HashFloodingResistantEqualityComparer.Get<object>()!;
        var hashes = new HashSet<int>();
        for (var i = 1; i < 1000; i++)
        {
            hashes.Add(comparer.GetHashCode((LongBackedEnum)(i * 0x100000001L))); // every value collides under Enum.GetHashCode
        }
        Assert.True(hashes.Count > 900, $"{hashes.Count} distinct hashes for 999 default-colliding boxed enum keys");
        Assert.Equal(comparer.GetHashCode(LongBackedEnum.A), comparer.GetHashCode((object)LongBackedEnum.A));
        Assert.True(comparer.Equals(LongBackedEnum.A, LongBackedEnum.A));
    }

    public enum LongBackedEnum : long
    {
        A = 1,
    }

    [Fact]
    public void FactoryNestedInInterface_Compiles()
    {
        Assert.Null(new IBridgeHost.Factory().CreateFormatter(typeof(SerializerFoundation.CompatibleArrayPoolListWriteBuffer), typeof(SerializerFoundation.CompatibleReadOnlySpanReadBuffer), typeof(int)));
    }
}

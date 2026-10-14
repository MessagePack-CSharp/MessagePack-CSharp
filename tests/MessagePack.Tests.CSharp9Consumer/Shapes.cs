// Representative annotated shapes, compiled at C# 9 against netstandard2.1 (see the project file). Keep this file at
// C# 9: no `required`, no file-local types, no generic attributes, no raw string literals, no `scoped`.
using System;
using System.Collections.Generic;
using MessagePack;
using SerializerFoundation;

namespace System.Runtime.CompilerServices
{
    // `init` accessors and positional records need this marker type, which netstandard2.1 does not ship; Unity
    // projects add it the same way (or through PolySharp)
    internal static class IsExternalInit
    {
    }
}

namespace MessagePack.Tests.CSharp9Consumer
{
    public enum Color
    {
        Red,
        Green,
        Blue,
    }

    [MessagePackObject]
    public class IntKeyed
    {
        [Key(0)] public int Id { get; set; }
        [Key(1)] public string? Name { get; set; }
        [Key(2)] public List<int>? Tags { get; set; }
        [Key(3)] public Dictionary<string, Color>? Colors { get; set; }
        [Key(4)] public int? Maybe { get; set; }
        [Key(5)] public Color Color { get; set; }
        [Key(6)] public Wrapper<int>? Wrapped { get; set; }
    }

    [MessagePackObject(true)]
    public class StringKeyed
    {
        public int Id { get; init; }
        public string? Name { get; set; }
        public double[]? Values { get; set; }
    }

    [MessagePackObject]
    public record PositionalRecord([property: Key(0)] int X, [property: Key(1)] string? Label);

    [MessagePackObject]
    public readonly struct Vec
    {
        public Vec(float x, float y)
        {
            X = x;
            Y = y;
        }

        [Key(0)] public readonly float X;
        [Key(1)] public readonly float Y;
    }

    [MessagePackObject(AllowPrivate = true)]
    public partial class Secretive
    {
        [Key(0)] private int secret = 7;
        [Key(1)] public int Shown { get; set; }
        [IgnoreMember] public int Secret => secret;
    }

    [MessagePackObject(AllowCircularReferences = true)]
    public class Node
    {
        [Key(0)] public int Value { get; set; }
        [Key(1)] public Node? Next { get; set; }
    }

    [MessagePackObject]
    [UnionTag(typeof(Circle), 0)]
    [UnionTag(typeof(Square), 1)]
    public abstract class Shape
    {
    }

    [MessagePackObject]
    public sealed class Circle : Shape
    {
        [Key(0)] public double Radius { get; set; }
    }

    [MessagePackObject]
    public sealed class Square : Shape
    {
        [Key(0)] public double Side { get; set; }
    }

    [MessagePackObject]
    public class Wrapper<T>
    {
        [Key(0)] public T? Value { get; set; }
    }

    // a type that cannot be annotated, serialized through a surrogate struct
    public sealed class Money
    {
        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public decimal Amount { get; }
        public string Currency { get; }
    }

    [MessagePackObject]
    public readonly struct MoneySurrogate : IMessagePackSurrogate<Money, MoneySurrogate>
    {
        public MoneySurrogate(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        [Key(0)] public readonly decimal Amount;
        [Key(1)] public readonly string Currency;

        public MoneySurrogate ToSurrogate(Money value) => new MoneySurrogate(value.Amount, value.Currency);

        public Money ToTarget() => new Money(Amount, Currency);
    }

    [MessagePackObject]
    public class WithCallbacks : IMessagePackSerializationCallbackReceiver
    {
        [Key(0)] public int Value { get; set; }
        [IgnoreMember] public int BeforeSerializeCalls { get; private set; }
        [IgnoreMember] public int AfterDeserializeCalls { get; private set; }

        public void OnBeforeSerialize() => BeforeSerializeCalls++;

        public void OnAfterDeserialize() => AfterDeserializeCalls++;
    }

    // a hand-written formatter: partial, no buffer constraints of its own (BufferConstraintsGenerator supplies them,
    // without `allows ref struct` on this target)
    [MessagePackFormatter(typeof(PointFormatter<,>))]
    public readonly struct Point
    {
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public readonly int X;
        public readonly int Y;
    }

    public sealed partial class PointFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Point>
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Point value)
        {
            buffer.WriteArrayHeader(2);
            buffer.WriteInt32(value.X);
            buffer.WriteInt32(value.Y);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Point value)
        {
            var count = buffer.ReadArrayHeader(ref state);
            var x = buffer.ReadInt32();
            var y = buffer.ReadInt32();
            for (int i = 2; i < count; i++)
            {
                buffer.Skip();
            }
            value = new Point(x, y);
        }
    }

    // a hand-written factory: the downlevel half of the README's two-signature shape, FactoryBridgeGenerator
    // generates the Type-based CreateFormatter
    [MessagePackFormatter(typeof(PixelFormatterFactory))]
    public readonly struct Pixel
    {
        public Pixel(int x, int y)
        {
            X = x;
            Y = y;
        }

        public readonly int X;
        public readonly int Y;
    }

    public sealed partial class PixelFormatterFactory : MessagePackFormatterFactory
    {
#if NET9_0_OR_GREATER
        public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
        public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
            where TWriteBuffer : struct, IWriteBuffer
            where TReadBuffer : struct, IReadBuffer
#endif
            => type == typeof(Pixel) ? new PixelFormatter<TWriteBuffer, TReadBuffer>() : null;
    }

    public sealed partial class PixelFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Pixel>
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Pixel value)
        {
            buffer.WriteInt64(((long)value.X << 32) | (uint)value.Y);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Pixel value)
        {
            var packed = buffer.ReadInt64();
            value = new Pixel((int)(packed >> 32), (int)packed);
        }
    }

    // a root that only appears as a serialization root: the non-generic attribute form, C# 9 has no generic attributes
    [MessagePackSerializable(typeof(IntKeyed[]))]
    [MessagePackSerializable(typeof(Dictionary<int, Shape>))]
    [MessagePackSerializable(typeof(Shape[]))] // a union array that is only ever a root
    public partial class Roots
    {
    }
}

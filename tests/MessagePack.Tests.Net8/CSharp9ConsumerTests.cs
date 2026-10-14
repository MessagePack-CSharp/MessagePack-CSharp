using MessagePack;
using MessagePack.Tests.CSharp9Consumer;

namespace MessagePack.Tests.Net8;

// MessagePack.Tests.CSharp9Consumer is a netstandard2.1 library compiled at C# 9 (the Unity level) whose types go
// through every generator; this net8.0 consumer runs that generated code on the netstandard2.1 core. DefaultAot has
// no reflection tier, so a round trip here proves the generated formatter served the type.
public class CSharp9ConsumerTests
{
    static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.DefaultAot;

    static T RoundTrip<T>(T value) => MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, Options), Options)!;

    [Fact]
    public void IntKeyedObject_WithHarvestedCollectionsEnumAndGeneric()
    {
        var value = new IntKeyed
        {
            Id = 1,
            Name = "n",
            Tags = [1, 2, 3],
            Colors = new() { ["a"] = Color.Blue },
            Maybe = null,
            Color = Color.Green,
            Wrapped = new Wrapper<int> { Value = 9 },
        };
        var back = RoundTrip(value);
        Assert.Equal(1, back.Id);
        Assert.Equal("n", back.Name);
        Assert.Equal([1, 2, 3], back.Tags);
        Assert.Equal(Color.Blue, back.Colors!["a"]);
        Assert.Null(back.Maybe);
        Assert.Equal(Color.Green, back.Color);
        Assert.Equal(9, back.Wrapped!.Value);
    }

    [Fact]
    public void StringKeyedObject_WithInitAccessor()
    {
        var back = RoundTrip(new StringKeyed { Id = 5, Name = "s", Values = [1.5, 2.5] });
        Assert.Equal(5, back.Id);
        Assert.Equal("s", back.Name);
        Assert.Equal([1.5, 2.5], back.Values!);
        Assert.Equal("{\"Id\":5,\"Name\":\"s\",\"Values\":[1.5,2.5]}", MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(new StringKeyed { Id = 5, Name = "s", Values = [1.5, 2.5] }, Options)));
    }

    [Fact]
    public void PositionalRecord_AndReadOnlyStruct_BindByConstructor()
    {
        Assert.Equal(new PositionalRecord(3, "l"), RoundTrip(new PositionalRecord(3, "l")));
        var vec = RoundTrip(new Vec(1.5f, -2f));
        Assert.Equal(1.5f, vec.X);
        Assert.Equal(-2f, vec.Y);
    }

    [Fact]
    public void AllowPrivate_NestedFormatter_ReadsAndWritesThePrivateField()
    {
        var bytes = MessagePackSerializer.Serialize(new Secretive { Shown = 2 }, Options);
        Assert.Equal("[7,2]", MessagePackSerializer.ConvertToJson(bytes));
        var back = MessagePackSerializer.Deserialize<Secretive>(bytes, Options)!;
        Assert.Equal(7, back.Secret);
        Assert.Equal(2, back.Shown);
    }

    [Fact]
    public void CircularReferences_AndUnion()
    {
        var node = new Node { Value = 1 };
        node.Next = new Node { Value = 2, Next = node };
        var back = RoundTrip(node);
        Assert.Equal(2, back.Next!.Value);
        Assert.Same(back, back.Next.Next);

        Shape[] shapes = [new Circle { Radius = 1 }, new Square { Side = 2 }];
        var backShapes = RoundTrip(shapes);
        Assert.Equal(1, Assert.IsType<Circle>(backShapes[0]).Radius);
        Assert.Equal(2, Assert.IsType<Square>(backShapes[1]).Side);
    }

    [Fact]
    public void Surrogate_AndCallbacks()
    {
        var money = RoundTrip(new Money(12.5m, "JPY"));
        Assert.Equal(12.5m, money.Amount);
        Assert.Equal("JPY", money.Currency);

        var value = new WithCallbacks { Value = 3 };
        var back = RoundTrip(value);
        Assert.Equal(1, value.BeforeSerializeCalls);
        Assert.Equal(1, back.AfterDeserializeCalls);
        Assert.Equal(3, back.Value);
    }

    [Fact]
    public void HandWrittenPartialFormatter_AndPartialFactory_GetTheirGeneratedHalves()
    {
        // PointFormatter: the buffer constraints came from BufferConstraintsGenerator; PixelFormatterFactory: the
        // Type-based CreateFormatter came from FactoryBridgeGenerator and is the member the netstandard core calls
        var point = RoundTrip(new Point(3, -4));
        Assert.Equal(3, point.X);
        Assert.Equal(-4, point.Y);
        Assert.Equal("[3,-4]", MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(new Point(3, -4), Options)));

        var pixel = RoundTrip(new Pixel(7, -8));
        Assert.Equal(7, pixel.X);
        Assert.Equal(-8, pixel.Y);
    }

    [Fact]
    public void SerializableRoots_AreRegisteredByTheGeneratedFactory()
    {
        var array = RoundTrip(new IntKeyed[] { new() { Id = 1 }, new() { Id = 2 } });
        Assert.Equal([1, 2], array.Select(static v => v.Id));
        var map = RoundTrip(new Dictionary<int, Shape> { [1] = new Circle { Radius = 2 } });
        Assert.Equal(2, Assert.IsType<Circle>(map[1]).Radius);
    }
}

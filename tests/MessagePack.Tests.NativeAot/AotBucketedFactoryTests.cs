using System.Reflection;
using MessagePack;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests.NativeAot;

// A generated factory that serves many types dispatches CreateFormatter through hash buckets (FactoryEmitter): the key
// is computed from Type.Name and the generic arguments at run time and from the symbol at generation time. The types
// below take this assembly's factory over the bucketing threshold, so the published Native AOT executable runs that
// dispatch, reflection calls included, for object types and for the harvested closed forms (List<T>, T[],
// Dictionary<K, V>, Nullable<TEnum>).
public class AotBucketedFactoryTests
{
    static readonly MessagePackSerializerOptions Options = new(new MessagePackFormatterResolver(
    [
        MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
        BuiltInFormatterFactory.Instance,
    ]));

    [Fact]
    public void ThisAssemblysFactoryIsBucketed()
    {
        // absent = the factory fell back under the threshold and the tests below no longer cover the bucket dispatch
        Assert.NotNull(typeof(MessagePack.Generated.GeneratedMessagePackFormatterFactory).GetMethod("MessagePackTypeHash", BindingFlags.NonPublic | BindingFlags.Static));
    }

    [Fact]
    public void EveryBucketedType_RoundTripsThroughBothEntries()
    {
        IAotBucket[] values =
        [
            new AotBucket00 { Value = 0 }, new AotBucket01 { Value = 1 }, new AotBucket02 { Value = 2 }, new AotBucket03 { Value = 3 },
            new AotBucket04 { Value = 4 }, new AotBucket05 { Value = 5 }, new AotBucket06 { Value = 6 }, new AotBucket07 { Value = 7 },
            new AotBucket08 { Value = 8 }, new AotBucket09 { Value = 9 }, new AotBucket10 { Value = 10 }, new AotBucket11 { Value = 11 },
            new AotBucket12 { Value = 12 }, new AotBucket13 { Value = 13 }, new AotBucket14 { Value = 14 }, new AotBucket15 { Value = 15 },
            new AotBucket16 { Value = 16 }, new AotBucket17 { Value = 17 }, new AotBucket18 { Value = 18 }, new AotBucket19 { Value = 19 },
            new AotBucket20 { Value = 20 }, new AotBucket21 { Value = 21 }, new AotBucket22 { Value = 22 }, new AotBucket23 { Value = 23 },
            new AotBucket24 { Value = 24 }, new AotBucket25 { Value = 25 }, new AotBucket26 { Value = 26 }, new AotBucket27 { Value = 27 },
            new AotBucket28 { Value = 28 }, new AotBucket29 { Value = 29 }, new AotBucket30 { Value = 30 }, new AotBucket31 { Value = 31 },
            new AotBucket32 { Value = 32 }, new AotBucket33 { Value = 33 }, new AotBucket34 { Value = 34 }, new AotBucket35 { Value = 35 },
            new AotBucket36 { Value = 36 }, new AotBucket37 { Value = 37 }, new AotBucket38 { Value = 38 }, new AotBucket39 { Value = 39 },
            new AotBucket40 { Value = 40 }, new AotBucket41 { Value = 41 }, new AotBucket42 { Value = 42 }, new AotBucket43 { Value = 43 },
            new AotBucket44 { Value = 44 }, new AotBucket45 { Value = 45 }, new AotBucket46 { Value = 46 }, new AotBucket47 { Value = 47 },
            new AotBucket48 { Value = 48 }, new AotBucket49 { Value = 49 }, new AotBucket50 { Value = 50 }, new AotBucket51 { Value = 51 },
            new AotBucket52 { Value = 52 }, new AotBucket53 { Value = 53 }, new AotBucket54 { Value = 54 }, new AotBucket55 { Value = 55 },
            new AotBucket56 { Value = 56 }, new AotBucket57 { Value = 57 }, new AotBucket58 { Value = 58 }, new AotBucket59 { Value = 59 },
            new AotBucket60 { Value = 60 }, new AotBucket61 { Value = 61 }, new AotBucket62 { Value = 62 }, new AotBucket63 { Value = 63 },
            new AotBucket64 { Value = 64 }, new AotBucket65 { Value = 65 }, new AotBucket66 { Value = 66 }, new AotBucket67 { Value = 67 },
            new AotBucket68 { Value = 68 }, new AotBucket69 { Value = 69 }, new AotBucket70 { Value = 70 }, new AotBucket71 { Value = 71 },
        ];
        for (int i = 0; i < values.Length; i++)
        {
            var type = values[i].GetType();
            // the Type-based entry (built on first use from the registration) and the formatter, both through the buckets
            var bytes = V4.Serialize(type, values[i], Options);
            var back = Assert.IsAssignableFrom<IAotBucket>(V4.Deserialize(type, bytes, Options));
            Assert.Equal(type, back.GetType());
            Assert.Equal(i, back.Value);
        }
    }

    [Fact]
    public void HarvestedClosedForms_RoundTrip()
    {
        var value = new AotBucketGraph
        {
            Items = [new AotBucket01 { Value = 1 }, new AotBucket01 { Value = 2 }],
            Array = [new AotBucket03 { Value = 3 }],
            Map = new Dictionary<int, AotBucket04> { [4] = new AotBucket04 { Value = 4 } },
            Kind = AotBucketKind.B,
        };
        var back = V4.Deserialize<AotBucketGraph>(V4.Serialize(value, Options), Options);
        Assert.Equal([1, 2], back.Items!.Select(static item => item.Value));
        Assert.Equal(3, Assert.Single(back.Array!).Value);
        Assert.Equal(4, back.Map![4].Value);
        Assert.Equal(AotBucketKind.B, back.Kind);

        // the closed forms as roots: their own factory entries, found by the hash of a constructed generic and of an array
        Assert.Equal(5, Assert.Single(V4.Deserialize<List<AotBucket01>>(V4.Serialize(new List<AotBucket01> { new() { Value = 5 } }, Options), Options)).Value);
        Assert.Equal(6, Assert.Single(V4.Deserialize<AotBucket03[]>(V4.Serialize(new[] { new AotBucket03 { Value = 6 } }, Options), Options)).Value);
    }
}

public interface IAotBucket
{
    int Value { get; }
}

public enum AotBucketKind
{
    A,
    B,
}

[MessagePackObject]
public class AotBucketGraph
{
    [Key(0)] public List<AotBucket01>? Items { get; set; }
    [Key(1)] public AotBucket03[]? Array { get; set; }
    [Key(2)] public Dictionary<int, AotBucket04>? Map { get; set; }
    [Key(3)] public AotBucketKind? Kind { get; set; }
}

[MessagePackObject] public class AotBucket00 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket01 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket02 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket03 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket04 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket05 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket06 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket07 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket08 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket09 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket10 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket11 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket12 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket13 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket14 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket15 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket16 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket17 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket18 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket19 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket20 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket21 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket22 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket23 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket24 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket25 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket26 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket27 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket28 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket29 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket30 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket31 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket32 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket33 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket34 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket35 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket36 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket37 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket38 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket39 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket40 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket41 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket42 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket43 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket44 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket45 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket46 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket47 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket48 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket49 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket50 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket51 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket52 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket53 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket54 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket55 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket56 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket57 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket58 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket59 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket60 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket61 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket62 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket63 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket64 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket65 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket66 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket67 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket68 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket69 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket70 : IAotBucket { [Key(0)] public int Value { get; set; } }
[MessagePackObject] public class AotBucket71 : IAotBucket { [Key(0)] public int Value { get; set; } }

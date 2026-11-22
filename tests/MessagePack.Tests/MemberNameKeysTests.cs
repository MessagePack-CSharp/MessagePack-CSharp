extern alias V3;

using System.Reflection;
using System.Runtime.Serialization;
using V3Formatters = V3::MessagePack.Formatters;
using V3Resolvers = V3::MessagePack.Resolvers;

namespace MessagePack.Tests;

// WithMemberNameKeys / ReflectionFormatterFactory(forceMemberNameKeys: true): annotated types written as maps keyed by
// member name, with the contractless tier's member rules. For the common shapes below the output equals that of v3's
// internal DynamicObjectResolver.BuildFormatterHelper with forceStringKey: true; the oracle is that v3 code path (reached
// through reflection), composed ahead of v3's StandardResolver. Known differences from v3 (overrides, shadowing, AllowPrivate
// on base classes, read validation) are pinned against v4's own output instead.
// The test types carry v3's attributes; v4 matches contract attributes by name, so both runtimes read the same contract.
public partial class MemberNameKeysTests
{
    static readonly MessagePackSerializerOptions Forced = new(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithMemberNameKeys()]));

    static readonly V3::MessagePack.MessagePackSerializerOptions V3Forced = V3::MessagePack.MessagePackSerializerOptions.Standard
        .WithResolver(V3Resolvers.CompositeResolver.Create(V3ForceStringKeyResolver.Instance, V3Resolvers.StandardResolver.Instance));

    [V3::MessagePack.MessagePackObject]
    public class IntKeyed
    {
        [V3::MessagePack.Key(0)]
        public int Id { get; set; }

        [V3::MessagePack.Key(1)]
        public string? Name { get; set; }

        [V3::MessagePack.Key(2)]
        public List<int>? Scores;

        [V3::MessagePack.IgnoreMember]
        public string? Secret { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public class StringKeyed
    {
        [V3::MessagePack.Key("id")]
        public int Id { get; set; }

        [V3::MessagePack.Key("display_name")]
        public string? DisplayName { get; set; }
    }

    [DataContract]
    public class Contract
    {
        [DataMember(Order = 2, Name = "n")]
        public string? Name { get; set; }

        [DataMember(Order = 1)]
        public int Id { get; set; }

        // no [DataMember]: v3's forceStringKey serialized every public member regardless
        public bool Extra { get; set; }

        [IgnoreDataMember]
        public string? Hidden { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public record Point([property: V3::MessagePack.Key(0)] int X, [property: V3::MessagePack.Key(1)] int Y);

    [V3::MessagePack.MessagePackObject]
    public class Outer
    {
        [V3::MessagePack.Key(0)]
        public IntKeyed? Inner { get; set; }

        [V3::MessagePack.Key(1)]
        public Dictionary<string, Point[]>? Points { get; set; }
    }

    [V3::MessagePack.MessagePackObject(AllowPrivate = true)]
    public partial class WithPrivate
    {
        [V3::MessagePack.Key(0)]
        public int Visible { get; set; }

        [V3::MessagePack.Key(1)]
        private int hidden;

        [V3::MessagePack.IgnoreMember]
        public int Hidden
        {
            get => hidden;
            set => hidden = value;
        }
    }

    [V3::MessagePack.Union(0, typeof(Circle))]
    [V3::MessagePack.Union(1, typeof(Square))]
    public interface IShape
    {
    }

    [V3::MessagePack.MessagePackObject]
    public class Circle : IShape
    {
        [V3::MessagePack.Key(0)]
        public double Radius { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public class Square : IShape
    {
        [V3::MessagePack.Key(0)]
        public double Side { get; set; }
    }

    [Fact]
    public void IntKeys_WrittenAsMemberNameMap()
    {
        var value = new IntKeyed { Id = 7, Name = "x", Scores = [1, 2], Secret = "s" };
        var payload = MessagePackSerializer.Serialize(value, Forced);

        var map = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(payload)!;
        Assert.Equal(["Id", "Name", "Scores"], map.Keys);

        var result = MessagePackSerializer.Deserialize<IntKeyed>(payload, Forced)!;
        Assert.Equal(7, result.Id);
        Assert.Equal("x", result.Name);
        Assert.Equal([1, 2], result.Scores);
        Assert.Null(result.Secret);
        Assert.Equal(V3Bytes(value), payload);
    }

    [Fact]
    public void StringKeys_ReplacedByMemberNames()
    {
        var value = new StringKeyed { Id = 3, DisplayName = "d" };
        var payload = MessagePackSerializer.Serialize(value, Forced);

        Assert.Equal(["Id", "DisplayName"], MessagePackSerializer.Deserialize<Dictionary<string, object?>>(payload)!.Keys);
        Assert.Equal("d", MessagePackSerializer.Deserialize<StringKeyed>(payload, Forced)!.DisplayName);
        Assert.Equal(V3Bytes(value), payload);
    }

    [Fact]
    public void DataContract_AllPublicMembers_OrderedByDataMemberOrder()
    {
        var value = new Contract { Id = 1, Name = "n", Extra = true, Hidden = "h" };
        var payload = MessagePackSerializer.Serialize(value, Forced);

        // ordered by [DataMember(Order)], members without [DataMember] last; Name is not used as the key
        Assert.Equal(["Id", "Name", "Extra"], MessagePackSerializer.Deserialize<Dictionary<string, object?>>(payload)!.Keys);

        var result = MessagePackSerializer.Deserialize<Contract>(payload, Forced)!;
        Assert.Equal(1, result.Id);
        Assert.Equal("n", result.Name);
        Assert.True(result.Extra);
        Assert.Null(result.Hidden);
        Assert.Equal(V3Bytes(value), payload);
    }

    [Fact]
    public void Record_BoundThroughConstructor()
    {
        var value = new Point(4, -5);
        var payload = MessagePackSerializer.Serialize(value, Forced);

        Assert.Equal(["X", "Y"], MessagePackSerializer.Deserialize<Dictionary<string, object?>>(payload)!.Keys);
        Assert.Equal(value, MessagePackSerializer.Deserialize<Point>(payload, Forced));
        Assert.Equal(V3Bytes(value), payload);
    }

    [Fact]
    public void NestedInCollectionsAndObjects()
    {
        var value = new Outer
        {
            Inner = new IntKeyed { Id = 1, Name = "a" },
            Points = new() { ["p"] = [new Point(1, 2), new Point(3, 4)] },
        };
        var payload = MessagePackSerializer.Serialize(value, Forced);

        var result = MessagePackSerializer.Deserialize<Outer>(payload, Forced)!;
        Assert.Equal("a", result.Inner!.Name);
        Assert.Equal([new Point(1, 2), new Point(3, 4)], result.Points!["p"]);
        Assert.Equal(V3Bytes(value), payload);

        // a top-level collection resolves its elements through the chain too
        var list = new List<Point> { new(5, 6) };
        Assert.Equal(V3Bytes(list), MessagePackSerializer.Serialize(list, Forced));
    }

    [Fact]
    public void AllowPrivate_FromAttribute()
    {
        var value = new WithPrivate { Visible = 1, Hidden = 2 };
        var payload = MessagePackSerializer.Serialize(value, Forced);

        Assert.Contains("hidden", MessagePackSerializer.Deserialize<Dictionary<string, object?>>(payload)!.Keys);
        Assert.Equal(2, MessagePackSerializer.Deserialize<WithPrivate>(payload, Forced)!.Hidden);
        Assert.Equal(V3Bytes(value), payload);
    }

    [Fact]
    public void UnionCases_Forced_EnvelopeUnchanged()
    {
        IShape[] value = [new Circle { Radius = 1.5 }, new Square { Side = 2 }];
        var payload = MessagePackSerializer.Serialize(value, Forced);

        var result = MessagePackSerializer.Deserialize<IShape[]>(payload, Forced)!;
        Assert.Equal(1.5, Assert.IsType<Circle>(result[0]).Radius);
        Assert.Equal(2, Assert.IsType<Square>(result[1]).Side);
        Assert.Equal(V3Bytes(value), payload);
    }

    [Fact]
    public void DefaultChain_Unchanged()
    {
        // without the option, [Key(int)] stays an array
        var payload = MessagePackSerializer.Serialize(new IntKeyed { Id = 7, Name = "x" });
        Assert.Equal(0x93, payload[0]);
    }

    [Fact]
    public void ConcreteUnionRoots_KeepEnvelope_CasesForced()
    {
        // C# unions and IUnion structs carry [MessagePackObject] but are union roots: the [tag, case] envelope stays,
        // and the case, resolved through the chain, is a member-name map
        UPet pet = new UCat { Name = "tama" };
        var payload = MessagePackSerializer.Serialize(pet, Forced);
        Assert.Equal(Convert.FromHexString("920081A44E616D65A474616D61"), payload); // [0, {"Name":"tama"}]
        Assert.Equal("tama", Assert.IsType<UCat>(MessagePackSerializer.Deserialize<UPet>(payload, Forced).Value).Name);
        Assert.Equal([0xC0], MessagePackSerializer.Serialize(default(UPet), Forced));

        var custom = MessagePackSerializer.Serialize(new UCustomUnion(new UDog { Bark = 7 }), Forced);
        Assert.Equal(Convert.FromHexString("920181A44261726B07"), custom); // [1, {"Bark":7}]
        Assert.Equal(7, Assert.IsType<UDog>(MessagePackSerializer.Deserialize<UCustomUnion>(custom, Forced).Value).Bark);
    }

    [V3::MessagePack.MessagePackObject]
    public struct PositionalStruct
    {
        public PositionalStruct(int a, int b)
        {
            X = a;
            Y = b;
        }

        [V3::MessagePack.Key(0)]
        public int X { get; }

        [V3::MessagePack.Key(1)]
        public int Y { get; }
    }

    [Fact]
    public void ConstructorBindingByPositionOnly_Throws()
    {
        // valid in array mode, but no constructor binds by name: writing {} would lose X and Y silently (v3 threw too)
        var ex = Assert.ThrowsAny<Exception>(() => MessagePackSerializer.Serialize(new PositionalStruct(1, 2), Forced));
        Assert.Contains("no constructor of this type does", ex.ToString());
        Assert.Contains("can't find matched constructor", Assert.ThrowsAny<Exception>(() => V3Bytes(new PositionalStruct(1, 2))).ToString());
    }

    [DataContract]
    public class FactoryOnly
    {
        private FactoryOnly(int a) => X = a;

        [DataMember]
        public int X { get; }

        public static FactoryOnly Create(int a) => new(a);
    }

    [Fact]
    public void NoUsableConstructor_Throws()
    {
        var ex = Assert.ThrowsAny<Exception>(() => MessagePackSerializer.Serialize(FactoryOnly.Create(1), Forced));
        Assert.Contains("with member-name keys", ex.ToString());
    }

    [Fact]
    public void TypeLevelFormatter_KeepsItsFormatter()
    {
        // the attribute assigns the type's wire to its own formatter, which has no member-name form to force
        var value = new TypeLevelWinsPoco { Value = 1 };
        Assert.Equal(MessagePackSerializer.Serialize(value), MessagePackSerializer.Serialize(value, Forced));
    }

    [Fact]
    public void CircularReferenceType_Throws()
    {
        // only the generated formatter tracks references; silently writing its int-keyed form would break the mode's promise
        var ex = Assert.Throws<NotSupportedException>(() => MessagePackSerializer.Serialize(new CircularNode { Value = 1 }, Forced));
        Assert.Contains("WithMemberNameKeys", ex.Message);
    }

    public class OverrideBase
    {
        public virtual int X { get; set; } = 1;

        public int Y { get; set; } = 2;
    }

    [DataContract]
    public class OverrideDerived : OverrideBase
    {
        public override int X { get; set; } = 3;
    }

    public class PrivateBase
    {
        private int hidden = 7;

        [V3::MessagePack.IgnoreMember]
        public int Hidden => hidden;
    }

    [V3::MessagePack.MessagePackObject(AllowPrivate = true)]
    public partial class PrivateDerived : PrivateBase
    {
        [V3::MessagePack.Key(0)]
        public int Visible = 1;
    }

    [Fact]
    public void KnownDifferencesFromV3_FollowContractlessRules()
    {
        // an override sorts at its base declaration (v3: {"Y":2,"X":3})
        Assert.Equal("{\"X\":3,\"Y\":2}", MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(new OverrideDerived(), Forced)));

        // AllowPrivate reaches private members of base classes (v3: {"Visible":1})
        Assert.Equal("{\"hidden\":7,\"Visible\":1}", MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(new PrivateDerived(), Forced)));
    }

    static byte[] V3Bytes<T>(T value) => V3::MessagePack.MessagePackSerializer.Serialize(value, V3Forced);

    // What v3 callers composed to get forceStringKey: v3's own dynamic object builder with forceStringKey: true,
    // contractless: false, allowPrivate: false (the attribute's AllowPrivate is still read inside), for annotated types.

    sealed class V3ForceStringKeyResolver : V3::MessagePack.IFormatterResolver
    {
        public static readonly V3ForceStringKeyResolver Instance = new();

        static readonly MethodInfo BuildFormatterHelper = typeof(V3Resolvers.DynamicObjectResolver)
            .GetMethod("BuildFormatterHelper", BindingFlags.NonPublic | BindingFlags.Static)!;

        static readonly object DynamicAssemblyFactory = typeof(V3Resolvers.DynamicObjectResolver)
            .GetField("DynamicAssemblyFactory", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

        public V3Formatters.IMessagePackFormatter<T>? GetFormatter<T>() => Cache<T>.Formatter;

        static class Cache<T>
        {
            public static readonly V3Formatters.IMessagePackFormatter<T>? Formatter = IsAnnotated(typeof(T))
                ? (V3Formatters.IMessagePackFormatter<T>?)BuildFormatterHelper.MakeGenericMethod(typeof(T))
                    .Invoke(null, [Instance, DynamicAssemblyFactory, true, false, false])
                : null;
        }

        static bool IsAnnotated(Type type) =>
            !type.IsInterface && !type.IsAbstract
            && (type.IsDefined(typeof(V3::MessagePack.MessagePackObjectAttribute), inherit: true)
                || type.IsDefined(typeof(DataContractAttribute), inherit: false));
    }
}

using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// a keyed base field hidden by a [NonSerialized] derived field: the hider is not a candidate, but `value.X` binds it,
// so the base storage must be reached through its declarer (or the derived value is what gets written and the base
// one lost, as happened: base=1/derived=2 serialized as [2])
public class HiddenBase
{
    [Key(0)] public int X;
}

[MessagePackObject]
public class HiddenDerived : HiddenBase
{
    [NonSerialized] public new int X;

    [Key(1)] public int Y { get; set; }
}

// (the self-expanding shape `class C<T> : List<C<T[]>>` that the harvest and the analyzer now bound at depth 8 cannot
// be exercised here: the CLR refuses to load such a type at all, "recursive generic definition", so the bound is a
// generator-only concern verified by the generator terminating on that source)

// reflection tier: a constructor parameter binds the most derived member of its name, as the generator does
public class RefBindingBase
{
    public int Value { get; set; }
}

public class RefBindingDerived : RefBindingBase
{
    public RefBindingDerived(int value)
    {
        Value = value;
    }

    public new int Value { get; }
}

// reflection tier, string keys: the key pass must not bind the hidden base "Value" either
public class KeyedRefBindingBase
{
    public int Value { get; set; }
}

[MessagePackObject(true, SuppressSourceGeneration = true)]
public class KeyedRefBindingDerived : KeyedRefBindingBase
{
    public KeyedRefBindingDerived(int value)
    {
        Value = value;
    }

    public new int Value { get; }
}

// a static `new` member hides the base declaration just the same (`value.X` would be CS0176)
public class StaticHiddenBase
{
    [Key(0)] public int X { get; set; }
}

[MessagePackObject]
public class StaticHiddenDerived : StaticHiddenBase
{
    public static new int X => 99;

    [Key(1)] public int Y { get; set; }
}

// the harvest depth bound must not cut ordinary deep BCL nesting: ten Lists down to a user type, every level
// registered for the AOT chain
[MessagePackObject]
public class DeepLeaf
{
    [Key(0)] public int Id { get; set; }
}

[MessagePackSerializable<List<List<List<List<List<List<List<List<List<List<DeepLeaf>>>>>>>>>>>]
public partial class DeepListFactory
{
}

// reflection tier: an ignored MessagePackUnknownMembers member is ignored, not refused
public class IgnoredPacketContractless
{
    public int Id { get; set; }

    [IgnoreMember] public MessagePackUnknownMembers? Packet { get; set; }
}

// reflection tier: [MessagePackFormatter] arguments bind a constructor with trailing optional parameters
public sealed partial class ScaledIntFormatter<TWriteBuffer, TReadBuffer>(int scale, bool negate) : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value) => buffer.WriteInt32((negate ? -value : value) * scale);

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value)
    {
        value = buffer.ReadInt32() / scale;
        value = negate ? -value : value;
    }
}

// (`long scale`: the attribute argument is the int literal 10, converted as a call site would convert it)
public sealed partial class ScaledIntFormatterFactory(long scale, bool negate = false) : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        => type == typeof(int) ? new ScaledIntFormatter<TWriteBuffer, TReadBuffer>((int)scale, negate) : null;
}

[MessagePackObject(SuppressSourceGeneration = true)]
public class OptionalArgumentFormatterModel
{
    [Key(0)]
    [MessagePackFormatter(typeof(ScaledIntFormatterFactory), 10)]
    public int Value { get; set; }
}

public class ReviewRound4RegressionTests
{
    static readonly MessagePackSerializerOptions Contractless = new(new MessagePackFormatterResolver(MessagePackFormatterFactory.Default.WithContractless()));

#pragma warning disable CS0618 // LoadAnyType: trusted test data
    static readonly MessagePackSerializerOptions Typeless = new(new MessagePackFormatterResolver(MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())));
#pragma warning restore CS0618

    [Fact]
    public void BaseFieldHiddenByNonSerializedDerivedField_KeepsItsOwnStorage()
    {
        var value = new HiddenDerived { X = 2, Y = 3 };
        ((HiddenBase)value).X = 1;
        var bytes = V4.Serialize(value);
        Assert.Equal(new byte[] { 0x92, 0x01, 0x03 }, bytes); // [1, 3]: the base field, never the hider
        var back = V4.Deserialize<HiddenDerived>(bytes)!;
        Assert.Equal(1, ((HiddenBase)back).X);
        Assert.Equal(0, back.X);
        Assert.Equal(3, back.Y);
    }

    [Fact]
    public void Reflection_ConstructorBindsTheMostDerivedMember()
    {
        var back = V4.Deserialize<RefBindingDerived>(V4.Serialize(new RefBindingDerived(42), Contractless), Contractless)!;
        Assert.Equal(42, back.Value);
    }

    [Fact]
    public void Typeless_BareObject_RoundtripsAsV3Wrote()
    {
        var bytes = V4.Serialize<object>(new object(), Typeless);
        // v3's wire: ext 100 holding the type name str and nothing else
        var name = System.Text.Encoding.UTF8.GetBytes(typeof(object).AssemblyQualifiedName!);
        Assert.Equal(0xC7, bytes[0]);
        Assert.Equal(name.Length + 2, bytes[1]); // str8 header + name, no body
        Assert.Equal(100, (sbyte)bytes[2]);
        Assert.Equal(typeof(object), V4.Deserialize<object>(bytes, Typeless)!.GetType());

        var array = V4.Deserialize<object[]>(V4.Serialize(new object[] { new object(), 1 }, Typeless), Typeless)!;
        Assert.Equal(typeof(object), array[0].GetType());
        Assert.Equal(1, array[1]);
    }

    [Fact]
    public void AllowedTypes_MatchAcrossRuntimes()
    {
        var loader = TypelessTypeLoader.AllowedTypes(typeof(Guid), typeof(List<int>));
        Assert.Same(typeof(Guid), loader.LoadType("System.Guid, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
        Assert.Same(typeof(Guid), loader.LoadType("System.Guid, System.Runtime, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
        Assert.Same(typeof(List<int>), loader.LoadType("System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
        Assert.Null(loader.LoadType("System.Diagnostics.Process, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")); // not registered
        Assert.Null(loader.LoadType("System.Guid2, mscorlib"));
    }

    [Fact]
    public void Lookup_NullKeyGroup_Roundtrips()
    {
        var lookup = new[] { 1, 2, 3 }.ToLookup(x => x == 3 ? "three" : (string)null!);
        var back = V4.Deserialize<ILookup<string, int>>(V4.Serialize(lookup))!;
        Assert.Equal(2, back.Count);
        Assert.Equal([1, 2], back[null!]);
        Assert.Equal([3], back["three"]);
        Assert.True(back.Contains(null!));
        Assert.Equal(lookup.Select(static g => g.Key), back.Select(static g => g.Key));
    }

    [Fact]
    public void Reflection_FormatterAttributeBindsOptionalConstructorParameters()
    {
        var bytes = V4.Serialize(new OptionalArgumentFormatterModel { Value = 4 });
        Assert.Equal(new byte[] { 0x91, 0x28 }, bytes); // [40]: scaled by 10, not negated (the default)
        Assert.Equal(4, V4.Deserialize<OptionalArgumentFormatterModel>(bytes)!.Value);
    }

    [Fact]
    public void Reflection_StringKeyConstructorBindsTheMostDerivedMember()
    {
        var back = V4.Deserialize<KeyedRefBindingDerived>(V4.Serialize(new KeyedRefBindingDerived(42)))!;
        Assert.Equal(42, back.Value);
    }

    [Fact]
    public void BaseMemberHiddenByStaticDerivedMember_IsReachedThroughItsDeclarer()
    {
        var value = new StaticHiddenDerived { Y = 2 };
        ((StaticHiddenBase)value).X = 1;
        var back = V4.Deserialize<StaticHiddenDerived>(V4.Serialize(value))!;
        Assert.Equal(1, ((StaticHiddenBase)back).X);
        Assert.Equal(2, back.Y);
    }

    [Fact]
    public void DeepBclNesting_IsHarvestedToTheBottom()
    {
        var resolver = new MessagePackFormatterResolver(MessagePackFormatterFactory.DefaultAot);
        var formatter = resolver.GetFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, List<DeepLeaf>>();
        Assert.DoesNotContain("Missing", formatter.GetType().Name);

        var aot = MessagePackSerializerOptions.DefaultAot;
        static List<T> L<T>(T item) => [item];
        var value = L(L(L(L(L(L(L(L(L(L(new DeepLeaf { Id = 7 }))))))))));
        var back = V4.Deserialize<List<List<List<List<List<List<List<List<List<List<DeepLeaf>>>>>>>>>>>(V4.Serialize(value, aot), aot)!;
        Assert.Equal(7, back[0][0][0][0][0][0][0][0][0][0].Id);
    }

    [Fact]
    public void Reflection_IgnoredUnknownMembersPacket_IsIgnored()
    {
        var back = V4.Deserialize<IgnoredPacketContractless>(V4.Serialize(new IgnoredPacketContractless { Id = 5 }, Contractless), Contractless)!;
        Assert.Equal(5, back.Id);
        Assert.Null(back.Packet);
    }
}

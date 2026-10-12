using System.Collections;
using MessagePack.Formatters;
using SerializerFoundation;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;

namespace MessagePack.Tests;

// The reflection tier against the generator (and v3), from the preview1 review's minor findings (F23, F24, F25, F26,
// F27, F28). The chains below leave the generated registry out: they are what an assembly the generator never saw (a
// pre-built v3 DLL, SuppressSourceGeneration) is served by.
public partial class ReflectionTierParityTests
{
    static V4Options Reflection(bool allowPrivate = false) => new(new MessagePackFormatterResolver(MessagePackFormatterFactory.Combine(
        BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance, new ReflectionFormatterFactory(annotatedOnly: true, allowPrivate))));

    // ---- F23: a readonly [Key] field is written only under AllowPrivate (the generator skips it with MsgPack007; v3 skipped it) ----

    [MessagePackObject]
    public class ReadOnlyFieldHolder
    {
        [Key(0)]
        public readonly int Id;

        [Key(1)]
        public int Other;
    }

    [Fact]
    public void ReadOnlyKeyField_SkippedWithoutAllowPrivate_WrittenWithIt()
    {
        byte[] payload = [0x92, 0x07, 0x08]; // [Id = 7, Other = 8]

        var plain = V4.Deserialize<ReadOnlyFieldHolder>(payload, Reflection())!;
        Assert.Equal(0, plain.Id); // skipped, as the generator and v3 do
        Assert.Equal(8, plain.Other);
        Assert.Equal(0, V4.Deserialize<ReadOnlyFieldHolder>(payload)!.Id); // the generated formatter agrees

        var widened = V4.Deserialize<ReadOnlyFieldHolder>(payload, Reflection(allowPrivate: true))!;
        Assert.Equal(7, widened.Id); // v3's AllowPrivate resolvers wrote initonly fields
        Assert.Equal(8, widened.Other);
    }

    // ---- F24: a union member's existing case instance is populated in place, as the generated union formatter does ----

    [MessagePackObject]
    [UnionTag(typeof(TaggedCircle), 0)]
    public interface ITaggedShape
    {
    }

    [MessagePackObject]
    public class TaggedCircle : ITaggedShape
    {
        [Key(0)]
        public int Radius { get; set; }

        [Key(1)]
        public List<string> Tags { get; set; } = new();
    }

    [MessagePackObject]
    public class TaggedHolder
    {
        [Key(0)]
        public ITaggedShape? Shape { get; set; }
    }

    [Fact]
    public void UnionMember_PopulatesTheExistingCaseInstance()
    {
        var payload = V4.Serialize(new TaggedHolder { Shape = new TaggedCircle { Radius = 5, Tags = { "x" } } });
        foreach (var options in new[] { Reflection(), V4Options.Default })
        {
            var circle = new TaggedCircle { Radius = 1, Tags = { "old" } };
            var holder = new TaggedHolder { Shape = circle };
            V4.Deserialize(payload, ref holder, options);
            Assert.Same(circle, holder!.Shape);
            Assert.Equal(5, circle.Radius);
            Assert.Equal(["x"], circle.Tags);
        }
    }

    // ---- F25: a member formatter without `allows ref struct` reroutes the graph to the Compatible buffers, no ArgumentException ----

    // the downlevel generic shape, as a netstandard-compiled formatter library presents it on net9+
    public sealed class DownlevelInt32Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value) => buffer.WriteInt32(value + 1000); // marks the path

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value) => value = buffer.ReadInt32() - 1000;
    }

    public class DownlevelFormatted
    {
        [MessagePackFormatter(typeof(DownlevelInt32Formatter<,>))]
        public int X { get; set; }
    }

    [Fact]
    public void DownlevelMemberFormatter_ReroutesToTheCompatibleBuffers()
    {
        var resolver = new MessagePackFormatterResolver(MessagePackFormatterFactory.Default.WithContractless());
        var fallback = new List<Type>();
        resolver.CompatibilityFallback += fallback.Add;
        var options = new V4Options(resolver);

        var bytes = V4.Serialize(new DownlevelFormatted { X = 5 }, options); // was ArgumentException out of MakeGenericType
        Assert.Contains(typeof(DownlevelFormatted), fallback);
        Assert.Equal(5, V4.Deserialize<DownlevelFormatted>(bytes, options)!.X);
        Assert.Equal(1005, V4.Deserialize<Dictionary<string, int>>(bytes, options)!["X"]); // the member formatter did run
    }

    // ---- F26: a type-level [MessagePackFormatter] on a base class does not claim a derived [MessagePackObject] type ----

#pragma warning disable MsgPack102 // the Type-based member alone, as a downlevel-compiled factory presents it
    public sealed class DeclinesEverythingFactory : MessagePackFormatterFactory
    {
        public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType) => null;
    }
#pragma warning restore MsgPack102

    [MessagePackFormatter(typeof(DeclinesEverythingFactory))]
    public class FormattedBase
    {
    }

    [MessagePackObject]
    public class DerivedFromFormatted : FormattedBase
    {
        [Key(0)]
        public int X { get; set; }
    }

    [Fact]
    public void InheritedTypeLevelFormatterAttribute_DoesNotClaimTheDerivedObject()
    {
        var options = Reflection();
        var bytes = V4.Serialize(new DerivedFromFormatted { X = 5 }, options); // was "TypeLevelNotCompiled" for the base's attribute
        Assert.Equal(new byte[] { 0x91, 0x05 }, bytes);
        Assert.Equal(5, V4.Deserialize<DerivedFromFormatted>(bytes, options)!.X);
        Assert.Equal(bytes, V4.Serialize(new DerivedFromFormatted { X = 5 })); // the generated formatter's contract
    }

    // ---- F27: a [MessagePackObject] that is also a collection shape is served as the object it declares ----

    [MessagePackObject]
    public class KeyedBag : ICollection<int>
    {
        [Key(0)]
        public string? Name { get; set; }

        [Key(1)]
        public List<int> Items { get; set; } = new();

        [IgnoreMember]
        public int Count => Items.Count;

        [IgnoreMember]
        public bool IsReadOnly => false;

        public void Add(int item) => Items.Add(item);

        public void Clear() => Items.Clear();

        public bool Contains(int item) => Items.Contains(item);

        public void CopyTo(int[] array, int arrayIndex) => Items.CopyTo(array, arrayIndex);

        public bool Remove(int item) => Items.Remove(item);

        public IEnumerator<int> GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void AnnotatedCollectionShape_IsServedAsTheObject_NotByTheCollectionCatchAll()
    {
        var bag = new KeyedBag { Name = "n", Items = { 1, 2 } };
        var options = Reflection();

        var bytes = V4.Serialize(bag, options);
        Assert.Equal(0x92, bytes[0]); // [Name, Items], not the catch-all's [1, 2] that dropped Name without a word
        var back = V4.Deserialize<KeyedBag>(bytes, options)!;
        Assert.Equal("n", back.Name);
        Assert.Equal([1, 2], back.Items);
        Assert.Equal(bytes, V4.Serialize(bag)); // the generated formatter's contract
    }

    // ---- F28: an explicit interface implementation is no member of an annotated type's contract (the generator skips it) ----

    public interface IHasSecret
    {
        int Secret { get; set; }
    }

    [MessagePackObject(true, AllowPrivate = true)]
    public partial class ExplicitlyImplemented : IHasSecret
    {
        public int Visible { get; set; }

        int IHasSecret.Secret { get; set; }
    }

    [Fact]
    public void ExplicitInterfaceImplementation_IsNotSerialized_UnderAllowPrivate()
    {
        var value = new ExplicitlyImplemented { Visible = 1 };
        ((IHasSecret)value).Secret = 2;

        var bytes = V4.Serialize(value, Reflection(allowPrivate: true));
        Assert.DoesNotContain("Secret", V4.ConvertToJson(bytes), StringComparison.Ordinal);
        Assert.Equal(V4.Serialize(value), bytes); // the generated formatter's contract
    }
}

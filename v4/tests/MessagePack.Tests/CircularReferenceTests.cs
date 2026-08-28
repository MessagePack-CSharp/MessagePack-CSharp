using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// [MessagePackObject(AllowCircularReferences = true)] end-to-end: the generated formatter
// wraps every definition in the [id, body] envelope, repeats become ext-97 back-references,
// and register-before-populate resolves cycles into in-progress instances. No v3 oracle
// here on purpose: the envelope wire is v4-specific (only annotated types pay it).
public class CircularReferenceTests
{
    const byte FixExt1 = 0xd4;
    const byte ExtCircular = 97;

    [Fact]
    public void NoSharing_WireIsEnvelopePlusBody()
    {
        var bytes = V4.Serialize(new CircularNode { Value = 1 });

        // [id 0, [1, nil]]
        Assert.Equal(new byte[] { 0x92, 0x00, 0x92, 0x01, 0xc0 }, bytes);

        var back = V4.Deserialize<CircularNode>(bytes)!;
        Assert.Equal(1, back.Value);
        Assert.Null(back.Next);
    }

    [Fact]
    public void SelfCycle_RoundtripsToSelf()
    {
        var node = new CircularNode { Value = 42 };
        node.Next = node;

        var bytes = V4.Serialize(node);
        // [id 0, [42, backref(0)]]
        Assert.Equal(new byte[] { 0x92, 0x00, 0x92, 0x2a, FixExt1, ExtCircular, 0x00 }, bytes);

        var back = V4.Deserialize<CircularNode>(bytes)!;
        Assert.Equal(42, back.Value);
        Assert.Same(back, back.Next);
    }

    [Fact]
    public void MutualCycle_TwoNodes()
    {
        var a = new CircularNode { Value = 1 };
        var b = new CircularNode { Value = 2 };
        a.Next = b;
        b.Next = a;

        var back = V4.Deserialize<CircularNode>(V4.Serialize(a))!;
        Assert.Equal(1, back.Value);
        Assert.Equal(2, back.Next!.Value);
        Assert.Same(back, back.Next.Next);
    }

    [Fact]
    public void CrossTypeCycle_IdsAreOperationGlobal()
    {
        var owner = new CircularOwner { Name = "o" };
        var pet = new CircularPet { Name = "p", Owner = owner };
        owner.Pet = pet;

        var back = V4.Deserialize<CircularOwner>(V4.Serialize(owner))!;
        Assert.Equal("o", back.Name);
        Assert.Equal("p", back.Pet!.Name);
        Assert.Same(back, back.Pet.Owner);
    }

    [Fact]
    public void DagSharing_ThroughUntrackedHolder()
    {
        var shared = new CircularNode { Value = 7 };
        var holder = new CircularSharedHolder { First = shared, Second = shared };

        var bytes = V4.Serialize(holder);
        // holder itself is untracked: [ [id 0, [7, nil]], backref(0) ]
        Assert.Equal(new byte[] { 0x92, 0x92, 0x00, 0x92, 0x07, 0xc0, FixExt1, ExtCircular, 0x00 }, bytes);

        var back = V4.Deserialize<CircularSharedHolder>(bytes)!;
        Assert.Equal(7, back.First!.Value);
        Assert.Same(back.First, back.Second);
    }

    [Fact]
    public void SharingInsideCollections()
    {
        var shared = new CircularNode { Value = 5 };
        var list = new List<CircularNode?> { shared, shared, null, shared };

        var back = V4.Deserialize<List<CircularNode?>>(V4.Serialize(list))!;
        Assert.Equal(4, back.Count);
        Assert.Equal(5, back[0]!.Value);
        Assert.Same(back[0], back[1]);
        Assert.Null(back[2]);
        Assert.Same(back[0], back[3]);
    }

    [Fact]
    public void ParentChildTree_ChildrenPointBack()
    {
        var root = new CircularTree { Name = "root", Children = [] };
        var left = new CircularTree { Name = "left", Parent = root };
        var right = new CircularTree { Name = "right", Parent = root };
        root.Children.Add(left);
        root.Children.Add(right);

        var back = V4.Deserialize<CircularTree>(V4.Serialize(root))!;
        Assert.Equal("root", back.Name);
        Assert.Equal(2, back.Children!.Count);
        Assert.Same(back, back.Children[0].Parent);
        Assert.Same(back, back.Children[1].Parent);
    }

    [Fact]
    public void StringKeyMapMode_SelfCycle()
    {
        var node = new CircularMapNode { Name = "m" };
        node.Next = node;

        var back = V4.Deserialize<CircularMapNode>(V4.Serialize(node))!;
        Assert.Equal("m", back.Name);
        Assert.Same(back, back.Next);
    }

    [Fact]
    public void GenericCircularType_SelfCycle()
    {
        var node = new CircularGenericNode<string> { Item = "x" };
        node.Next = node;

        var back = V4.Deserialize<CircularGenericNode<string>>(V4.Serialize(node))!;
        Assert.Equal("x", back.Item);
        Assert.Same(back, back.Next);
    }

    [Fact]
    public void Null_StaysNil()
    {
        var bytes = V4.Serialize<CircularNode?>(null);
        Assert.Equal(new byte[] { 0xc0 }, bytes);
        Assert.Null(V4.Deserialize<CircularNode>(bytes));
    }

    [Fact]
    public void IdsResetPerOperation()
    {
        var node = new CircularNode { Value = 3 };
        node.Next = node;

        // a second serialize of the same instance starts a fresh table: identical bytes
        Assert.Equal(V4.Serialize(node), V4.Serialize(node));
    }

    [Fact]
    public void SkippedDefinition_WithoutBackReference_StaysVersionTolerant()
    {
        var extra = new CircularNode { Value = 1 };
        var fresh = new CircularNode { Value = 2 };
        var bytes = V4.Serialize(new CircularSkewV2 { Id = 10, Extra = extra, Shared = fresh });

        // the V1 shape skips key 1 (Extra's definition); Shared carries its own definition
        var back = V4.Deserialize<CircularSkewV1>(bytes)!;
        Assert.Equal(10, back.Id);
        Assert.Equal(2, back.Shared!.Value);
    }

    [Fact]
    public void SkippedDefinition_WithBackReference_FailsLoud()
    {
        var extra = new CircularNode { Value = 1 };
        var bytes = V4.Serialize(new CircularSkewV2 { Id = 10, Extra = extra, Shared = extra });

        // Shared back-references the definition that only existed inside the skipped member
        var exception = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<CircularSkewV1>(bytes));
        Assert.Contains("no definition", exception.Message);
    }

    [Fact]
    public void UnknownBackReferenceId_Throws()
    {
        var exception = Assert.Throws<MessagePackSerializationException>(
            () => V4.Deserialize<CircularNode>(new byte[] { FixExt1, ExtCircular, 0x05 }));
        Assert.Contains("no definition", exception.Message);
    }

    [Fact]
    public void EnvelopeWithWrongElementCount_Throws()
    {
        var exception = Assert.Throws<MessagePackSerializationException>(
            () => V4.Deserialize<CircularNode>(new byte[] { 0x93, 0x00, 0x00, 0x00 }));
        Assert.Contains("envelope", exception.Message);
    }

    [Fact]
    public void BackReferenceWithInvalidWidth_Throws()
    {
        // fixext8 with the circular-reference type code: matched code, malformed width
        var exception = Assert.Throws<MessagePackSerializationException>(
            () => V4.Deserialize<CircularNode>(new byte[] { 0xd7, ExtCircular, 0, 0, 0, 0, 0, 0, 0, 0 }));
        Assert.Contains("1, 2 or 4", exception.Message);
    }

    [Fact]
    public void DuplicateDefinitionId_Throws()
    {
        // [id 0, [1, [id 0, [2, nil]]]] — the nested definition reuses id 0
        var exception = Assert.Throws<MessagePackSerializationException>(
            () => V4.Deserialize<CircularNode>(new byte[] { 0x92, 0x00, 0x92, 0x01, 0x92, 0x00, 0x92, 0x02, 0xc0 }));
        Assert.Contains("more than once", exception.Message);
    }

    [Fact]
    public void BackReferenceToWrongType_Throws()
    {
        // owner definition id 0 whose Pet member back-references id 0 (the owner itself)
        var exception = Assert.Throws<MessagePackSerializationException>(
            () => V4.Deserialize<CircularOwner>(new byte[] { 0x92, 0x00, 0x92, 0xc0, FixExt1, ExtCircular, 0x00 }));
        Assert.Contains("was expected", exception.Message);
    }

    [Fact]
    public void PolymorphicCycle_ThroughUnionBaseTypedMembers()
    {
        // the composite pattern: Parent is typed as the ABSTRACT union root. The union
        // formatter dispatches to the case formatter, which owns the tracking — so the
        // flag lives on the concrete case types and identity flows through base-typed
        // references (the union ROOT itself neither needs nor accepts the flag)
        var branch = new CircularBranch { Name = "assembly" };
        var leaf = new CircularLeafPart { Name = "bolt", Parent = branch };
        branch.Parts = [leaf, new CircularLeafPart { Name = "nut", Parent = branch }];

        var back = (CircularBranch)V4.Deserialize<CircularPart>(V4.Serialize<CircularPart>(branch))!;
        Assert.Equal("assembly", back.Name);
        Assert.Equal(2, back.Parts!.Count);
        Assert.Same(back, ((CircularLeafPart)back.Parts[0]).Parent);
        Assert.Same(back, ((CircularLeafPart)back.Parts[1]).Parent);
    }

    [Fact]
    public void ReflectionTier_RefusesCircularReferenceTypes()
    {
        // the envelope wire lives only in the source-generated formatter; the reflection
        // tier throws instead of silently producing an untracked (different) wire format
        var reflectionOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

        Assert.Throws<NotSupportedException>(() => V4.Serialize(new CircularNode { Value = 1 }, reflectionOptions));
    }

    [Fact]
    public void WideIds_RoundTripAcrossWidths()
    {
        // enough distinct nodes to push ids past the 1-byte width, all shared once so
        // every id is also read back through a back-reference
        var nodes = new List<CircularNode?>();
        for (int i = 0; i < 300; i++)
        {
            nodes.Add(new CircularNode { Value = i });
        }
        for (int i = 0; i < 300; i++)
        {
            nodes.Add(nodes[i]);
        }

        var back = V4.Deserialize<List<CircularNode?>>(V4.Serialize(nodes))!;
        Assert.Equal(600, back.Count);
        for (int i = 0; i < 300; i++)
        {
            Assert.Equal(i, back[i]!.Value);
            Assert.Same(back[i], back[i + 300]);
        }
    }
}

[MessagePackObject(AllowCircularReferences = true)]
public class CircularNode
{
    [Key(0)] public int Value { get; set; }
    [Key(1)] public CircularNode? Next { get; set; }
}

[MessagePackObject(AllowCircularReferences = true)]
public class CircularTree
{
    [Key(0)] public string? Name { get; set; }
    [Key(1)] public CircularTree? Parent { get; set; }
    [Key(2)] public List<CircularTree>? Children { get; set; }
}

[MessagePackObject(true, AllowCircularReferences = true)]
public class CircularMapNode
{
    public string? Name { get; set; }
    public CircularMapNode? Next { get; set; }
}

[MessagePackObject(AllowCircularReferences = true)]
public class CircularOwner
{
    [Key(0)] public string? Name { get; set; }
    [Key(1)] public CircularPet? Pet { get; set; }
}

[MessagePackObject(AllowCircularReferences = true)]
public class CircularPet
{
    [Key(0)] public string? Name { get; set; }
    [Key(1)] public CircularOwner? Owner { get; set; }
}

[MessagePackObject(AllowCircularReferences = true)]
public class CircularGenericNode<T>
{
    [Key(0)] public T? Item { get; set; }
    [Key(1)] public CircularGenericNode<T>? Next { get; set; }
}

// polymorphic composite: the union ROOT carries no flag (MsgPack015 if it tried);
// the concrete case types do, so base-typed references still resolve identity
[MessagePackObject]
[UnionTag(typeof(CircularBranch), 0)]
[UnionTag(typeof(CircularLeafPart), 1)]
public abstract class CircularPart
{
}

[MessagePackObject(AllowCircularReferences = true)]
public class CircularBranch : CircularPart
{
    [Key(0)] public string? Name { get; set; }
    [Key(1)] public List<CircularPart>? Parts { get; set; }
}

[MessagePackObject(AllowCircularReferences = true)]
public class CircularLeafPart : CircularPart
{
    [Key(0)] public string? Name { get; set; }
    [Key(1)] public CircularPart? Parent { get; set; }
}

// deliberately NOT circular: plain object holding two references to the same node
[MessagePackObject]
public class CircularSharedHolder
{
    [Key(0)] public CircularNode? First { get; set; }
    [Key(1)] public CircularNode? Second { get; set; }
}

// version-skew pair: V2 writes three members, the V1 shape skips key 1 on read
[MessagePackObject(AllowCircularReferences = true)]
public class CircularSkewV2
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public CircularNode? Extra { get; set; }
    [Key(2)] public CircularNode? Shared { get; set; }
}

[MessagePackObject(AllowCircularReferences = true)]
public class CircularSkewV1
{
    [Key(0)] public int Id { get; set; }
    [Key(2)] public CircularNode? Shared { get; set; }
}

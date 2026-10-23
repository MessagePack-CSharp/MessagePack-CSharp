using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;

namespace MessagePack.Tests;

// a union whose cases form a hierarchy: a payload tagged as the base case must never populate an existing instance of
// the derived case (the old `value as Base` accepted it, filled the base members, and left the derived type and its
// extra members behind, so the next Serialize dispatched it under the derived tag)
[MessagePackObject]
[UnionTag(typeof(ReviewBaseCase), 0)]
[UnionTag(typeof(ReviewDerivedCase), 1)]
public interface IReviewCase
{
}

[MessagePackObject]
public class ReviewBaseCase : IReviewCase
{
    [Key(0)]
    public int A { get; set; }
}

[MessagePackObject]
public class ReviewDerivedCase : ReviewBaseCase
{
    [Key(1)]
    public int B { get; set; }
}

[MessagePackObject]
public class ReviewCaseHolder
{
    [Key(0)]
    public IReviewCase? Case { get; set; }
}

// the plain-Deserialize route into the same bug: the member initializer hands the union formatter a derived instance
[MessagePackObject]
public class ReviewCaseHolderWithDerivedDefault
{
    [Key(0)]
    public IReviewCase? Case { get; set; } = new ReviewDerivedCase { A = 1, B = 2 };
}

// Regressions from the external review of v4.0.0-preview.1 (2026-10-23): the union populate type match and the
// MessagePackUnknownMembers x AllowCircularReferences interaction.
public class PostReleaseReviewRegressionTests
{
    // the generated tier and the reflection tier (the chain an assembly the generator never saw is served by)
    static IEnumerable<V4Options> BothTiers()
    {
        yield return V4Options.Default;
        yield return new V4Options(new MessagePackFormatterResolver(MessagePackFormatterFactory.Combine(
            BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance, new ReflectionFormatterFactory(annotatedOnly: true, allowPrivate: false))));
    }

    [Fact]
    public void UnionPopulate_BaseTagIntoDerivedInstance_CreatesTheBaseCase()
    {
        var bytes = V4.Serialize(new ReviewCaseHolder { Case = new ReviewBaseCase { A = 5 } });
        foreach (var options in BothTiers())
        {
            var holder = new ReviewCaseHolder { Case = new ReviewDerivedCase { A = 1, B = 2 } };
            V4.Deserialize(bytes, ref holder, options);
            var back = Assert.IsType<ReviewBaseCase>(holder!.Case); // exact: a derived instance is not "a base case"
            Assert.Equal(5, back.A);
            Assert.Equal(bytes, V4.Serialize(holder, options)); // and the round trip keeps tag 0
        }
    }

    [Fact]
    public void UnionPopulate_DerivedInitializer_PlainDeserializeCreatesTheBaseCase()
    {
        var bytes = V4.Serialize(new ReviewCaseHolderWithDerivedDefault { Case = new ReviewBaseCase { A = 5 } });
        foreach (var options in BothTiers())
        {
            var holder = V4.Deserialize<ReviewCaseHolderWithDerivedDefault>(bytes, options)!;
            var back = Assert.IsType<ReviewBaseCase>(holder.Case);
            Assert.Equal(5, back.A);
            Assert.Equal(bytes, V4.Serialize(holder, options));
        }
    }

    [Fact]
    public void UnionPopulate_ExactCase_StillReadsIntoTheExistingInstance()
    {
        var bytes = V4.Serialize(new ReviewCaseHolder { Case = new ReviewDerivedCase { A = 5, B = 6 } });
        foreach (var options in BothTiers())
        {
            var existing = new ReviewDerivedCase { A = 1, B = 2 };
            var holder = new ReviewCaseHolder { Case = existing };
            V4.Deserialize(bytes, ref holder, options);
            Assert.Same(existing, holder!.Case);
            Assert.Equal(5, existing.A);
            Assert.Equal(6, existing.B);
        }
    }

    // ---- unknown members must not capture a circular-reference back-reference (ext 97) ----

    const byte FixExt1 = 0xd4;
    const byte Ext8 = 0xc7;
    const byte ExtCircular = 97;

    // {"A":1,"B":"b","C":<value>} for UnkNarrowMap, which declares A and B and captures the rest
    static byte[] MapWithUnknownValue(params byte[] value) =>
        [0x83, 0xa1, (byte)'A', 0x01, 0xa1, (byte)'B', 0xa1, (byte)'b', 0xa1, (byte)'C', .. value];

    [Fact]
    public void UnknownMembers_BackReferenceValue_IsRejected()
    {
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<UnkNarrowMap>(MapWithUnknownValue(FixExt1, ExtCircular, 0x00)));
        Assert.Contains("back-reference", ex.Message);
    }

    [Fact]
    public void UnknownMembers_BackReferenceNestedInValue_IsRejected()
    {
        // [1, ext97(0)]: the walk covers the whole captured subtree
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<UnkNarrowMap>(MapWithUnknownValue(0x92, 0x01, FixExt1, ExtCircular, 0x00)));
        // and every ext header width: ext8 with a 1-byte payload
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<UnkNarrowMap>(MapWithUnknownValue(Ext8, 0x01, ExtCircular, 0x00)));
    }

    [Fact]
    public void UnknownMembers_BackReferenceInArrayMode_IsRejected()
    {
        // [1, "b", ext97(0)] for UnkNarrowArray, which declares keys 0 and 1 and captures the trailing elements
        byte[] bytes = [0x93, 0x01, 0xa1, (byte)'b', FixExt1, ExtCircular, 0x00];
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<UnkNarrowArray>(bytes));
    }

    [Fact]
    public void UnknownMembers_OtherExtensions_StillCaptureVerbatim()
    {
        // a user extension of another type code (and a definition envelope, which is a plain array) pass through
        var bytes = MapWithUnknownValue(FixExt1, 0x01, 0x00);
        var narrow = V4.Deserialize<UnkNarrowMap>(bytes)!;
        Assert.Equal(1, narrow.Extra!.Count);
        Assert.Equal(bytes, V4.Serialize(narrow));
    }

    // ---- Tuple`8 with a nil Rest is a serialization error, not the Tuple constructor's ArgumentException ----

    [Fact]
    public void Tuple8_NilRest_ThrowsSerializationException()
    {
        byte[] bytes = [0x98, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0xc0];
        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Tuple<int, int, int, int, int, int, int, Tuple<int>>>(bytes));
        Assert.Contains("Rest", ex.Message);
    }

    // ---- a negative MaxDepth is rejected instead of being silently unlimited ----

    [Fact]
    public void MaxDepth_Negative_IsRejected()
    {
        // the options are the gate; the per-call states fold anything <= 0 into unlimited with a single compare
        Assert.Throws<ArgumentOutOfRangeException>(() => V4Options.Default with { MaxDepth = -1 });
        Assert.Equal(0, (V4Options.Default with { MaxDepth = 0 }).MaxDepth); // 0 stays the documented "unlimited"
    }
}

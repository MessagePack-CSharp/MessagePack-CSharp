using System.Collections.ObjectModel;
using MessagePack.Formatters;
using Xunit;

namespace MessagePack.Tests;

// The populate contract as README §3 states it, pinned for both tiers. A plain Deserialize<T> constructs the object
// (its initializers run) and then refills in place whatever those initializers put in the settable members, the same
// as populating a caller-supplied instance: one allocation for `= new()` members, a comparer or concrete collection
// type chosen there is kept. This differs from v3, which constructed every member value afresh; a review (2026-10)
// weighed switching to v3's rule and kept this one, so these tests document the decision. Construction-shape types
// (init-only, required, constructor-bound) are the exception: always a fresh instance, in the reflection tier too.
public class PopulateContractTests
{
    static readonly MessagePackSerializerOptions ReflectionOptions = new(new MessagePackFormatterResolver(
    [
        BuiltInFormatterFactory.Instance,
        GenericFormatterFactory.Instance,
        new ReflectionFormatterFactory(annotatedOnly: true),
    ]));

    public static TheoryData<string> Tiers => new() { "generated", "reflection" };

    static MessagePackSerializerOptions Options(string tier) => tier == "generated" ? MessagePackSerializerOptions.Default : ReflectionOptions;

    public sealed class NegatedIntComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;

        public int GetHashCode(int value) => -value;
    }

    [MessagePackObject]
    public class Initialized
    {
        [Key(0)] public List<int> Items { get; set; } = new() { 99 }; // pre-filled: Newtonsoft would append, this replaces the contents
        [Key(1)] public Dictionary<int, int> Custom { get; set; } = new(new NegatedIntComparer());
        [Key(2)] public ICollection<long> Shape { get; set; } = new HashSet<long>();
        [Key(3)] public Settings Child { get; set; } = new() { Name = "initial", Flag = true };
    }

    [MessagePackObject]
    public class Settings
    {
        [Key(0)] public string? Name { get; set; }
        [Key(1)] public bool Flag { get; set; }
    }

    [Theory]
    [MemberData(nameof(Tiers))]
    public void PlainDeserialize_RefillsInitializerValuesInPlace(string tier)
    {
        var bytes = MessagePackSerializer.Serialize(new Initialized { Items = [1, 2], Custom = { [3] = 30 }, Shape = new HashSet<long> { 6 }, Child = new Settings { Name = "read" } }, Options(tier));

        var back = MessagePackSerializer.Deserialize<Initialized>(bytes, Options(tier));

        Assert.Equal([1, 2], back.Items);
        Assert.Equal(30, back.Custom[3]);
        Assert.IsType<NegatedIntComparer>(back.Custom.Comparer); // the initializer's comparer is kept
        Assert.IsType<HashSet<long>>(back.Shape);                // and its concrete collection type
        Assert.Contains(6, back.Shape);
        Assert.Equal("read", back.Child.Name);
        Assert.False(back.Child.Flag);                           // Flag was in the payload (false), so it is overwritten
    }

    [MessagePackObject]
    public class Observed
    {
        public Observed()
        {
            Items.CollectionChanged += (_, _) => Events++;
        }

        [IgnoreMember] public int Events;
        [Key(0)] public ObservableCollection<int> Items { get; set; } = new();
    }

    // the documented consequence: the initializer's collection is refilled, so its handlers stay wired and fire
    [Theory]
    [MemberData(nameof(Tiers))]
    public void InitializerCollection_IsRefilled_AndKeepsItsHandlers(string tier)
    {
        var source = new Observed();
        source.Items.Add(1);
        source.Items.Add(2);
        var bytes = MessagePackSerializer.Serialize(source, Options(tier));

        var back = MessagePackSerializer.Deserialize<Observed>(bytes, Options(tier));

        Assert.Equal([1, 2], back.Items);
        Assert.Equal(3, back.Events); // Reset (Clear raises it even when empty) + two Adds
        back.Items.Add(3);
        Assert.Equal(4, back.Events); // the handler is still attached, unlike a replaced collection
    }

    [MessagePackObject]
    public class Covariant
    {
        [Key(0)] public object[] Items { get; set; } = new string[2];
        [Key(1)] public ArraySegment<object> Segment { get; set; } = new(new string[2]);
    }

    // a string[] behind an object[] cannot take the ints read for object: replaced, not refilled (it used to leak
    // ArrayTypeMismatchException from AsSpan, and only when the payload length matched the initializer's)
    [Theory]
    [MemberData(nameof(Tiers))]
    public void CovariantArrayInitializer_IsReplaced(string tier)
    {
        var bytes = MessagePackSerializer.Serialize(new Covariant { Items = [1, 2], Segment = new object[] { 3, 4 } }, Options(tier));

        var back = MessagePackSerializer.Deserialize<Covariant>(bytes, Options(tier));

        Assert.Equal([1, 2], back.Items.Select(Convert.ToInt32));
        Assert.Equal([3, 4], back.Segment.Select(Convert.ToInt32));

        // an exact-typed array of matching length is still refilled in place
        var target = new Covariant { Items = new object[2], Segment = new(new object[2]) };
        var items = target.Items;
        MessagePackSerializer.Deserialize(bytes, ref target, Options(tier));
        Assert.Same(items, target.Items);
    }

    [MessagePackObject]
    public class Parent
    {
        [Key(0)] public Settings Child { get; set; } = new() { Name = "initial" };
        [Key(1)] public int Id { get; set; }
    }

    [Theory]
    [MemberData(nameof(Tiers))]
    public void ExplicitPopulate_RefillsTheCallersMembersInPlace(string tier)
    {
        var bytes = MessagePackSerializer.Serialize(new Parent { Child = new Settings { Name = "read" }, Id = 7 }, Options(tier));
        var target = new Parent();
        var child = target.Child;

        MessagePackSerializer.Deserialize(bytes, ref target, Options(tier));

        Assert.Same(child, target.Child);
        Assert.Equal("read", child.Name);
        Assert.Equal(7, target.Id);
    }

    [MessagePackObject]
    public class InitOnly
    {
        [Key(0)] public int Id { get; init; }
        [Key(1)] public string? Name { get; set; }
    }

    [MessagePackObject]
    public class RequiredMember
    {
        [Key(0)] public required int Id { get; set; }
    }

    // README's exception: a type whose members can only be set at construction is constructed afresh, in the
    // reflection tier too (it used to call the init accessor on the caller's instance after construction)
    [Theory]
    [MemberData(nameof(Tiers))]
    public void ExplicitPopulate_OfInitOnlyOrRequiredType_ConstructsAfresh(string tier)
    {
        var initOnly = new InitOnly { Id = 1, Name = "old" };
        var original = initOnly;
        MessagePackSerializer.Deserialize(MessagePackSerializer.Serialize(new InitOnly { Id = 2, Name = "new" }, Options(tier)), ref initOnly, Options(tier));
        Assert.NotSame(original, initOnly);
        Assert.Equal(1, original.Id);
        Assert.Equal((2, "new"), (initOnly.Id, initOnly.Name));

        var required = new RequiredMember { Id = 1 };
        var originalRequired = required;
        MessagePackSerializer.Deserialize(MessagePackSerializer.Serialize(new RequiredMember { Id = 2 }, Options(tier)), ref required, Options(tier));
        Assert.NotSame(originalRequired, required);
        Assert.Equal(2, required.Id);
    }
}

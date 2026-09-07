using System.Buffers;
using MessagePack;

namespace MessagePack.Tests.NativeAot;

[MessagePackObject]
public class AotIntKeyPoco
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public int Count { get; set; }
    [Key(2)] public string? Name { get; set; }
}

[MessagePackObject]
public class AotStringKeyPoco
{
    [Key("id")] public int Id { get; set; }
    [Key("name")] public string? Name { get; set; }
}

[MessagePackObject]
public class AotNestedPoco
{
    [Key(0)] public AotIntKeyPoco? Inner { get; set; }
    [Key(1)] public int[]? Numbers { get; set; }
    [Key(2)] public DateTime Stamp { get; set; }
    [Key(3)] public byte[]? Blob { get; set; }
    [Key(4)] public double Score { get; set; }
    [Key(5)] public bool Flag { get; set; }
    [Key(6)] public long Big { get; set; }
    [Key(7)] public string? Long { get; set; }
}

[MessagePackObject(AllowCircularReferences = true)]
public class AotCircularNode
{
    [Key(0)] public int Value { get; set; }
    [Key(1)] public AotCircularNode? Next { get; set; }
}

[MessagePackObject]
public class AotNarrowIntKeyPoco
{
    [Key(0)] public int Id { get; set; }
    public MessagePackUnknownMembers? Extra { get; set; }
}

[MessagePackObject]
public struct AotStructPoco
{
    [Key(0)] public int X { get; set; }
    [Key(1)] public long Y { get; set; }
}

[MessagePackObject]
public record AotCtorRecord([property: Key(0)] int Id, [property: Key(1)] string? Name);

[MessagePackObject]
public class AotInitPoco
{
    [Key(0)] public int A { get; init; }
    [Key(1)] public string? B { get; init; }
}

// harvested built-in instantiations: enums and Nullable<T> are served by the
// RequiresDynamicCode GenericFormatterFactory on CoreCLR, which the AOT chain omits —
// the factory's static constructions and registrations must serve them under ILC
public enum AotColor : byte
{
    Red = 1,
    Green = 2,
}

[MessagePackObject]
public class AotEnumHolder
{
    [Key(0)] public AotColor Color { get; set; }
    [Key(1)] public AotColor? MaybeColor { get; set; }
    [Key(2)] public DayOfWeek Day { get; set; }
    [Key(3)] public int? MaybeCount { get; set; }
}

// harvested generic instantiations: AotGenericBox<int> / <string> appear only as member
// types below, so the factory's static closed constructions must serve them under ILC
// (the MakeGenericType path is declined on Native AOT)
[MessagePackObject]
public class AotGenericBox<T>
{
    [Key(0)] public T? Item { get; set; }
}

[MessagePackObject]
public class AotGenericHolder
{
    [Key(0)] public AotGenericBox<int>? Numbers { get; set; }
    [Key(1)] public AotGenericBox<string>? Words { get; set; }
}

// harvested BCL collection instantiations: List<string>, Dictionary<K,V>, string[],
// tuples and custom ICollection<T> shapes ride GenericFormatterFactory on CoreCLR, which
// the AOT chain omits — the factory's static constructions must serve them under ILC.
// List<int> deliberately stays: BuiltInFormatterFactory owns the primitive closed forms
// and the harvest must not shadow them.
[MessagePackObject]
public class AotCollectionHolder
{
    [Key(0)] public List<string>? Tags { get; set; }
    [Key(1)] public Dictionary<string, int>? Scores { get; set; }
    [Key(2)] public string[]? Names { get; set; }
    [Key(3)] public HashSet<string>? Unique { get; set; }
    [Key(4)] public List<List<int>>? Nested { get; set; }
    [Key(5)] public (int, string) Point { get; set; }
    [Key(6)] public AotCustomList? Custom { get; set; }
    [Key(7)] public List<int>? Ints { get; set; }
}

public class AotCustomList : System.Collections.ObjectModel.Collection<string>
{
}

// IMessagePackSurrogate under ILC: the conversions are constrained instance calls over a
// struct surrogate (direct, no reflection). No attribute on the target: the
// interface implementation below is the declaration, auto-registered through the
// generated factory's statically closed SurrogateFormatter construction
public class AotUserId
{
    public int Value { get; }
    public string Realm { get; }

    public AotUserId(int value, string realm)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        Value = value;
        Realm = realm;
    }
}

[MessagePackObject]
public readonly record struct AotUserIdSurrogate(
    [property: Key(0)] int Value,
    [property: Key(1)] string Realm)
    : IMessagePackSurrogate<AotUserId, AotUserIdSurrogate>
{
    public AotUserIdSurrogate ToSurrogate(AotUserId value) => new(value.Value, value.Realm);

    public AotUserId ToTarget() => new(Value, Realm);
}

// [MessagePackSerializable] roots: shapes that appear ONLY as serialization roots (never
// as a member) are invisible to the member-graph harvest — this factory's generated half
// statically constructs and registers them, and its Instance composes into explicit
// chains. List<AotIntKeyPoco> deliberately stays undeclared: the unregistered-type test
// depends on it.
[MessagePackSerializable<AotIntKeyPoco[]>]
[MessagePackSerializable<List<AotStructPoco>>]
public partial class AotRootFactory;

[MessagePackObject]
[UnionTag(typeof(AotCircle), 0)]
[UnionTag(typeof(AotSquare), 1)]
public interface IAotShape
{
}

[MessagePackObject]
public class AotCircle : IAotShape
{
    [Key(0)] public double Radius { get; set; }
}

[MessagePackObject]
public class AotSquare : IAotShape
{
    [Key(0)] public double Side { get; set; }
}

[MessagePackObject]
public class AotCallbackPoco : IMessagePackSerializationCallbackReceiver
{
    [Key(0)] public int Raw { get; set; }
    [Key(1)] public int Normalized { get; set; }
    [IgnoreMember] public bool Restored { get; set; }

    public void OnBeforeSerialize() => Normalized = Raw * 2;

    public void OnAfterDeserialize() => Restored = true;
}

[MessagePackObject]
public class AotWrapper<T>
{
    [Key(0)] public T? Value { get; set; }
    [Key(1)] public int Count { get; set; }
}

// two-segment ReadOnlySequence builder for the split-deserialize tests
sealed class SplitSegment : ReadOnlySequenceSegment<byte>
{
    SplitSegment(ReadOnlyMemory<byte> memory, SplitSegment? next, long runningIndex)
    {
        Memory = memory;
        Next = next;
        RunningIndex = runningIndex;
    }

    public static ReadOnlySequence<byte> CreateSplit(byte[] data, int splitAt)
    {
        var second = new SplitSegment(data.AsMemory(splitAt), null, splitAt);
        var first = new SplitSegment(data.AsMemory(0, splitAt), second, 0);
        return new ReadOnlySequence<byte>(first, 0, second, data.Length - splitAt);
    }
}

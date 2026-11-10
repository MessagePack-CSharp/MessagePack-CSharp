using MessagePack;

namespace MessagePack.Tests.Robustness;

// Target shapes spanning the deserializer's structural variety, so a mutated payload
// exercises: int-key array formatters, string-key map formatters, constructor/init-only
// construction, polymorphic union dispatch, unknown-member capture, nested graphs,
// collections, enums and Nullable. All source-generated (the in-box tier); the reflection
// tier is covered by re-running the same bytes through a suppressed-generation resolver in
// RobustnessTests.

[MessagePackObject]
public class SampleArrayPoco
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
    [Key(2)] public double Score { get; set; }
    [Key(3)] public bool Flag { get; set; }
    [Key(4)] public SampleColor Color { get; set; }
    [Key(5)] public DateTime When { get; set; }
    [Key(6)] public int[]? Numbers { get; set; }
    [Key(7)] public List<string>? Tags { get; set; }
    [Key(8)] public SampleArrayPoco? Child { get; set; }
    [Key(9)] public Dictionary<string, int>? Map { get; set; }
}

[MessagePackObject(keyAsPropertyName: true)]
public class SampleMapPoco
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public double Score { get; set; }
    public SampleColor Color { get; set; }
    public List<int>? Numbers { get; set; }
    public SampleMapPoco? Child { get; set; }
}

[MessagePackObject]
public class SampleCtorPoco
{
    public SampleCtorPoco(int id, string name, SampleColor color)
    {
        Id = id;
        Name = name;
        Color = color;
    }

    [Key(0)] public int Id { get; }
    [Key(1)] public string Name { get; }
    [Key(2)] public SampleColor Color { get; }
    [Key(3)] public int Extra { get; init; }
}

[MessagePackObject(keyAsPropertyName: true)]
public class SampleUnknownPoco
{
    public int Id { get; set; }
    public string? Name { get; set; }
    // captures whatever map keys the declared members don't (map-mode only, no [Key])
    public MessagePackUnknownMembers? Extra { get; set; }
}

[MessagePackObject]
[UnionTag(typeof(SampleCircle), 0)]
[UnionTag(typeof(SampleRectangle), 1)]
[UnionTag(typeof(SampleTriangle), 7)]
public interface ISampleShape
{
}

[MessagePackObject]
public class SampleCircle : ISampleShape
{
    [Key(0)] public double Radius { get; set; }
}

[MessagePackObject]
public class SampleRectangle : ISampleShape
{
    [Key(0)] public double Width { get; set; }
    [Key(1)] public double Height { get; set; }
}

[MessagePackObject]
public class SampleTriangle : ISampleShape
{
    [Key(0)] public double Base { get; set; }
    [Key(1)] public double Height { get; set; }
    [Key(2)] public ISampleShape? Inscribed { get; set; }
}

// a recursive type that holds collections of itself: the shape of the nested-header allocation amplification (a chain
// of array headers each claiming the remaining bytes, so every level preallocates before the depth check stops the
// descent; Nerdbank.MessagePack GHSA-grf3-jgmf-2g5j, bounded here by DeserializeState's declared-element budget)
[MessagePackObject]
public class SampleTreePoco
{
    [Key(0)] public List<SampleTreePoco>? Children { get; set; }
    [Key(1)] public SampleTreePoco?[]? Kids { get; set; }
    [Key(2)] public Dictionary<string, SampleTreePoco>? Named { get; set; }
    [Key(3)] public int Value { get; set; }
}

public enum SampleColor : byte
{
    Red = 0,
    Green = 1,
    Blue = 2,
    Ultraviolet = 200,
}

[Flags]
public enum SamplePermission
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
}

// enums by name: the payload's strings are attacker input matched against the declared names (in place over the UTF-8
// in the generated name formatters, through EnumAsStringFormatter in the reflection tier), and a [Flags] value is a
// comma-separated list of them. The Reflected twin runs the same members through the reflection tier's formatter tree.
[MessagePackObject(true, EnumAsString = true)]
public class SampleEnumNamesPoco
{
    public SampleColor Color { get; set; }
    public SampleColor? MaybeColor { get; set; }
    public SamplePermission Permission { get; set; }
    public List<SampleColor>? Colors { get; set; }
    public Dictionary<SampleColor, int>? Counts { get; set; }
    public SamplePermission[]? Permissions { get; set; }
}

[MessagePackObject(true, EnumAsString = true, SuppressSourceGeneration = true)]
public class SampleEnumNamesReflectedPoco
{
    public SampleColor Color { get; set; }
    public SampleColor? MaybeColor { get; set; }
    public SamplePermission Permission { get; set; }
    public List<SampleColor>? Colors { get; set; }
    public Dictionary<SampleColor, int>? Counts { get; set; }
    public SamplePermission[]? Permissions { get; set; }
}

// circular references: an instance already written goes on the wire as a back-reference (an ext carrying its id), so
// the ids are attacker input: one that points nowhere, at a later instance, at itself mid-construction, or at an
// instance of another type. Source-generated only: the reflection tier does not read this wire (MsgPack015).
[MessagePackObject(AllowCircularReferences = true)]
public class SampleGraphNode
{
    [Key(0)] public int Value { get; set; }
    [Key(1)] public SampleGraphNode? Next { get; set; }
    [Key(2)] public List<SampleGraphNode>? Others { get; set; }
}


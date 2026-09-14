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

public enum SampleColor : byte
{
    Red = 0,
    Green = 1,
    Blue = 2,
    Ultraviolet = 200,
}

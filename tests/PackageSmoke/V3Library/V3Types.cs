// Compiled against MessagePack.Annotations 3.1.8 only. The v4 side reads these through the 4.x shim's type forwarders
// (MessagePackObject, Key, IgnoreMember, SerializationConstructor) and its loadable legacy attributes (Union).
using MessagePack;

namespace V3Library
{
    [MessagePackObject]
    public class V3Dto
    {
        [Key(0)] public int Id { get; set; }
        [Key(1)] public string? Name { get; set; }
        [Key(2)] public V3Inner? Inner { get; set; }
        [IgnoreMember] public int NotSerialized { get; set; }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class V3Inner
    {
        public double Value { get; set; }
    }

    [MessagePackObject]
    public class V3Immutable
    {
        [SerializationConstructor]
        public V3Immutable(int id, string label)
        {
            Id = id;
            Label = label;
        }

        [Key(0)] public int Id { get; }
        [Key(1)] public string Label { get; }
    }

    // v3's [Union]: the 4.x shim keeps the attribute in its v3 shape (a compile-time [Obsolete] error against 4.x, but
    // loadable at run time), so a v3-built hierarchy still describes its cases to the reflection tier
    [Union(0, typeof(V3Circle))]
    [Union(1, typeof(V3Square))]
    public abstract class V3Shape
    {
    }

    [MessagePackObject]
    public class V3Circle : V3Shape
    {
        [Key(0)] public double Radius { get; set; }
    }

    [MessagePackObject]
    public class V3Square : V3Shape
    {
        [Key(0)] public double Side { get; set; }
    }
}

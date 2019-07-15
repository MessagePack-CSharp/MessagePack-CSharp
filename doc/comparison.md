# Compare with protobuf, JSON, ZeroFormatter

[protbuf-net](https://github.com/mgravell/protobuf-net) is major, most used binary-format library on .NET. I love protobuf-net and respect that great work. But if uses protobuf-net for general-purpose serialization format, you may encounts annoying issue.

```csharp
[ProtoContract]
public class Parent
{
    [ProtoMember(1)]
    public int Primitive { get; set; }
    [ProtoMember(2)]
    public Child Prop { get; set; }
    [ProtoMember(3)]
    public int[] Array { get; set; }
}

[ProtoContract]
public class Child
{
    [ProtoMember(1)]
    public int Number { get; set; }
}

using (var ms = new MemoryStream())
{
    // serialize null.
    ProtoBuf.Serializer.Serialize<Parent>(ms, null);

    ms.Position = 0;
    var result = ProtoBuf.Serializer.Deserialize<Parent>(ms);

    Console.WriteLine(result != null); // True, not null. but all property are zero formatted.
    Console.WriteLine(result.Primitive); // 0
    Console.WriteLine(result.Prop); // null
    Console.WriteLine(result.Array); // null
}

using (var ms = new MemoryStream())
{
    // serialize empty array.
    ProtoBuf.Serializer.Serialize<Parent>(ms, new Parent { Array = new int[0] });

    ms.Position = 0;
    var result = ProtoBuf.Serializer.Deserialize<Parent>(ms);

    Console.WriteLine(result.Array == null); // True, null!
}
```

protobuf(-net) can not handle null and empty collection correctly. Because protobuf has no `null` representation( [this](http://stackoverflow.com/questions/21631428/protobuf-net-deserializes-empty-collection-to-null-when-the-collection-is-a-prop) is the protobuf-net authors answer).

[MessagePack specification](https://github.com/msgpack/msgpack/blob/master/spec.md) can completely serialize C# type system. This is the reason to recommend MessagePack over protobuf.

Protocol Buffers has good IDL and [gRPC](http://www.grpc.io/), that is a much good point than MessagePack. If you want to use IDL, I recommend [Google.Protobuf](https://github.com/google/protobuf/tree/master/csharp/src/Google.Protobuf) than MessagePack.

JSON is good general-purpose format. It is perfect, simple and enough spec. [Utf8Json](https://github.com/neuecc/Utf8Json) which created me that adopts same architecture as MessagePack for C# and avoid encoding/decoing cost so work like binary. If you want to know about binary vs text, see [Utf8Json/which serializer should be used](https://github.com/neuecc/Utf8Json#which-serializer-should-be-used) section.

[ZeroFormatter](https://github.com/neuecc/ZeroFormatter/) is similar as [FlatBuffers](https://google.github.io/flatbuffers/) but specialized to C#. It is special. Deserialization is infinitely fast but instead the binary size is large. And ZeroFormatter's caching algorithm requires additional memory.

Again, ZeroFormatter is special. When situation matches with ZeroFormatter, it demonstrates power of format. But for many common uses, MessagePack for C# would be better.

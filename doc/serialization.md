Object Serialization
---
MessagePack for C# can serialize your own public `Class` or `Struct`. Serialization target must marks `[MessagePackObject]` and `[Key]`. Key type can choose int or string. If key type is int, serialized format is used array. If key type is string, serialized format is used map. If you define `[MessagePackObject(keyAsPropertyName: true)]`, does not require `KeyAttribute`.

```csharp
[MessagePackObject]
public class Sample1
{
    [Key(0)]
    public int Foo { get; set; }
    [Key(1)]
    public int Bar { get; set; }
}

[MessagePackObject]
public class Sample2
{
    [Key("foo")]
    public int Foo { get; set; }
    [Key("bar")]
    public int Bar { get; set; }
}

[MessagePackObject(keyAsPropertyName: true)]
public class Sample3
{
    // no needs KeyAttribute
    public int Foo { get; set; }

    // If ignore public member, you can use IgnoreMemberAttribute
    [IgnoreMember]
    public int Bar { get; set; }
}

// [10,20]
Console.WriteLine(MessagePackSerializer.ToJson(new Sample1 { Foo = 10, Bar = 20 }));

// {"foo":10,"bar":20}
Console.WriteLine(MessagePackSerializer.ToJson(new Sample2 { Foo = 10, Bar = 20 }));

// {"Foo":10}
Console.WriteLine(MessagePackSerializer.ToJson(new Sample3 { Foo = 10, Bar = 20 }));
```

All patterns serialization target are public instance member(field or property). If you want to avoid serialization target, you can add `[IgnoreMember]` to target member.

> target class must be public, does not allows private, internal class.

Which should uses int key or string key? I recommend use int key because faster and compact than string key. But string key has key name information, it is useful for debugging.

MessagePackSerializer requests target must put attribute is for robustness. If class is grown, you need to be conscious of versioning. MessagePackSerializer uses default value if key does not exists. If uses int key, should be start from 0 and should be sequential. If unnecessary properties come out, please make a missing number. Reuse is bad. Also, if Int Key's jump number is too large, it affects binary size.

```csharp
[MessagePackObject]
public class IntKeySample
{
    [Key(3)]
    public int A { get; set; }
    [Key(10)]
    public int B { get; set; }
}

// [null,null,null,0,null,null,null,null,null,null,0]
Console.WriteLine(MessagePackSerializer.ToJson(new IntKeySample()));
```

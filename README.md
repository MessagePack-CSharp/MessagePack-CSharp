# MessagePack for C# (.NET, .NET Core, Unity, Xamarin)

[![NuGet](https://img.shields.io/nuget/v/MessagePack.svg)](https://www.nuget.org/packages/messagepack)
[![NuGet](https://img.shields.io/nuget/vpre/MessagePack.svg)](https://www.nuget.org/packages/messagepack)
[![Releases](https://img.shields.io/github/release/neuecc/MessagePack-CSharp.svg)](https://github.com/neuecc/MessagePack-CSharp/releases)

[![Join the chat at https://gitter.im/MessagePack-CSharp/Lobby](https://badges.gitter.im/MessagePack-CSharp/Lobby.svg)](https://gitter.im/MessagePack-CSharp/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Build Status](https://dev.azure.com/ils0086/MessagePack-CSharp/_apis/build/status/MessagePack-CSharp-CI)](https://dev.azure.com/ils0086/MessagePack-CSharp/_build/latest?definitionId=2)

The extremely fast [MessagePack](http://msgpack.org/) serializer for C#. It is 10x faster than [MsgPack-Cli](https://github.com/msgpack/msgpack-cli) and outperforms other C# serializers. MessagePack for C# also ships with built-in support for LZ4 compression - an extremely fast compression algorithm. Performance is important, particularly in applications like game development, distributed computing, microservice architecture, and caching.

![Perf comparison graph](https://cloud.githubusercontent.com/assets/46207/23835716/89c8ab08-07af-11e7-9183-9e9415bdc87f.png)

MessagePack has compact binary size and full set of general purpose expression. Please see the [comparison with JSON, protobuf, ZeroFormatter section](#comparison). Learn [why MessagePack C# is fastest](#performance).

## Installation

This library is distributed via NuGet package and with special [support for Unity](#unity).

### NuGet packages

We target .NET Standard 2.0 with special optimizations for .NET Core 2.1+.

```ps1
Install-Package MessagePack
```

Install the optional C# analyzer to get warnings for coding mistakes and code fixes to save you time:

```ps1
Install-Package MessagePackAnalyzer
```

Extension Packages (learn more in our [extensions section](#extensions)):

```ps1
Install-Package MessagePack.ImmutableCollection
Install-Package MessagePack.ReactiveProperty
Install-Package MessagePack.UnityShims
Install-Package MessagePack.AspNetCoreMvcFormatter
```

### Unity

For Unity, download from [releases](https://github.com/neuecc/MessagePack-CSharp/releases) page, providing `.unitypackage`. Unity IL2CPP or Xamarin AOT Environment, check the [pre-code generation section](#aot).

### Migration notes from v1.x

If you were using MessagePack-CSharp v1.x, check out [how to update to our new v2.x version](doc/migration.md).

## Quick Start

Define the class to be serialized and attribute the class with `[MessagePackObject]`.
Attribute public members (property or field) with `[Key]`.

```csharp
[MessagePackObject]
public class MyClass
{
    // Key is serialization index, it is important for versioning.
    [Key(0)]
    public int Age { get; set; }

    [Key(1)]
    public string FirstName { get; set; }

    [Key(2)]
    public string LastName { get; set; }

    // public members and does not serialize target, mark IgnoreMemberttribute
    [IgnoreMember]
    public string FullName { get { return FirstName + LastName; } }
}
```

Call `MessagePackSerializer.Serialize<T>/Deserialize<T>` to serialize/deserialize your object graph.
You can also use the `ConvertToJson` method to see a human readable representation of the msgpack that was written.

```csharp
class Program
{
    static void Main(string[] args)
    {
        var mc = new MyClass
        {
            Age = 99,
            FirstName = "hoge",
            LastName = "huga",
        };

        // call Serialize/Deserialize, that's all.
        byte[] bytes = MessagePackSerializer.Serialize(mc);
        MyClass mc2 = MessagePackSerializer.Deserialize<MyClass>(bytes);

        // you can dump msgpack binary to human readable json.
        // In default, MeesagePack for C# reduce property name information.
        // [99,"hoge","huga"]
        var json = MessagePackSerializer.ConvertToJson(bytes);
        Console.WriteLine(json);
    }
}
```

By default the attribute is required. Optionally it can be unnecessary, see [Object Serialization section](#object-serialization) and [Formatter Resolver section](#resolvers) for details.

## Analyzer

The MessagePackAnalyzer helps to:

1. Automate defining your serializable objects.
1. Produces compiler warnings due to incorrect attribute use, accessibility, and more.

![analyzergif](https://cloud.githubusercontent.com/assets/46207/23837445/ce734eae-07cb-11e7-9758-d69f0f095bc1.gif)

If you want to allow a specific type (for example, when registering a custom type), put `MessagePackAnalyzer.json` at the project root and make the Build Action to `AdditionalFiles`.

![image](https://cloud.githubusercontent.com/assets/46207/23837427/8a8d507c-07cb-11e7-9277-5a566eb0bfde.png)

This is a sample of the contents of `MessagePackAnalyzer.json`:

```json
[ "MyNamespace.FooClass", "MyNameSpace.BarStruct" ]
```

## Built-in support types

These types can serialize by default.

Primitives(`int`, `string`, etc...), `Enum`, `Nullable<>`,  `TimeSpan`,  `DateTime`, `DateTimeOffset`, `Nil`, `Guid`, `Uri`, `Version`, `StringBuilder`, `BitArray`, `ArraySegment<>`, `BigInteger`, `Complex`, `Array[]`, `Array[,]`, `Array[,,]`, `Array[,,,]`, `KeyValuePair<,>`, `Tuple<,...>`, `ValueTuple<,...>`, `List<>`, `LinkedList<>`, `Queue<>`, `Stack<>`, `HashSet<>`, `ReadOnlyCollection<>`, `IList<>`, `ICollection<>`, `IEnumerable<>`, `Dictionary<,>`, `IDictionary<,>`, `SortedDictionary<,>`, `SortedList<,>`, `ILookup<,>`, `IGrouping<,>`, `ObservableCollection<>`, `ReadOnlyOnservableCollection<>`, `IReadOnlyList<>`, `IReadOnlyCollection<>`, `ISet<>`, `ConcurrentBag<>`, `ConcurrentQueue<>`, `ConcurrentStack<>`, `ReadOnlyDictionary<,>`, `IReadOnlyDictionary<,>`, `ConcurrentDictionary<,>`, `Lazy<>`, custom inherited `ICollection<>` or `IDictionary<,>` with paramterless constructor, `IList`, `IDictionary` and custom inherited `ICollection` or `IDictionary` with paramterless constructor(includes `ArrayList` and `Hashtable`).

You can add custom type support and has some official/third-party extension package. for ImmutableCollections(`ImmutableList<>`, etc), for ReactiveProperty and for Unity(`Vector3`, `Quaternion`, etc...), for F#(Record, FsList, Discriminated Unions, etc...). Please see [extensions section](https://github.com/neuecc/MessagePack-CSharp#extensions).

`MessagePack.Nil` is built-in null/void/unit representation type of MessagePack for C#.

## Object Serialization

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

I want to use like JSON.NET! I don't want to put attribute! If you think that way, you can use a contractless resolver.

```csharp
public class ContractlessSample
{
    public int MyProperty1 { get; set; }
    public int MyProperty2 { get; set; }
}

var data = new ContractlessSample { MyProperty1 = 99, MyProperty2 = 9999 };
var bin = MessagePackSerializer.Serialize(data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

// {"MyProperty1":99,"MyProperty2":9999}
Console.WriteLine(MessagePackSerializer.ToJson(bin));

// You can set ContractlessStandardResolver as default.
MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);

// serializable.
var bin2 = MessagePackSerializer.Serialize(data);
```

I want to serialize private member! In default, can not serialize/deserialize private members. But you can use allow-private resolver.

```csharp
[MessagePackObject]
public class PrivateSample
{
    [Key(0)]
    int x;

    public void SetX(int v)
    {
        x = v;
    }

    public int GetX()
    {
        return x;
    }
}

var data = new PrivateSample();
data.SetX(9999);

// You can choose StandardResolverAllowPrivate or  ContractlessStandardResolverAllowPrivate
var bin = MessagePackSerializer.Serialize(data, MessagePack.Resolvers.DynamicObjectResolverAllowPrivate.Instance);
```

I don't need type, I want to use like BinaryFormatter! You can use as typeless resolver and helpers. Please see [Typeless section](https://github.com/neuecc/MessagePack-CSharp#typeless).

Resolver is key customize point of MessagePack for C#. Details, please see [extension point](https://github.com/neuecc/MessagePack-CSharp#extension-point-iformatterresolver).

## DataContract compatibility

You can use `[DataContract]` instead of `[MessagePackObject]`. If type is marked DataContract, you can use `[DataMember]` instead of `[Key]` and `[IgnoreDataMember]` instead of `[IgnoreMember]`.

`[DataMember(Order = int)]` is same as `[Key(int)]`, `[DataMember(Name = string)]` is same as `[Key(string)]`. If use `[DataMember]`, same as `[Key(nameof(propertyname)]`.

Using DataContract makes it a shared class library and you do not have to refer to MessagePack for C#. However, it is not included in analysis by Analyzer or code generation by `mpc.exe`. Also, functions like `UnionAttribute`, `MessagePackFormatterAttribute`, `SerializationConstructorAttribute` etc can not be used. For this reason, I recommend that you use the MessagePack for C# attribute basically.

## Serialize ImmutableObject (SerializationConstructor)

MessagePack for C# supports deserialize immutable object. For example, this struct can serialize/deserialize naturally.

```csharp
[MessagePackObject]
public struct Point
{
    [Key(0)]
    public readonly int X;
    [Key(1)]
    public readonly int Y;

    public Point(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }
}

var data = new Point(99, 9999);
var bin = MessagePackSerializer.Serialize(data);

// Okay to deserialize immutable obejct
var point = MessagePackSerializer.Deserialize<Point>(bin);
```

MessagePackSerializer choose constructor with the most matched argument, match index if key in integer or match name(ignore case) if key is string. If encounts `MessagePackDynamicObjectResolverException: can't find matched constructor parameter` you should check about this.

If can not match automatically, you can specify to use constructor manually by `[SerializationConstructorAttribute]`.

```csharp
[MessagePackObject]
public struct Point
{
    [Key(0)]
    public readonly int X;
    [Key(1)]
    public readonly int Y;

    [SerializationConstructor]
    public Point(int x)
    {
        this.X = x;
        this.Y = -1;
    }

    // If not marked attribute, used this(most matched argument)
    public Point(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }
}
```

## Serialization Callback

If object implements `IMessagePackSerializationCallbackReceiver`, received `OnBeforeSerialize` and `OnAfterDeserialize` on serilization process.

```csharp
[MessagePackObject]
public class SampleCallback : IMessagePackSerializationCallbackReceiver
{
    [Key(0)]
    public int Key { get; set; }

    public void OnBeforeSerialize()
    {
        Console.WriteLine("OnBefore");
    }

    public void OnAfterDeserialize()
    {
        Console.WriteLine("OnAfter");
    }
}
```

## Union

MessagePack for C# supports serialize interface. It is like `XmlInclude` or `ProtoInclude`. MessagePack for C# there called *Union*. `UnionAttribute` can only attach to interface or abstract class. It requires discriminated integer key and sub-type.

```csharp
// mark inheritance types
[MessagePack.Union(0, typeof(FooClass))]
[MessagePack.Union(1, typeof(BarClass))]
public interface IUnionSample
{
}

[MessagePackObject]
public class FooClass : IUnionSample
{
    [Key(0)]
    public int XYZ { get; set; }
}

[MessagePackObject]
public class BarClass : IUnionSample
{
    [Key(0)]
    public string OPQ { get; set; }
}

// ---

IUnionSample data = new FooClass() { XYZ = 999 };

// serialize interface.
var bin = MessagePackSerializer.Serialize(data);

// deserialize interface.
var reData = MessagePackSerializer.Deserialize<IUnionSample>(bin);

// use type-switch of C# 7.0
switch (reData)
{
    case FooClass x:
        Console.WriteLine(x.XYZ);
        break;
    case BarClass x:
        Console.WriteLine(x.OPQ);
        break;
    default:
        break;
}
```

C# 7.0 type-switch is best match for Union. Union is serialized to two-length array.

```csharp
IUnionSample data = new BarClass { OPQ = "FooBar" };

var bin = MessagePackSerializer.Serialize(data);

// Union is serialized to two-length array, [key, object]
// [1,["FooBar"]]
Console.WriteLine(MessagePackSerializer.ToJson(bin));
```

Using Union in Abstract Class, you can use same of interface.

```csharp
[Union(0, typeof(SubUnionType1))]
[Union(1, typeof(SubUnionType2))]
[MessagePackObject]
public abstract class ParentUnionType
{
    [Key(0)]
    public int MyProperty { get; set; }
}

[MessagePackObject]
public class SubUnionType1 : ParentUnionType
{
    [Key(1)]
    public int MyProperty1 { get; set; }
}

[MessagePackObject]
public class SubUnionType2 : ParentUnionType
{
    [Key(1)]
    public int MyProperty2 { get; set; }
}
```

Serialization of inherited type, flatten in array(or map), be carefult to integer key, it cannot duplicate parent and all childrens.

## Dynamic (Untyped) Deserialization

If use `MessagePackSerializer.Deserialize<object>` or `MessagePackSerializer.Deserialize<dynamic>`, convert messagepack binary to primitive values that convert from msgpack-primitive to `bool`, `char`, `sbyte`, `byte`, `short`, `int`, `long`, `ushort`, `uint`, `ulong`, `float`, `double`, `DateTime`, `string`, `byte[]`, `object[]`, `IDictionary<object, object>`.

```csharp
// sample binary.
var model = new DynamicModel { Name = "foobar", Items = new[] { 1, 10, 100, 1000 } };
var bin = MessagePackSerializer.Serialize(model, ContractlessStandardResolver.Instance);

// dynamic, untyped
var dynamicModel = MessagePackSerializer.Deserialize<dynamic>(bin, ContractlessStandardResolver.Instance);

Console.WriteLine(dynamicModel["Name"]); // foobar
Console.WriteLine(dynamicModel["Items"][2]); // 100
```

So you can access indexer for msgpack map and array.

## Object Type Serialization

`StandardResolver` and `ContractlessStandardResolver` can serialize `object` type as concrete type by `DynamicObjectTypeFallbackResolver`.

```csharp
var objects = new object[] { 1, "aaa", new ObjectFieldType { Anything = 9999 } };
var bin = MessagePackSerializer.Serialize(objects);

// [1,"aaa",[9999]]
Console.WriteLine(MessagePackSerializer.ToJson(bin));

// Support Anonymous Type Serialize
var anonType = new { Foo = 100, Bar = "foobar" };
var bin2 = MessagePackSerializer.Serialize(anonType, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

// {"Foo":100,"Bar":"foobar"}
Console.WriteLine(MessagePackSerializer.ToJson(bin2));
```

> Unity supports is limited.

When deserializing, same as Dynamic(Untyped) Deserialization.

## Typeless

Typeless API is like `BinaryFormatter`, embed type information to binary so no needs type to deserialize.

```csharp
object mc = new Sandbox.MyClass()
{
    Age = 10,
    FirstName = "hoge",
    LastName = "huga"
};

// serialize to typeless
var bin = MessagePackSerializer.Typeless.Serialize(mc);

// binary data is embeded type-assembly information.
// ["Sandbox.MyClass, Sandbox",10,"hoge","huga"]
Console.WriteLine(MessagePackSerializer.ToJson(bin));

// can deserialize to MyClass with typeless
var objModel = MessagePackSerializer.Typeless.Deserialize(bin) as MyClass;
```

Type information is serialized by mspgack `ext` format, typecode is 100.

`MessagePackSerializer.Typeless` is shortcut of `Serialize/Deserialize<object>(TypelessContractlessStandardResolver.Instance)`. If you want to configure default typeless resolver, you can set by `MessagePackSerializer.Typeless.RegisterDefaultResolver`.

TypelessFormatter can use standalone and combinate with existing resolvers.

```csharp
// replace `object` uses typeless
var resolver = MessagePack.Resolvers.CompositeResolver.Create(
    new[] { MessagePack.Formatters.TypelessFormatter.Instance },
    new[] { MessagePack.Resolvers.StandardResolver.Instance });

public class Foo
{
    // use Typeless(this field only)
    [MessagePackFormatter(typeof(TypelessFormatter))]
    public object Bar;
}
```

If type name was changed, can not deserialize. If you need to typename fallback, you can use `TypelessFormatter.BindToType`.

```csharp
MessagePack.Formatters.TypelessFormatter.BindToType = typeName =>
{
    if (typeName.StartsWith("SomeNamespace"))
    {
        typeName = typeName.Replace("SomeNamespace", "AnotherNamespace");
    }

    return Type.GetType(typeName, false);
};
```

## Performance

Benchmarks comparing to other serializers run on `Windows 10 Pro x64 Intel Core i7-6700K 4.00GHz, 32GB RAM`. Benchmark code is [here](https://github.com/neuecc/ZeroFormatter/tree/master/sandbox/PerformanceComparison) - and there [version info](https://github.com/neuecc/ZeroFormatter/blob/bc63cb925d/sandbox/PerformanceComparison/packages.config), ZeroFormatter and [FlatBuffers](https://google.github.io/flatbuffers/) has infinitely fast deserializer so ignore deserialize performance.

![image](https://cloud.githubusercontent.com/assets/46207/23835765/55fe494e-07b0-11e7-98be-5e7a9411da40.png)

 MessagePack for C# uses many techniques for improve performance.

* Serializer uses only `IBufferWriter<byte>` rather than `System.IO.Stream` for reduced overhead.
* Buffers are rented from pools to reduce allocations, keeping throughput high through reduced GC pressure.
* Don't create intermediate utility instance(XxxWriter/Reader, XxxContext, etc...)
* Utilize dynamic code generation to avoid boxing value types. Use AOT generation on platforms that prohibit JIT.
* Getting cached generated formatter on static generic field (don't use dictinary-cache because dictionary lookup is overhead). See [Resolvers](https://github.com/neuecc/MessagePack-CSharp/tree/209f301e2e595ed366408624011ba2e856d23429/src/MessagePack/Resolvers)
* Heavily tuned dynamic IL code generation to avoid boxing value types. See [DynamicObjectTypeBuilder](https://github.com/neuecc/MessagePack-CSharp/blob/209f301e2e595ed366408624011ba2e856d23429/src/MessagePack/Resolvers/DynamicObjectResolver.cs#L142-L754).
Use AOT generation on platforms that prohibit JIT.
* Call PrimitiveAPI directly when il code generation knows target is primitive
* Reduce branch of variable length format when il code generation knows target(integer/string) range
* Don't use `IEnumerable<T>` abstraction on iterate collection, [see:CollectionFormatterBase](https://github.com/neuecc/MessagePack-CSharp/blob/209f301e2e595ed366408624011ba2e856d23429/src/MessagePack/Formatters/CollectionFormatter.cs#L192-L355) and inherited collection formatters
* Uses pre generated lookup table to reduce check messagepack type, [see: MessagePackBinary](https://github.com/neuecc/MessagePack-CSharp/blob/209f301e2e595ed366408624011ba2e856d23429/src/MessagePack/MessagePackBinary.cs#L15-L212)
* Uses optimized type key dictionary for non-generic methods, [see: ThreadsafeTypeKeyHashTable](https://github.com/neuecc/MessagePack-CSharp/blob/91312921cb7fe987f48336768c898a76ac7dbb40/src/MessagePack/Internal/ThreadsafeTypeKeyHashTable.cs)
* Avoid string key decode for lookup map(string key) key and uses automata based name lookup with il inlining code generation, see: [AutomataDictionary](https://github.com/neuecc/MessagePack-CSharp/blob/bcedbce3fd98cb294210d6b4a22bdc4c75ccd916/src/MessagePack/Internal/AutomataDictionary.cs)
* For string key encode, pre-generated member name bytes and use fixed sized binary copy in IL, see: [UnsafeMemory.cs](https://github.com/neuecc/MessagePack-CSharp/blob/f17ddc5d107d3a2f66f60398b214ef87919ff892/src/MessagePack/Internal/UnsafeMemory.cs)

Before creating this library, I implemented a fast fast serializer with [ZeroFormatter#Performance](https://github.com/neuecc/ZeroFormatter#performance). And this is a further evolved implementation. MessagePack for C# is always fast, optimized for all types(primitive, small struct, large object, any collections).

### Deserialize Performance per options

Performance varies depending on options. This is a micro benchamark with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet). Target object has 9 members(`MyProperty1` ~ `MyProperty9`), value are zero.

 |              Method |        Mean | Error | Scaled |  Gen 0 | Allocated |
 |-------------------- |------------:|------:|-------:|-------:|----------:|
 |              IntKey |    72.67 ns |    NA |   1.00 | 0.0132 |      56 B |
 |           StringKey |   217.95 ns |    NA |   3.00 | 0.0131 |      56 B |
 |     Typeless_IntKey |   176.71 ns |    NA |   2.43 | 0.0131 |      56 B |
 |  Typeless_StringKey |   378.64 ns |    NA |   5.21 | 0.0129 |      56 B |
 |       MsgPackCliMap | 1,355.26 ns |    NA |  18.65 | 0.1431 |     608 B |
 |     MsgPackCliArray |   455.28 ns |    NA |   6.26 | 0.0415 |     176 B |
 |         ProtobufNet |   265.85 ns |    NA |   3.66 | 0.0319 |     136 B |
 |            Hyperion |   366.47 ns |    NA |   5.04 | 0.0949 |     400 B |
 |       JsonNetString | 2,783.39 ns |    NA |  38.30 | 0.6790 |    2864 B |
 | JsonNetStreamReader | 3,297.90 ns |    NA |  45.38 | 1.4267 |    6000 B |
 |           JilString |   553.65 ns |    NA |   7.62 | 0.0362 |     152 B |
 |     JilStreamReader | 1,408.46 ns |    NA |  19.38 | 0.8450 |    3552 B |

IntKey, StringKey, Typeless_IntKey, Typeless_StringKey are MessagePack for C# options. All MessagePack for C# options achive zero memory allocation on deserialization process. JsonNetString/JilString is deserialized from string. JsonNetStreamReader/JilStreamReader is deserialized from UTF8 byte[] with StreamReader. Deserialization is normally read from Stream. Thus, it will be restored from byte[](or Stream) instead of string.

MessagePack for C# IntKey is fastest. StringKey is slower than IntKey because matching from the character string is required. If IntKey, read array length, for(array length) { binary decode }. If StringKey, read map length, for(map length) { decode key, lookup by key, binary decode } so requires additional two steps(decode key and lookup by key).

String key is often useful, contractless, simple replacement of JSON, interoperability with other languages, and more certain versioning. MessagePack for C# is also optimized for String Key. First of all, it do not decode UTF8 byte[] to String for matching with the member name, it will look up the byte[] as it is(avoid decode cost and extra allocation).

And It will try to match each `long type` (per 8 character, if it is not enough, pad with 0) using [automata](https://en.wikipedia.org/wiki/Automata_theory) and inline it when IL code generating.

![image](https://user-images.githubusercontent.com/46207/29754771-216b40e2-8bc7-11e7-8310-1c3602e80a08.png)

This also avoids calculating the hash code of byte[], and the comparison can be made several times on a long unit.

This is the sample decompile of generated deserializer code by [ILSpy](http://ilspy.net/).

![image](https://user-images.githubusercontent.com/46207/29754804-b5ba0f44-8bc7-11e7-9f6b-0c8f3c041237.png)

If the number of nodes is large, search with a embedded binary search.

Extra note, this is serialize benchmark result.

 |              Method |        Mean | Error | Scaled |  Gen 0 | Allocated |
 |-------------------- |------------:|------:|-------:|-------:|----------:|
 |              IntKey |    84.11 ns |    NA |   1.00 | 0.0094 |      40 B |
 |           StringKey |   126.75 ns |    NA |   1.51 | 0.0341 |     144 B |
 |     Typeless_IntKey |   183.31 ns |    NA |   2.18 | 0.0265 |     112 B |
 |  Typeless_StringKey |   193.95 ns |    NA |   2.31 | 0.0513 |     216 B |
 |       MsgPackCliMap |   967.68 ns |    NA |  11.51 | 0.1297 |     552 B |
 |     MsgPackCliArray |   284.20 ns |    NA |   3.38 | 0.1006 |     424 B |
 |         ProtobufNet |   176.43 ns |    NA |   2.10 | 0.0665 |     280 B |
 |            Hyperion |   280.14 ns |    NA |   3.33 | 0.1674 |     704 B |
 |       ZeroFormatter |   149.95 ns |    NA |   1.78 | 0.1009 |     424 B |
 |       JsonNetString | 1,432.55 ns |    NA |  17.03 | 0.4616 |    1944 B |
 | JsonNetStreamWriter | 1,775.72 ns |    NA |  21.11 | 1.5526 |    6522 B |
 |           JilString |   547.51 ns |    NA |   6.51 | 0.3481 |    1464 B |
 |     JilStreamWriter |   778.78 ns |    NA |   9.26 | 1.4448 |    6066 B |

 Of course, IntKey is fastest but StringKey also good.

## LZ4 Compression

MessagePack is a fast and *compact* format but it is not compression. [LZ4](https://github.com/lz4/lz4) is extremely fast compression algorithm, with MessagePack for C# can achive extremely fast perfrormance and extremely compact binary size!

MessagePack for C# has built-in LZ4 support. You can activate it using a modified options object and passing it into an API like this:

```cs
var lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);
MessagePackSerializer.Serialize(obj, lz4Options);
```

Builtin support is special, I've created serialize-compression pipeline and special tuned for the pipeline so share the working memory, don't allocate, don't resize until finished.

Serialized binary is not simply compressed lz4 binary. Serialized binary is valid MessagePack binary used ext-format and custom typecode(99).

```csharp
var array = Enumerable.Range(1, 100).Select(x => new MyClass { Age = 5, FirstName = "foo", LastName = "bar" }).ToArray();

// Use lz4Options instead of default
var lz4Bytes = MessagePackSerializer.Serialize(array, lz4Options);
var mc2 = MessagePackSerializer.Deserialize<MyClass[]>(lz4Bytes, lz4Options);

// you can dump lz4 message pack
// [[5,"hoge","huga"],[5,"hoge","huga"],....]
var json = MessagePackSerializer.ConvertToJson(lz4Bytes, lz4Options);
Console.WriteLine(json);

// lz4Bytes is valid MessagePack, it is using ext-format( [TypeCode:99, SourceLength|CompressedBinary] )
// [99,"0gAAA+vf3ABkkwWjZm9vo2JhcgoA////yVBvo2Jhcg=="]
var rawJson = MessagePackSerializer.ConvertToJson(lz4Bytes);
Console.WriteLine(rawJson);
```

Built-in LZ4 support uses primitive LZ4 API(LZ4 Block Format). The LZ4 API is more efficient if you know the size of original source length. Therefore, size is written as a header to the extension payload as a msgpack Int32 value. To decompress with a different LZ4 implementation you may need to read or manually seek past this length header.

If target binary size under 64 bytes, LZ4MessagePackSerializer does not compress to optimize small size serialization.

### Attributions

lz4 compression support is using Milosz Krajewski's [lz4net](https://github.com/MiloszKrajewski/lz4net) code with some modified.

## <a name="comparison"></a>Compare with protobuf, JSON, ZeroFormatter

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

For many common uses, MessagePack for C# would be better.

## Hint to achieve maximum performance to use MessagePack-CSharp

MessagePack for C# prioritizes maximize performance by default. However, there are also default options that sacrifice max performance for convenience.

### Use IntKey instead of StringKey(Contractless)

In [Deserialize Performance per options](https://github.com/neuecc/MessagePack-CSharp#deserialize-performance-per-options) sections shows the result of IntKey vs StringKey. IntKey serializes object-graph as msgpack-array format. StringKey serializes object-graph as msgpack-map format.

For example this type is serialized to

```csharp
[MessagePackObject]
public class Person
{
    [Key(0)] or [Key("name")]
    public string Name { get; set;}
    [Key(1)] or [Key("age")]
    public int Age { get; set;}
}

new Person { Name = "foobar", Age = 999 }
```

IntKey: `["foobar", 999]`, MapKey: `{"name:"foobar","age":999}`. IntKey is always fast in both serialize and deserialize because does not handle map key name, and always has the smallest binary size.

StringKey is often useful, contractless, simple replacement of JSON, interoperability with other languages, and more certain versioning. But to achieve maximum performance, use IntKey.

### Create own custom composite resolver

`CompositeResolver.Create` is easy way to create combine resolvers. But formatter lookup has some overhead. If you create custom resolver (or use `StaticCompositeResolver.Instance`), you can avoid overhead.

```csharp
public class MyApplicationResolver : IFormatterResolver
{
    public static readonly IFormatterResolver Instance = new MyApplicationResolver();

    // configure your custom resolvers.
    private static readonly IFormatterResolver[] Resolvers = new IFormatterResolver[]
    {
    };

    private MyApplicationResolver() { }

    public IMessagePackFormatter<T> GetFormatter<T>()
    {
        return Cache<T>.Formatter;
    }

    private static class Cache<T>
    {
        public static IMessagePackFormatter<T> Formatter;

        static Cache()
        {
            // configure your custom formatters.
            if (typeof(T) == typeof(XXX))
            {
                Formatter = new ICustomFormatter();
                return;
            }

            foreach (var resolver in Resolvers)
            {
                var f = resolver.GetFormatter<T>();
                if (f != null)
                {
                    Formatter = f;
                    return;
                }
            }
        }
    }
}
```

> NOTE: If you are creating a library, recommend using the above custom resolver instead of CompositeResolver.Create. Also, it must not use StaticCompositeResolver.

### Use native resolvers

In default, MessagePack-CSharp serializes GUID as string. This is much slower than the native .NET format GUID. The same applies to Decimal. If your application makes heavy use of GUIDs and Decimals and you don't have to worry about interconnection with other languages, you can replace them with native serializers by `NativeGuidResolver`, `NativeDecimalResolver`.

Also, DateTime is serialized using the MessagePack timestamp format. By using the `NativeDateTimeResolver`, it is possible to maintain Kind and perform faster serialization.

### Careful to copying buffer

`MessagePackSerializer.Serialize` returns `byte[]` in default. The final `byte[]` is copied from an internal buffer pool. That is an extra cost.  You can use `IBufferWriter<T>` or `Stream` API, it writes buffer directly. If you want to require a buffer pool outside of serializer, you should implement custom `IBufferWriter<byte>`.

In deserialization, `MessagePackSerializer.Deserialize(ReadOnlyMemory<byte> buffer)` is better than `Deserialize(Stream stream)` overload. This is because the overload of Stream starts reading the data, generating `ReadOnlySequence<byte>`, and then starting deserialization.

## Extensions

MessagePack for C# has extension point and you can add external type's serialization support. There are official extension support.

```ps1
Install-Package MessagePack.ImmutableCollection
Install-Package MessagePack.ReactiveProperty
Install-Package MessagePack.UnityShims
Install-Package MessagePack.AspNetCoreMvcFormatter
```

`MessagePack.ImmutableCollection` package add support for [System.Collections.Immutable](https://www.nuget.org/packages/System.Collections.Immutable/) library. It adds `ImmutableArray<>`, `ImmutableList<>`, `ImmutableDictionary<,>`, `ImmutableHashSet<>`, `ImmutableSortedDictionary<,>`, `ImmutableSortedSet<>`, `ImmutableQueue<>`, `ImmutableStack<>`, `IImmutableList<>`, `IImmutableDictionary<,>`, `IImmutableQueue<>`, `IImmutableSet<>`, `IImmutableStack<>` serialization support.

`MessagePack.ReactiveProperty` package add support for [ReactiveProperty](https://github.com/runceel/ReactiveProperty) library. It adds `ReactiveProperty<>`, `IReactiveProperty<>`, `IReadOnlyReactiveProperty<>`, `ReactiveCollection<>`, `Unit` serialization support. It is useful for save viewmodel state.

`MessagePack.UnityShims` package provides shim of [Unity](https://unity3d.com/)'s standard struct(`Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`, `Bounds`, `Rect`, `AnimationCurve`, `Keyframe`, `Matrix4x4`, `Gradient`, `Color32`, `RectOffset`, `LayerMask`, `Vector2Int`, `Vector3Int`, `RangeInt`, `RectInt`, `BoundsInt`) and their formatters. It can enable to communicate between server and Unity client.

After install, extension package must enable by configuration. Here is sample of enable all extension.

```csharp
// set extensions to default resolver.
var resolver = MessagePack.Resolvers.CompositeResolver.Create(
    // enable extension packages first
    ImmutableCollectionResolver.Instance,
    ReactivePropertyResolver.Instance,
    MessagePack.Unity.Extension.UnityBlitResolver.Instance,
    MessagePack.Unity.UnityResolver.Instance,

    // finaly use standard(default) resolver
    StandardResolver.Instance
);
var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

// pass options to every time or set as default
MessagePackSerializer.DefaultOptions = options;
```

Configuration details, see:[Extension Point section](https://github.com/neuecc/MessagePack-CSharp#extension-point-iformatterresolver).

`MessagePack.AspNetCoreMvcFormatter` is add-on of [ASP.NET Core MVC](https://github.com/aspnet/Mvc)'s serialization to boostup performance. This is configuration sample.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc().AddMvcOptions(option =>
    {
        option.OutputFormatters.Clear();
        option.OutputFormatters.Add(new MessagePackOutputFormatter(ContractlessStandardResolver.Options));
        option.InputFormatters.Clear();
        option.InputFormatters.Add(new MessagePackInputFormatter(ContractlessStandardResolver.Options));
    });
}
```

Author is creating other extension packages, too.

* [MagicOnion](https://github.com/Cysharp/MagicOnion) - gRPC based HTTP/2 RPC Streaming Framework
* [MasterMemory](https://github.com/Cysharp/MasterMemory) - Embedded Readonly In-Memory Document Database

You can make your own extension serializers or integrate with framework, let's create them and share it!

* [MessagePack.FSharpExtensions](https://github.com/pocketberserker/MessagePack.FSharpExtensions) - supports F# list,set,map,unit,option,discriminated union
* [MessagePack.NodaTime](https://github.com/ARKlab/MessagePack) -
Support for NodaTime types to MessagePack C#
* [WebApiContrib.Core.Formatter.MessagePack](https://github.com/WebApiContrib/WebAPIContrib.Core#formatters) - supports ASP.NET Core MVC([details in blog post](https://www.strathweb.com/2017/06/using-messagepack-with-asp-net-core-mvc/))
* [MessagePack.MediaTypeFormatter](https://github.com/sketch7/MessagePack.MediaTypeFormatter) - MessagePack MediaTypeFormatter

# API

## High-Level API (`MessagePackSerializer`)

The `MessagePackSerializer` class is the entry point of MessagePack for C#. Its static methods make up the main API of MessagePack for C#.

| API | Description |
| --- | --- |
| `Serialize<T>` | Serializes an object graph to msgpack binary. Async variety for Stream available. Non-generic overloads available. |
| `Deserialize<T>` | Deserializes msgpack binary to an object graph. Async variety for Stream available. Non-generic overloads available. |
| `SerializeToJson` | Serialize a MessagePack-compatible object graph to JSON instead of msgpack. Useful for debugging. |
| `ConvertToJson` | Convert msgpack binary to JSON. Useful for debugging.  |
| `ConvertFromJson` | Convert JSON to msgpack binary. |

A `MessagePackSerializer.Typeless` class offers most of the same APIs as above, but removes all type arguments, forcing serialization to include the full type name of the root object. It uses the `TypelessContractlessStandardResolver`. Consider the result to be a .NET-specific msgpack binary.

MessagePack for C# fundamentally serializes with `IBufferWriter<byte>` level and deserializes with `ReadOnlySequence<byte>` or `Memory<byte>`. Method overloads are provided for conveniently working with common buffer types and the .NET `Stream` class, but some of these convenience overloads require copying a buffer once.

High-Level API uses memory pool internally to avoid unnecessary memory allocation. If result size is under 64K, allocates GC memory only for the return bytes.

Each serialize/deserialize method takes an optional `MessagePackSerializerOptions` parameter which can be used to specify a custom `IFormatterResolver` to use or to activate LZ4 compression support.

## Low-Level API (`IMessagePackFormatter<T>`)

The `IMessagePackFormatter<T>` interface is responsible for serializing a unique type. For example `Int32Formatter : IMessagePackFormatter<Int32>` represents Int32 MessagePack serializer.

```csharp
public interface IMessagePackFormatter<T>
{
    void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options);
    T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options);
}
```

Many built-in formatters exists under `MessagePack.Formatters`. Your custom types are usually automatically supported with the built-in type resolvers that generate new `IMessagePackFormatter<T>` types on-the-fly using dynamic code generation. See our [AOT code generation](#aot) support for platforms that do not support this.

To take more control over the serialization of your custom types, write your own `IMessagePackFormatter<T>` implementation.
Here is sample of write own formatter. Note its use of the primitive API that is described in the next section.

```csharp
/// <summary>Serializes a <see cref="FileInfo" /> by its full path as a string.</summary>
public class FileInfoFormatter<T> : IMessagePackFormatter<FileInfo>
{
    public void Serialize(ref MessagePackWriter writer, FileInfo value, MessagePackSerializerOptions optionsr)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteString(value.FullName);
    }

    public FileInfo Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        var path = reader.ReadString();
        return new FileInfo(path);
    }
}
```

Your custom formatters must be discoverable via some `IFormatterResolver`. Learn more in our [resolvers](#resolvers).

You can see many other samples from [builtin formatters](https://github.com/neuecc/MessagePack-CSharp/tree/master/src/MessagePack/Formatters).

## Primitive API (`MessagePackWriter`, `MessagePackReader`)

The `MessagePackWriter` and `MessagePackReader` structs make up the lowest-level API. They read and write the primitives types defined in the msgpack specification.

### `MessagePackReader`

A `MessagePackReader` can efficiently read from `ReadOnlyMemory<byte>` or `ReadOnlySequence<byte>` without any allocating
except to allocate a new `string` as required by the `ReadString()` method. All other methods return either value structs
or `ReadOnlySequence<byte>` slices for extensions/arrays.
Reading directly from `ReadOnlySequence<byte>` means the reader can directly consume some modern high perforamnce APIs such as `PipeReader`.

| Method | Description |
| --- | --- |
| `Skip` | Advances the reader's position past the current value. If the value is complex (e.g. map, array) the entire structure is skipped. |
| `Read*` | Read and return a value whose type is named by the method name from the current reader position. Throws if the expected type does not match the actual type. When reading numbers, the type need not match the binary-specified type exactly. The numeric value will be coerced into the desired type or throw if the integer type is too small for a large value. |
| `TryReadNil` | Advances beyond the current value if the current value is `nil` and returns `true`; otherwise leaves the reader's position unchanged and returns `false`. |
| `ReadBytes` | Returns a slice of the input sequence representing the contents of a `byte[]`, and advances the reader. |
| `ReadStringSequence` | Returns a slice of the input sequence representing the contents of a `string` without decoding it, and advances the reader. |
| `Clone` | Creates a new `MessagePackReader` with the specified input sequence and the same settings as the original reader. |
| `CreatePeekReader` | Creates a new reader with the same position as this one, allowing the caller to "read ahead" without impacting the original reader's position. |
| `NextCode` | Reads the low-level msgpack `byte` that describes the type of the next value. Does not advance the reader. See [msgpack format of first byte](https://github.com/msgpack/msgpack/blob/master/spec.md#overview). Its static class has `ToMessagePackType` and `ToFormatName` utility methods. `MessagePackRange` means Min-Max fix range of msgpack format. |
| `NextMessagePackType` | Describes the `NextCode` value as a higher level category. Does not advance the reader. See [msgpack spec of source types](https://github.com/msgpack/msgpack/blob/master/spec.md#serialization-type-to-format-conversion). |
| (others) | Other methods and properties as described by the .xml doc comment file and Intellisense. |

The `MessagePackReader` is capable of automatically interpreting both the old and new msgpack spec.

### `MessagePackWriter`

A `MessagePackWriter` writes to a given instance of `IBufferWriter<byte>`. Several common implementations of this exist, allowing zero allocations and minimal buffer copies while writing directly to several I/O APIs including `PipeWriter`.

The `MessagePackWriter` writes the new msgpack spec by default, but can write msgpack compatible with the old spec by setting the `OldSpec` property to `true`.

| Method | Description |
| --- | --- |
| `Clone` | Creates a new `MessagePackWriter` with the specified underlying `IBufferWriter<byte>` and the same settings as the original writer. |
| `Flush` | Writes any buffered bytes to the underlying `IBufferWriter<byte>`. |
| `WriteNil` | Writes the msgpack equivalent of .NET's `null` value. |
| `Write` | Writes any msgpack primitive value in the most compact form possible. Has overloads for every primitive type defined by the msgpack spec. |
| `Write*IntType*` | Writes an integer value in exactly the msgpack type specified, even if a more compact format exists. |
| `WriteMapHeader` | Introduces a map by specifying the number of key=value pairs it contains. |
| `WriteArrayHeader` | Introduces an array by specifying the number of elements it contains. |
| `WriteExtensionFormat` | Writes the full content of an extension value including length, type code and content. |
| `WriteExtensionFormatHeader` | Writes just the header (length and type code) of an extension value. |
| `WriteRaw` | Copies the specified bytes directly to the underlying `IBufferWriter<byte>` without any validation. |
| (others) | Other methods and properties as described by the .xml doc comment file and Intellisense. |

`DateTime` is serialized to [MessagePack Timestamp format](https://github.com/msgpack/msgpack/blob/master/spec.md#formats-timestamp), it serialize/deserialize UTC and loses `Kind` info and requires that `MessagePackWriter.OldSpec == false`.
If you use the `NativeDateTimeResolver`, `DateTime` values will be serialized using .NET's native `Int64` represnetation, which preserves `Kind` info but may not be interoperable with non-.NET platforms.

## <a name="resolvers"></a>Extension Point (`IFormatterResolver`)

An `IFormatterResolver` is storage of typed serializers. The `MessagePackSerializer` API accepts a `MessagePackSerializerOptions` object which specifies the `IFormatterResolver` to use, allowing customization of the serialization of complex types.

| Resolver Name | Description |
| --- | --- |
| BuiltinResolver | Builtin primitive and standard classes resolver. It includes primitive(int, bool, string...) and there nullable, array and list. and some extra builtin types(Guid, Uri, BigInteger, etc...). |
| StandardResolver | Composited resolver. It resolves in the following order `builtin -> attribute -> dynamic enum -> dynamic generic -> dynamic union -> dynamic object -> dynamic object fallback`. This is the default of MessagePackSerializer. |
| ContractlessStandardResolver | Composited `StandardResolver`(except dynamic object fallback) -> `DynamicContractlessObjectResolver` -> `DynamicObjectTypeFallbackResolver`. It enables contractless serialization. |
| StandardResolverAllowPrivate | Same as StandardResolver but allow serialize/deserialize private members. |
| ContractlessStandardResolverAllowPrivate | Same as ContractlessStandardResolver but allow serialize/deserialize private members. |
| PrimitiveObjectResolver | MessagePack primitive object resolver. It is used fallback in `object` type and supports `bool`, `char`, `sbyte`, `byte`, `short`, `int`, `long`, `ushort`, `uint`, `ulong`, `float`, `double`, `DateTime`, `string`, `byte[]`, `ICollection`, `IDictionary`. |
| DynamicObjectTypeFallbackResolver | Serialize is used type in from `object` type, deserialize is used PrimitiveObjectResolver. |
| AttributeFormatterResolver | Get formatter from `[MessagePackFormatter]` attribute. |
| CompositeResolver | Composes several resolvers and/or formatters together in an ordered list, allowing reuse and overriding of behaviors of existing resolvers and formatters. |
| NativeDateTimeResolver | Serialize by .NET native DateTime binary format. It keeps `DateTime.Kind` that loses by standard(msgpack timestamp) format. |
| NativeGuidResolver | Serialize by .NET native Guid binary representation. It is faster than standard(string) representation. |
| NativeDecimalResolver | Serialize by .NET natvie decimal binary representation. It is faster than standard(string) representation. |
| DynamicEnumResolver | Resolver of enum and there nullable, serialize there underlying type. It uses dynamic code generation to avoid boxing and boostup performance serialize there name. |
| DynamicEnumAsStringResolver | Resolver of enum and there nullable.  It uses reflection call for resolve nullable at first time. |
| DynamicGenericResolver | Resolver of generic type(`Tuple<>`, `List<>`, `Dictionary<,>`, `Array`, etc). It uses reflection call for resolve generic argument at first time. |
| DynamicUnionResolver | Resolver of interface marked by UnionAttribute. It uses dynamic code generation to create dynamic formatter. |
| DynamicObjectResolver | Resolver of class and struct maked by MessagePackObjectAttribute. It uses dynamic code generation to create dynamic formatter. |
| DynamicContractlessObjectResolver | Resolver of all classes and structs. It does not needs MessagePackObjectAttribute and serialized key as string(same as marked [MessagePackObject(true)]). |
| DynamicObjectResolverAllowPrivate | Same as DynamicObjectResolver but allow serialize/deserialize private members. |
| DynamicContractlessObjectResolverAllowPrivate | Same as DynamicContractlessObjectResolver but allow serialize/deserialize private members. |
| TypelessObjectResolver | Used for `object`, embed .NET type in binary by `ext(100)` format so no need to pass type in deserilization.  |
| TypelessContractlessStandardResolver | Composited resolver. It resolves in the following order `nativedatetime -> builtin -> attribute -> dynamic enum -> dynamic generic -> dynamic union -> dynamic object -> dynamiccontractless -> typeless`. This is the default of `MessagePackSerializer.Typeless`  |

Each invocation of `MessagePackSerializer` accepts only a single resolver. Most object graphs will need more than one for serialization, so composing a single resolver made up of several is often required, and can be done with the `CompositeResolver` as shown below:

```csharp
// Do this once and store it for reuse.
var resolver = new MessagePack.Resolvers.CompositeResolver();
resolver.RegisterResolver(
    // resolver custom types first
    ImmutableCollectionResolver.Instance,
    ReactivePropertyResolver.Instance,
    MessagePack.Unity.Extension.UnityBlitResolver.Instance,
    MessagePack.Unity.UnityResolver.Instance,

    // finally use standard resolver
    StandardResolver.Instance);
var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

// Each time you serialize/deserialize, specify the options:
byte[] msgpackBytes = MessagePackSerializer.Serialize(myObject, options);
T myObject2 = MessagePackSerializer.Deserialize<MyObject>(msgpackBytes, options);
```

Created resolver can set as default by `MessagePackSerializer.DefaultOptions = options`, it can avoid explicitly pass custom resolver but **WARNING**: When developing an application where you control all MessagePack-related code it may be safe to rely on this mutable static to control behavior.
For all other libraries or multi-purpose applications that use `MessagePackSerializer` you should explicitly specify the `MessagePackSerializerOptions` to use with each method invocation to guarantee your code behaves as you expect even when sharing an `AppDomain` or process with other MessagePack users that may change this static property.

Here is sample of use `DynamicEnumAsStringResolver` with `DynamicContractlessObjectResolver` (It is JSON.NET-like lightweight setting.)

```csharp
// composite same as StandardResolver
var resolver = new MessagePack.Resolvers.CompositeResolver();
resolver.RegisterResolver(
    MessagePack.Resolvers.BuiltinResolver.Instance,
    MessagePack.Resolvers.AttributeFormatterResolver.Instance,

    // replace enum resolver
    MessagePack.Resolvers.DynamicEnumAsStringResolver.Instance,

    MessagePack.Resolvers.DynamicGenericResolver.Instance,
    MessagePack.Resolvers.DynamicUnionResolver.Instance,
    MessagePack.Resolvers.DynamicObjectResolver.Instance,

    MessagePack.Resolvers.PrimitiveObjectResolver.Instance,

    // final fallback(last priority)
    MessagePack.Resolvers.DynamicContractlessObjectResolver.Instance);
```

If you want to make your extension package, you should write both the formatter and resolver
for easier consumption.
Here is sample of a resolver:

```csharp
public class SampleCustomResolver : IFormatterResolver
{
    // Resolver should be singleton.
    public static readonly IFormatterResolver Instance = new SampleCustomResolver();

    private SampleCustomResolver()
    {
    }

    // GetFormatter<T>'s get cost should be minimized so use type cache.
    public IMessagePackFormatter<T> GetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    private static class FormatterCache<T>
    {
        public static readonly IMessagePackFormatter<T> Formatter;

        // generic's static constructor should be minimized for reduce type generation size!
        // use outer helper method.
        static FormatterCache()
        {
            Formatter = (IMessagePackFormatter<T>)SampleCustomResolverGetFormatterHelper.GetFormatter(typeof(T));
        }
    }
}

internal static class SampleCustomResolverGetFormatterHelper
{
    // If type is concrete type, use type-formatter map
    static readonly Dictionary<Type, object> formatterMap = new Dictionary<Type, object>()
    {
        {typeof(FileInfo), new FileInfoFormatter()}
        // add more your own custom serializers.
    };

    internal static object GetFormatter(Type t)
    {
        object formatter;
        if (formatterMap.TryGetValue(t, out formatter))
        {
            return formatter;
        }

        // If target type is generics, use MakeGenericType.
        if (t.IsGenericParameter && t.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
        {
            return Activator.CreateInstance(typeof(ValueTupleFormatter<,>).MakeGenericType(t.GenericTypeArguments));
        }

        // If type can not get, must return null for fallback mecanism.
        return null;
    }
}
```

## MessagePackFormatterAttribute

MessagePackFormatterAttribute is lightweight extension point of class, struct, interface, enum and property/field. This is like JSON.NET's JsonConverterAttribute. For example, serialize private field, serialize x10 formatter.

```csharp
[MessagePackFormatter(typeof(CustomObjectFormatter))]
public class CustomObject
{
    string internalId;

    public CustomObject()
    {
        this.internalId = Guid.NewGuid().ToString();
    }

    // serialize/deserialize internal field.
    class CustomObjectFormatter : IMessagePackFormatter<CustomObject>
    {
        public void Serialize(ref MessagePackWriter writer, CustomObject value, MessagePackSerializerOptions options)
        {
            options.Resolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.internalId, options);
        }

        public CustomObject Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var id = options.Resolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
            return new CustomObject { internalId = id };
        }
    }
}

// per field, member

public class Int_x10Formatter : IMessagePackFormatter<int>
{
    public int Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return reader.ReadInt32() * 10;
    }

    public void Serialize(ref MessagePackWriter writer, int value, MessagePackSerializerOptions options)
    {
        writer.WriteInt32(value * 10);
    }
}

[MessagePackObject]
public class MyClass
{
    // You can attach custom formatter per member.
    [Key(0)]
    [MessagePackFormatter(typeof(Int_x10Formatter))]
    public int MyProperty1 { get; set; }
}
```

Formatter is retrieved by `AttributeFormatterResolver`, it is included in `StandardResolver`.

## IgnoreFormatter

`IgnoreFormatter<T>` is lightweight extension point of class and struct, if exists can't serializable type in external type, you can register `IgnoreFormatter<T>` that serialize to nil.

```csharp
// CompositeResolver can set custom formatter.
var resolver = MessagePack.Resolvers.CompositeResolver.Create(
    new IMessagePackFormatter[]
    {
        // for example, register reflection infos(can not serialize in default)
        new IgnoreFormatter<MethodBase>(),
        new IgnoreFormatter<MethodInfo>(),
        new IgnoreFormatter<PropertyInfo>(),
        new IgnoreFormatter<FieldInfo>()
    },
    new IFormatterResolver[]
    {
        ContractlessStandardResolver.Instance
    });
```

## Reserved Extension Types

MessagePack for C# already used some messagepack ext type codes, be careful to use same ext code.

| Code | Type | Use by |
| ---  | ---  | --- |
| -1 | DateTime | msgpack-spec reserved for timestamp |
| 30 | Vector2[] | for Unity, UnsafeBlitFormatter |
| 31 | Vector3[] | for Unity, UnsafeBlitFormatter |
| 32 | Vector4[] | for Unity, UnsafeBlitFormatter |
| 33 | Quaternion[] | for Unity, UnsafeBlitFormatter |
| 34 | Color[] | for Unity, UnsafeBlitFormatter |
| 35 | Bounds[] | for Unity, UnsafeBlitFormatter |
| 36 | Rect[] | for Unity, UnsafeBlitFormatter |
| 37 | Int[] | for Unity, UnsafeBlitFormatter |
| 38 | Float[] | for Unity, UnsafeBlitFormatter |
| 39 | Double[] | for Unity, UnsafeBlitFormatter |
| 99 | All | LZ4MessagePackSerializer |
| 100 | object | TypelessFormatter |

## Unity support

Unity lower support version is `2018.3`, Api Compatibility Level is supporrt both `.NET 4.x` and `.NET Standard 2.0`.

You can install by unitypackage in [releases](https://github.com/neuecc/MessagePack-CSharp/releases/) page. If build target as PC, you can use as is but if build target uses IL2CPP, you can not use `Dynamic***Resolver` so require to use pre-code generation. Please see [pre-code generation section](#aot).

Currently `CompositeResolver.Create` can not work on IL2CPP so recommend to use `StaticCompositeResolver.Instance.Register` instead.

In Unity, MessagePackSerializer can serialize `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`, `Bounds`, `Rect`, `AnimationCurve`, `Keyframe`, `Matrix4x4`, `Gradient`, `Color32`, `RectOffset`, `LayerMask`, `Vector2Int`, `Vector3Int`, `RangeInt`, `RectInt`, `BoundsInt` and there nullable, there array, there list by built-in extension `UnityResolver`. It is included StandardResolver by default.

MessagePack for C# has additional unsafe extension.  `UnsafeBlitResolver` is special resolver for extremely fast unsafe serialization/deserialization for struct array.

![image](https://cloud.githubusercontent.com/assets/46207/23837633/76589924-07ce-11e7-8b26-e50eab548938.png)

x20 faster Vector3[] serialization than native JsonUtility. If use `UnsafeBlitResolver`, serialize special format(ext:typecode 30~39)  `Vector2[]`, `Vector3[]`, `Quaternion[]`, `Color[]`, `Bounds[]`, `Rect[]`. If use `UnityBlitWithPrimitiveArrayResolver`, supports `int[]`, `float[]`, `double[]` too. This special feature is useful for serialize Mesh(many `Vector3[]`) or many transform position.

If you want to use unsafe resolver, register `UnityBlitResolver` or `UnityBlitWithPrimitiveArrayResolver`.

Here is sample of configuration.

```csharp
StaticCompositeResolver.Instance.Register(
    MessagePack.Unity.UnityResolver.Instance,
    MessagePack.Unity.Extension.UnityBlitWithPrimitiveArrayResolver.Instance,
    MessagePack.Resolvers.StandardResolver.Instance,
);

var options = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);
MessagePackSerializer.DefaultOptions = options;
```

`MessagePack.UnityShims` NuGet package is for .NET ServerSide serialization support to communicate with Unity. It includes shim of Vector3 etc and Safe/Unsafe serialization extension.

If you want to share class between Unity and Server, you can use `SharedProject` or `Reference as Link` or glob reference(with `LinkBase`) etc. Anyway you need to source-code level share. This is sample project structure of use glob reference(recommended).

- ServerProject(.NET 4.6/.NET Core/.NET Standard)
  - [`<Compile Include="..\UnityProject\Assets\Scripts\Shared\**\*.cs" LinkBase="Shared" />`]
  - [MessagePack]
  - [MessagePack.UnityShims]
- UnityProject
  - [Concrete SharedCodes]
  - [MessagePack](not dll/NuGet, use MessagePack.Unity.unitypackage's sourcecodes)

## <a name="aot"></a>AOT Code Generation (to support Unity/Xamarin)

By default, MessagePack for C# serializes custom objects by using [generating IL](https://msdn.microsoft.com/en-us/library/system.reflection.emit.ilgenerator.aspx) at runtime for custom, highly tuned formatters for each type. This code generation has a minor upfront perf cost.
Because strict-AOT environments such as Xamarin and Unity IL2CPP forbid runtime code generation, MessagePack provides a way for you to run a code generator ahead of time as well.

> Note: When Unity targets the PC it allows dynamic code generation, so AOT is not required.

If you want to avoid the upfront dynamic generation cost or you need to run on Xamarin or Unity, you need AOT code generation. `mpc` (MessagePackCompiler) is the code generator of MessagePack for C#. mpc uses [Roslyn](https://github.com/dotnet/roslyn) to analyze source code.

The easiest way to acquire and run mpc is as a dotnet tool.

```
dotnet tool install --global MasterMemory.Generator
```

Installing it as a local tool allows you to include the tools and versions that you use in your source control system. Run these commands in the root of your repo:

```
dotnet new tool-manifest
dotnet tool install MessagePack.Generator
```

Check in your `.config\dotnet-tools.json` file. On another machine you can "restore" your tool using the `dotnet tool restore` command.

Once you have the tool installed, simply invoke using `dotnet mpc` within your repo:

```
dotnet mpc -h
```

Alternatively, you can download mpc from the [releases](https://github.com/neuecc/MessagePack-CSharp/releases/) page, that includes platform native binaries(don't require dotnet runtime).

```
argument list:
-i, -input: Input path of analyze csproj or directory, if input multiple csproj split with ','.
-o, -output: Output file path(.cs) or directory(multiple generate file).
-c, -conditionalSymbol: [default=null]Conditional compiler symbols, split with ','.
-r, -resolverName: [default=GeneratedResolver]Set resolver name.
-n, -namespace: [default=MessagePack]Set namespace root name.
-m, -useMapMode: [default=False]Force use map mode serialization.
-ms, -multipleIfDirectiveOutputSymbols: [default=null]Generate #if-- files by symbols, split with ','.
```

```cmd
// Simple Sample:
mpc.exe -i "..\src\Sandbox.Shared.csproj" -o "MessagePackGenerated.cs"

// Use force map simulate DynamicContractlessObjectResolver
mpc.exe -i "..\src\Sandbox.Shared.csproj" -o "MessagePackGenerated.cs" -m
```

By default, `mpc.exe` generates resolver to `MessagePack.Resolvers.GeneratedResolver` and formatters generates to `MessagePack.Formatters.***`.

```csharp
// Do this once
StaticCompositeResolver.Instance.Register(new IFormatterResolver[]
{
    MessagePack.Resolvers.GeneratedResolver.Instance,
    MessagePack.Resolvers.StandardResolver.Instance,
});

// Store it for reuse.
var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

// Each time you serialize/deserialize, specify the options:
byte[] msgpackBytes = MessagePackSerializer.Serialize(myObject, options);
T myObject2 = MessagePackSerializer.Deserialize<MyObject>(msgpackBytes, options);

// or set as application default.
MessagePackSerializer.DefaultOptions = options;
```

In Unity, you can use MessagePack CodeGen windows at `Windows -> MessagePack -> CodeGenerator`.

![](https://user-images.githubusercontent.com/46207/69414381-f14da400-0d55-11ea-9f8d-9af448d347dc.png)

It helps install .NET Core runtime, install mpc(as Global Tools), execute mpc. Currently this tool is experimental so please tell me your opinion.

For Xamarin, you can use `MessagePack.MSBuild.Tasks` that is same as mpc but you can use in csproj easily.

```xml
<ItemGroup>
    <!-- Install MSBuild Task(with PrivateAssets="All", it means to use dependency only in build time). -->
    <PackageReference Include="MessagePack.MSBuild.Tasks" Version="*" PrivateAssets="All" />
</ItemGroup>

<!-- Call code generator before-build. -->
<Target Name="MessagePackGen" BeforeTargets="BeforeBuild">
    <!-- Configuration of Code-Generator -->
    <MessagePackGenerator Input="$(ProjectPath)" Output="$(ProjectDir)MessagePack" />
</Target>
```

MSBuild Task's configuration list is here.

```xml
<MessagePackGenerator
    Input="string:required"
    Output="string:required"
    ConditionalSymbol="string:optional"
    ResolverName="string:optional"
    Namespace="string:optional"
    UseMapMode="bool:optional"
    MultipleIfDirectiveOutputSymbols="string:optional"
/>
```

## RPC

MessagePack advocated [MessagePack RPC](https://github.com/msgpack-rpc/msgpack-rpc), but formulation is stopped and it is not widely used.

### MagicOnion

I've created gRPC based MessagePack HTTP/2 RPC streaming framework called [MagicOnion](https://github.com/Cysharp/MagicOnion). gRPC usually communicates with Protocol Buffers using IDL. But MagicOnion uses MessagePack for C# and does not needs IDL. If communicates C# to C#, schemaless(C# classes as schema) is better than IDL.

### StreamJsonRpc

The StreamJsonRpc library is based on [JSON-RPC](https://www.jsonrpc.org/) and includes [a pluggable formatter architecture](https://github.com/microsoft/vs-streamjsonrpc/blob/master/doc/extensibility.md#alternative-formatters) and includes [a sample MessagePack plugin](https://github.com/microsoft/vs-streamjsonrpc/blob/master/src/StreamJsonRpc.Tests/MessagePackFormatter.cs).

## How to build

See our [contributor's guide](CONTRIBUTING.md).

## Author Info

Yoshifumi Kawai(a.k.a. neuecc) is a software developer in Japan.
He is the Director/CTO at Grani, Inc.
Grani is a mobile game developer company in Japan and well known for using C#.
He is awarding Microsoft MVP for Visual C# since 2011.
He is known as the creator of [UniRx](http://github.com/neuecc/UniRx/) (Reactive Extensions for Unity)

* Blog: [https://medium.com/@neuecc](https://medium.com/@neuecc) (English)
* Blog: [http://neue.cc/](http://neue.cc/) (Japanese)
* Twitter: [https://twitter.com/neuecc](https://twitter.com/neuecc) (Japanese)

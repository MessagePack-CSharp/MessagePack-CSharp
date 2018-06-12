MessagePack for C# (.NET, .NET Core, Unity, Xamarin)
===
[![AppVeyor](https://img.shields.io/appveyor/ci/neuecc/messagepack-csharp/master.svg?label=appveyor)](https://ci.appveyor.com/project/neuecc/messagepack-csharp/branch/master) 
[![Join the chat at https://gitter.im/MessagePack-CSharp/Lobby](https://badges.gitter.im/MessagePack-CSharp/Lobby.svg)](https://gitter.im/MessagePack-CSharp/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![Releases](https://img.shields.io/github/release/neuecc/MessagePack-CSharp.svg)](https://github.com/neuecc/MessagePack-CSharp/releases)

The extremely fast [MessagePack](http://msgpack.org/) serializer for C#. It is 10x faster than [MsgPack-Cli](https://github.com/msgpack/msgpack-cli) and outperforms other C# serializers. MessagePack for C# also ships with built-in support for LZ4 compression - an extremely fast compression algorithm. Performance is important, particularly in applications like game development, distributed computing, microservice architecture, and caching.

![image](https://cloud.githubusercontent.com/assets/46207/23835716/89c8ab08-07af-11e7-9183-9e9415bdc87f.png)

MessagePack has compact binary size and full set of general purpose expression. Please see the [comparison with JSON, protobuf, ZeroFormatter section](https://github.com/neuecc/MessagePack-CSharp#compare-with-protobuf-json-zeroformatter). If you want to know why MessagePack C# is fastest, please see [performance section](https://github.com/neuecc/MessagePack-CSharp#performance).

Installation
---
The library provides in NuGet except for Unity.

Standard library for .NET Framework 4.5, .NET Standard 1.6 and .NET Standard 2.0(.NET Core, Xamarin).

```
Install-Package MessagePack
```

Visual Studio Analyzer to help object definition

```
Install-Package MessagePackAnalyzer
```

Extension Packages(info is see [extensions section](https://github.com/neuecc/MessagePack-CSharp#extensions)).

```
Install-Package MessagePack.ImmutableCollection
Install-Package MessagePack.ReactiveProperty
Install-Package MessagePack.UnityShims
Install-Package MessagePack.AspNetCoreMvcFormatter
```

for Unity, download from [releases](https://github.com/neuecc/MessagePack-CSharp/releases) page, providing `.unitypackage`. Unity IL2CPP or Xamarin AOT Environment, check the [pre-code generation section](https://github.com/neuecc/MessagePack-CSharp#pre-code-generationunityxamarin-supports).

Quick Start
---
Define class and mark as `[MessagePackObject]` and public members(property or field) mark as `[Key]`, call `MessagePackSerializer.Serialize<T>/Deserialize<T>`. `ToJson` helps dump binary.

```csharp
// mark MessagePackObjectAttribute
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
        var bytes = MessagePackSerializer.Serialize(mc);
        var mc2 = MessagePackSerializer.Deserialize<MyClass>(bytes);

        // you can dump msgpack binary to human readable json.
        // In default, MeesagePack for C# reduce property name information.
        // [99,"hoge","huga"]
        var json = MessagePackSerializer.ToJson(bytes);
        Console.WriteLine(json);
    }
}
```

By default the attribute is required. Optionally it can be unnecessary, see [Object Serialization section](https://github.com/neuecc/MessagePack-CSharp#object-serialization) and [Formatter Resolver section](https://github.com/neuecc/MessagePack-CSharp#extension-pointiformatterresolver) for details.

Analyzer
---
MessagePackAnalyzer helps object definition. Attributes, accessibility etc are detected and it becomes a compiler error.

![analyzergif](https://cloud.githubusercontent.com/assets/46207/23837445/ce734eae-07cb-11e7-9758-d69f0f095bc1.gif)

If you want to allow a specific type (for example, when registering a custom type), put `MessagePackAnalyzer.json` at the project root and make the Build Action to `AdditionalFiles`.

![image](https://cloud.githubusercontent.com/assets/46207/23837427/8a8d507c-07cb-11e7-9277-5a566eb0bfde.png)

This is a sample of the contents of MessagePackAnalyzer.json. 

```
[ "MyNamespace.FooClass", "MyNameSpace.BarStruct" ]
```

Built-in support types
---
These types can serialize by default.

Primitives(`int`, `string`, etc...), `Enum`, `Nullable<>`,  `TimeSpan`,  `DateTime`, `DateTimeOffset`, `Nil`, `Guid`, `Uri`, `Version`, `StringBuilder`, `BitArray`, `ArraySegment<>`, `BigInteger`, `Complext`, `Task`, `Array[]`, `Array[,]`, `Array[,,]`, `Array[,,,]`, `KeyValuePair<,>`, `Tuple<,...>`, `ValueTuple<,...>`, `List<>`, `LinkedList<>`, `Queue<>`, `Stack<>`, `HashSet<>`, `ReadOnlyCollection<>`, `IList<>`, `ICollection<>`, `IEnumerable<>`, `Dictionary<,>`, `IDictionary<,>`, `SortedDictionary<,>`, `SortedList<,>`, `ILookup<,>`, `IGrouping<,>`, `ObservableCollection<>`, `ReadOnlyOnservableCollection<>`, `IReadOnlyList<>`, `IReadOnlyCollection<>`, `ISet<>`, `ConcurrentBag<>`, `ConcurrentQueue<>`, `ConcurrentStack<>`, `ReadOnlyDictionary<,>`, `IReadOnlyDictionary<,>`, `ConcurrentDictionary<,>`, `Lazy<>`, `Task<>`, custom inherited `ICollection<>` or `IDictionary<,>` with paramterless constructor, `IList`, `IDictionary` and custom inherited `ICollection` or `IDictionary` with paramterless constructor(includes `ArrayList` and `Hashtable`).

You can add custom type support and has some official/third-party extension package. for ImmutableCollections(`ImmutableList<>`, etc), for ReactiveProperty and for Unity(`Vector3`, `Quaternion`, etc...), for F#(Record, FsList, Discriminated Unions, etc...). Please see [extensions section](https://github.com/neuecc/MessagePack-CSharp#extensions).

`MessagePack.Nil` is built-in null/void/unit representation type of MessagePack for C#.

Object Serialization
---
MessagePack for C# can serialze your own public `Class` or `Struct`. Serialization target must marks `[MessagePackObject]` and `[Key]`. Key type can choose int or string. If key type is int, serialized format is used array. If key type is string, serialized format is used map. If you define `[MessagePackObject(keyAsPropertyName: true)]`, does not require `KeyAttribute`.

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

Resolver is key customize point of MessagePack for C#. Details, please see [extension point](https://github.com/neuecc/MessagePack-CSharp#extension-pointiformatterresolver).

DataContract compatibility
---
You can use `[DataContract]` instead of `[MessagePackObject]`. If type is marked DataContract, you can use `[DataMember]` instead of `[Key]` and `[IgnoreDataMember]` instead of `[IgnoreMember]`.

`[DataMember(Order = int)]` is same as `[Key(int)]`, `[DataMember(Name = string)]` is same as `[Key(string)]`. If use `[DataMember]`, same as `[Key(nameof(propertyname)]`.

Using DataContract makes it a shared class library and you do not have to refer to MessagePack for C#. However, it is not included in analysis by Analyzer or code generation by `mpc.exe`. Also, functions like `UnionAttribute`, `MessagePackFormatterAttribute`, `SerializationConstructorAttribute` etc can not be used. For this reason, I recommend that you use the MessagePack for C# attribute basically.

Serialize ImmutableObject(SerializationConstructor)
---
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

MessagePackSerializer choose constructor with the least matched argument, match index if key in integer or match name(ignore case) if key is string. If encounts `MessagePackDynamicObjectResolverException: can't find matched constructor parameter` you should check about this.

If can not match automatically, you can specify to use constructor manually by `[SerializationConstructorAttribute]`.

```csharp
[MessagePackObject]
public struct Point
{
    [Key(0)]
    public readonly int X;
    [Key(1)]
    public readonly int Y;

    // If not marked attribute, used this(least matched argument)
    public Point(int x)
    {
        X = x;
    }

    [SerializationConstructor]
    public Point(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }
}
```

Serialization Callback
---
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

Union
---
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

Dynamic(Untyped) Deserialization
---
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

Object Type Serialization
---
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

Typeless
---
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
MessagePack.Resolvers.CompositeResolver.RegisterAndSetAsDefault(
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

Performance
---
Benchmarks comparing to other serializers run on `Windows 10 Pro x64 Intel Core i7-6700K 4.00GHz, 32GB RAM`. Benchmark code is [here](https://github.com/neuecc/ZeroFormatter/tree/master/sandbox/PerformanceComparison) - and there [version info](https://github.com/neuecc/ZeroFormatter/blob/bc63cb925d/sandbox/PerformanceComparison/packages.config), ZeroFormatter and [FlatBuffers](https://google.github.io/flatbuffers/) has infinitely fast deserializer so ignore deserialize performance.

![image](https://cloud.githubusercontent.com/assets/46207/23835765/55fe494e-07b0-11e7-98be-5e7a9411da40.png)

 MessagePack for C# uses many techniques for improve performance.

* Serializer uses only ref byte[] and int offset, don't use (Memory)Stream(call Stream api has overhead)
* High-level API uses internal memory pool, don't allocate working memory under 64K
* Don't create intermediate utility instance(XxxWriter/Reader, XxxContext, etc...)
* Avoid boxing all codes, all platforms(include Unity/IL2CPP)
* Getting cached generated formatter on static generic field(don't use dictinary-cache because dictionary lookup is overhead): [see:Resolvers](https://github.com/neuecc/MessagePack-CSharp/tree/209f301e2e595ed366408624011ba2e856d23429/src/MessagePack/Resolvers)
* Heavyly tuned dynamic il code generation: [see:DynamicObjectTypeBuilder](https://github.com/neuecc/MessagePack-CSharp/blob/209f301e2e595ed366408624011ba2e856d23429/src/MessagePack/Resolvers/DynamicObjectResolver.cs#L142-L754)
* Call PrimitiveAPI directly when il code generation knows target is primitive
* Reduce branch of variable length format when il code generation knows target(integer/string) range
* Don't use `IEnumerable<T>` abstraction on iterate collection, [see:CollectionFormatterBase](https://github.com/neuecc/MessagePack-CSharp/blob/209f301e2e595ed366408624011ba2e856d23429/src/MessagePack/Formatters/CollectionFormatter.cs#L192-L355) and inherited collection formatters
* Uses pre generated lookup table to reduce check messagepack type, [see: MessagePackBinary](https://github.com/neuecc/MessagePack-CSharp/blob/209f301e2e595ed366408624011ba2e856d23429/src/MessagePack/MessagePackBinary.cs#L15-L212)
* Uses optimized type key dictionary for non-generic methods, [see: ThreadsafeTypeKeyHashTable](https://github.com/neuecc/MessagePack-CSharp/blob/91312921cb7fe987f48336768c898a76ac7dbb40/src/MessagePack/Internal/ThreadsafeTypeKeyHashTable.cs)
* Avoid string key decode for lookup map(string key) key and uses automata based name lookup with il inlining code generation, see: [AutomataDictionary](https://github.com/neuecc/MessagePack-CSharp/blob/bcedbce3fd98cb294210d6b4a22bdc4c75ccd916/src/MessagePack/Internal/AutomataDictionary.cs)
* For string key encode, pre-generated member name bytes and use fixed sized binary copy in IL, see: [UnsafeMemory.cs](https://github.com/neuecc/MessagePack-CSharp/blob/f17ddc5d107d3a2f66f60398b214ef87919ff892/src/MessagePack/Internal/UnsafeMemory.cs)

Before creating this library, I implemented a fast fast serializer with [ZeroFormatter#Performance](https://github.com/neuecc/ZeroFormatter#performance). And this is a further evolved implementation. MessagePack for C# is always fast, optimized for all types(primitive, small struct, large object, any collections).

Deserialize Performance per options
---
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

LZ4 Compression
---
MessagePack is a fast and *compact* format but it is not compression. [LZ4](https://github.com/lz4/lz4) is extremely fast compression algorithm, with MessagePack for C# can achive extremely fast perfrormance and extremely compact binary size!

MessagePack for C# has built-in LZ4 support. You can use `LZ4MessagePackSerializer` instead of `MessagePackSerializer`. Builtin support is special, I've created serialize-compression pipeline and special tuned for the pipeline so share the working memory, don't allocate, don't resize until finished.

Serialized binary is not simply compressed lz4 binary. Serialized binary is valid MessagePack binary used ext-format and custom typecode(99).

```csharp
var array= Enumerable.Range(1, 100).Select(x => new MyClass { Age = 5, FirstName = "foo", LastName = "bar" }).ToArray();

// call LZ4MessagePackSerializer instead of MessagePackSerializer, api is completely same
var lz4Bytes = LZ4MessagePackSerializer.Serialize(array);
var mc2 = LZ4MessagePackSerializer.Deserialize<MyClass[]>(lz4Bytes);

// you can dump lz4 message pack
// [[5,"hoge","huga"],[5,"hoge","huga"],....]
var json = LZ4MessagePackSerializer.ToJson(lz4Bytes);
Console.WriteLine(json);

// lz4Bytes is valid MessagePack, it is using ext-format( [TypeCode:99, SourceLength|CompressedBinary] )
// [99,"0gAAA+vf3ABkkwWjZm9vo2JhcgoA////yVBvo2Jhcg=="]
var rawJson = MessagePackSerializer.ToJson(lz4Bytes);
Console.WriteLine(rawJson);
```

built-in LZ4 support uses primitive LZ4 API. The LZ4 API is more efficient if you know the size of original source length. Therefore, size is written on the top.

> Compression speed is not always fast. Depending on the target binary, it may be short or longer. However, even at worst, it is about twice, but it is still often faster than other uncompressed serializers.

If target binary size under 64 bytes, LZ4MessagePackSerializer does not compress to optimize small size serialization.

Compare with protobuf, JSON, ZeroFormatter
---
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

Extensions
---
MessagePack for C# has extension point and you can add external type's serialization support. There are official extension support.

```
Install-Package MessagePack.ImmutableCollection
Install-Package MessagePack.ReactiveProperty
Install-Package MessagePack.UnityShims
Install-Package MessagePack.AspNetCoreMvcFormatter
```

`MessagePack.ImmutableCollection` package add support for [System.Collections.Immutable](https://www.nuget.org/packages/System.Collections.Immutable/) library. It adds `ImmutableArray<>`, `ImmutableList<>`, `ImmutableDictionary<,>`, `ImmutableHashSet<>`, `ImmutableSortedDictionary<,>`, `ImmutableSortedSet<>`, `ImmutableQueue<>`, `ImmutableStack<>`, `IImmutableList<>`, `IImmutableDictionary<,>`, `IImmutableQueue<>`, `IImmutableSet<>`, `IImmutableStack<>` serialization support.

`MessagePack.ReactiveProperty` package add support for [ReactiveProperty](https://github.com/runceel/ReactiveProperty) library. It adds `ReactiveProperty<>`, `IReactiveProperty<>`, `IReadOnlyReactiveProperty<>`, `ReactiveCollection<>`, `Unit` serialization support. It is useful for save viewmodel state.

`MessagePack.UnityShims` package provides shim of [Unity](https://unity3d.com/)'s standard struct(`Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`, `Bounds`, `Rect`, `AnimationCurve`, `Keyframe`, `Matrix4x4`, `Gradient`, `Color32`, `RectOffset`, `LayerMask`, `Vector2Int`, `Vector3Int`, `RangeInt`, `RectInt`, `BoundsInt`) and there formatter. It can enable to commnicate between server and Unity client.

After install, extension package must enable by configuration. Here is sample of enable all extension.

```csharp
// set extensions to default resolver.
MessagePack.Resolvers.CompositeResolver.RegisterAndSetAsDefault(
    // enable extension packages first
    ImmutableCollectionResolver.Instance,
    ReactivePropertyResolver.Instance,
    MessagePack.Unity.Extension.UnityBlitResolver.Instance,
    MessagePack.Unity.UnityResolver.Instance,

    // finaly use standard(default) resolver
    StandardResolver.Instance);
);
```

Configuration details, see:[Extension Point section](https://github.com/neuecc/MessagePack-CSharp#extension-pointiformatterresolver).

`MessagePack.AspNetCoreMvcFormatter` is add-on of [ASP.NET Core MVC](https://github.com/aspnet/Mvc)'s serialization to boostup performance. This is configuration sample.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc().AddMvcOptions(option =>
    {
        option.OutputFormatters.Clear();
        option.OutputFormatters.Add(new MessagePackOutputFormatter(ContractlessStandardResolver.Instance));
        option.InputFormatters.Clear();
        option.InputFormatters.Add(new MessagePackInputFormatter(ContractlessStandardResolver.Instance));
    });
}
```

Author is creating other extension packages, too.

* [MasterMemory](https://github.com/neuecc/MasterMemory) - Embedded Readonly In-Memory Document Database
* [MagicOnion](https://github.com/neuecc/MagicOnion) - gRPC based HTTP/2 RPC Streaming Framework
* [DatadogSharp](https://github.com/neuecc/DatadogSharp) - C# Datadog client

You can make your own extension serializers or integrate with framework, let's create them and share it!

* [MessagePack.FSharpExtensions](https://github.com/pocketberserker/MessagePack.FSharpExtensions) - supports F# list,set,map,unit,option,discriminated union
* [MessagePack.NodaTime](https://github.com/ARKlab/MessagePack) - 
Support for NodaTime types to MessagePack C#
* [WebApiContrib.Core.Formatter.MessagePack](https://github.com/WebApiContrib/WebAPIContrib.Core#formatters) - supports ASP.NET Core MVC([details in blog post](https://www.strathweb.com/2017/06/using-messagepack-with-asp-net-core-mvc/))
* [MessagePack.MediaTypeFormatter](https://github.com/sketch7/MessagePack.MediaTypeFormatter) - MessagePack MediaTypeFormatter

High-Level API(MessagePackSerializer)
---
`MessagePackSerializer` is the entry point of MessagePack for C#. Its static methods are main API of MessagePack for C#.

| API | Description |
| --- | --- |
| `DefaultResolver` | FormatterResolver that used resolver less overloads. If does not set it, used StandardResolver. |
| `SetDefaultResolver` | Set default resolver of MessagePackSerializer APIs. |
| `Serialize<T>` | Convert object to byte[] or write to stream. There has IFormatterResolver overload, used specified resolver. |
| `SerializeUnsafe<T>` | Same as `Serialize<T>` but return `ArraySegement<byte>`. The result of ArraySegment is contains internal buffer pool, it can not share across thread and can not hold, so use quickly. |
| `Deserialize<T>` | Convert byte[] or `ArraySegment<byte>` or stream to object. There has IFormatterResolver overload, used specified resolver. |
| `NonGeneric.*` | NonGeneric APIs of Serialize/Deserialize. There accept type parameter at first argument. This API is bit slower than generic API but useful for framework integration such as ASP.NET formatter. |
| `Typeless.*` | Typeless APIs of Serialize/Deserialize. This API no needs type parameter like BinaryFormatter. This API makes .NET specific binary and bit slower than standard APIs. |
| `ToJson` | Dump message-pack binary to JSON string. It is useful for debugging.  |
| `FromJson` | From Json string to MessagePack binary.  |
| `ToLZ4Binary` | (LZ4 only)Convert msgpack binary to LZ4 msgpack binary. |
| `Decode` | (LZ4 only)Convert LZ4 msgpack binary to standard msgpack binary. |

MessagePack for C# operates at the byte[] level, so byte[] API is faster than Stream API. If byte [] can be used for I/O, I recommend using the byte [] API.

`Deserialize<T>(Stream)` has `bool readStrict` overload. It means read byte[] from stream strictly size. The default is false, it reads all stream data, it is faster than `readStrict` but if the data is contiguous, you can use readStrict = true.

High-Level API uses memory pool internaly to avoid unnecessary memory allocation. If result size is under 64K, allocates GC memory only for the return bytes.

`LZ4MessagePackSerializer` has same api with `MessagePackSerializer` and `DefaultResolver` is shared. `LZ4MessagePackSerializer` has additional `SerializeToBlock` method.

Low-Level API(IMessagePackFormatter)
---
IMessagePackFormatter is serializer by each type. For example `Int32Formatter : IMessagePackFormatter<Int32>` represents Int32 MessagePack serializer.

```csharp
public interface IMessagePackFormatter<T>
{
    int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver);
    T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);
}
```

All api works on byte[] level, no use Stream, no use Writer/Reader so improve performance. Many builtin formatters exists under `MessagePack.Formatters`. You can get sub type serializer by `formatterResolver.GetFormatter<T>`. Here is sample of write own formatter.

```csharp
// serialize fileinfo as string fullpath.
public class FileInfoFormatter<T> : IMessagePackFormatter<FileInfo>
{
    public int Serialize(ref byte[] bytes, int offset, FileInfo value, IFormatterResolver formatterResolver)
    {
        if (value == null)
        {
            return MessagePackBinary.WriteNil(ref bytes, offset);
        }

        return MessagePackBinary.WriteString(ref bytes, offset, value.FullName);
    }

    public FileInfo Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
    {
        if (MessagePackBinary.IsNil(bytes, offset))
        {
            readSize = 1;
            return null;
        }

        var path = MessagePackBinary.ReadString(bytes, offset, out readSize);
        return new FileInfo(path);
    }
}
```

Created formatter needs to register to `IFormatterResolver`. Please see [Extension Point section](https://github.com/neuecc/MessagePack-CSharp#extension-pointiformatterresolver).

You can see many other samples from [builtin formatters](https://github.com/neuecc/MessagePack-CSharp/tree/master/src/MessagePack/Formatters).

Primitive API(MessagePackBinary)
---

`MessagePackBinary` is most low-level API like `Reader/Writer` of other serializers. `MessagePackBinary` is static class because avoid create `Reader/Writer` allocation.

| Method | Description |
| --- | --- |
| ReadNext | Skip MessagePackFormat binary block, returns read size. |
| ReadNextBlock | Skip MessagePackFormat binary block with sub structures(array/map), returns read size. This is useful for create deserializer. |
| ReadMessageBlockFromStreamUnsafe | Read binary block from Stream, if readOnlySingleMessage = false then read sub structures(array/map). | 
| ReadStringSegment | Read string format but do not decode UTF8, returns `ArraySegment<byte>`. |
| ReadBytesSegment | Read binary format but do not copy bytes, returns `ArraySegment<byte>`. |
| Write/ReadMapHeader | Write/Read map format header(element length). |
| WriteMapHeaderForceMap32Block | Write map format header, always use map32 format(length is fixed, 5). |
| Write/ReadArrayHeader | Write/Read array format header(element length). |
| WriteArrayHeaderForceArray32Block | Write array format header, always use array32 format(length is fixed, 5). |
| Write/Read*** | *** is primitive type name(`Int32`, `Single`, `String`, etc...) |
| Write***Force***Block | *** is primitive integer name(`Byte`, `Int32`, `UInt64`, etc...), acquire strict block and write code |
| Write/ReadBytes | Write/Read byte[] to use bin format. |
| Write/ReadExtensionFormat | Write/Read ext format header(Length + TypeCode) and content byte[]. |
| Write/ReadExtensionFormatHeader | Write/Read ext format, header(Length + TypeCode) only. |
| WriteExtensionFormatHeaderForceExt32Block | Write ext format header, always use ext32 format(length is fixed, 6). |
| WriteRaw | Write msgpack block directly. |
| IsNil | Is TypeCode Nil? |
| GetMessagePackType | Return MessagePackType of target MessagePack bianary position. |
| GetExtensionFormatHeaderLength | Calculate extension formatter header length. |
| GetEncodedStringBytes | Get msgpack packed raw binary. |
| EnsureCapacity | Resize if byte can not fill.  |
| FastResize | Buffer.BlockCopy version of Array.Resize. |
| FastCloneWithResize | Same as FastResize but return copied byte[]. |

Read API returns deserialized primitive and read size. Write API returns write size and guranteed auto ensure ref byte[]. Write/Read API has `byte[]` overload and `Stream` overload, basically the byte[] API is faster.

DateTime is serialized to [MessagePack Timestamp format](https://github.com/msgpack/msgpack/blob/master/spec.md#formats-timestamp), it serialize/deserialize UTC and loses `Kind` info. If you use`NativeDateTimeResolver` serialized native DateTime binary format and it can keep `Kind` info but cannot communicate other platforms.

`MessagePackType` means [msgpack spec of source types](https://github.com/msgpack/msgpack/blob/master/spec.md#serialization-type-to-format-conversion).

`MessagePackCode` means [msgpack format of first byte](https://github.com/msgpack/msgpack/blob/master/spec.md#overview). Its static class has `ToMessagePackType` and `ToFormatName` utility methods.

`MessagePackRange` means Min-Max fix range of msgpack format.

Extension Point(IFormatterResolver)
---
`IFormatterResolver` is storage of typed serializers. Serializer api accepts resolver and can customize serialization.

| Resovler Name | Description |
| --- | --- |
| BuiltinResolver | Builtin primitive and standard classes resolver. It includes primitive(int, bool, string...) and there nullable, array and list. and some extra builtin types(Guid, Uri, BigInteger, etc...). |
| StandardResolver | Composited resolver. It resolves in the following order `builtin -> attribute -> dynamic enum -> dynamic generic -> dynamic union -> dynamic object -> dynamic object fallback`. This is the default of MessagePackSerializer. |
| ContractlessStandardResolver | Composited `StandardResolver`(except dynamic object fallback) -> `DynamicContractlessObjectResolver` -> `DynamicObjectTypeFallbackResolver`. It enables contractless serialization. |
| StandardResolverAllowPrivate | Same as StandardResolver but allow serialize/deserialize private members. |
| ContractlessStandardResolverAllowPrivate | Same as ContractlessStandardResolver but allow serialize/deserialize private members. |
| PrimitiveObjectResolver | MessagePack primitive object resolver. It is used fallback in `object` type and supports `bool`, `char`, `sbyte`, `byte`, `short`, `int`, `long`, `ushort`, `uint`, `ulong`, `float`, `double`, `DateTime`, `string`, `byte[]`, `ICollection`, `IDictionary`. |
| DynamicObjectTypeFallbackResolver | Serialize is used type in from `object` type, deserialize is used PrimitiveObjectResolver. |
| AttributeFormatterResolver | Get formatter from `[MessagePackFormatter]` attribute. |
| CompositeResolver | Singleton helper of setup custom resolvers. You can use `Register` or `RegisterAndSetAsDefault` API. |
| NativeDateTimeResolver | Serialize by .NET native DateTime binary format. |
| UnsafeBinaryResolver | Guid and Decimal serialize by binary representation. It is faster than standard(string) representation. |
| OldSpecResolver | str and bin serialize/deserialize follows old messagepack spec(use raw format) |
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

It is the only configuration point to assemble the resolver's priority. In most cases, it is sufficient to have one custom resolver globally. CompositeResolver will be its helper.

```csharp
// use global-singleton CompositeResolver.
// This method initialize CompositeResolver and set to default MessagePackSerializer
CompositeResolver.RegisterAndSetAsDefault(
    // resolver custom types first
    ImmutableCollectionResolver.Instance,
    ReactivePropertyResolver.Instance,
    MessagePack.Unity.Extension.UnityBlitResolver.Instance,
    MessagePack.Unity.UnityResolver.Instance,

    // finaly use standard resolver
    StandardResolver.Instance);
```

Here is sample of use DynamicEnumAsStringResolver with DynamicContractlessObjectResolver(It is JSON.NET-like lightweight setting.)

```csharp
// composite same as StandardResolver
CompositeResolver.RegisterAndSetAsDefault(
    MessagePack.Resolvers.BuiltinResolver.Instance,
    MessagePack.Resolvers.AttributeFormatterResolver.Instance,

    // replace enum resolver
    MessagePack.Resolvers.DynamicEnumAsStringResolver.Instance,

    MessagePack.Resolvers.DynamicGenericResolver.Instance,
    MessagePack.Resolvers.DynamicUnionResolver.Instance,
    MessagePack.Resolvers.DynamicObjectResolver.Instance,

    MessagePack.Resolvers.PrimitiveObjectResolver.Instance,

    // final fallback(last priority)
    MessagePack.Resolvers.DynamicContractlessObjectResolver.Instance
);
```

If you want to write custom composite resolver, you can write like following.

```csharp
public class CustomCompositeResolver : IFormatterResolver
{
    public static IFormatterResolver Instance = new CustomCompositeResolver();

    static readonly IFormatterResolver[] resolvers = new[]
    {
        // resolver custom types first
        ImmutableCollectionResolver.Instance,
        ReactivePropertyResolver.Instance,
        MessagePack.Unity.Extension.UnityBlitResolver.Instance,
        MessagePack.Unity.UnityResolver.Instance,
            
        // finaly use standard resolver
        StandardResolver.Instance
    };

    CustomCompositeResolver()
    {
    }

    public IMessagePackFormatter<T> GetFormatter<T>()
    {
        return FormatterCache<T>.formatter;
    }

    static class FormatterCache<T>
    {
        public static readonly IMessagePackFormatter<T> formatter;

        static FormatterCache()
        {
            foreach (var item in resolvers)
            {
                var f = item.GetFormatter<T>();
                if (f != null)
                {
                    formatter = f;
                    return;
                }
            }
        }
    }
}
```

`IFormatterResolver` can use per serialize/deserialize method.

```csharp
MessagePackSerializer.Serialize(data, CustomCompositeResolver.Instance);
```

If you want to make your extension package, you need to make formatter and resolver. `IMessagePackFormatter` accepts `IFormatterResolver` on every request of serialize/deserialize. You can get child-type serialize on resolver.`GetFormatterWithVerify<T>`.

Here is sample of own resolver.

```csharp
public class SampleCustomResolver : IFormatterResolver
{
    // Resolver should be singleton.
    public static IFormatterResolver Instance = new SampleCustomResolver();

    SampleCustomResolver()
    {
    }

    // GetFormatter<T>'s get cost should be minimized so use type cache.
    public IMessagePackFormatter<T> GetFormatter<T>()
    {
        return FormatterCache<T>.formatter;
    }

    static class FormatterCache<T>
    {
        public static readonly IMessagePackFormatter<T> formatter;

        // generic's static constructor should be minimized for reduce type generation size!
        // use outer helper method.
        static FormatterCache()
        {
            formatter = (IMessagePackFormatter<T>)SampleCustomResolverGetFormatterHelper.GetFormatter(typeof(T));
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

MessagePackFormatterAttribute
---
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
        public int Serialize(ref byte[] bytes, int offset, CustomObject value, IFormatterResolver formatterResolver)
        {
            return formatterResolver.GetFormatterWithVerify<string>().Serialize(ref bytes, offset, value.internalId, formatterResolver);
        }

        public CustomObject Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var id = formatterResolver.GetFormatterWithVerify<string>().Deserialize(bytes, offset, formatterResolver, out readSize);
            return new CustomObject { internalId = id };
        }
    }
}

// per field, member

public class Int_x10Formatter : IMessagePackFormatter<int>
{
    public int Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
    {
        return MessagePackBinary.ReadInt32(bytes, offset, out readSize) * 10;
    }

    public int Serialize(ref byte[] bytes, int offset, int value, IFormatterResolver formatterResolver)
    {
        return MessagePackBinary.WriteInt32(ref bytes, offset, value * 10);
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

IgnoreFormatter
---
`IgnoreFormatter<T>` is lightweight extension point of class and struct, if exists can't serializable type in external type, you can register `IgnoreFormatter<T>` that serialize to nil.

```csharp
// CompositeResolver can set custom formatter.
MessagePack.Resolvers.CompositeResolver.RegisterAndSetAsDefault(
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

Reserved Extension Types
---
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

for Unity
---
You can install by package and includes source code. If build target as PC, you can use as is but if build target uses IL2CPP, you can not use `Dynamic***Resolver` so use pre-code generation. Please see [pre-code generation section](https://github.com/neuecc/MessagePack-CSharp#pre-code-generationunityxamarin-supports).

In Unity, MessagePackSerializer can serialize `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`, `Bounds`, `Rect`, `AnimationCurve`, `Keyframe`, `Matrix4x4`, `Gradient`, `Color32`, `RectOffset`, `LayerMask`, `Vector2Int`, `Vector3Int`, `RangeInt`, `RectInt`, `BoundsInt` and there nullable, there array, there list by built-in extension `UnityResolver`. It is included StandardResolver by default.

MessagePack for C# has additional unsafe extension.  `UnsafeBlitResolver` is special resolver for extremely fast unsafe serialization/deserialization for struct array.

![image](https://cloud.githubusercontent.com/assets/46207/23837633/76589924-07ce-11e7-8b26-e50eab548938.png)

x20 faster Vector3[] serialization than native JsonUtility. If use `UnsafeBlitResolver`, serialize special format(ext:typecode 30~39)  `Vector2[]`, `Vector3[]`, `Quaternion[]`, `Color[]`, `Bounds[]`, `Rect[]`. If use `UnityBlitWithPrimitiveArrayResolver`, supports `int[]`, `float[]`, `double[]` too. This special feature is useful for serialize Mesh(many Vector3[]) or many transform position.

If you want to use unsafe resolver, you must enables unsafe option and define additional symbols. At first, write `-unsafe` on `smcs.rsp`, `gmcs.rsp` etc. And define `ENABLE_UNSAFE_MSGPACK` symbol.

![image](https://cloud.githubusercontent.com/assets/46207/23837456/fc01c828-07cb-11e7-92bf-f23eb2575115.png)

Here is sample of configuration.

```csharp
Resolvers.CompositeResolver.RegisterAndSetAsDefault(
    MessagePack.Unity.UnityResolver.Instance,
    MessagePack.Unity.Extension.UnityBlitWithPrimitiveArrayResolver.Instance,

    // If PC, use StandardResolver
    // MessagePack.Resolvers.StandardResolver.Instance,

    // If IL2CPP, Builtin + GeneratedResolver.
    // MessagePack.Resolvers.BuiltinResolver.Instance,
);
```

`MessagePack.UnityShims` NuGet package is for .NET ServerSide serialization support to communicate with Unity. It includes shim of Vector3 etc and Safe/Unsafe serialization extension.

If you want to share class between Unity and Server, you can use `SharedProject` or `Reference as Link` or new MSBuild(VS2017)'s wildcard reference etc. Anyway you need to source-code level share. This is sample project structure of use SharedProject.

- SharedProject(source code sharing)
  - Source codes of server-client shared
- ServerProject(.NET 4.6/.NET Core/.NET Standard)
  - [SharedProject]
  - [MessagePack]
  - [MessagePack.UnityShims]
- ClientDllProject(.NET 3.5)
  - [SharedProject]
  - [MessagePack](not dll, use MessagePack.unitypackage's sourcecodes)
- Unity
  - [Builded ClientDll]

Other ways, use plain POCO by `DataContract`/`DataMember` can use.

Pre Code Generation(Unity/Xamarin Supports)
---
MessagePack for C# generates object formatter dynamically by [ILGenerator](https://msdn.microsoft.com/en-us/library/system.reflection.emit.ilgenerator.aspx). It is fast and transparently generated at run time. But it needs generate cost at first time and it does not work on AOT environment(Xamarin, Unity IL2CPP, etc.).

> Note: If Unity's build target as PC, does not need code generation. It works well.

If you want to avoid generate cost or run on Xamarin or Unity, you need pre-code generation. `mpc.exe`(MessagePackCompiler) is code generator of MessagePack for C#. mpc can download from [releases](https://github.com/neuecc/MessagePack-CSharp/releases/) page, `mpc.zip`. mpc is using [Roslyn](https://github.com/dotnet/roslyn) so analyze source code.

```
mpc arguments help:
  -i, --input              [required]Input path of analyze csproj
  -o, --output             [required]Output file path
  -c, --conditionalsymbol  [optional, default=empty]conditional compiler symbol
  -r, --resolvername       [optional, default=GeneratedResolver]Set resolver name
  -n, --namespace          [optional, default=MessagePack]Set namespace root name
  -m, --usemapmode         [optional, default=false]Force use map mode serialization
```

```
// Simple Sample:
mpc.exe -i "..\src\Sandbox.Shared.csproj" -o "MessagePackGenerated.cs"

// Use force map simulate DynamicContractlessObjectResolver
mpc.exe -i "..\src\Sandbox.Shared.csproj" -o "MessagePackGenerated.cs" -m
```

If you create DLL by msbuild project, you can use Pre/Post build event.

```
<PropertyGroup>
    <PreBuildEvent>
        mpc.exe, here is useful for analyze/generate target is self project.
    </PreBuildEvent>
    <PostBuildEvent>
        mpc.exe, here is useful for analyze target is another project.
    </PostBuildEvent>
</PropertyGroup>
```

In default, `mpc.exe` generates resolver to `MessagePack.Resolvers.GeneratedResolver` and formatters generates to `MessagePack.Formatters.***`. And application launch, you need to set Resolver at first.

```csharp
// CompositeResolver is singleton helper for use custom resolver.
// Ofcourse you can also make custom resolver.
MessagePack.Resolvers.CompositeResolver.RegisterAndSetAsDefault(
    // use generated resolver first, and combine many other generated/custom resolvers
    MessagePack.Resolvers.GeneratedResolver.Instance,

    // finally, use builtin/primitive resolver(don't use StandardResolver, it includes dynamic generation)
    MessagePack.Resolvers.BuiltinResolver.Instance,
    AttributeFormatterResolver.Instance,
    MessagePack.Resolvers.PrimitiveObjectResolver.Instance
);
```

> Note: mpc.exe is basically run on only Windows. But you can run on [Mono](http://www.mono-project.com/), that supports Mac and Linux.

RPC
---
MessagePack advocated [MessagePack RPC](https://github.com/msgpack-rpc/msgpack-rpc), but formulation is stopped and it is not widely used. I've created gRPC based MessagePack HTTP/2 RPC streaming framework called [MagicOnion](https://github.com/neuecc/MagicOnion). gRPC usually communicates with Protocol Buffers using IDL. But MagicOnion uses MessagePack for C# and does not needs IDL. If communicates C# to C#, schemaless(C# classes as schema) is better than IDL.

How to Build
---
Open `MessagePack.sln` on Visual Studio 2017.

Unity Project is using symbolic link. At first, run `make_unity_symlink.bat` so linked under Unity project. You can open `src\MessagePack.UnityClient` on Unity Editor.

Author Info
---
Yoshifumi Kawai(a.k.a. neuecc) is a software developer in Japan.  
He is the Director/CTO at Grani, Inc.  
Grani is a mobile game developer company in Japan and well known for using C#.  
He is awarding Microsoft MVP for Visual C# since 2011.  
He is known as the creator of [UniRx](http://github.com/neuecc/UniRx/)(Reactive Extensions for Unity)  

Blog: [https://medium.com/@neuecc](https://medium.com/@neuecc) (English)  
Blog: [http://neue.cc/](http://neue.cc/) (Japanese)  
Twitter: [https://twitter.com/neuecc](https://twitter.com/neuecc) (Japanese)   

License
---
This library is under the MIT License.

lz4 compression support is using Milosz Krajewski's [lz4net](https://github.com/MiloszKrajewski/lz4net) code with some modified.

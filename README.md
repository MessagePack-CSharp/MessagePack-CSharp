MessagePack for C#(.NET, .NET Core, Unity, Xamarin)
===

> TODO:Writing document now.

Extremely fast [MessagePack](http://msgpack.org/) serializer for C#, x10~20 faster than MsgPack-Cli and acquires best performance compared with all the other C# serializers.

![image](https://cloud.githubusercontent.com/assets/46207/23487810/e263277a-ff2b-11e6-81a6-6b4ca7acd8e3.png)

![image](https://cloud.githubusercontent.com/assets/46207/23487813/e7b22e1a-ff2b-11e6-8eeb-386c6a305628.png)

MessagePack has compact binary size and full set of general purpose expression. Please see the comparison with JSON, protobuf, ZeroFormatter section. If you want to know why MessagePack C# is fastest, please see performance section.

Install
---
The library provides in NuGet except for Unity.

Standard library for .NET, .NET Core, Xamarin.

```
Install-Package MessagePack
```

Visual Studio Analyzer to help object definition

```
Install-Package MessagePackAnalyzer
```

Extension Packages(info is see extension section).

```
Install-Package MessagePack.ImmutableCollection
Install-Package MessagePack.ReactiveProperty
Install-Package MessagePack.UnityShims
```

for Unity, download from [releases](https://github.com/neuecc/MessagePack-CSharp/releases) page, providing `.unitypackage`. Unity IL2CPP or Xamarin AOT Environment, check the pre-code generation section.

Quick Start
---
Define class and mark as `[MessagePackObject]` and public members(property or field) mark `[Key]`, call `MessagePackSerializer.Serialize<T>/Deserialize<T>`. 

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
    }
}
```

By default the attribute is required. Optionally it can be unnecessary, see Object Serialization Format section and Formatter Resolver section for details.

Analyzer
---
MessagePackAnalyzer helps object definition. Attributes, accessibility etc are detected and it becomes a compiler error.

TODO:Replace images

![zeroformatteranalyzer](https://cloud.githubusercontent.com/assets/46207/20078766/3ea54f14-a585-11e6-9873-b99cb5d9efe5.gif)

If you want to allow a specific type (for example, when registering a custom type), put `MessagePackAnalyzer.json` at the project root and make the Build Action to `AdditionalFiles`.

![image](https://cloud.githubusercontent.com/assets/46207/20149311/0e6f73d6-a6f4-11e6-91cb-44c771c267cb.png)

This is a sample of the contents of MessagePackAnalyzer.json. 

```
[ "System.Uri" ]
```

Built-in support types
---
These types can serialize by default.

Primitives(`int`, `string`, etc...), `Enum`, `Nullable<>`,  `TimeSpan`,  `DateTime`, `DateTimeOffset`, `Nil`, `Guid`, `Uri`, `Version`, `StringBuilder`, `BitArray`, `ArraySegment<>`, `BigInteger`, `Complext`, `Task`, `Array[]`, `Array[,]`, `Array[,,]`, `Array[,,,]`, `KeyValuePair<,>`, `Tuple<,...>`, `ValueTuple<,...>`, `List<>`, `LinkedList<>`, `Queue<>`, `Stack<>`, `HashSet<>`, `ReadOnlyCollection<>`, `IList<>`, `ICollection<>`, `IEnumerable<>`, `Dictionary<,>`, `IDictionary<,>`, `SortedDictionary<,>`, `SortedList<,>`, `ILookup<,>`, `IGrouping<,>`, `ObservableCollection<>`, `ReadOnlyOnservableCollection<>`, `IReadOnlyList<>`, `IReadOnlyCollection<>`, `ISet<>`, `ConcurrentBag<>`, `ConcurrentQueue<>`, `ConcurrentStack<>`, `ReadOnlyDictionary<,>`, `IReadOnlyDictionary<,>`, `ConcurrentDictionary<,>`, `Lazy<>`, `Task<>` and custom inherited `ICollection<>` or `IDictionary<,>` with paramterless constructor.

You can add custom type support and has some official extension package. for ImmutableCollections(`ImmutableList<>`, etc), for ReactiveProperty and for Unity(`Vector3`, `Quaternion`, etc...). Please see extension section.

Object Serialization
---
TODO:

MessagePackObjectAttribute
KeyAttribute
IgnoreMemberAttribute
SerializationConstructorAttribute

Nil

Union
---
TODO:
UnionAttribute

Note:Versioning....


Performance
---
TODO:



Extensions
---
uses(see Extension Point section).


```
Install-Package MessagePack.ImmutableCollection
Install-Package MessagePack.ReactiveProperty
Install-Package MessagePack.UnityShims
```

Author is creating other extension packages, too.

* [MasterMemory](https://github.com/neuecc/MasterMemory) - Embedded Readonly In-Memory Document Database
* [MagicOnion](https://github.com/neuecc/MagicOnion) - gRPC based HTTP/2 RPC Streaming Framework

High-Level API(MessagePackSerializer)
---
This is the entry point of MessagePack for C#. `Serialize<T>` convert object to byte[] or write to Stream.

| API | Description |
| --- | --- |
| DefaultResolver | FormatterResolver that used resolver less overloads. If does not set it, used StandardResolver. |
| SetDefaultResolver | Set default resolver of MessagePackSerializer APIs. |
| `Serialize<T>` | Convert object to byte[] or write to stream. There has IFormatterResolver overload, used specified resolver. |
| `SerializeUnsafe<T>` | Same as `Serialize<T>` but return `ArraySegement<byte>`. The result of ArraySegment is contains internal buffer pool's, it can not share across thread and can not hold, so use quickly. |
| `Deserialize<T>` | Convert byte[] or stream to object. There has IFormatterResolver overload, used specified resolver. |

MessagePack for C# operates at the byte[] level, so byte[] API is faster than Stream API.

High-Level API uses memory pool internaly to avoid unnecessary memory allocation. If result size is under 64K, allocates GC memory only for the return bytes.

Low-Level API(IMessagePackFormatter)
---
IMessagePackFormatter is serializer by each type.  For example `Int32Formatter : IMessagePackFormatter<Int32>` represents Int32 MessagePack serializer.

```csharp
public interface IMessagePackFormatter<T>
{
    int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver);
    T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);
}
```

Many builtin formatters exists under `MessagePack.Formatters`.


TODO:write document more...


Primitive API(MessagePackBinary)
---
TODO:








Extension Point(IFormatterResolver)
---
TODO:


GetFormatter, GetFormatterWithVerify, GetFormatterDynamic 

| Resovler Name | Description |
| --- | --- |
| BuiltinResolver | Builtin primitive and standard classes resolver. It includes primitive(int, bool, string...) and there nullable, array and list. and some extra builtin types(Guid, Uri, BigInteger, etc...). |
| StandardResolver | Composited resolver . It resolves in the following order `builtin -> dynamic enum -> dynamic generic -> dynamic union -> dynamic object`. This is the default of MessagePackSerializer. |
| CompositeResolver | Singleton helper of setup custom resolvers. You can use `Register` or `RegisterAndSetAsDefault` API. |
| DynamicEnumResolver | Resolver of enum and there nullable. It uses dynamic code generation to avoid boxing and boostup performance. |
| DynamicGenericResolver | Resolver of generic type(`Tuple<>`, `List<>`, `Dictionary<,>`, `Array`, etc). It uses reflection call for resolve generic argument. |
| DynamicUnionResolver | Resolver of interface marked by UnionAttribute. It uses dynamic code generation to create dynamic formatter. |
| DynamicObjectResolver | Resolver of class and struct maked by MessagePackObjectAttribute. It uses dynamic code generation to create dynamic formatter. |
| DynamicContractlessObjectResolver | Resolver of all classes and structs. It does not needs MessagePackObjectAttribute and serialized key as string(same as marked [MessagePackObject(true)]). |






Pre Code Generation(Unity/Xamarin Supports)
---
MessagePack for C# generates object formatter dynamically by [ILGenerator](https://msdn.microsoft.com/en-us/library/system.reflection.emit.ilgenerator.aspx). It is fast and transparently generated at run time. But it needs generate cost at first time and it does not work on AOT environment(Xamarin, Unity IL2CPP, etc.).

> Note: If Unity's build target as PC, does not need code generation. It works well.

If you want to avoid generate cost or run on Xamarin or Unity, you need pre-code generation. `mpc.exe`(MessagePackCompiler) is code generator of MessagePack for C#. mpc is located in `packages\MessagePack.*.*.*\tools\mpc.exe` or includes for unity's package. mpc is using [Roslyn](https://github.com/dotnet/roslyn) so analyze source code.

```
mpc arguments help:
  -i, --input              [required]Input path of analyze csproj
  -o, --output             [required]Output file path
  -c, --conditionalsymbol  [optional, default=empty]conditional compiler symbol
  -r, --resolvername       [optional, default=GeneratedResolver]Set resolver name
  -n, --namespace          [optional, default=MessagePack]Set namespace root name
```

```
// Simple Sample:
mpc.exe -i "..\src\Sandbox.Shared.csproj" -o "MessagePackGenerated.cs"
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

    // finally, use builtin resolver(don't use StandardResolver, it includes dynamic generation)
    MessagePack.Resolvers.BuiltinResolver.Instance
);
```

> Note: mpc.exe is currently only run on Windows. It is .NET Core's Roslyn workspace API limitation but I want to implements to all platforms...

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
Grani is a top social game developer in Japan.  
He is awarding Microsoft MVP for Visual C# since 2011.  
He is known as the creator of [UniRx](http://github.com/neuecc/UniRx/)(Reactive Extensions for Unity)  

Blog: https://medium.com/@neuecc (English)  
Blog: http://neue.cc/ (Japanese)  
Twitter: https://twitter.com/neuecc (Japanese)   

License
---
This library is under the MIT License.
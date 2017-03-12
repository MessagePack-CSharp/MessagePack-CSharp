MessagePack for C#(.NET, .NET Core, Unity, Xamarin)
===

> TODO:Writing document now.

Extremely fast [MessagePack](http://msgpack.org/) serializer for C#, x10~20 faster than MsgPack-Cli and acquires best performance compared with all the other C# serializers. Built-in LZ4 compression extension can achieve super fast and small binary size.  Performance is always important! for Game, Distributed Computing, MicroServices, Store Data to Redis, etc.

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
Install-Package MessagePack.AspNetCoreMvcFormatter
```

for Unity, download from [releases](https://github.com/neuecc/MessagePack-CSharp/releases) page, providing `.unitypackage`. Unity IL2CPP or Xamarin AOT Environment, check the pre-code generation section.

Quick Start
---
Define class and mark as `[MessagePackObject]` and public members(property or field) mark `[Key]`, call `MessagePackSerializer.Serialize<T>/Deserialize<T>`. `ToJson` helps dump binary.

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

By default the attribute is required. Optionally it can be unnecessary, see Object Serialization section and Formatter Resolver section for details.

Analyzer
---
MessagePackAnalyzer helps object definition. Attributes, accessibility etc are detected and it becomes a compiler error.

TODO:Replace images

![zeroformatteranalyzer](https://cloud.githubusercontent.com/assets/46207/20078766/3ea54f14-a585-11e6-9873-b99cb5d9efe5.gif)

If you want to allow a specific type (for example, when registering a custom type), put `MessagePackAnalyzer.json` at the project root and make the Build Action to `AdditionalFiles`.

![image](https://cloud.githubusercontent.com/assets/46207/20149311/0e6f73d6-a6f4-11e6-91cb-44c771c267cb.png)

This is a sample of the contents of MessagePackAnalyzer.json. 

```
[ "MyNamespace.FooClass", "MyNameSpace.BarStruct" ]
```

Built-in support types
---
These types can serialize by default.

Primitives(`int`, `string`, etc...), `Enum`, `Nullable<>`,  `TimeSpan`,  `DateTime`, `DateTimeOffset`, `Nil`, `Guid`, `Uri`, `Version`, `StringBuilder`, `BitArray`, `ArraySegment<>`, `BigInteger`, `Complext`, `Task`, `Array[]`, `Array[,]`, `Array[,,]`, `Array[,,,]`, `KeyValuePair<,>`, `Tuple<,...>`, `ValueTuple<,...>`, `List<>`, `LinkedList<>`, `Queue<>`, `Stack<>`, `HashSet<>`, `ReadOnlyCollection<>`, `IList<>`, `ICollection<>`, `IEnumerable<>`, `Dictionary<,>`, `IDictionary<,>`, `SortedDictionary<,>`, `SortedList<,>`, `ILookup<,>`, `IGrouping<,>`, `ObservableCollection<>`, `ReadOnlyOnservableCollection<>`, `IReadOnlyList<>`, `IReadOnlyCollection<>`, `ISet<>`, `ConcurrentBag<>`, `ConcurrentQueue<>`, `ConcurrentStack<>`, `ReadOnlyDictionary<,>`, `IReadOnlyDictionary<,>`, `ConcurrentDictionary<,>`, `Lazy<>`, `Task<>` and custom inherited `ICollection<>` or `IDictionary<,>` with paramterless constructor.

You can add custom type support and has some official extension package. for ImmutableCollections(`ImmutableList<>`, etc), for ReactiveProperty and for Unity(`Vector3`, `Quaternion`, etc...). Please see extension section.

Object Serialization
---
MessagePack for C# can serialze your own `Class` or `Struct`.



MessagePackObjectAttribute


KeyAttribute
IgnoreMemberAttribute
SerializationConstructorAttribute


`MessagePack.Nil`



Union
---
TODO:
UnionAttribute

Note:Versioning....


Performance
---
TODO:


LZ4 Compression
---
TODO:

![image](https://cloud.githubusercontent.com/assets/46207/23766172/0c0a6158-0547-11e7-92ff-2db7a9b69877.png)

achieved extremely fast and very small binary!

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

`MessagePack.UnityShims` package provides shim of [Unity](https://unity3d.com/)'s standard struct(`Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`, `Bounds`, `Rect`) and there formatter. It can enable to commnicate between server and Unity client.

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

Configuration details, see:Extension section.

`MessagePack.AspNetCoreMvcFormatter` is add-on of ASP.NET MVC's serialization to boostup performance. This is configuration sample.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc().AddMvcOptions(option =>
    {
	    // ContractlessStandardResolver supports anonymous type, or use your custom composite resolver.
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

You can make your own extension serializers, let's create them and share it!

High-Level API(MessagePackSerializer)
---
`MessagePackSerializer` is the entry point of MessagePack for C#. Its static methods are main API of MessagePack for C#.

| API | Description |
| --- | --- |
| `DefaultResolver` | FormatterResolver that used resolver less overloads. If does not set it, used StandardResolver. |
| `SetDefaultResolver` | Set default resolver of MessagePackSerializer APIs. |
| `Serialize<T>` | Convert object to byte[] or write to stream. There has IFormatterResolver overload, used specified resolver. |
| `SerializeUnsafe<T>` | Same as `Serialize<T>` but return `ArraySegement<byte>`. The result of ArraySegment is contains internal buffer pool, it can not share across thread and can not hold, so use quickly. |
| `Deserialize<T>` | Convert byte[] or stream to object. There has IFormatterResolver overload, used specified resolver. |
| `NonGeneric.*` | NonGeneric APIs of Serialize/Deserialize. There accept type parameter at first argument. This API is bit slower than generic API. |
| `ToJson` | Dump message-pack binary to JSON string. It is useful for debugging.  |

MessagePack for C# operates at the byte[] level, so byte[] API is faster than Stream API.

High-Level API uses memory pool internaly to avoid unnecessary memory allocation. If result size is under 64K, allocates GC memory only for the return bytes.

`LZ4MessagePackSerializer` has same api with `MessagePackSerializer` and `DefaultResolver` is shared.

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

Created formatter needs to register to `IFormatterResolver`. Please see Extension Point section.

You can see many other samples from [builtin formatters](https://github.com/neuecc/MessagePack-CSharp/tree/master/src/MessagePack/Formatters).

Primitive API(MessagePackBinary)
---

`MessagePackBinary` is most low-level API like `Reader/Writer` of other serializers. `MessagePackBinary` is static class because avoid create `Reader/Writer` allocation.

| Method | Description |
| --- | --- |
| ReadNext | Skip MessagePackFormat binary block, returns read size. |
| ReadNextBlock | Skip MessagePackFormat binary block with sub structures(array/map), returns read size. This is useful for create deserializer. |
| Write/ReadMapHeader | Write/Read map format header(element length). |
| Write/ReadArrayHeader | Write/Read array format header(element length). |
| Write/Read*** | *** is primitive type name(`Int32`, `Single`, `String`, etc...) |
| Write/ReadBytes | Write/Read byte[] to use bin format. |
| Write/ReadExtensionFormat | Write/Read ext format header(Length + TypeCode) and content byte[]. |
| Write/ReadExtensionFormatHeader | Write/Read ext format, header(Length + TypeCode) only. |
| IsNil | Is TypeCode Nil? |
| IsMessagePackPrimitive | Is Type can become MessagePack Primitive(Sbyte, Byte, Int16, Int32, Int64, UInt16,UInt32, UInt64, Single, Double, Boolean,  DateTime, Char, Byte[], String)? |
| GetMessagePackType | Return MessagePackType of target MessagePack bianary position. |
| EnsureCapacity | Resize if byte can not fill.  |
| FastResize | Buffer.BlockCopy version of Array.Resize. |
| FastCloneWithResize | Same as FastResize but return copied byte[]. |

Read API returns deserialized primitive and read size. Write API returns write size and guranteed auto ensure ref byte[].

DateTime is serialized to [new MessagePack extension spec proposal](https://github.com/msgpack/msgpack/pull/209).

`MessagePackType` means [msgpack spec of source types](https://github.com/msgpack/msgpack/blob/master/spec.md#serialization-type-to-format-conversion).

`MessagePackCode` means [msgpack format of first byte](https://github.com/msgpack/msgpack/blob/master/spec.md#overview). Its static class has `ToMessagePackType` and `ToFormatName` utility methods.

`MessagePackRange` means Min-Max fix range of msgpack format.

Extension Point(IFormatterResolver)
---
`IFormatterResolver` is storage of typed serializers. Serializer api accepts resolver and can customize serialization.

| Resovler Name | Description |
| --- | --- |
| BuiltinResolver | Builtin primitive and standard classes resolver. It includes primitive(int, bool, string...) and there nullable, array and list. and some extra builtin types(Guid, Uri, BigInteger, etc...). |
| StandardResolver | Composited resolver . It resolves in the following order `builtin -> dynamic enum -> dynamic generic -> dynamic union -> dynamic object`. This is the default of MessagePackSerializer. |
| ContractlessStandardResolver | Composited `StandardResolver` -> `DynamicContractlessObjectResolver`. It enables contractless serialization. |
| CompositeResolver | Singleton helper of setup custom resolvers. You can use `Register` or `RegisterAndSetAsDefault` API. |
| DynamicEnumResolver | Resolver of enum and there nullable. It uses dynamic code generation to avoid boxing and boostup performance. |
| DynamicGenericResolver | Resolver of generic type(`Tuple<>`, `List<>`, `Dictionary<,>`, `Array`, etc). It uses reflection call for resolve generic argument. |
| DynamicUnionResolver | Resolver of interface marked by UnionAttribute. It uses dynamic code generation to create dynamic formatter. |
| DynamicObjectResolver | Resolver of class and struct maked by MessagePackObjectAttribute. It uses dynamic code generation to create dynamic formatter. |
| DynamicContractlessObjectResolver | Resolver of all classes and structs. It does not needs MessagePackObjectAttribute and serialized key as string(same as marked [MessagePackObject(true)]). |



GetFormatter, GetFormatterWithVerify, GetFormatterDynamic 


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
        //if (t.IsGenericParameter && t.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
        //{
        //    return Activator.CreateInstance(typeof(ValueTupleFormatter<,>).MakeGenericType(t.GenericTypeArguments));
        //}

        // If type can not get, must return null for fallback mecanism.
        return null;
    }
}
```








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



for Unity
---



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

lz4 compression support is using Milosz Krajewski's [lz4net](https://github.com/MiloszKrajewski/lz4net) code with some modified.
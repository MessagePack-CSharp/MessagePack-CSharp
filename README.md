# MessagePack for C# v4 (preview)

v4 is a complete redesign: more than twice the performance, stronger safety, and full Native AOT support. It also supports the new C# 15 language features (`union` / `closed`) and the new .NET 11 types. v4 is built on three principles:

* Performant by default
* Secure by default
* Schema-resilient by default

v4 is feature-compatible and wire-compatible with v3. The basic API shape is the same, but there are breaking changes. For the migration, the MessagePack for C# maintainers offer three options:

* Stay on v3: no new features, but the maintainers (@AArnott / @neuecc) keep providing security updates
* Move to [Nerdbank.MessagePack](https://github.com/AArnott/Nerdbank.MessagePack/): a new MessagePack serializer designed by maintainer @AArnott
* Move to MessagePack for C# v4: the MessagePack serializer redesigned by maintainer @neuecc

## Performant by default

### POCO

For a fair benchmark the subject is the Stack Overflow `Answer` type from [Jil](https://github.com/kevin-montrose/jil): a fairly large type with more than 20 fields, including nested types. MessagePack has two modes for POCOs, array and map, and MessagePack for C# defaults to array, the one with the higher performance ceiling. Serializers are normally used with their default settings, and the analyzer support applies most strongly to the default, so for a binary serializer we judge that array should be the default.

#### Serialize

| Method | Mean | Allocated |
|---|---:|---:|
| **MessagePack for C# v4 (array/default)** | 423.5 ns | 1.65 KB |
| MessagePack for C# v4 (map) | 532.3 ns | 2.84 KB |
| Nerdbank.MessagePack (array) | 997.1 ns | 1.65 KB |
| BcsSharp | 1,075.1 ns | 1.78 KB |
| **MessagePack for C# v3 (array/default)** | 1,170.7 ns | 1.65 KB |
| Orleans | 1,450.2 ns | 1.91 KB |
| MessagePack for C# v3 (map) | 1,494.9 ns | 2.84 KB |
| **Nerdbank.MessagePack (map/default)** | 1,540.8 ns | 2.84 KB |
| REDox.MessagePack (map) | 1,543.5 ns | 2.91 KB |
| ShapeShift.MsgPack (array) | 1,701.5 ns | 1.9 KB |
| protobuf-net | 1,734.9 ns | 5.32 KB |
| ShapeShift.MsgPack (map) | 2,605.2 ns | 4.17 KB |
| System.Text.Json | 3,022.1 ns | 5.01 KB |
| ShapeShift.Json | 3,389.9 ns | 3.81 KB |
| System.Text.Json source-gen | 3,434.2 ns | 3.63 KB |
| ShapeShift.Protobuf | 5,359.6 ns | 22.95 KB |
| Newtonsoft.Json | 5,740.3 ns | 21.48 KB |

#### Deserialize

| Method | Mean | Allocated |
|---|---:|---:|
| **MessagePack for C# v4 (array/default)** | 909.5 ns | 4.65 KB |
| MessagePack for C# v4 (map) | 1,159.7 ns | 4.65 KB |
| BcsSharp | 1,557.8 ns | 4.67 KB |
| Orleans | 1,712.4 ns | 4.61 KB |
| **MessagePack for C# v3 (array/default)** | 1,735.1 ns | 4.65 KB |
| Nerdbank.MessagePack (array) | 1,836.2 ns | 4.65 KB |
| protobuf-net | 2,034.6 ns | 4.72 KB |
| MessagePack for C# v3 (map) | 2,746.0 ns | 4.65 KB |
| REDox.MessagePack (map) | 3,190.6 ns | 4.65 KB |
| ShapeShift.MsgPack (array) | 3,396.9 ns | 8.91 KB |
| **Nerdbank.MessagePack (map/default)** | 3,520.2 ns | 4.65 KB |
| System.Text.Json source-gen | 5,287.1 ns | 5.12 KB |
| System.Text.Json | 5,324.4 ns | 5.12 KB |
| ShapeShift.Protobuf | 6,165.2 ns | 12.55 KB |
| ShapeShift.MsgPack (map) | 6,583.6 ns | 14.08 KB |
| ShapeShift.Json | 7,755.3 ns | 14 KB |
| Newtonsoft.Json | 10,201.8 ns | 17.51 KB |

### int -> bool

Values as small as a primitive measure the serializer's own setup cost. The measurement alternates `int` and `bool` calls, which rules out two things that do not happen in production: optimizations that come from profiling a single type, and benchmark-hack style type caches.

### Serialize

| Method | Mean | Allocated |
|---|---:|---:|
| **MessagePack for C# v4** | 13.587 ns | 64 B |
| Nerdbank.MessagePack | 28.391 ns | 64 B |
| MessagePack for C# v3 | 32.580 ns | 64 B |
| System.Text.Json | 62.928 ns | 64 B |

### Deserialize

| Method | Mean | Allocated |
|---|---:|---:|
| **MessagePack for C# v4** | 5.756 ns | - |
| MessagePack for C# v3 | 33.222 ns | - |
| Nerdbank.MessagePack | 47.620 ns | - |
| System.Text.Json | 47.825 ns | - |

## Quick Start

```bash
dotnet add package MessagePack --prerelease
```

Target frameworks are `netstandard2.0`, `netstandard2.1`, `net9.0`, `net10.0` and `net11.0`. The basic API is the same as v3; see the [v3 README](https://github.com/MessagePack-CSharp/MessagePack-CSharp/tree/master) for the details.

```csharp
using MessagePack;

byte[] bytes = MessagePackSerializer.Serialize(new Person { Id = 1, Name = "neuecc", Created = DateTime.Now });
Person back = MessagePackSerializer.Deserialize<Person>(bytes);

[MessagePackObject]
public partial class Person
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
    [Key(2)] public DateTime Created { get; set; }
}
```

`[MessagePackObject]` types are serialized as arrays indexed by `[Key(int)]` (the compact, versionable default). `[MessagePackObject(true)]` or `[MessagePackObject(KeyNamingPolicy.CamelCase)]` writes a map keyed by member name instead.

### Native AOT

Objects marked with `[MessagePackObject]` are serialized by formatters the source generator produces, so they work on Native AOT as they are. Shapes that involve generics, such as `Person[]` or `List<Person>`, have to be resolved through `MakeGenericType`, so the defaults are not Native AOT safe in every case. The unsafe cases are detected: a Native AOT publish reports warnings. To clear them, pass `MessagePackSerializerOptions.DefaultAot`.

```csharp
var person = new Person { Id = 1, Name = "neuecc", Created = DateTime.Now };
byte[] bytes = MessagePackSerializer.Serialize(person, MessagePackSerializerOptions.DefaultAot);
Person back = MessagePackSerializer.Deserialize<Person>(bytes, MessagePackSerializerOptions.DefaultAot);
```

`DefaultAot` resolves the `[MessagePackObject]` types and the types their members contain. A root type outside that set cannot be serialized; declare it explicitly with `[MessagePackSerializable]` so the generator constructs it.

```csharp
[MessagePackSerializable<Person[]>]
public partial class MySerializationRoots { }
```

A library designed for AOT only would refuse `Person[]`, a type one expects to just work, even on the JIT, which does not make for a usable library. MessagePack for C# v4 therefore takes a two-step approach: the JIT chain is the default, a Native AOT publish warns clearly (`RequiresDynamicCode` / `RequiresUnreferencedCode` are applied where Native AOT is not supported, so no unsupported path is left open), and the AOT options become mandatory there. Usability and Native AOT support coexist that way.

## Migrating from v3

The wire format is the same: v3 and v4 read each other's output, including primitives, collections, `[MessagePackObject]` arrays and maps, `[Union]` and the LZ4 envelopes. v4 also serializes and deserializes types declared with the v3 attributes.

### 1. Packages and attributes

| Package | What it is |
|---|---|
| `MessagePack` | The serializer, the attributes and the source generator (the generator runs automatically in every project that references this package). |
| `MessagePack.LZ4` | LZ4 compression, including the v3 `Lz4Block` / `Lz4BlockArray` envelopes. |
| `MessagePack.Zstandard` | Zstandard compression (standard zstd frames). |
| `MessagePack.UnityShims` | The UnityEngine value types (`Vector3`, `Quaternion`, ...) for servers that talk to Unity clients. |
| `MessagePack.AspNetCoreMvcFormatter` | ASP.NET Core MVC input/output formatters for `application/x-msgpack` (`net10.0`). |
| `MessagePack.SignalR` | The SignalR `messagepack` hub protocol, wire-compatible with `Microsoft.AspNetCore.SignalR.Protocols.MessagePack`, which it replaces (`net10.0`). |
| `MessagePack.Annotations` | Migration shim only: type-forwards the v3 attributes to `MessagePack`. |

- Attributes moved into the `MessagePack` package. `MessagePack.Annotations` is now a type-forwarding shim. Drop it once nothing else pulls it in: while any dependency built against v3 still references `MessagePack.Annotations`, keep an explicit reference to the 4.x shim, because without it NuGet resolves that dependency's `MessagePack.Annotations` 3.x next to `MessagePack` 4.x and every attribute is defined twice (CS0433).
- `MessagePackAnalyzer` is gone; the generator and its analyzers ship inside the `MessagePack` package.
- `[Union(key, typeof(T))]` became `[UnionTag(typeof(T), key)]` / `[UnionTag<T>(key)]` on a root that also carries `[MessagePackObject]`. The string (assembly-qualified name) form is gone. Generic unions can name a type parameter: `[UnionTag("T", 0)]`.
- `[MessagePackAssumedFormattable]` -> `[assembly: MessagePackKnownType(typeof(T))]`. `[MessagePackKnownFormatter]` and `[ExcludeFormatterFromSourceGeneratedResolver]` are deleted (the generator never collects formatters into a resolver). `GeneratedMessagePackResolver` no longer exists: generated formatters register themselves.
- `[MessagePackFormatter(typeof(X))]` now names a **factory** (or an open generic formatter type), not a closed formatter. Its property is `FactoryType`.
- LZ4 compression needs a reference to the `MessagePack.LZ4` package. The compression itself moved from the built-in managed implementation to the [NativeCompressions](https://github.com/Cysharp/NativeCompressions/) library.
- The generic attribute forms (`[UnionTag<T>(key)]`, `[MessagePackSerializable<T>]`, `[MessagePackFormatter<TFactory>]`) exist only in the `net9.0` and later builds. A `netstandard` project uses the `typeof` forms: `[UnionTag(typeof(T), key)]`, `[MessagePackSerializable(typeof(T))]`, `[MessagePackFormatter(typeof(TFactory))]`.

### 2. Options and resolvers

Where v3 had resolvers all the way down, v4 has three distinct things:

```
MessagePackSerializerOptions     (per-call settings: MessageProcessor, MaxDepth, MaxBufferedMessageSize)
  MessagePackFormatterResolver   (creates and caches the formatters of one chain; owns the safety flags)
    MessagePackFormatterFactory  (which formatter serves which type; factories compose into a chain)
```

Formatter customization, which v3 did with a chain of resolvers, is now a chain of factories.

```csharp
// v4: MessagePackFormatterFactory.Default, spelled out
var factory = MessagePackFormatterFactory.Combine(
    SourceGeneratedFormatterFactory.Instance,
    ObjectFallbackFormatterFactory.Instance,
    BuiltInFormatterFactory.Instance,
    GenericFormatterFactory.Instance,
    new ReflectionFormatterFactory(annotatedOnly: true)); // the reflection-based formatters: the fallback tier for JIT environments

var resolver = new MessagePackFormatterResolver(factory); // the resolver creates and caches the formatters

var defaultOptions = new MessagePackSerializerOptions(resolver);
```

The expensive operations (formatter construction, reflection) are isolated in the resolver, so changing the options per operation (`options with { ... }`) is fine.

| v3 | v4 |
|---|---|
| `MessagePackSerializerOptions.Standard` | `MessagePackSerializerOptions.Default` |
| `options.WithResolver(resolver)` | `options with { Resolver = resolver }` |
| `StandardResolver.Instance` | `MessagePackFormatterFactory.Default` |
| `ContractlessStandardResolver` | `MessagePackFormatterFactory.Default.WithContractless()` |
| `CompositeResolver.Create(a, b)` | `MessagePackFormatterFactory.Combine(a, b)` |
| `TypelessContractlessStandardResolver` / `MessagePackSerializer.Typeless` | `MessagePackFormatterFactory.Default.WithTypeless(TypelessTypeLoader.LoadAnyType())` |
| `options.WithCompression(MessagePackCompression.Lz4Block)` | `options.WithLz4Block()` from the `MessagePack.LZ4` package. |
| `options.WithCompression(MessagePackCompression.Lz4BlockArray)` | `options.WithLz4BlockArray()` from the `MessagePack.LZ4` package. |
| `options.WithSecurity(MessagePackSecurity.UntrustedData)` | On by default. The knobs are `MaxDepth` (500) and `MaxBufferedMessageSize` (64 MB) on the options, `HashFloodingResistant` on the resolver, and `MaxDecompressedSize` on the compression processors. There is no "trusted data" mode. |
| `options.WithOmitAssemblyVersion(true)` | `WithTypeless(loader, omitAssemblyVersion: true)` |
| `options.WithAllowAssemblyVersionMismatch(true)` | `TypelessTypeLoader.LoadAnyType(allowAssemblyVersionMismatch: true)` |
| `UnityResolver` | `MessagePackFormatterFactory.Default.WithUnity()` (`MessagePack.UnityShims` / the Unity package). |
| `UnityBlitResolver`, `UnityBlitWithPrimitiveArrayResolver` | Dropped in v4. |
| `MessagePackSerializer.SerializeToJson(value, options)` | Dropped in v4. |
| `MessagePackSerializer.DefaultOptions = ...` | Gone: there is no mutable process-wide default. |
| `NativeGuidFormatter`, `NativeDateTimeFormatter` | `MessagePackFormatterFactory.DotNetOptimized` |

### 3. Entry points

- `Deserialize<T>(ReadOnlyMemory<byte>)` has no overload; pass `.Span`. `byte[]` converts implicitly.
- `Deserialize<T>(..., out int bytesRead)` is gone. For a stream of messages use `DeserializeMessagesAsync` (or read from a `PipeReader`, which leaves the following message unconsumed).
- `MessagePackStreamReader` is gone; `DeserializeMessagesAsync` / `DeserializeElementsAsync` over `Stream` or `PipeReader` replace it.
- An `options` argument is required wherever an overload takes one (`ArgumentNullException`); v3 read `null` as `DefaultOptions`, which no longer exists.

### 4. Custom formatters

`MessagePackWriter` and `MessagePackReader` are removed. The formatter interface is generic over the buffers, and the reader/writer structs are replaced by extension methods on the buffer:

```csharp
// v3
public class PointFormatter : IMessagePackFormatter<Point>
{
    public void Serialize(ref MessagePackWriter writer, Point value, MessagePackSerializerOptions options) { ... }
    public Point Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) { ... }
}

// v4
using MessagePack;
using SerializerFoundation;

// declare it partial and leave the buffer constraints out, the generator adds them for every target
public sealed partial class PointFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Point>
{
    public void Initialize(MessagePackFormatterResolver resolver) { } // acquire nested formatters here

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Point value)
    {
        buffer.WriteArrayHeader(2);
        buffer.WriteInt32(value.X);
        buffer.WriteInt32(value.Y);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Point value)
    {
        var count = buffer.ReadArrayHeader(ref state);
        if (count != 2) throw new InvalidOperationException();
        var x = buffer.ReadInt32();
        var y = buffer.ReadInt32();
        value = new Point(x, y);
    }
}

public readonly record struct Point(int X, int Y);
```

The generator supplies the buffer constraints only when the formatter declares none of its own: partial declarations that both carry `where` clauses must carry the same ones (CS0265), so a formatter with `where T : struct, Enum` on a third type parameter writes the buffer constraints itself (`where TWriteBuffer : struct, IWriteBuffer`, `where TReadBuffer : struct, IReadBuffer`, each with `, allows ref struct` under `#if NET9_0_OR_GREATER`); MsgPack028 spells them out.

A formatter is attached either with `[MessagePackFormatter(typeof(...))]` on the type (an open generic formatter, or a `MessagePackFormatterFactory` for anything that needs arguments or serves several types) or by putting a factory in front of the chain. When a formatter calls into another formatter it brackets the call with `state.Enter()` / `state.Exit()` (an analyzer enforces it). Nested formatters come from `resolver.GetFormatter<TWriteBuffer, TReadBuffer, TNested>()` inside `Initialize`.

A type-level attribute needs no factory: the source generator registers it, and the built-in chains consult those registrations first (the type's assembly must be built with the generator; without it the type fails with a message naming the attribute). For example, a type that keeps a str payload as its UTF-8 bytes instead of decoding it:

```csharp
[MessagePackFormatter(typeof(Utf8StringFormatter<,>))]
public readonly struct Utf8String(ReadOnlyMemory<byte> utf8Bytes)
{
    public Utf8String(string value) : this(Encoding.UTF8.GetBytes(value)) { }

    public ReadOnlyMemory<byte> Memory => utf8Bytes;
    public ReadOnlySpan<byte> Span => utf8Bytes.Span;

    public override string ToString() => Encoding.UTF8.GetString(Span);
}

public sealed partial class Utf8StringFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Utf8String>
{
    public void Initialize(MessagePackFormatterResolver resolver) { }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Utf8String value)
    {
        buffer.WriteString(value.Span); // writes the bytes as they are; the caller guarantees valid UTF-8
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Utf8String value)
    {
        if (buffer.TryReadNil())
        {
            value = default;
            return;
        }
        var byteCount = buffer.ReadStringHeader(); // advances past the header only, and rejects a count larger than the remaining data
        if (byteCount == 0)
        {
            value = default;
            return;
        }
        // copy out: the read buffer may be pooled, so a span over it must not outlive the call
        var bytes = GC.AllocateUninitializedArray<byte>(byteCount);
        buffer.CopyTo(bytes);
        buffer.Advance(byteCount);
        value = new Utf8String(bytes);
    }
}
```

The buffer is valid only for the duration of the call that received it: use it through the `ref` parameter and never copy it into a local, a field, a static or a box. On netstandard the buffers are plain structs over pinned or pooled memory (a `ref struct` cannot be a type argument there), so a copy that outlives the call reads memory that has been moved, returned to the pool or popped off the stack, and a copied write buffer returns its pooled array twice. SerializerFoundation's SF002 analyzer (an error, flowing to your project with the MessagePack package) reports such copies; a formatter compiled without it, or with it suppressed, is not checked at run time.

A factory is written once for every target framework. On `net9.0` and later the base class has a virtual generic `CreateFormatter<TWriteBuffer, TReadBuffer>(Type)` to override; the netstandard builds have no such member, so the same method is declared there without `override` and with the buffer constraints spelled out. The `Type`-based `CreateFormatter(Type, Type, Type)` is abstract on every target: a `partial` factory that writes the generic method gets it generated (the Type-based call maps the buffer types back onto the generic one), otherwise write it yourself.

```csharp
public sealed partial class PointFormatterFactory : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
        => type == typeof(Point) ? new PointFormatter<TWriteBuffer, TReadBuffer>() : null;
    // CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType) is generated for this partial class
}
```

### 5. Behaviour differences to check

- **Constructor binding**: parameters bind to members by name (`[Key("name")]` or member name, case-insensitive), never by position. v3 bound int-keyed parameters by position, so a constructor whose parameter names do not match its members is now reported at compile time (MsgPack004, or MsgPack009 when it carries `[SerializationConstructor]`, which selects a constructor but does not relax the name match). Rename the parameters, or give the members setters and a parameterless constructor.
- **Required members** (`required`, or a constructor parameter without a default) missing from the payload throw by default (`ValidateRequiredMembers`). v3 left them at their default.
- **Init-only members**: a type with `{ get; init; }` members is built through its constructor and an object initializer, which assigns every such member unconditionally, so a member the payload omits (an older writer's) reads as default, not as its initializer (`{ get; init; } = 3` reads 0). The reflection tier and v3 keep the initializer. Use a plain `set` accessor where the initializer must survive an omitted member.
- **Member initializers are populated**: the instance a member initializer assigns is filled from the payload rather than replaced (v3 always created a new one). This applies to collections and to nested objects whose serialized members all have setters, so a shared mutable default such as `= Style.DefaultMutable` is overwritten by every deserialization and shared by all results. Initialize with `new()` or make the shared instance immutable.
- **Depth**: `MaxDepth` defaults to 500 for both serialization and deserialization. `0` disables the check; do that only for data you trust completely, since the formatters recurse. Independently of `MaxDepth`, nesting that would exhaust the thread's stack throws `MessagePackSerializationException` instead of crashing the process.
- **Decompression cap**: v3 had no limit; for security v4 caps the decompressed size of one message at 64 MB by default. Configure it with `MaxDecompressedSize` on the compression processor.
- **Not on the default chain**: types whose deserialization looks a payload-provided name up in the runtime or the OS are opt-in, composed before `Default` with their own factory. `System.Type` (`TypeFormatterFactory`, `[Obsolete]`-gated: `Type.GetType` can load assemblies) and `ExpandoObject` (`ExpandoObjectFormatterFactory`: quadratic `Add`) were on v3's default resolvers and are not on v4's. `CultureInfo` (`CultureInfoFormatterFactory`, predefined culture names only) and `TimeZoneInfo` (`TimeZoneInfoFormatterFactory`, `FindSystemTimeZoneById` on the reading machine) are new in v4 and opt-in from the start. The default chain throws `InvalidOperationException` naming the factory for all four.
- **Primitive writers**: `MessagePackPrimitives.TryWriteInt16/32/64` and `TryWriteUInt16/32/64` now write the *smallest* msgpack form for the value (1 to 9 bytes), the same as `WriteInt32` on a buffer. v3's methods of the same name wrote the fixed-width code regardless of value; that form is now `TryWriteForcedInt16/32/64` and `TryWriteForcedUInt16/32/64` (and `TryWriteForcedInt8` / `TryWriteForcedUInt8`). Code that reserves a slot and fills it in later must use the `Forced` methods; with the old names it would produce a shorter token than the slot and corrupt what follows, without any error.
- **Diagnostics IDs**: the generator's `MsgPack001`-`MsgPack018` are new rules under v3's numbers (v3's analyzer is gone).
- **Hash flooding**: `HashFloodingResistant` (on by default) gives the dictionaries and sets the formatters build a per-process randomized comparer for bool, char, integer, enum, `TimeSpan`, `Guid`, `DateOnly`, `TimeOnly`, `Int128` and `UInt128` keys (and their nullable forms; `string` on .NET 9+ relies on the runtime's own randomized hashing, downlevel it gets the resistant comparer too). Any other key type hashes with its own `GetHashCode`, so a user-defined key should use `HashCode.Combine`; and only the exact BCL types (`Dictionary`, `HashSet`, `ConcurrentDictionary`, the frozen and immutable ones) are covered, not a class derived from them, `Hashtable`, or a custom collection the generic tier constructs. The deserialized collections keep that comparer (v3 returned the default one), which makes their later lookups slower than the default. `new MessagePackFormatterResolver(factory) { HashFloodingResistant = false }` turns it off for trusted data. Populating an existing dictionary or set follows the same rule: an instance on the default comparer (a `= new()` member) is replaced by a fresh one carrying the resistant comparer, not cleared and refilled, while an instance with a comparer you chose is refilled in place.
- **`object` slots**: strings inside `object`, `object[]` and `Dictionary<object, object>` payloads are read and written by the object formatter directly; `StringInterningFormatterFactory` and a custom `string` formatter do not apply to them.
- **String interning**: `StringInterningFormatterFactory` replaces v3's `StringInterningFormatter`, which interned into a process-wide, weakly referenced pool (`Strings.WeakIntern`). The v4 pool is a `HashSet<string>` per factory (or the set you pass in), holds strong references for as long as the factory lives, and takes a lock per string read, so parallel deserialization through one factory serializes on it. Use it where the value space is small and the input is trusted; it is not in the default chain.

## New Features

### Union / Closed Hierarchy

v3's `[Union]` became `[UnionTag]`, and the root type carries `[MessagePackObject]` too. The C# 14 `union` types and `closed` hierarchies are supported, and for a `closed` hierarchy the analyzer reports a case that is missing its `[UnionTag]`. A union over an interface or abstract class written the traditional way is therefore safer declared as `closed`.

```csharp
[MessagePackObject]
[UnionTag<Dog>(0)]
[UnionTag<Cat>(1)]
public closed class Pet
{
    [Key(0)] public string? Name { get; set; }
}

[MessagePackObject]
public sealed class Dog : Pet
{
    [Key(1)] public string? FavouriteToy { get; set; }
}

[MessagePackObject]
public sealed class Cat : Pet
{
    [Key(1)] public int Lives { get; set; }
}

// a C# 14 union type works as it is
[MessagePackObject]
[UnionTag<int>(0)]
[UnionTag<string>(1)]
public union IntOrString(int, string);
```

Targets without generic attributes (`netstandard`, Unity) use the `[UnionTag(typeof(Dog), 0)]` form.

### Circular references

A type with `AllowCircularReferences = true` writes a repeated reference to the same instance as a back-reference, so a graph with cycles or shared references round-trips as it is. Generated formatters only; the wire form is specific to v4.

```csharp
[MessagePackObject(AllowCircularReferences = true)]
public class Node
{
    [Key(0)] public int Value { get; set; }
    [Key(1)] public Node? Next { get; set; }
}

var a = new Node { Value = 1 };
a.Next = new Node { Value = 2, Next = a }; // a cycle

var bytes = MessagePackSerializer.Serialize(a);
var back = MessagePackSerializer.Deserialize<Node>(bytes);
// back.Next.Next == back
```

Identity is tracked per instance, not per declared type: an instance written first through a member of a base type and again through one of a derived type is written once, in the base type's shape, and the derived-typed read then fails with a type mismatch. In an element stream (`SerializeElementsAsync` / `DeserializeElementsAsync`) the identity table spans the whole array, as it does for `Serialize<T[]>`, so it grows with the number of distinct instances streamed.

### Deserialize(ref)

Populates an existing instance. Collections are cleared and refilled, and objects are overwritten member by member, so buffers and pooled objects can be reused. An instance the target graph holds in several places is populated once per occurrence in the payload, the last one winning.

```csharp
var list = new List<int>(capacity: 1024);
var state = new GameState();

MessagePackSerializer.Deserialize(bytes, ref list);  // refills the same List instance
MessagePackSerializer.Deserialize(bytes, ref state); // overwrites the same GameState
```

Note that the plain `Deserialize<T>` also refills, in place, a collection that a member initializer created (`List<int> Items { get; set; } = new()` is not replaced). v3 always constructed a new one, so this is a behaviour change.

### Surrogate

For a type you cannot annotate (a third-party type, or one with constructor-enforced invariants), declare a `struct` that is serialized in its place. The generator finds the implementation and registers a formatter for the target type; the target itself needs no attribute.

```csharp
// a type you cannot change
public sealed class Money
{
    public Money(decimal amount, string currency) { ... }
    public decimal Amount { get; }
    public string Currency { get; }
}

[MessagePackObject]
public readonly record struct MoneySurrogate([property: Key(0)] decimal Amount, [property: Key(1)] string Currency)
    : IMessagePackSurrogate<Money, MoneySurrogate>
{
    public MoneySurrogate ToSurrogate(Money value) => new(value.Amount, value.Currency);
    public Money ToTarget() => new(Amount, Currency);
}

var bytes = MessagePackSerializer.Serialize(new Money(100m, "JPY")); // Money is serializable as it is
```

### Streaming serialize

Two families of entry points put several values on one stream, JSON Lines style. `SerializeMessagesAsync` concatenates one message per value; `SerializeElementsAsync` writes one array, element by element. Both work over `Stream` and `PipeWriter` / `PipeReader`, and the deserialize side is an `IAsyncEnumerable<T>`.

```csharp
// a sequence of messages: writer and reader work one message at a time
await MessagePackSerializer.SerializeMessagesAsync(stream, GetEventsAsync());

await foreach (var e in MessagePackSerializer.DeserializeMessagesAsync<Event>(stream))
{
    Handle(e);
}

// one array, element by element: the writer never holds the whole array, and the reader may also use Deserialize<Event[]>
await MessagePackSerializer.SerializeElementsAsync(stream, events, count: events.Count);

await foreach (var e in MessagePackSerializer.DeserializeElementsAsync<Event>(stream))
{
    Handle(e);
}
```

### MessagePackUnknownMembers

Like Protocol Buffers' unknown fields: a `[MessagePackObject]` type can declare one member of type `MessagePackUnknownMembers` (without `[Key]`), and the generated formatter keeps whatever a newer writer added and writes it back on re-serialization. A relay running an older version of the service no longer drops the fields it does not know.

```csharp
[MessagePackObject(true)]
public class Packet
{
    public int Id { get; set; }
    public MessagePackUnknownMembers? Unknown { get; set; } // catches the members this version does not know
}

var packet = MessagePackSerializer.Deserialize<Packet>(bytesFromNewerWriter);
var bytes = MessagePackSerializer.Serialize(packet); // includes the members the newer writer added
```

The packet replays bytes verbatim, so it cannot carry circular-reference wire: a captured value that contains a back-reference is rejected on deserialization (it would point at a different instance once replayed), and a type cannot combine `AllowCircularReferences` with a `MessagePackUnknownMembers` member (MsgPack015).

## Compression

Compression comes in separate packages.

```bash
dotnet add package MessagePack.LZ4 --prerelease
dotnet add package MessagePack.Zstandard --prerelease
```

```csharp
static readonly MessagePackSerializerOptions Lz4BlockOptions = MessagePackSerializerOptions.Default.WithLz4Block();
static readonly MessagePackSerializerOptions Lz4BlockArrayOptions = MessagePackSerializerOptions.Default.WithLz4BlockArray();
static readonly MessagePackSerializerOptions Lz4FrameOptions = MessagePackSerializerOptions.Default.WithLz4Frame();
static readonly MessagePackSerializerOptions ZstdFrameOptions = MessagePackSerializerOptions.Default.WithZstandardFrame();
```

Unlike v3's managed LZ4, `MessagePack.LZ4` (and `MessagePack.Zstandard` below .NET 11) runs on [NativeCompressions](https://github.com/Cysharp/NativeCompressions)' native libraries, so the platform conditions of that package apply: Windows, Linux and macOS on x64 / arm64 (macOS 15 or later), Android (arm, arm64, x64), iOS and Mac Catalyst (.NET 10 or later). There is no x86 (32-bit Intel) library, so a .NET Framework exe must set `<PlatformTarget>x64</PlatformTarget>` (or `arm64`) or a `RuntimeIdentifier`, because the SDK restores an unspecified one as `win-x86` and the first call would throw `DllNotFoundException`. Unity needs the NuGetForUnity runtime settings and, for iOS, the `NativeCompressions.Unity` editor package described in the [NativeCompressions README](https://github.com/Cysharp/NativeCompressions#unity).

`WithLz4Frame()` is new. `Lz4Block` and `Lz4BlockArray` are a MessagePack for C# specific format that wraps LZ4 blocks in a MessagePack ext; `WithLz4Frame` instead wraps the raw MessagePack payload in the standard LZ4 frame format, which other tools and languages read, so interoperability improves. For new uses of LZ4, Lz4Frame is recommended.

Zstandard uses the codec bundled with the BCL on .NET 11 and NativeCompressions' Zstandard support below it. The format is the same idea as Lz4Frame: the raw MessagePack payload wrapped in a Zstandard frame.

## Unity

The minimum Unity version is 2022.3.12f1, the first release whose Roslyn runs the source generator. The serializer core (`MessagePack`, `SerializerFoundation` and their dependencies) comes from NuGet, for example through [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity); the formatters for the UnityEngine types (`Vector3` and friends) are a separate UPM install:

```
https://github.com/MessagePack-CSharp/MessagePack-CSharp.git?path=src/MessagePack.Unity#v4.0.0-preview.1
```

Then build a `MessagePackSerializerOptions` explicitly, with the formatters added through `WithUnity()`:

```csharp
using MessagePack;
using MessagePack.Unity;

// for IL2CPP: no runtime generic instantiation
static readonly MessagePackSerializerOptions UnityOptions = new MessagePackSerializerOptions(MessagePackFormatterFactory.DefaultAot.WithUnity());
```

In a JIT environment `MessagePackFormatterFactory.Default` works too. If you plan to publish with IL2CPP, base the options on `DefaultAot`.

To talk to a Unity client from a C# server, reference the `MessagePack.UnityShims` package on the server and apply the same options: both sides then serialize each other's data.

## ASP.NET Core (MVC / SignalR)

The two ASP.NET Core packages register through the usual builders:

```csharp
builder.Services.AddControllers().AddMessagePackFormatters(); // MVC: application/x-msgpack in and out
builder.Services.AddSignalR().AddMessagePackProtocol();       // SignalR: the "messagepack" hub protocol, Microsoft-compatible defaults
// both take a MessagePackSerializerOptions overload; the SignalR one is also the overload for trimmed / Native AOT apps
```

For SignalR on Native AOT, mind the enums: the defaults (like the official protocol) send enum *names*, and `DefaultAot` sends integers, which the official .NET, JavaScript and Java clients then fail to read. Compose `EnumAsStringFormatterFactory<TEnum>` for each enum in front of `DefaultAot` (`MessagePackFormatterFactory.Combine(new EnumAsStringFormatterFactory<Status>(), MessagePackFormatterFactory.DefaultAot)`), and annotate the hub argument types, since the contractless tier is not available on that chain.

## For library authors

A library that declares `[MessagePackObject]` types or formatters and targets only `netstandard2.x` gets generated formatters for the Compatible (non-ref-struct) buffers only. On a `net9.0`+ host every root type whose object graph includes one of them (a `List<Dto>` too) is rerouted to those buffers: the output is identical and the cost measured here is about 5% on serialize and 8% on deserialize, but nothing reports it by default. Multi-target such a library (`<TargetFrameworks>netstandard2.1;net9.0</TargetFrameworks>`); to find out whether a reroute happens, subscribe to `MessagePackFormatterResolver.CompatibilityFallback` (raised with the root type) or set `ThrowOnLegacyFormatter = true` on the resolver to make it an error. The buffer-level entry points (`Serialize(ref TWriteBuffer, ...)`, `Deserialize(ref TReadBuffer, ...)`) do not reroute: they exist to run the caller's own buffer, which staging through the Compatible buffers would defeat, so such a type throws there until its library targets `net9.0`.

The source generator's output needs **C# 9** (C# 13 on `net9.0` and later, which is those SDKs' default). `net48` and `netstandard2.0` projects default to C# 7.3, so a project that declares `[MessagePackObject]` types there must set `<LangVersion>9.0</LangVersion>` or later; otherwise the generator reports MsgPack024 and generates nothing. Projects that only use the reflection tier are unaffected. On those targets the generator also emits a `ModuleInitializerAttribute` polyfill when the compilation has none; a project that uses [PolySharp](https://github.com/Sergio0694/PolySharp) is detected through its build properties and gets no second copy (if you exclude that attribute from PolySharp with `PolySharpExcludeGeneratedTypes`, the generator emits its own again).

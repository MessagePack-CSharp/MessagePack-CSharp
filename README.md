# MessagePack for C# v4 (preview)

Extremely fast [MessagePack](https://msgpack.org/) serializer for C#, rewritten from the ground up for .NET 9+ while staying **wire-compatible with v3**. This is a preview: the API can still change between previews, and this document is deliberately short. It is written for two readers — people migrating from v3 (most of it) and people starting with v4 (the first two sections).

```
dotnet add package MessagePack --prerelease
```

| Package | What it is |
|---|---|
| `MessagePack` | The serializer, the attributes and the source generator (the generator runs automatically in every project that references this package). |
| `MessagePack.LZ4` | LZ4 compression, including the v3 `Lz4Block` / `Lz4BlockArray` envelopes. |
| `MessagePack.Zstandard` | Zstandard compression (standard zstd frames). |
| `MessagePack.UnityShims` | The UnityEngine value types (`Vector3`, `Quaternion`, ...) for servers that talk to Unity clients. |
| `MessagePack.AspNetCoreMvcFormatter` | ASP.NET Core MVC input/output formatters for `application/x-msgpack`. |
| `MessagePack.SignalR` | The SignalR `messagepack` hub protocol, wire-compatible with `Microsoft.AspNetCore.SignalR.Protocols.MessagePack`. |
| `MessagePack.Annotations` | Migration shim only: type-forwards the v3 attributes to `MessagePack`. See [Migrating from v3](#migrating-from-v3). |

Targets: `netstandard2.0`, `netstandard2.1`, `net9.0`, `net10.0`, `net11.0`. The full-speed path (ref struct buffers, `allows ref struct`) exists from `net9.0` up; .NET Framework and Unity get the `netstandard2.0` build. The core depends on [SerializerFoundation](https://github.com/Cysharp/SerializerFoundation), which provides the buffer abstractions.

## Quick start

```csharp
using MessagePack;

byte[] bytes = MessagePackSerializer.Serialize(new Person { Id = 1, Name = "neuecc" });
Person back = MessagePackSerializer.Deserialize<Person>(bytes);

[MessagePackObject]
public partial class Person
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
    [Key(2)] public DateTime Created { get; set; }
    [IgnoreMember] public string Cached => $"{Id}:{Name}";
}
```

`[MessagePackObject]` types are serialized as arrays indexed by `[Key(int)]` (the compact, versionable default). `[MessagePackObject(true)]` or `[MessagePackObject(KeyNamingPolicy.CamelCase)]` writes a map keyed by member name instead. The source generator produces the formatter at compile time and reports problems as `MsgPack0xx` diagnostics (a public member without `[Key]`/`[IgnoreMember]`, duplicate keys, a type that cannot be constructed, ...). Making the type `partial` is not required, but it lets the generator put the formatter inside the type, which is what you want for `private` members (`AllowPrivate = true`) and for Native AOT.

Everything goes through `MessagePackSerializerOptions`:

```csharp
// the defaults: source-generated and built-in formatters, generic collections, attributed types
var options = MessagePackSerializerOptions.Default;

// per-call settings are init-only properties of the record; compose with `with`
var guarded = options with { MaxDepth = 64, MaxBufferedMessageSize = 1024 * 1024 };

MessagePackSerializer.Serialize(stream, value, guarded);
await MessagePackSerializer.SerializeAsync(pipeWriter, value, guarded);
var value2 = await MessagePackSerializer.DeserializeAsync<Person>(pipeReader, guarded);
```

Entry points cover `byte[]`/`ReadOnlySpan<byte>`/`ReadOnlySequence<byte>`, `IBufferWriter<byte>`, `Stream` (sync and async), `PipeWriter`/`PipeReader`, message streams (`SerializeMessagesAsync` / `DeserializeMessagesAsync`, one MessagePack value per message), element streams (`SerializeElementsAsync` / `DeserializeElementsAsync`, a single array streamed element by element), the non-generic `Type` forms, and JSON conversion (`ConvertToJson` / `ConvertFromJson`). An element stream writes the array header and then one message per element. Without a `MessageProcessor` that is exactly the whole-array wire `Serialize` / `Deserialize` produce and read. That includes circular references: an element stream shares one identity table across its elements, so an instance shared between elements is written once and back-referenced, and read back as one instance, exactly as `Serialize(array)` / `Deserialize<T[]>` do. Under an envelope processor such as `WithFraming()` each element is wrapped on its own, so the stream round-trips with `DeserializeElementsAsync` but is not readable as one array (nor the reverse). Under a container processor (the LZ4 / Zstandard frames, which own the message boundaries) the element entry points are not available and throw `NotSupportedException`; stream whole messages with `SerializeMessagesAsync` / `DeserializeMessagesAsync` instead.

### Formatter chain, resolver, options

Where v3 had resolvers all the way down, v4 has three distinct things:

```
MessagePackFormatterFactory   (which formatter serves which type; factories compose into a chain)
      └─ MessagePackFormatterResolver   (creates and caches the formatters of one chain; owns the safety flags)
             └─ MessagePackSerializerOptions   (per-call settings: MessageProcessor, MaxDepth, MaxBufferedMessageSize)
```

```csharp
var factory = MessagePackFormatterFactory.Default      // or DefaultAot, DotNetOptimized, DotNetOptimizedAot
    .WithContractless()                                 // types without attributes, as maps (v3 ContractlessStandardResolver)
    .WithTypeless(TypelessTypeLoader.AllowedTypes(typeof(Person), typeof(Order)));

var resolver = new MessagePackFormatterResolver(factory)
{
    HashFloodingResistant = true,        // default
    ValidateRequiredMembers = true,      // default: a missing `required` member or ctor parameter is an error
    ValidateNullableAnnotations = false, // opt-in: nil into a non-nullable reference member is an error
};

var options = new MessagePackSerializerOptions(resolver) { MaxDepth = 500 };
```

`MessagePackFormatterFactory.Combine(a, b, c)` builds a chain by hand; the first factory that serves a type wins, so overrides go first. A resolver is fixed for its lifetime (the cached formatters capture its settings), which is why these are constructor-time properties and not `with`-able options.

## Migrating from v3

The wire format is the same: v3 and v4 read each other's output for everything — primitives, collections, `[MessagePackObject]` arrays and maps, `[Union]`, Typeless, `DateTime` (ext -1), and the LZ4 envelopes. Migration is a source-level exercise.

### 1. Packages and attributes

- Attributes moved into `MessagePack`; `MessagePack.Annotations` is now a type-forwarding shim. Keep referencing it only until the project compiles against v4, then drop it. If a v3-built library in your graph still references `MessagePack.Annotations` 3.x, add an explicit reference to `MessagePack.Annotations` 4.x so that both versions do not end up loaded (CS0433).
- `MessagePackAnalyzer` is gone; the generator and its analyzers ship inside `MessagePack`.
- `[Union(key, typeof(T))]` became `[UnionTag(typeof(T), key)]` / `[UnionTag<T>(key)]` on a root that also carries `[MessagePackObject]`. The string (assembly-qualified name) form is gone. Generic unions can name a type parameter: `[UnionTag("T", 0)]`.
- `[MessagePackAssumedFormattable]` → `[assembly: MessagePackKnownType(typeof(T))]`. `[MessagePackKnownFormatter]` and `[ExcludeFormatterFromSourceGeneratedResolver]` are deleted (the generator never collects formatters into a resolver). `GeneratedMessagePackResolver` no longer exists: generated formatters register themselves.
- `[MessagePackFormatter(typeof(X))]` now names a **factory** (or an open generic formatter type), not a closed formatter. Its property is `FactoryType`.

### 2. Options and resolvers

| v3 | v4 |
|---|---|
| `MessagePackSerializerOptions.Standard` | `MessagePackSerializerOptions.Default` |
| `options.WithResolver(resolver)` | `new MessagePackSerializerOptions(new MessagePackFormatterResolver(factory))` — the resolver is chosen at construction |
| `StandardResolver.Instance` | `MessagePackFormatterFactory.Default` |
| `ContractlessStandardResolver` | `MessagePackFormatterFactory.Default.WithContractless()` |
| `CompositeResolver.Create(a, b)` | `MessagePackFormatterFactory.Combine(a, b)` |
| `TypelessContractlessStandardResolver` / `MessagePackSerializer.Typeless` | `Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())` — prefer `TypelessTypeLoader.AllowedTypes(...)` for untrusted input |
| `options.WithCompression(MessagePackCompression.Lz4BlockArray)` | `options.WithLz4BlockArray()` from `MessagePack.LZ4` (kept for v3 data, `[Obsolete]`); new data should use `WithLz4Frame()` or `MessagePack.Zstandard`'s `WithZstandardFrame()` |
| `options.WithSecurity(MessagePackSecurity.UntrustedData)` | On by default. The knobs are `MaxDepth` (500) and `MaxBufferedMessageSize` (64 MB) on the options, `HashFloodingResistant` on the resolver, and `MaxDecompressedSize` on the compression processors. There is no "trusted data" mode. |
| `options.WithOmitAssemblyVersion(true)` | `WithTypeless(loader, omitAssemblyVersion: true)` |
| `options.WithAllowAssemblyVersionMismatch(true)` | `TypelessTypeLoader.LoadAnyType(allowAssemblyVersionMismatch: true)` |
| `UnityResolver` | `MessagePackFormatterFactory.Default.WithUnity()` (`MessagePack.UnityShims` / the Unity package) |

`DotNetOptimized` is new: `Guid`/`decimal` as 16-byte binary, `DateTime` via `ToBinary` (keeps `DateTimeKind`), `DateTimeOffset` as ticks + offset, `BitArray` bit-packed. It is for .NET-to-.NET exchange and is **not** what v3 wrote; the `Default` chain is.

### 3. Entry points

- `Deserialize<T>(ReadOnlyMemory<byte>)` has no overload yet; pass `.Span`. `byte[]` converts implicitly.
- `Deserialize<T>(..., out int bytesRead)` is gone. For a stream of messages use `DeserializeMessagesAsync` (or read from a `PipeReader`, which leaves the following message unconsumed).
- `MessagePackStreamReader` is gone; `DeserializeMessagesAsync` / `DeserializeElementsAsync` over `Stream` or `PipeReader` replace it.
- `Deserialize<T>(source, ref T value)` populates an existing instance when the type can be refilled: collections are cleared and refilled, and objects with settable members are overwritten member by member. A type whose members can only be set at construction (a constructor-bound, init-only or required member) is constructed afresh instead, like a collection that cannot be cleared, so a member initializer such as `Child = new Child(0)` never shadows the value being read.

### 4. Custom formatters

The formatter interface is generic over the buffers, and the reader/writer structs are replaced by extension methods on the buffer:

```csharp
// v3
public class PointFormatter : IMessagePackFormatter<Point>
{
    public void Serialize(ref MessagePackWriter writer, Point value, MessagePackSerializerOptions options) { ... }
    public Point Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) { ... }
}

// v4: declare it partial and leave the buffer constraints out, the generator adds them for every target
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
        var count = buffer.ReadArrayHeader(ref state); // every header read is bomb-guarded
        var x = buffer.ReadInt32();
        var y = buffer.ReadInt32();
        for (int i = 2; i < count; i++) buffer.Skip(); // version tolerance
        value = new Point(x, y);
    }
}

[MessagePackFormatter(typeof(PointFormatter<,>))] // the open formatter type, or a factory type
public readonly record struct Point(int X, int Y);
```

A formatter is attached either with `[MessagePackFormatter(typeof(...))]` on the type (an open generic formatter, or a `MessagePackFormatterFactory` for anything that needs arguments or serves several types) or by putting a factory in front of the chain. When a formatter calls into another formatter it brackets the call with `state.Enter()` / `state.Exit()` (an analyzer enforces it). Nested formatters come from `resolver.GetFormatter<TWriteBuffer, TReadBuffer, TNested>()` inside `Initialize`.

For types you cannot annotate, `IMessagePackSurrogate<TTarget, TSurrogate>` on a `[MessagePackObject]` struct declares a serialized stand-in; the generator registers the formatter for the target.

### 5. Behaviour differences to check

- **Constructor binding**: parameters bind to members by name (`[Key("name")]` or member name, case-insensitive). v3 bound int-keyed parameters by position; a constructor whose parameter names do not match its members now needs `[SerializationConstructor]` on a matching one or a settable member.
- **Required members** (`required`, or a constructor parameter without a default) missing from the payload throw by default (`ValidateRequiredMembers`). v3 left them at their default.
- **Depth**: `MaxDepth` defaults to 500 for both serialization and deserialization. `0` disables the check — do that only for data you trust completely, since the formatters recurse.
- **Unknown members**: a `[MessagePackObject]` type can declare one `MessagePackUnknownMembers` member (no `[Key]`) and the generated formatter round-trips whatever a newer writer added.
- **Circular references**: `[MessagePackObject(AllowCircularReferences = true)]` (generated types only).
- **Typeless** requires an explicit `TypelessTypeLoader`; `LoadAnyType()` (v3 behaviour, `Type.GetType` over payload-provided names) is `[Obsolete]` to make the trust decision visible.
- **LZ4**: the v3 envelopes read and write exactly as before but are `[Obsolete]`; `WithLz4Frame()` writes standard LZ4 frames that the `lz4` tool reads. A Block reader does not read frames and vice versa, so switch a fleet together.

## Native AOT and trimming

```csharp
var options = MessagePackSerializerOptions.DefaultAot; // source-generated + built-in formatters only, no reflection
```

Everything the generator can see from `[MessagePackObject]` member graphs is covered. Roots that only appear at call sites (`Person[]`, `List<Person>`, `Dictionary<string, Order>` passed straight to `Serialize`) are declared the `JsonSerializerContext` way:

```csharp
[MessagePackSerializable<Person[]>]
[MessagePackSerializable<Dictionary<string, Order>>]
public partial class MySerializationRoots { }
```

One shape stays outside the generator's reach: a generic `[MessagePackObject]` type from **another assembly** closed over this assembly's types (`Shared.Box<AppDto>`). Its formatter is internal to the assembly that declares `Box<T>`, so neither its harvest (which cannot see `AppDto`) nor this assembly's `[MessagePackSerializable<Shared.Box<AppDto>>]` can register the closed form for the AOT chain. Declare the closed instantiation in the assembly that owns the generic type, or keep such a type non-generic.

The `Default` chain is annotated with `RequiresDynamicCode` / `RequiresUnreferencedCode`, so the trimmer tells you where it is used.

## Compression

```csharp
var lz4 = MessagePackSerializerOptions.Default.WithLz4Frame();        // MessagePack.LZ4
var zstd = MessagePackSerializerOptions.Default.WithZstandardFrame(3); // MessagePack.Zstandard, level 3
```

Both are `MessagePackMessageProcessor`s set through `options.MessageProcessor`; a frame is one message, and a stream of messages is a concatenation of frames that the command-line tools decode as one stream. Each processor caps the decompressed size (`MaxDecompressedSize`, 64 MB by default) so a small payload cannot claim gigabytes. Both take a dictionary (`WithLz4Frame(dictionary, dictionaryId)`, `WithZstandardFrame(dictionary)`: a trained zstd dictionary or raw content typical of the messages) that the writer and the reader must share; the frames declare the dictionary id, and a reader refuses a frame declaring one it does not hold. `MessagePack.Zstandard` uses the in-box `System.IO.Compression` codec on .NET 11 and [NativeCompressions](https://github.com/Cysharp/NativeCompressions) below it.

Unlike v3's managed LZ4, `MessagePack.LZ4` (and `MessagePack.Zstandard` below .NET 11) runs on NativeCompressions' native libraries, so the platform conditions of that package apply: Windows, Linux and macOS on x64 / arm64 (macOS 15 or later), Android (arm, arm64, x64), iOS and Mac Catalyst (.NET 10 or later). There is no x86 (32-bit Intel) library, so a .NET Framework exe must set `<PlatformTarget>x64</PlatformTarget>` (or `arm64`) or a `RuntimeIdentifier`, because the SDK restores an unspecified one as `win-x86` and the first call would throw `DllNotFoundException`. Unity needs the NuGetForUnity runtime settings and, for iOS, the `NativeCompressions.Unity` editor package described in the [NativeCompressions README](https://github.com/Cysharp/NativeCompressions#unity).

## Unity

The serializer core (`MessagePack`, `SerializerFoundation` and their dependencies) comes from NuGet, for example through [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity); the UnityEngine formatters are the `MessagePack.Unity` UPM package (`src/MessagePack.Unity`, Unity 2022.3.12f1 or later, the first release whose Roslyn runs the source generator). Compose them into the chain:

```csharp
var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
    MessagePackFormatterFactory.DefaultAot.WithUnity())); // IL2CPP: no runtime generic instantiation
```

Under IL2CPP use `DefaultAot` and make the annotated types `partial` so the formatters are generated into them. The wire form of the Unity types is v3's, so v3 clients and v4 servers (with `MessagePack.UnityShims`) interoperate.

## What is not in this preview

- A `ReadOnlyMemory<byte>` overload of `Deserialize`, and `[Obsolete]` bridges for the removed v3 option/resolver names (today they are plain compile errors; the table above is the map).
- A v4 Unity test project; the Unity package is exercised through `MessagePack.UnityShims` on .NET only.
- Unloading of collectible `AssemblyLoadContext`s that share the `MessagePack` assembly with their host: the generated module initializers register a plugin's types and factories in the process-wide registry (and the `Type`-based entries cache per type), which keeps the plugin assembly alive. Load MessagePack into the plugin context too, or restart the process to reclaim it.
- Documentation beyond this file. The XML docs on the public types are complete, and the `tests/` directory is the most precise description of behaviour, including the `V3Compat` suite that runs v3's own tests against v4.

Issues and feedback: https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues

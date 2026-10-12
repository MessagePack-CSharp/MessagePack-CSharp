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
| `MessagePack.AspNetCoreMvcFormatter` | ASP.NET Core MVC input/output formatters for `application/x-msgpack` (`net10.0` only). |
| `MessagePack.SignalR` | The SignalR `messagepack` hub protocol, wire-compatible with `Microsoft.AspNetCore.SignalR.Protocols.MessagePack`, which it replaces (`net10.0` only). |
| `MessagePack.Annotations` | Migration shim only: type-forwards the v3 attributes to `MessagePack`. See [Migrating from v3](#migrating-from-v3). |

Targets: `netstandard2.0`, `netstandard2.1`, `net9.0`, `net10.0`, `net11.0`. The full-speed path (ref struct buffers, `allows ref struct`) exists from `net9.0` up; .NET Framework and Unity get the `netstandard2.0` build, and **a `net8.0` project gets the `netstandard2.1` build**. The netstandard builds have no formatters for `Half`, `Int128`, `UInt128`, `DateOnly`, `TimeOnly`, `Rune` and `NFloat` (and `netstandard2.0` none for `Index` / `Range`): serializing them throws, under `WithContractless()` too, so target `net9.0` or later for them. The core depends on [SerializerFoundation](https://github.com/Cysharp/SerializerFoundation), which provides the buffer abstractions.

A library that declares `[MessagePackObject]` types or formatters and targets only `netstandard2.x` gets generated formatters for the Compatible (non-ref-struct) buffers only. On a `net9.0`+ host every root type whose object graph includes one of them (a `List<Dto>` too) is rerouted to those buffers: the output is identical and the cost measured here is about 5% on serialize and 8% on deserialize, but nothing reports it by default. Multi-target such a library (`<TargetFrameworks>netstandard2.1;net9.0</TargetFrameworks>`); to find out whether a reroute happens, subscribe to `MessagePackFormatterResolver.CompatibilityFallback` (raised with the root type) or set `ThrowOnLegacyFormatter = true` on the resolver to make it an error.

The `netstandard2.0` and `netstandard2.1` builds of the core depend on the `System.Text.Json` 10.0.0 package (`ConvertToJson` / `ConvertFromJson`); `net9.0` and later use the in-box one.

The two ASP.NET Core packages register through the usual builders:

```csharp
builder.Services.AddControllers().AddMessagePackFormatters(); // MVC: application/x-msgpack in and out
builder.Services.AddSignalR().AddMessagePackProtocol();       // SignalR: the "messagepack" hub protocol, Microsoft-compatible defaults
// both take a MessagePackSerializerOptions overload; the SignalR one is also the overload for trimmed / Native AOT apps
```

For SignalR on Native AOT, mind the enums: the defaults (like the official protocol) send enum *names*, and `DefaultAot` sends integers, which the official .NET, JavaScript and Java clients then fail to read. Compose `EnumAsStringFormatterFactory<TEnum>` for each enum in front of `DefaultAot` (`MessagePackFormatterFactory.Combine(new EnumAsStringFormatterFactory<Status>(), MessagePackFormatterFactory.DefaultAot)`), and annotate the hub argument types, since the contractless tier is not available on that chain.

The source generator's output needs **C# 9** (C# 13 on `net9.0` and later, which is those SDKs' default). `net48` and `netstandard2.0` projects default to C# 7.3, so a project that declares `[MessagePackObject]` types there must set `<LangVersion>9.0</LangVersion>` or later; otherwise the generator reports MsgPack024 and generates nothing. Projects that only use the reflection tier are unaffected. On those targets the generator also emits a `ModuleInitializerAttribute` polyfill when the compilation has none; a project that uses [PolySharp](https://github.com/Sergio0694/PolySharp) is detected through its build properties and gets no second copy (if you exclude that attribute from PolySharp with `PolySharpExcludeGeneratedTypes`, the generator emits its own again).

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

Entry points cover `byte[]`/`ReadOnlySpan<byte>`/`ReadOnlySequence<byte>`, `IBufferWriter<byte>`, `Stream` (sync and async), `PipeWriter`/`PipeReader`, message streams (`SerializeMessagesAsync` / `DeserializeMessagesAsync`, one MessagePack value per message), element streams (`SerializeElementsAsync` / `DeserializeElementsAsync`, a single array streamed element by element), the non-generic `Type` forms, and JSON conversion (`ConvertToJson` / `ConvertFromJson`). An element stream writes the array header and then one message per element. Without a `MessageProcessor` that is exactly the whole-array wire `Serialize` / `Deserialize` produce and read. That includes circular references: an element stream shares one identity table across its elements, so an instance shared between elements is written once and back-referenced, and read back as one instance, exactly as `Serialize(array)` / `Deserialize<T[]>` do. The identity table is what makes that possible, so for an `AllowCircularReferences` element type the enumerator keeps every element's instances reachable until the enumeration ends (as `Deserialize<T[]>` does, where the array holds them anyway); for untrusted input of such a type, stream whole messages with `DeserializeMessagesAsync`, which keeps nothing between messages. Under an envelope processor such as `WithFraming()` each element is wrapped on its own, so the stream round-trips with `DeserializeElementsAsync` but is not readable as one array (nor the reverse). Under a container processor (the LZ4 / Zstandard frames, which own the message boundaries) the element entry points are not available and throw `NotSupportedException`; stream whole messages with `SerializeMessagesAsync` / `DeserializeMessagesAsync` instead.

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
    .WithTypeless(TypelessTypeLoader.AllowedTypes(typeof(Person), typeof(Order))); // every non-primitive type an object slot can hold (Guid, DateTime, enums, collections too)

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

The wire format is the same: v3 and v4 read each other's output — primitives, collections, `[MessagePackObject]` arrays and maps, `[Union]`, `DateTime` (ext -1), and the LZ4 envelopes — as long as the v4 chain matches the v3 resolver that wrote the data. The table in §2 gives the matching chain; Typeless is the case to watch, because v3's Typeless chain wrote `DateTime` in its native form (see the Typeless row). Migration is a source-level exercise.

**Before you start: v4 replaces v3 for the whole dependency graph.** The package id and the strong name are v3's, so NuGet unifies every `MessagePack >= 2.x / 3.x` dependency in the graph to the v4 version you reference, without a warning. A library compiled against v3 then fails at run time (`MissingMethodException` / `TypeLoadException` on first use): `Microsoft.AspNetCore.SignalR.Protocols.MessagePack`, `StreamJsonRpc`, `MagicOnion` and any in-house v3 library included. Move to v4 only when everything in the graph that uses MessagePack can move with it. `MessagePack.SignalR` is the replacement for `Microsoft.AspNetCore.SignalR.Protocols.MessagePack` (same `messagepack` protocol name), not a companion to it.

### 1. Packages and attributes

- Attributes moved into `MessagePack`; `MessagePack.Annotations` is now a type-forwarding shim. Keep referencing it only until the project compiles against v4, then drop it. If a v3-built library in your graph still references `MessagePack.Annotations` 3.x, add an explicit reference to `MessagePack.Annotations` 4.x so that both versions do not end up loaded (CS0433).
- `MessagePackAnalyzer` is gone; the generator and its analyzers ship inside `MessagePack`.
- `[Union(key, typeof(T))]` became `[UnionTag(typeof(T), key)]` / `[UnionTag<T>(key)]` on a root that also carries `[MessagePackObject]`. The string (assembly-qualified name) form is gone. Generic unions can name a type parameter: `[UnionTag("T", 0)]`.
- `[MessagePackAssumedFormattable]` → `[assembly: MessagePackKnownType(typeof(T))]`. `[MessagePackKnownFormatter]` and `[ExcludeFormatterFromSourceGeneratedResolver]` are deleted (the generator never collects formatters into a resolver). `GeneratedMessagePackResolver` no longer exists: generated formatters register themselves.
- `[MessagePackFormatter(typeof(X))]` now names a **factory** (or an open generic formatter type), not a closed formatter. Its property is `FactoryType`.
- The generic attribute forms (`[UnionTag<T>(key)]`, `[MessagePackSerializable<T>]`, `[MessagePackFormatter<TFactory>]`) exist only in the `net9.0` and later builds. A `netstandard`, .NET Framework or Unity project uses the `typeof` forms: `[UnionTag(typeof(T), key)]`, `[MessagePackSerializable(typeof(T))]`, `[MessagePackFormatter(typeof(TFactory))]`.

### 2. Options and resolvers

| v3 | v4 |
|---|---|
| `MessagePackSerializerOptions.Standard` | `MessagePackSerializerOptions.Default` |
| `options.WithResolver(resolver)` | `new MessagePackSerializerOptions(new MessagePackFormatterResolver(factory))` — the resolver is chosen at construction |
| `StandardResolver.Instance` | `MessagePackFormatterFactory.Default` |
| `ContractlessStandardResolver` | `MessagePackFormatterFactory.Default.WithContractless()` |
| `CompositeResolver.Create(a, b)` | `MessagePackFormatterFactory.Combine(a, b)` |
| `TypelessContractlessStandardResolver` / `MessagePackSerializer.Typeless` | `MessagePackFormatterFactory.Combine(NativeDateTimeFormatterFactory.Instance, MessagePackFormatterFactory.Default).WithContractless(allowPrivate: true).WithTypeless(TypelessTypeLoader.LoadAnyType())`. v3's Typeless chain wrote `DateTime` (also in `DateTime?`, `DateTime[]` and collections) as a `ToBinary` int64 and included non-public members; without `NativeDateTimeFormatterFactory` those `DateTime`s fail to read in both directions, and without `allowPrivate` non-public members are silently dropped. Prefer `TypelessTypeLoader.AllowedTypes(...)` for untrusted input; it must list every type that can appear in an `object` or abstract slot, `Guid`, `DateTime`, enums and collection types included, since everything but bool, the integers, float, double, string and byte[] travels with its type name |
| `options.WithCompression(MessagePackCompression.Lz4BlockArray)` | `options.WithLz4BlockArray()` from `MessagePack.LZ4` (kept for v3 data, `[Obsolete]`); new data should use `WithLz4Frame()` or `MessagePack.Zstandard`'s `WithZstandardFrame()` |
| `options.WithSecurity(MessagePackSecurity.UntrustedData)` | On by default. The knobs are `MaxDepth` (500) and `MaxBufferedMessageSize` (64 MB) on the options, `HashFloodingResistant` on the resolver, and `MaxDecompressedSize` on the compression processors. There is no "trusted data" mode. |
| `options.WithOmitAssemblyVersion(true)` | `WithTypeless(loader, omitAssemblyVersion: true)` |
| `options.WithAllowAssemblyVersionMismatch(true)` | `TypelessTypeLoader.LoadAnyType(allowAssemblyVersionMismatch: true)` |
| `UnityResolver` | `MessagePackFormatterFactory.Default.WithUnity()` (`MessagePack.UnityShims` / the Unity package) |
| `MessagePackSerializer.SerializeToJson(value, options)` / `ConvertToJson(bytes, options)` | `ConvertToJson(bytes)` has no `options`: it reads one plain MessagePack value. For a compressed or framed payload, unwrap it first with `options.MessageProcessor.TryDecode(bytes, out var message)` and pass `message`'s value; `SerializeToJson` is `ConvertToJson(Serialize(value))`. |
| `MessagePackSerializer.DefaultOptions = ...` | Gone: there is no mutable process-wide default. Build one `MessagePackSerializerOptions` and pass it to every call (the parameterless overloads use `MessagePackSerializerOptions.Default`, which cannot be changed). In Unity, v3's package set `DefaultOptions` to the `UnityResolver` chain at startup; v4 does not, so a call without options fails for `Vector3` and friends until the `WithUnity()` options are passed. |

`DotNetOptimized` is new: `Guid`/`decimal` as 16-byte binary, `DateTime` via `ToBinary` (keeps `DateTimeKind`), `DateTimeOffset` as ticks + offset, `BitArray` bit-packed. It is for .NET-to-.NET exchange and is **not** what v3 wrote; the `Default` chain is.

### 3. Entry points

- `Deserialize<T>(ReadOnlyMemory<byte>)` has no overload yet; pass `.Span`. `byte[]` converts implicitly.
- `Deserialize<T>(..., out int bytesRead)` is gone. For a stream of messages use `DeserializeMessagesAsync` (or read from a `PipeReader`, which leaves the following message unconsumed).
- `MessagePackStreamReader` is gone; `DeserializeMessagesAsync` / `DeserializeElementsAsync` over `Stream` or `PipeReader` replace it.
- `Deserialize<T>(source, ref T value)` populates an existing instance when the type can be refilled: collections are cleared and refilled, and objects with settable members are overwritten member by member, recursively into the member values that instance holds. A type whose members can only be set at construction (a constructor-bound, init-only or required member) is constructed afresh instead, like a collection that cannot be cleared, so a member initializer such as `Child = new Child(0)` never shadows the value being read. Element instances survive only where the collection is refilled in place: `T[]` of the same length (and an `ArraySegment<T>` / `Memory<T>` over one), multi-dimensional arrays of the same dimensions, and `List<T>`; every other collection, an `IList<T>`-typed member holding that same `List<T>` included, is cleared and re-added, so its elements are new instances.
- **This applies to member initializers too, and differs from v3.** A plain `Deserialize<T>(source)` constructs the object (running its initializers) and then refills, in place, whatever the initializers put in its settable members: `List<int> Items { get; set; } = new()` is cleared and refilled (one allocation, and a comparer or concrete collection type chosen in the initializer is kept), the same way Newtonsoft.Json's `ObjectCreationHandling.Auto` works. v3 constructed a new value for every member. Two consequences: an `ObservableCollection` initializer raises its events while the object is still being read, and an instance shared through an initializer (`= Defaults.Settings`) is written to. A member absent from the payload keeps the value its initializer gave it. A covariant array initializer (`object[] Tags = new string[n]`) is replaced rather than refilled.

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

For types you cannot annotate, `IMessagePackSurrogate<TTarget, TSurrogate>` on a `[MessagePackObject]` struct declares a serialized stand-in; the generator registers the formatter for the target. Registration rides the declaring assembly's module initializer, which runs when that assembly's code first executes. For a target that lives in a *third* assembly (a surrogate in library B for a type of library A), every assembly that references B adopts the surrogate into its own generated factory and module initializer, so serializing the A type never depends on B being called into first; a `public` surrogate is enough, no declaration on the consumer side. If two referenced libraries declare surrogates for the same foreign type, neither is adopted (MsgPack019) and the chain must be composed by hand with `SurrogateFormatterFactory<TTarget, TSurrogate>`. Do not serialize from inside a `[ModuleInitializer]` or a static constructor: the resolver builds formatter graphs under a lock and, on a miss, runs the module initializers of the missing type's assembly, so an initializer that serializes on one thread while another thread resolves a type of that assembly deadlocks with it.

### 5. Behaviour differences to check

- **Constructor binding**: parameters bind to members by name (`[Key("name")]` or member name, case-insensitive). v3 bound int-keyed parameters by position; a constructor whose parameter names do not match its members now needs `[SerializationConstructor]` on a matching one or a settable member.
- **Required members** (`required`, or a constructor parameter without a default) missing from the payload throw by default (`ValidateRequiredMembers`). v3 left them at their default.
- **Depth**: `MaxDepth` defaults to 500 for both serialization and deserialization. `0` disables the check — do that only for data you trust completely, since the formatters recurse. Independently of `MaxDepth`, nesting that would exhaust the thread's stack (a small thread, such as IIS's 512 KB, with a stack-hungry shape like `object[]` on .NET Framework) throws `MessagePackSerializationException` instead of crashing the process.
- **Not on the default chain**: types whose deserialization looks a payload-provided name up in the runtime or the OS are opt-in, composed before `Default` with their own factory. `System.Type` (`TypeFormatterFactory`, `[Obsolete]`-gated: `Type.GetType` can load assemblies) and `ExpandoObject` (`ExpandoObjectFormatterFactory`: quadratic `Add`) were on v3's default resolvers and are not on v4's. `CultureInfo` (`CultureInfoFormatterFactory`, predefined culture names only) and `TimeZoneInfo` (`TimeZoneInfoFormatterFactory`, `FindSystemTimeZoneById` on the reading machine) are new in v4 and opt-in from the start. The default chain throws `InvalidOperationException` naming the factory for all four.
- **Unknown members**: a `[MessagePackObject]` type can declare one `MessagePackUnknownMembers` member (no `[Key]`) and the generated formatter round-trips whatever a newer writer added.
- **Circular references**: `[MessagePackObject(AllowCircularReferences = true)]` (generated types only).
- **Typeless** requires an explicit `TypelessTypeLoader`; `LoadAnyType()` (v3 behaviour, `Type.GetType` over payload-provided names) is `[Obsolete]` to make the trust decision visible.
- **Primitive writers**: `MessagePackPrimitives.TryWriteInt16/32/64` and `TryWriteUInt16/32/64` now write the *smallest* msgpack form for the value (1 to 9 bytes), the same as `WriteInt32` on a buffer. v3's methods of the same name wrote the fixed-width code regardless of value; that form is now `TryWriteForcedInt16/32/64` and `TryWriteForcedUInt16/32/64` (and `TryWriteForcedInt8` / `TryWriteForcedUInt8`). Code that reserves a slot and fills it in later must use the `Forced` methods; with the old names it would produce a shorter token than the slot and corrupt what follows, without any error.
- **LZ4**: the v3 envelopes read as before but are `[Obsolete]`; `WithLz4Frame()` writes standard LZ4 frames that the `lz4` tool reads. A Block reader does not read frames and vice versa, so switch a fleet together. `Lz4BlockArray` output is read by v3 and by every v4 build, but it is not byte-identical to v3's, nor between the `net9.0+` and `netstandard` builds (the block boundaries follow the writer's buffer segments), so do not compare or hash compressed payloads across writers.
- **Diagnostics IDs**: the generator's `MsgPack001`-`MsgPack018` are new rules under v3's numbers (v3's analyzer is gone). Remove any `dotnet_diagnostic.MsgPack0xx.severity` suppression carried over from v3: it would now hide a different error, such as a duplicate `[Key]`.
- **Hash flooding**: `HashFloodingResistant` (on by default) gives the dictionaries and sets the formatters build a per-process randomized comparer for bool, char, integer, enum, `TimeSpan`, `Guid`, `DateOnly`, `TimeOnly`, `Int128` and `UInt128` keys (and their nullable forms; `string` on .NET 9+ relies on the runtime's own randomized hashing, downlevel it gets the resistant comparer too). Any other key type hashes with its own `GetHashCode`, so a user-defined key should use `HashCode.Combine`; and only the exact BCL types (`Dictionary`, `HashSet`, `ConcurrentDictionary`, the frozen and immutable ones) are covered, not a class derived from them, `Hashtable`, or a custom collection the generic tier constructs. The deserialized collections keep that comparer, which makes their later lookups somewhat slower than the default; `new MessagePackFormatterResolver(factory) { HashFloodingResistant = false }` turns it off for trusted data.
- **`object` slots**: strings inside `object`, `object[]` and `Dictionary<object, object>` payloads are read and written by the object formatter directly; `StringInterningFormatterFactory` and a custom `string` formatter do not apply to them.

## Native AOT and trimming

```csharp
var options = MessagePackSerializerOptions.DefaultAot; // source-generated + built-in formatters only, no reflection
```

Everything the generator can see from `[MessagePackObject]` member graphs is covered, and `DefaultAot` never closes a generic formatter at run time, on Native AOT or on the JIT: an instantiation the generator did not construct (`Box<Guid>` used only at a call site) is formatter-not-found in a JIT test run exactly as in the published app, where `Default` would have closed it with `MakeGenericType`. Roots that only appear at call sites (`Person[]`, `List<Person>`, `Dictionary<string, Order>` passed straight to `Serialize`) are declared the `JsonSerializerContext` way:

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

Three things to know when a processor is set. `MaxBufferedMessageSize` counts the *compressed* bytes a Stream or PipeReader entry buffers; the decoded size is bounded only by the processor's `MaxDecompressedSize`, and a 2 KB frame can legitimately decode to the full 64 MB, so for untrusted input lower both (`WithZstandardFrame(3, maxDecompressedSize: 1 << 20)`). `Deserialize(stream)` reads the stream to its end as one message; several frames in one stream are read with `DeserializeMessagesAsync` or through a `PipeReader`. The Zstandard decoder's window cap follows `MaxDecompressedSize`: a frame written by another producer with a larger window (`zstd --long`, or any zstd output once the cap is below the window) is rejected as invalid, so raise `MaxDecompressedSize` to at least that window (128 MB for `--long`).

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

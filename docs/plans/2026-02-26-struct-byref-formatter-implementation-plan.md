# By-Ref Struct Formatter API Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add compatibility-preserving by-reference serialization/deserialization APIs for struct optimization, including custom formatter reuse of existing struct instances during deserialization.

**Architecture:** Keep `IMessagePackFormatter<T>` unchanged and introduce additive optional interfaces plus serializer overloads. Route calls through a dispatch layer that prefers new interfaces and falls back to legacy behavior. Integrate dispatch into serializer entrypoints, dynamic resolver IL generation, and source-generator templates so nested struct members can benefit.

**Tech Stack:** C#/.NET 8-9, MessagePack runtime (`MessagePackWriter`/`MessagePackReader`), Reflection.Emit (DynamicObjectResolver), T4 templates for source generator, xUnit/Shouldly tests.

---

Required execution skills: `@superpowers/test-driven-development`, `@superpowers/verification-before-completion`.

### Task 1: Add failing API behavior tests for by-ref formatter pathways

**Files:**
- Create: `tests/MessagePack.Tests/ByRefFormatterApiTests.cs`
- Test: `tests/MessagePack.Tests/MessagePack.Tests.csproj`

**Step 1: Write the failing test**

```csharp
using System.Buffers;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Nerdbank.Streams;
using Xunit;

namespace MessagePack.Tests;

public class ByRefFormatterApiTests
{
    [Fact]
    public void SerializeIn_UsesByRefInterface_WhenAvailable()
    {
        var formatter = new CounterFormatter();
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(new IMessagePackFormatter[] { formatter }, new IFormatterResolver[] { StandardResolver.Instance }));

        var sequence = new Sequence<byte>();
        var writer = new MessagePackWriter(sequence);
        var value = new TestStruct { Id = 3, PooledObject = "old" };

        MessagePackSerializer.Serialize(ref writer, in value, options);

        Assert.Equal(1, formatter.SerializeInCalls);
        Assert.Equal(0, formatter.SerializeByValueCalls);
    }

    [Fact]
    public void DeserializeRef_ReusesExistingValue_WhenAvailable()
    {
        var formatter = new CounterFormatter();
        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(new IMessagePackFormatter[] { formatter }, new IFormatterResolver[] { StandardResolver.Instance }));

        var payload = MessagePackSerializer.Serialize(new TestStruct { Id = 9, PooledObject = "new" }, options);
        var reader = new MessagePackReader(payload);
        var value = new TestStruct { Id = -1, PooledObject = "old-pooled" };

        MessagePackSerializer.Deserialize(ref reader, ref value, options);

        Assert.Equal("old-pooled", formatter.LastObservedOldObject);
        Assert.Equal(9, value.Id);
        Assert.Equal("new", value.PooledObject);
        Assert.Equal(1, formatter.DeserializeRefCalls);
    }

    [MessagePackObject]
    public struct TestStruct
    {
        [Key(0)] public int Id;
        [Key(1)] public object? PooledObject;
    }

    private sealed class CounterFormatter :
        IMessagePackFormatter<TestStruct>,
        IMessagePackFormatterSerializeIn<TestStruct>,
        IMessagePackFormatterDeserializeRef<TestStruct>
    {
        internal int SerializeByValueCalls;
        internal int SerializeInCalls;
        internal int DeserializeByValueCalls;
        internal int DeserializeRefCalls;
        internal object? LastObservedOldObject;

        public void Serialize(ref MessagePackWriter writer, TestStruct value, MessagePackSerializerOptions options)
        {
            SerializeByValueCalls++;
            writer.WriteArrayHeader(2);
            writer.Write(value.Id);
            MessagePackSerializer.Serialize(ref writer, value.PooledObject, options);
        }

        public void Serialize(ref MessagePackWriter writer, in TestStruct value, MessagePackSerializerOptions options)
        {
            SerializeInCalls++;
            writer.WriteArrayHeader(2);
            writer.Write(value.Id);
            MessagePackSerializer.Serialize(ref writer, value.PooledObject, options);
        }

        public TestStruct Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            DeserializeByValueCalls++;
            var result = default(TestStruct);
            Deserialize(ref reader, ref result, options);
            return result;
        }

        public void Deserialize(ref MessagePackReader reader, ref TestStruct value, MessagePackSerializerOptions options)
        {
            DeserializeRefCalls++;
            LastObservedOldObject = value.PooledObject;
            _ = reader.ReadArrayHeader();
            value.Id = reader.ReadInt32();
            value.PooledObject = MessagePackSerializer.Deserialize<object?>(ref reader, options);
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/MessagePack.Tests/MessagePack.Tests.csproj -f net9.0 --filter "FullyQualifiedName~ByRefFormatterApiTests"`
Expected: FAIL with compile errors for missing by-ref interfaces and missing serializer overloads.

**Step 3: Write minimal implementation**

No production change in this task.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/MessagePack.Tests/MessagePack.Tests.csproj -f net9.0 --filter "FullyQualifiedName~ByRefFormatterApiTests"`
Expected: still FAIL (red state intentionally preserved for next task).

**Step 5: Commit**

```bash
git add tests/MessagePack.Tests/ByRefFormatterApiTests.cs
git commit -m "test: add failing by-ref formatter API tests"
```

### Task 2: Add additive formatter interfaces and dispatch core

**Files:**
- Modify: `src/MessagePack/Formatters/IMessagePackFormatter\`1.cs`
- Create: `src/MessagePack/Internal/FormatterDispatch.cs`
- Modify: `src/MessagePack/MessagePack.csproj` (include new file if required by project style)
- Test: `tests/MessagePack.Tests/ByRefFormatterApiTests.cs`

**Step 1: Write the failing test**

Use Task 1 tests as the red test baseline.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/MessagePack.Tests/MessagePack.Tests.csproj -f net9.0 --filter "FullyQualifiedName~ByRefFormatterApiTests"`
Expected: FAIL.

**Step 3: Write minimal implementation**

```csharp
// src/MessagePack/Formatters/IMessagePackFormatter`1.cs
public interface IMessagePackFormatterSerializeIn<T>
{
    void Serialize(ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options);
}

public interface IMessagePackFormatterDeserializeRef<T>
{
    void Deserialize(ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options);
}
```

```csharp
// src/MessagePack/Internal/FormatterDispatch.cs
using MessagePack.Formatters;

namespace MessagePack.Internal;

internal static class FormatterDispatch<T>
{
    internal static void Serialize(ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options)
    {
        var formatter = options.Resolver.GetFormatterWithVerify<T>();
        if (formatter is IMessagePackFormatterSerializeIn<T> byRef)
        {
            byRef.Serialize(ref writer, in value, options);
            return;
        }

        formatter.Serialize(ref writer, value, options);
    }

    internal static T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var formatter = options.Resolver.GetFormatterWithVerify<T>();
        return formatter.Deserialize(ref reader, options);
    }

    internal static void Deserialize(ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options)
    {
        var formatter = options.Resolver.GetFormatterWithVerify<T>();
        if (formatter is IMessagePackFormatterDeserializeRef<T> byRef)
        {
            byRef.Deserialize(ref reader, ref value, options);
            return;
        }

        value = formatter.Deserialize(ref reader, options);
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/MessagePack.Tests/MessagePack.Tests.csproj -f net9.0 --filter "FullyQualifiedName~ByRefFormatterApiTests"`
Expected: still FAIL because serializer overloads do not exist yet.

**Step 5: Commit**

```bash
git add src/MessagePack/Formatters/IMessagePackFormatter`1.cs src/MessagePack/Internal/FormatterDispatch.cs src/MessagePack/MessagePack.csproj
git commit -m "feat: add by-ref formatter interfaces and dispatch core"
```

### Task 3: Add serializer overloads and wire dispatch in runtime entrypoints

**Files:**
- Modify: `src/MessagePack/MessagePackSerializer.cs`
- Test: `tests/MessagePack.Tests/ByRefFormatterApiTests.cs`
- Test: `tests/MessagePack.Tests/MessagePackSerializerTest.cs` (optional parity assertions)

**Step 1: Write the failing test**

Add parity and fallback assertion:

```csharp
[Fact]
public void DeserializeRef_FallsBackToLegacyFormatter_WhenRefInterfaceMissing()
{
    var options = MessagePackSerializerOptions.Standard.WithResolver(
        CompositeResolver.Create(new IMessagePackFormatter[] { new LegacyOnlyFormatter() }, new IFormatterResolver[] { StandardResolver.Instance }));

    var bytes = MessagePackSerializer.Serialize(new LegacyStruct { Value = 17 }, options);
    var reader = new MessagePackReader(bytes);
    var value = new LegacyStruct { Value = -1 };

    MessagePackSerializer.Deserialize(ref reader, ref value, options);

    Assert.Equal(17, value.Value);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/MessagePack.Tests/MessagePack.Tests.csproj -f net9.0 --filter "FullyQualifiedName~ByRefFormatterApiTests"`
Expected: FAIL.

**Step 3: Write minimal implementation**

```csharp
// MessagePackSerializer.cs
public static void Serialize<T>(ref MessagePackWriter writer, in T value, MessagePackSerializerOptions? options = null)
{
    options ??= DefaultOptions;
    bool originalOldSpecValue = writer.OldSpec;
    if (options.OldSpec.HasValue)
    {
        writer.OldSpec = options.OldSpec.Value;
    }

    try
    {
        if (options.Compression.IsCompression() && !PrimitiveChecker<T>.IsMessagePackFixedSizePrimitive)
        {
            using var scratchRental = options.SequencePool.Rent();
            var scratchWriter = writer.Clone(scratchRental.Value);
            Internal.FormatterDispatch<T>.Serialize(ref scratchWriter, in value, options);
            scratchWriter.Flush();
            ToLZ4BinaryCore(scratchRental.Value, ref writer, options.Compression, options.CompressionMinLength);
        }
        else
        {
            Internal.FormatterDispatch<T>.Serialize(ref writer, in value, options);
        }
    }
    catch (Exception ex)
    {
        throw new MessagePackSerializationException($"Failed to serialize {typeof(T).FullName} value.", ex);
    }
    finally
    {
        writer.OldSpec = originalOldSpecValue;
    }
}

public static void Deserialize<T>(ref MessagePackReader reader, ref T value, MessagePackSerializerOptions? options = null)
{
    options ??= DefaultOptions;

    try
    {
        if (options.Compression.IsCompression())
        {
            using var msgPackUncompressedRental = options.SequencePool.Rent();
            if (TryDecompress(ref reader, msgPackUncompressedRental.Value))
            {
                var uncompressedReader = reader.Clone(msgPackUncompressedRental.Value.AsReadOnlySequence);
                Internal.FormatterDispatch<T>.Deserialize(ref uncompressedReader, ref value, options);
            }
            else
            {
                Internal.FormatterDispatch<T>.Deserialize(ref reader, ref value, options);
            }
        }
        else
        {
            Internal.FormatterDispatch<T>.Deserialize(ref reader, ref value, options);
        }
    }
    catch (Exception ex)
    {
        throw new MessagePackSerializationException($"Failed to deserialize {typeof(T).FullName} value.", ex);
    }
}
```

Also route existing `Serialize<T>(ref MessagePackWriter writer, T value, ...)` through the new `in` overload to keep one serialization path.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/MessagePack.Tests/MessagePack.Tests.csproj -f net9.0 --filter "FullyQualifiedName~ByRefFormatterApiTests"`
Expected: PASS.

**Step 5: Commit**

```bash
git add src/MessagePack/MessagePackSerializer.cs tests/MessagePack.Tests/ByRefFormatterApiTests.cs
git commit -m "feat: add serializer in/ref overloads with dispatch fallback"
```

### Task 4: Integrate by-ref dispatch into DynamicObjectResolver emitted formatters

**Files:**
- Modify: `src/MessagePack/Resolvers/DynamicObjectResolver.cs`
- Create: `tests/MessagePack.Tests/DynamicObjectResolverByRefTests.cs`
- Test: `tests/MessagePack.Tests/MessagePack.Tests.csproj`

**Step 1: Write the failing test**

```csharp
[MessagePackObject]
public class Holder
{
    [Key(0)]
    public PooledStruct Value { get; set; }
}

[Fact]
public void DynamicObjectResolver_UsesRefDeserialize_WhenFormatterSupportsIt()
{
    var formatter = new PooledStructFormatterWithCounters();
    var options = MessagePackSerializerOptions.Standard.WithResolver(
        CompositeResolver.Create(new IMessagePackFormatter[] { formatter }, new IFormatterResolver[] { DynamicObjectResolver.Instance }));

    var holder = new Holder { Value = new PooledStruct { Id = 10, PooledObject = "new" } };
    byte[] bytes = MessagePackSerializer.Serialize(holder, options);

    var existing = new Holder { Value = new PooledStruct { Id = -1, PooledObject = "old" } };
    var reader = new MessagePackReader(bytes);
    MessagePackSerializer.Deserialize(ref reader, ref existing, options);

    Assert.Equal("old", formatter.LastObservedOldObject);
    Assert.Equal(10, existing.Value.Id);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/MessagePack.Tests/MessagePack.Tests.csproj -f net9.0 --filter "FullyQualifiedName~DynamicObjectResolverByRefTests"`
Expected: FAIL; dynamic emitted formatter path still binds legacy calls.

**Step 3: Write minimal implementation**

Update IL emit binding sites to call dispatch methods for serialize/deserialize member operations, not direct `IMessagePackFormatter<T>.Serialize/Deserialize` method infos.

```csharp
// Replace getSerialize/getDeserialize MethodInfo plumbing with:
private static readonly MethodInfo dispatchSerialize = typeof(Internal.FormatterDispatch<>)
    .GetMethod("Serialize", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, ...);
private static readonly MethodInfo dispatchDeserializeRef = typeof(Internal.FormatterDispatch<>).GetMethod(...);
private static readonly MethodInfo dispatchDeserialize = typeof(Internal.FormatterDispatch<>).GetMethod(...);
```

Ensure emitted code keeps old behavior for classes and for formatters that do not implement by-ref interfaces.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/MessagePack.Tests/MessagePack.Tests.csproj -f net9.0 --filter "FullyQualifiedName~DynamicObjectResolverByRefTests"`
Expected: PASS.

**Step 5: Commit**

```bash
git add src/MessagePack/Resolvers/DynamicObjectResolver.cs tests/MessagePack.Tests/DynamicObjectResolverByRefTests.cs
git commit -m "feat: route DynamicObjectResolver through by-ref formatter dispatch"
```

### Task 5: Integrate by-ref calls into source-generator templates and runtime execution tests

**Files:**
- Modify: `src/MessagePack.SourceGenerator/Transforms/FormatterTemplate.tt`
- Modify: `src/MessagePack.SourceGenerator/Transforms/FormatterTemplate.cs`
- Modify: `src/MessagePack.SourceGenerator/Transforms/StringKey/StringKeyFormatterTemplate.tt`
- Modify: `src/MessagePack.SourceGenerator/Transforms/StringKey/StringKeyFormatterTemplate.cs`
- Modify: `tests/MessagePack.SourceGenerator.ExecutionTests/ExecutionTests.cs`
- Create: `tests/MessagePack.SourceGenerator.ExecutionTests/ByRefStructFormatterExecutionTypes.cs`

**Step 1: Write the failing test**

Add source-generator execution test with a struct type and custom formatter implementing new by-ref interfaces, then assert generated resolver path triggers by-ref counters.

```csharp
[Fact]
public void GeneratedFormatter_PrefersByRefFormatterInterfaces_ForStructMembers()
{
    var before = new ByRefExecutionContainer { Value = new ByRefExecutionStruct { Id = 1, PooledObject = "x" } };
    var after = this.Roundtrip(before);

    Assert.Equal(before.Value.Id, after.Value.Id);
    Assert.True(ByRefExecutionStructFormatter.SerializeInCalls > 0);
    Assert.True(ByRefExecutionStructFormatter.DeserializeRefCalls > 0);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/MessagePack.SourceGenerator.ExecutionTests/MessagePack.SourceGenerator.ExecutionTests.csproj -f net9.0 --filter "FullyQualifiedName~GeneratedFormatter_PrefersByRefFormatterInterfaces_ForStructMembers"`
Expected: FAIL.

**Step 3: Write minimal implementation**

Template change strategy:
- Preserve generated formatter compatibility.
- For member formatter invocations, emit by-ref dispatch call paths.
- Keep fallback to legacy behavior if by-ref interfaces are not implemented.

Use helper emission pattern in templates:

```csharp
var formatter = MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<TMember>(formatterResolver);
MsgPack::Internal.FormatterDispatch<TMember>.Serialize(ref writer, in memberValue, options);
```

and for deserialization into existing writable struct members:

```csharp
var temp = member;
MsgPack::Internal.FormatterDispatch<TMember>.Deserialize(ref reader, ref temp, options);
member = temp;
```

Mirror the same behavior for string-key template.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/MessagePack.SourceGenerator.ExecutionTests/MessagePack.SourceGenerator.ExecutionTests.csproj -f net9.0 --filter "FullyQualifiedName~GeneratedFormatter_PrefersByRefFormatterInterfaces_ForStructMembers"`
Expected: PASS.

**Step 5: Commit**

```bash
git add src/MessagePack.SourceGenerator/Transforms/FormatterTemplate.tt src/MessagePack.SourceGenerator/Transforms/FormatterTemplate.cs src/MessagePack.SourceGenerator/Transforms/StringKey/StringKeyFormatterTemplate.tt src/MessagePack.SourceGenerator/Transforms/StringKey/StringKeyFormatterTemplate.cs tests/MessagePack.SourceGenerator.ExecutionTests/ExecutionTests.cs tests/MessagePack.SourceGenerator.ExecutionTests/ByRefStructFormatterExecutionTypes.cs
git commit -m "feat: source generator emits by-ref formatter dispatch for struct members"
```

### Task 6: Refresh source-generator snapshot resources and stabilize generator test suite

**Files:**
- Modify: `tests/MessagePack.SourceGenerator.Tests/Resources/**` (only files changed by template output)
- Modify (temporary local-only toggle): `tests/MessagePack.SourceGenerator.Tests/Verifiers/CSharpSourceGeneratorVerifier\`1+Test.cs`
- Test: `tests/MessagePack.SourceGenerator.Tests/MessagePack.SourceGenerator.Tests.csproj`

**Step 1: Write the failing test**

Use existing source-generator tests as the red state.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/MessagePack.SourceGenerator.Tests/MessagePack.SourceGenerator.Tests.csproj -f net8.0`
Expected: FAIL with generated source mismatch against embedded resources.

**Step 3: Write minimal implementation**

Regenerate resource baselines:
1. Temporarily uncomment `#define WRITE_EXPECTED` in verifier test helper.
2. Re-run targeted generator tests that cover changed templates.
3. Re-disable `WRITE_EXPECTED` before commit.

```bash
dotnet test tests/MessagePack.SourceGenerator.Tests/MessagePack.SourceGenerator.Tests.csproj -f net8.0 --filter "FullyQualifiedName~GenerationTests|FullyQualifiedName~GenericsFormatterTests|FullyQualifiedName~PrivateMemberAccessTests"
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/MessagePack.SourceGenerator.Tests/MessagePack.SourceGenerator.Tests.csproj -f net8.0`
Expected: PASS.

**Step 5: Commit**

```bash
git add tests/MessagePack.SourceGenerator.Tests/Resources
git commit -m "test: refresh source-generator expected outputs for by-ref dispatch"
```

### Task 7: Document new low-level APIs and add compatibility notes

**Files:**
- Modify: `README.md` (Low-Level API section)
- Modify: `doc/migrating_v2-v3.md` (optional additive API note)

**Step 1: Write the failing test**

Documentation task; use build/test green baseline from previous tasks.

**Step 2: Run test to verify it fails**

No failing code test expected.

**Step 3: Write minimal implementation**

Add docs for:
- `IMessagePackFormatterSerializeIn<T>`
- `IMessagePackFormatterDeserializeRef<T>`
- new serializer overloads with `in/ref`
- fallback semantics and caution for reclaiming old value state on deserialization failures.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/MessagePack.Tests/MessagePack.Tests.csproj -f net9.0 --filter "FullyQualifiedName~ByRefFormatterApiTests|FullyQualifiedName~DynamicObjectResolverByRefTests"`
Expected: PASS.

**Step 5: Commit**

```bash
git add README.md doc/migrating_v2-v3.md
git commit -m "docs: describe by-ref formatter and serializer APIs"
```

### Task 8: Final verification pass before merge

**Files:**
- Test only (no source change expected)

**Step 1: Write the failing test**

No new tests; run full verification matrix.

**Step 2: Run test to verify it fails**

No intentional fail in this task.

**Step 3: Write minimal implementation**

No implementation expected.

**Step 4: Run test to verify it passes**

Run:

```bash
dotnet test tests/MessagePack.Tests/MessagePack.Tests.csproj -f net9.0
dotnet test tests/MessagePack.SourceGenerator.ExecutionTests/MessagePack.SourceGenerator.ExecutionTests.csproj -f net9.0
dotnet test tests/MessagePack.SourceGenerator.Tests/MessagePack.SourceGenerator.Tests.csproj -f net8.0
```

Expected: all PASS.

**Step 5: Commit**

```bash
git status
# Expect: clean working tree
```

No additional commit unless fixes were required.

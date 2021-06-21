Proposal of MessagePack v3
===
It has been a while since the implementation of v2, and I believe there are a few major things that need to be resolved.

1. `MessagePackSerializer.Deserialize` should be accept `ReadOnlySpan<byte>`
2. Requires static write/read function(such as v1's `MessagePackBinary`)
3. `ref T value` for large struct(for Serialize) and overwrite(for Deserialize)
4. Bufferless StreamingSerializer/StreamingDeserializer
5. Unified code-generation(dynamicassembly/source-generator/standalone code-generator/analyzer)
6. nullable
7. .NET 6
8. CI to GitHub Actions
9. avoid SlowSpan for Unity
10. more performance improvement

If we do change it, it will have to be at the same time as v3, as it will be a major interface changes.
I have prepared a minimal implementation and benchmark results as a base for discussion.

![image](https://user-images.githubusercontent.com/46207/122743956-7f876480-d2c2-11eb-8fc7-53f819d22e61.png)

(v3 is x2 faster in .NET 5)

## 1. `MessagePackSerializer.Deserialize` should be accept `ReadOnlySpan<byte>`
I think the lack of acceptance of `ReadOnlySpan<byte>` is a major failure of v2.
To fix it, we need to change the `MessagePackReader`.
I think we need to abolish `SequenceReader` and control `ReadOnlySpan` and `ReadOnlySequence` manually in MessagePackReader.

## 2. Requires static write/read function(such as v1's `MessagePackBinary`)
It is a big frustration not to be able to write even a small thing without an `IBufferWriter<byte>`.
Another drawback is that it cannot be used in an async environment.

We need to API like `System.Buffers.BinaryPrimitives`.

```csharp
public static partial class MessagePackPrimitives
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryWriteInt32(Span<byte> buffer, int value)
    {
        if (buffer.Length < 5) return false;

        unchecked
        {
            buffer[4] = (byte)value;
            buffer[3] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[1] = (byte)(value >> 24);
            buffer[0] = MessagePackCode.Int32;
        }

        return true;
    }
```

This will result in the following implementation of `MessagePackWriter`.

```csharp
// MessagePackWriter
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void WriteInt32(int value)
{
    if (!MessagePackPrimitives.TryWriteInt32(WritableSpan, value))
    {
        Ensure(5);
        MessagePackPrimitives.WriteInt32(WritableSpan, value);
    }
}
```

## 3. `ref T value` for large struct(for Serialize) and overwrite(for Deserialize)
Change the `IMessagePackFormatter<T>` interface like the following.

```csharp
public interface IMessagePackFormatter<T>
{
    void Serialize(ref MessagePackWriter writer, ref T? value, MessagePackOptions options);
    void Deserialize(ref MessagePackReader reader, ref T? value, MessagePackOptions options);
}
```

The ability to override an existing value with Deserialize has been requested for some time. I also want to use zero-allocation serialization for real-time communication in games, so this feature is definitely important.

I think this can be toggled between new and overwrite by setting the value of Deserialzie to ref, and by using the Overwrite option in MessagePackOptions, for example the following code

```csharp
// generated serializer code
void Deserialize(ref MessagePackWriter writer, ref T? value, MessagePackOptions options);
{
    if (!writer.Overwrite) // Overwrite option will provide from MessagePackOptions
    {
        value = new T();
    }
    value.Foo = Read(); 
}
```

We also want to avoid copying when serializing large structs, so let's use ref when serializing as well. The MessagePackSerializer takes values as `in`.

```csharp
public static class MessagePackSerializer
{
    public static void Serialize<T>(IBufferWriter<byte> bufferWriter, in T value)
    {
        var writer = new MessagePackWriter(writer);
        formatter.Serialize(ref writer, ref Unsafe.AsRef(value), options);
    }
}
```

## 4. Bufferless StreamingSerializer/StreamingDeserializer
The MessagePack format itself supports streaming read/write, but currently requires a full buffer for both Serialize and Deserialize (Stream APIs, etc. also read into the buffer once).

It is written in Issues several times, but it requires complex code, so we think it is better to provide it as standard.

In reality, it would be better to support only Array/Map, and the unit of reading/writing streaming should be one element at a time, since it would be a performance problem to run a check on every write.

If you assume .NET 6, then only `System.IO.Pipelines` will be supported.

```csharp
public static class MessagePackStreamingSerializer
{
    public static async ValueTask SerializeAsync<T>(PipeWriter pipeWriter, IEnumerable<T?> values, int count, MessagePackOptions options)
    public static async ValueTask SerializeMapAsync<TKey, TValue>(PipeWriter pipeWriter, int count, IEnumerable<KeyValuePair<TKey, TValue>> value, ....)
    public static async IAsyncEnumerable<T> DeserializeAsync<T>(PipeReader pipeReader)
}
```

Since we think it is important to control the interval between flushes for performance, it is a good idea to prepare a separate Flush strategy option and call FlushAsync accordingly.

```csharp
public sealed class FlushStrategy
{
    public static FlushStrategy FlushOnEveryWritten = new FlushStrategy { FlushPerItemCount = true, ItemCount = 1 };
    public static FlushStrategy FlushOnEvery64K = new FlushStrategy { FlushPerBytesWritten = true, BytesWritten = 65536 };

    public bool FlushPerItemCount { get; init; }
    public bool FlushPerBytesWritten { get; init; }
    public int ItemCount { get; init; }
    public int BytesWritten { get; init; }

    public bool ShouldFlush(int itemCount, int bytesWritten)
    {
        if (FlushPerItemCount && (ItemCount <= itemCount))
        {
            return true;
        }
        if (FlushPerBytesWritten && (BytesWritten <= bytesWritten))
        {
            return true;
        }
        return false;
    }
}

// MessagePackStreamingSerializer
public static async ValueTask SerializeAsync<T>(PipeWriter pipeWriter, IEnumerable<T?> values, int count, MessagePackOptions options, FlushStrategy flushStrategy, CancellationToken cancellationToken = default)
{
    static int WriteCore(PipeWriter pipeWriter, T? item, IMessagePackFormatter<T> formatter, MessagePackOptions options)
    {
        var writer = new MessagePackWriter(pipeWriter);
        formatter.Serialize(ref writer, ref item, options);
        return writer.WrittenCount;
    }

    IMessagePackFormatter<T> formatter = default!;
    int bytesWritten = 0;
    var itemCount = 0;
    foreach (var item in values)
    {
        bytesWritten += WriteCore(pipeWriter, item, formatter, options);
        itemCount++;
        if (flushStrategy.ShouldFlush(itemCount, bytesWritten))
        {
            await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    await pipeWriter.FlushAsync().ConfigureAwait(false);
}
```

As for Deserialize, it will be a 2-pass process, but how about preparing a buffer of 1 unit with TrySkip, and sending it to Reader?

I'd like to reuse the generation of `Writer/Reader`, but it's difficult due to the compatibility of async/await and ref struct. I don't have any good ideas.

## 5. Unified code-generation(dynamicassembly/source-generator/standalone code-generator/analyzer)
DynamicAssembly, DynamicMethod, Analyzer, Commandline Code Generator, MSBuild Task, and Source Generator. overlapping and scattered code is very frustrating and a breeding ground for bugs. Duplicate and scattered code is very frustrating and a breeding ground for bugs. I would like to introduce an intermediate abstract metatype to centralize the management. It will also allow users to dynamically create this MetaType and generate custom Formatters.

```csharp
public abstract class MessagePackMetaType
{
    public MessagePackMetaMember[] Members { get; set; }
    public MessagePackMetaConstructor[] Constructors { get; set; }

    // Diagnostics of generated type....
    public override string ToString()
    {
        // Serialize:""
        // Deserialize:""
        // return @"new Foo(int , int, int,...) {} ";
    }
}

public abstract class MessagePackMetaConstructor
{
    public MessagePackMetaParameter[] Parameters { get; set; }
}

public abstract class MessagePackMetaMember
{
    public bool IsIntKey { get; set; }
    public int IntKey { get; set; }
    public string? StringKey { get; set; }
    public bool IsWritable { get; set; }
    public bool IsReadable { get; set; }
}

public abstract class MessagePackMetaParameter
{
    public string Name { get; set; }
    public bool HasDefaultValue { get; set; }
    public object DefaultValue { get; set; }
}

// ReflectionType
public sealed class ReflectionMetaType : MessagePackMetaType
{
    public Type Type { get; set; }
}

public sealed class ReflectionMetaMember : MessagePackMetaMember
{
    public MemberInfo MemberInfo { get; set; } // PropertyInfo or FieldInfo
}


public sealed class ReflectionMetaConstructor : MessagePackMetaConstructor
{
    public ConstructorInfo ConstructorInfo { get; set; }
}

public sealed class ReflectionMetaParameter : MessagePackMetaParameter
{
    public ParameterInfo ParameterInfo { get; set; }
}

// RoslynType
// ...
```

## 6. nullable
Unity support needs to continue; LTS does not yet support C# 8.0. We also need to wait for eventual C# 9.0 support for Generic Constraint. It does not work with the current Unity.

In my ZString, ZLogger, and MessagePipe, I handled it by copying from the post-build rewrite. NET side instead of Unity/Client side, so it is easier to handle the source code. NET side, so it's easier to handle the source code. Currently, I'm not able to make everything a source reference, such as copying the T4 stuff, so it should be good to split it up and make everything a copy.

## 7. .NET 6
using VS2022 preview. record, struct record, new types, etc...

## 8. CI to GitHub Actions
For OSS, CI with GitHub Actions would be better, and Unity is built as follows
https://github.com/Cysharp/ZString/blob/master/.github/workflows/build-debug.yml

I think it's a good idea to continue to put Canary builds in Azure DevOps. In my other repositories, I have achieved this by doing the following
https://github.com/Cysharp/MagicOnion/blob/master/.github/workflows/build-canary.yml

## 9. avoid SlowSpan for Unity
In Unity, Span uses Slow Span, and doesn't perform very well (I tested this issue with ZString). If you can use arrays, you can get back better performance by using arrays. Here are the results from a simple benchmark in Unity IL2CPP, arrays are 5 times faster.

![image](https://user-images.githubusercontent.com/46207/122743924-76969300-d2c2-11eb-9d27-b16b80dc80d3.png)

## 10. more performance improvement
This is a simple benchmark result from the current prototype implementation.

```csharp
ArrayBufferWriter<byte> bufferWriter = default!;

[GlobalSetup]
public void Setup()
{
    bufferWriter = new ArrayBufferWriter<byte>(1024);
}

[Benchmark]
public byte[] MessagePackV2()
{
    var writer = new MessagePack.MessagePackWriter(bufferWriter);
    writer.WriteArrayHeader(10);
    for (int i = 0; i < 10; i++)
    {
        writer.WriteInt32(1000);
        writer.WriteInt32(2000);
        writer.WriteInt32(3000);
        writer.WriteInt32(4000);
    }
    writer.Flush();
    var xs = bufferWriter.WrittenSpan.ToArray();
    bufferWriter.Clear();
    return xs;
}

[Benchmark]
public byte[] MessagePackV3_Array()
{
    var writer = new MessagePackv3.MessagePackWriter(bufferWriter, true);
    writer.WriteArrayHeader(10);
    for (int i = 0; i < 10; i++)
    {
        writer.WriteInt32(1000);
        writer.WriteInt32(2000);
        writer.WriteInt32(3000);
        writer.WriteInt32(4000);
    }
    writer.Flush();
    var xs = bufferWriter.WrittenSpan.ToArray();
    bufferWriter.Clear();
    return xs;
}

[Benchmark]
public byte[] MessagePackV3_Span()
{
    var writer = new MessagePackv3.MessagePackWriter(bufferWriter, false);
    writer.WriteArrayHeader(10);
    for (int i = 0; i < 10; i++)
    {
        writer.WriteInt32(1000);
        writer.WriteInt32(2000);
        writer.WriteInt32(3000);
        writer.WriteInt32(4000);
    }
    writer.Flush();
    var xs = bufferWriter.WrittenSpan.ToArray();
    bufferWriter.Clear();
    return xs;
}
```

![image](https://user-images.githubusercontent.com/46207/122743956-7f876480-d2c2-11eb-8fc7-53f819d22e61.png)

In .NET 5 is twice as fast.

This is probably due in large part to the elimination of the BufferWriter (there are Primitives introduced in 2, and Array/Span switching for 9).

I think it will be much faster in Reader(Deserialize) as well.

Other consider things
---
* Generics of `IBufferWriter<byte>`

It is useful but `MessagePackWriter<TBufferWriter>`, will affects `IMessagePackFormatter<T, TBufferWriter>`, This has a significant impact on code generation, so I reject it.

* `following dotnet/runtime editor.config`

My style deviates somewhat from that of `dotnet/runtime`. I'm starting to think that I need to start following it instead of my own preferences.

* `ContractlessStandardResolver.Options → StandardResolver.ContractlessOptions`

for the IntelliSensebility.
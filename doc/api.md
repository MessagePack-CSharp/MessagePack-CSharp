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

Many built-in formatters exists under `MessagePack.Formatters`. Your custom types are usually automatically supported with the built-in type resolvers that generate new `IMessagePackFormatter<T>` types on-the-fly using dynamic code generation. See our [AOT code generation](aot.md) support for platforms that do not support this.

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

Your custom formatters must be discoverable via some `IFormatterResolver`. Learn more in our [resolvers](resolvers.md).

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
| `ReadStringSegment` | Returns a slice of the input sequence representing the contents of a `string` without decoding it, and advances the reader. |
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

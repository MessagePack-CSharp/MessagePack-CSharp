# Abstract

`UnsafeUnmanagedStructFormatter`s (de)serialize the blittable value(s) directly.
They are very performant but unstable against the endian.

You should be careful not to share the encoded byte[] among the different endian environments.

Supported types (T where T : unamanaged)

- `T` → `UnsafeUnmanagedStructFormatter<T>`
- `T[]` → `UnsafeUnmanagedStructArrayFormatter<T>`
- `Memory<T>` → `UnsafeUnmanagedStructMemoryFormatter<T>`
- `ReadOnlyMemory<T>` → `UnsafeUnmanagedStructReadOnlyMemoryFormatter<T>`
- `ReadOnlySequence<T>` → `UnsafeUnmanagedStructReadOnlySequenceFormatter<T>`

# Usage

```csharp
var resolver = MessagePack.Resolvers.CompositeResolver.Create(
    new[] { new UnsafeUnmanagedStructFormatter<Matrix4x4>(typeCode: 96) },
    new[] { MessagePack.Resolvers.StandardResolver.Instance });
```

The constructor takes 1 sbyte value.
The sbyte value is the extension type code embedded in serialized byte sequence.

# When will this feature become official?

- Requests are needed.
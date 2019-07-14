# LZ4 Compression

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

### Attributions

lz4 compression support is using Milosz Krajewski's [lz4net](https://github.com/MiloszKrajewski/lz4net) code with some modified.

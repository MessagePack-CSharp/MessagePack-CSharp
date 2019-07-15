# MessagePack Performance

Benchmarks comparing to other serializers run on `Windows 10 Pro x64 Intel Core i7-6700K 4.00GHz, 32GB RAM`. Benchmark code is [here](https://github.com/neuecc/ZeroFormatter/tree/master/sandbox/PerformanceComparison) - and there [version info](https://github.com/neuecc/ZeroFormatter/blob/bc63cb925d/sandbox/PerformanceComparison/packages.config), ZeroFormatter and [FlatBuffers](https://google.github.io/flatbuffers/) has infinitely fast deserializer so ignore deserialize performance.

![image](https://cloud.githubusercontent.com/assets/46207/23835765/55fe494e-07b0-11e7-98be-5e7a9411da40.png)

 MessagePack for C# uses many techniques for improve performance.

* Serializer uses only `IBufferWriter<byte>` rather than `System.IO.Stream` for reduced overhead.
* Buffers are rented from pools to reduce allocations, keeping throughput high through reduced GC pressure.
* Don't create intermediate utility instance(XxxWriter/Reader, XxxContext, etc...)
* Utilize dynamic code generation to avoid boxing value types. Use AOT generation on platforms that prohibit JIT.
* Getting cached generated formatter on static generic field (don't use dictinary-cache because dictionary lookup is overhead). See [Resolvers](https://github.com/neuecc/MessagePack-CSharp/tree/209f301e2e595ed366408624011ba2e856d23429/src/MessagePack/Resolvers)
* Heavily tuned dynamic IL code generation to avoid boxing value types. See [DynamicObjectTypeBuilder](https://github.com/neuecc/MessagePack-CSharp/blob/209f301e2e595ed366408624011ba2e856d23429/src/MessagePack/Resolvers/DynamicObjectResolver.cs#L142-L754).
Use AOT generation on platforms that prohibit JIT.
* Call PrimitiveAPI directly when il code generation knows target is primitive
* Reduce branch of variable length format when il code generation knows target(integer/string) range
* Don't use `IEnumerable<T>` abstraction on iterate collection, [see:CollectionFormatterBase](https://github.com/neuecc/MessagePack-CSharp/blob/209f301e2e595ed366408624011ba2e856d23429/src/MessagePack/Formatters/CollectionFormatter.cs#L192-L355) and inherited collection formatters
* Uses pre generated lookup table to reduce check messagepack type, [see: MessagePackBinary](https://github.com/neuecc/MessagePack-CSharp/blob/209f301e2e595ed366408624011ba2e856d23429/src/MessagePack/MessagePackBinary.cs#L15-L212)
* Uses optimized type key dictionary for non-generic methods, [see: ThreadsafeTypeKeyHashTable](https://github.com/neuecc/MessagePack-CSharp/blob/91312921cb7fe987f48336768c898a76ac7dbb40/src/MessagePack/Internal/ThreadsafeTypeKeyHashTable.cs)
* Avoid string key decode for lookup map(string key) key and uses automata based name lookup with il inlining code generation, see: [AutomataDictionary](https://github.com/neuecc/MessagePack-CSharp/blob/bcedbce3fd98cb294210d6b4a22bdc4c75ccd916/src/MessagePack/Internal/AutomataDictionary.cs)
* For string key encode, pre-generated member name bytes and use fixed sized binary copy in IL, see: [UnsafeMemory.cs](https://github.com/neuecc/MessagePack-CSharp/blob/f17ddc5d107d3a2f66f60398b214ef87919ff892/src/MessagePack/Internal/UnsafeMemory.cs)

Before creating this library, I implemented a fast fast serializer with [ZeroFormatter#Performance](https://github.com/neuecc/ZeroFormatter#performance). And this is a further evolved implementation. MessagePack for C# is always fast, optimized for all types(primitive, small struct, large object, any collections).

Deserialize Performance per options
---
Performance varies depending on options. This is a micro benchamark with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet). Target object has 9 members(`MyProperty1` ~ `MyProperty9`), value are zero.

 |              Method |        Mean | Error | Scaled |  Gen 0 | Allocated |
 |-------------------- |------------:|------:|-------:|-------:|----------:|
 |              IntKey |    72.67 ns |    NA |   1.00 | 0.0132 |      56 B |
 |           StringKey |   217.95 ns |    NA |   3.00 | 0.0131 |      56 B |
 |     Typeless_IntKey |   176.71 ns |    NA |   2.43 | 0.0131 |      56 B |
 |  Typeless_StringKey |   378.64 ns |    NA |   5.21 | 0.0129 |      56 B |
 |       MsgPackCliMap | 1,355.26 ns |    NA |  18.65 | 0.1431 |     608 B |
 |     MsgPackCliArray |   455.28 ns |    NA |   6.26 | 0.0415 |     176 B |
 |         ProtobufNet |   265.85 ns |    NA |   3.66 | 0.0319 |     136 B |
 |            Hyperion |   366.47 ns |    NA |   5.04 | 0.0949 |     400 B |
 |       JsonNetString | 2,783.39 ns |    NA |  38.30 | 0.6790 |    2864 B |
 | JsonNetStreamReader | 3,297.90 ns |    NA |  45.38 | 1.4267 |    6000 B |
 |           JilString |   553.65 ns |    NA |   7.62 | 0.0362 |     152 B |
 |     JilStreamReader | 1,408.46 ns |    NA |  19.38 | 0.8450 |    3552 B |

IntKey, StringKey, Typeless_IntKey, Typeless_StringKey are MessagePack for C# options. All MessagePack for C# options achive zero memory allocation on deserialization process. JsonNetString/JilString is deserialized from string. JsonNetStreamReader/JilStreamReader is deserialized from UTF8 byte[] with StreamReader. Deserialization is normally read from Stream. Thus, it will be restored from byte[](or Stream) instead of string.

MessagePack for C# IntKey is fastest. StringKey is slower than IntKey because matching from the character string is required. If IntKey, read array length, for(array length) { binary decode }. If StringKey, read map length, for(map length) { decode key, lookup by key, binary decode } so requires additional two steps(decode key and lookup by key).

String key is often useful, contractless, simple replacement of JSON, interoperability with other languages, and more certain versioning. MessagePack for C# is also optimized for String Key. First of all, it do not decode UTF8 byte[] to String for matching with the member name, it will look up the byte[] as it is(avoid decode cost and extra allocation).

And It will try to match each `long type` (per 8 character, if it is not enough, pad with 0) using [automata](https://en.wikipedia.org/wiki/Automata_theory) and inline it when IL code generating.

![image](https://user-images.githubusercontent.com/46207/29754771-216b40e2-8bc7-11e7-8310-1c3602e80a08.png)

This also avoids calculating the hash code of byte[], and the comparison can be made several times on a long unit.

This is the sample decompile of generated deserializer code by [ILSpy](http://ilspy.net/).

![image](https://user-images.githubusercontent.com/46207/29754804-b5ba0f44-8bc7-11e7-9f6b-0c8f3c041237.png)

If the number of nodes is large, search with a embedded binary search.

Extra note, this is serialize benchmark result.

 |              Method |        Mean | Error | Scaled |  Gen 0 | Allocated |
 |-------------------- |------------:|------:|-------:|-------:|----------:|
 |              IntKey |    84.11 ns |    NA |   1.00 | 0.0094 |      40 B |
 |           StringKey |   126.75 ns |    NA |   1.51 | 0.0341 |     144 B |
 |     Typeless_IntKey |   183.31 ns |    NA |   2.18 | 0.0265 |     112 B |
 |  Typeless_StringKey |   193.95 ns |    NA |   2.31 | 0.0513 |     216 B |
 |       MsgPackCliMap |   967.68 ns |    NA |  11.51 | 0.1297 |     552 B |
 |     MsgPackCliArray |   284.20 ns |    NA |   3.38 | 0.1006 |     424 B |
 |         ProtobufNet |   176.43 ns |    NA |   2.10 | 0.0665 |     280 B |
 |            Hyperion |   280.14 ns |    NA |   3.33 | 0.1674 |     704 B |
 |       ZeroFormatter |   149.95 ns |    NA |   1.78 | 0.1009 |     424 B |
 |       JsonNetString | 1,432.55 ns |    NA |  17.03 | 0.4616 |    1944 B |
 | JsonNetStreamWriter | 1,775.72 ns |    NA |  21.11 | 1.5526 |    6522 B |
 |           JilString |   547.51 ns |    NA |   6.51 | 0.3481 |    1464 B |
 |     JilStreamWriter |   778.78 ns |    NA |   9.26 | 1.4448 |    6066 B |

 Of course, IntKey is fastest but StringKey also good.

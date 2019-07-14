# Formatters

## Built-in support types

These types can serialize by default.

Primitives(`int`, `string`, etc...), `Enum`, `Nullable<>`,  `TimeSpan`,  `DateTime`, `DateTimeOffset`, `Nil`, `Guid`, `Uri`, `Version`, `StringBuilder`, `BitArray`, `ArraySegment<>`, `BigInteger`, `Complext`, `Task`, `Array[]`, `Array[,]`, `Array[,,]`, `Array[,,,]`, `KeyValuePair<,>`, `Tuple<,...>`, `ValueTuple<,...>`, `List<>`, `LinkedList<>`, `Queue<>`, `Stack<>`, `HashSet<>`, `ReadOnlyCollection<>`, `IList<>`, `ICollection<>`, `IEnumerable<>`, `Dictionary<,>`, `IDictionary<,>`, `SortedDictionary<,>`, `SortedList<,>`, `ILookup<,>`, `IGrouping<,>`, `ObservableCollection<>`, `ReadOnlyOnservableCollection<>`, `IReadOnlyList<>`, `IReadOnlyCollection<>`, `ISet<>`, `ConcurrentBag<>`, `ConcurrentQueue<>`, `ConcurrentStack<>`, `ReadOnlyDictionary<,>`, `IReadOnlyDictionary<,>`, `ConcurrentDictionary<,>`, `Lazy<>`, `Task<>`, custom inherited `ICollection<>` or `IDictionary<,>` with paramterless constructor, `IList`, `IDictionary` and custom inherited `ICollection` or `IDictionary` with paramterless constructor(includes `ArrayList` and `Hashtable`).

You can add custom type support and has some official/third-party extension package. for ImmutableCollections(`ImmutableList<>`, etc), for ReactiveProperty and for Unity(`Vector3`, `Quaternion`, etc...), for F#(Record, FsList, Discriminated Unions, etc...). Please see [extensions section](https://github.com/neuecc/MessagePack-CSharp#extensions).

`MessagePack.Nil` is built-in null/void/unit representation type of MessagePack for C#.

## MessagePackFormatterAttribute

MessagePackFormatterAttribute is lightweight extension point of class, struct, interface, enum and property/field. This is like JSON.NET's JsonConverterAttribute. For example, serialize private field, serialize x10 formatter.

```csharp
[MessagePackFormatter(typeof(CustomObjectFormatter))]
public class CustomObject
{
    string internalId;

    public CustomObject()
    {
        this.internalId = Guid.NewGuid().ToString();
    }

    // serialize/deserialize internal field.
    class CustomObjectFormatter : IMessagePackFormatter<CustomObject>
    {
        public void Serialize(ref MessagePackWriter writer, CustomObject value, IFormatterResolver formatterResolver)
        {
            formatterResolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.internalId, formatterResolver);
        }

        public CustomObject Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var id = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver);
            return new CustomObject { internalId = id };
        }
    }
}

// per field, member

public class Int_x10Formatter : IMessagePackFormatter<int>
{
    public int Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return reader.ReadInt32() * 10;
    }

    public void Serialize(ref MessagePackWriter writer, int value, IFormatterResolver formatterResolver)
    {
        writer.WriteInt32(value * 10);
    }
}

[MessagePackObject]
public class MyClass
{
    // You can attach custom formatter per member.
    [Key(0)]
    [MessagePackFormatter(typeof(Int_x10Formatter))]
    public int MyProperty1 { get; set; }
}
```

Formatter is retrieved by `AttributeFormatterResolver`, it is included in `StandardResolver`.

## IgnoreFormatter

`IgnoreFormatter<T>` is lightweight extension point of class and struct, if exists can't serializable type in external type, you can register `IgnoreFormatter<T>` that serialize to nil.

```csharp
// CompositeResolver can set custom formatter.
MessagePack.Resolvers.CompositeResolver.RegisterAndSetAsDefault(
    new IMessagePackFormatter[]
    {
        // for example, register reflection infos(can not serialize in default)
        new IgnoreFormatter<MethodBase>(),
        new IgnoreFormatter<MethodInfo>(),
        new IgnoreFormatter<PropertyInfo>(),
        new IgnoreFormatter<FieldInfo>()
    },
    new IFormatterResolver[]
    {
        ContractlessStandardResolver.Instance
    });
```

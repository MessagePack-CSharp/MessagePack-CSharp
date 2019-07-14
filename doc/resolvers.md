# Resolvers (`IFormatterResolver`)

An `IFormatterResolver` is storage of typed serializers. The `MessagePackSerializer` API accepts a `MessagePackSerializerOptions` object which specifies the `IFormatterResolver` to use, allowing customization of the serialization of complex types.

| Resolver Name | Description |
| --- | --- |
| BuiltinResolver | Builtin primitive and standard classes resolver. It includes primitive(int, bool, string...) and there nullable, array and list. and some extra builtin types(Guid, Uri, BigInteger, etc...). |
| StandardResolver | Composited resolver. It resolves in the following order `builtin -> attribute -> dynamic enum -> dynamic generic -> dynamic union -> dynamic object -> dynamic object fallback`. This is the default of MessagePackSerializer. |
| ContractlessStandardResolver | Composited `StandardResolver`(except dynamic object fallback) -> `DynamicContractlessObjectResolver` -> `DynamicObjectTypeFallbackResolver`. It enables contractless serialization. |
| StandardResolverAllowPrivate | Same as StandardResolver but allow serialize/deserialize private members. |
| ContractlessStandardResolverAllowPrivate | Same as ContractlessStandardResolver but allow serialize/deserialize private members. |
| PrimitiveObjectResolver | MessagePack primitive object resolver. It is used fallback in `object` type and supports `bool`, `char`, `sbyte`, `byte`, `short`, `int`, `long`, `ushort`, `uint`, `ulong`, `float`, `double`, `DateTime`, `string`, `byte[]`, `ICollection`, `IDictionary`. |
| DynamicObjectTypeFallbackResolver | Serialize is used type in from `object` type, deserialize is used PrimitiveObjectResolver. |
| AttributeFormatterResolver | Get formatter from `[MessagePackFormatter]` attribute. |
| CompositeResolver | Composes several resolvers and/or formatters together in an ordered list, allowing reuse and overriding of behaviors of existing resolvers and formatters. |
| NativeDateTimeResolver | Serialize by .NET native DateTime binary format. |
| UnsafeBinaryResolver | Guid and Decimal serialize by binary representation. It is faster than standard(string) representation. |
| DynamicEnumResolver | Resolver of enum and there nullable, serialize there underlying type. It uses dynamic code generation to avoid boxing and boostup performance serialize there name. |
| DynamicEnumAsStringResolver | Resolver of enum and there nullable.  It uses reflection call for resolve nullable at first time. |
| DynamicGenericResolver | Resolver of generic type(`Tuple<>`, `List<>`, `Dictionary<,>`, `Array`, etc). It uses reflection call for resolve generic argument at first time. |
| DynamicUnionResolver | Resolver of interface marked by UnionAttribute. It uses dynamic code generation to create dynamic formatter. |
| DynamicObjectResolver | Resolver of class and struct maked by MessagePackObjectAttribute. It uses dynamic code generation to create dynamic formatter. |
| DynamicContractlessObjectResolver | Resolver of all classes and structs. It does not needs MessagePackObjectAttribute and serialized key as string(same as marked [MessagePackObject(true)]). |
| DynamicObjectResolverAllowPrivate | Same as DynamicObjectResolver but allow serialize/deserialize private members. |
| DynamicContractlessObjectResolverAllowPrivate | Same as DynamicContractlessObjectResolver but allow serialize/deserialize private members. |
| TypelessObjectResolver | Used for `object`, embed .NET type in binary by `ext(100)` format so no need to pass type in deserilization.  |
| TypelessContractlessStandardResolver | Composited resolver. It resolves in the following order `nativedatetime -> builtin -> attribute -> dynamic enum -> dynamic generic -> dynamic union -> dynamic object -> dynamiccontractless -> typeless`. This is the default of `MessagePackSerializer.Typeless`  |

Each invocation of `MessagePackSerializer` accepts only a single resolver. Most object graphs will need more than one for serialization, so composing a single resolver made up of several is often required, and can be done with the `CompositeResolver` as shown below:

```csharp
// Do this once and store it for reuse.
var resolver = new MessagePack.Resolvers.CompositeResolver();
resolver.RegisterResolver(
    // resolver custom types first
    ImmutableCollectionResolver.Instance,
    ReactivePropertyResolver.Instance,
    MessagePack.Unity.Extension.UnityBlitResolver.Instance,
    MessagePack.Unity.UnityResolver.Instance,

    // finaly use standard resolver
    StandardResolver.Instance);
var options = MessagePackSerializerOptions.Default.WithResolver(resolver);

// Each time you serialize/deserialize, specify the options:
byte[] msgpackBytes = MessagePackSerializer.Serialize(myObject, options);
T myObject2 = MessagePackSerializer.Deserialize<MyObject>(msgpackBytes, options);
```

Here is sample of use `DynamicEnumAsStringResolver` with `DynamicContractlessObjectResolver` (It is JSON.NET-like lightweight setting.)

```csharp
// composite same as StandardResolver
var resolver = new MessagePack.Resolvers.CompositeResolver();
resolver.RegisterResolver(
    MessagePack.Resolvers.BuiltinResolver.Instance,
    MessagePack.Resolvers.AttributeFormatterResolver.Instance,

    // replace enum resolver
    MessagePack.Resolvers.DynamicEnumAsStringResolver.Instance,

    MessagePack.Resolvers.DynamicGenericResolver.Instance,
    MessagePack.Resolvers.DynamicUnionResolver.Instance,
    MessagePack.Resolvers.DynamicObjectResolver.Instance,

    MessagePack.Resolvers.PrimitiveObjectResolver.Instance,

    // final fallback(last priority)
    MessagePack.Resolvers.DynamicContractlessObjectResolver.Instance);
```

If you want to make your extension package, you should write both the formatter and resolver
for easier consumption.
Here is sample of a resolver:

```csharp
public class SampleCustomResolver : IFormatterResolver
{
    // Resolver should be singleton.
    public static readonly IFormatterResolver Instance = new SampleCustomResolver();

    private SampleCustomResolver()
    {
    }

    // GetFormatter<T>'s get cost should be minimized so use type cache.
    public IMessagePackFormatter<T> GetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    private static class FormatterCache<T>
    {
        public static readonly IMessagePackFormatter<T> Formatter;

        // generic's static constructor should be minimized for reduce type generation size!
        // use outer helper method.
        static FormatterCache()
        {
            Formatter = (IMessagePackFormatter<T>)SampleCustomResolverGetFormatterHelper.GetFormatter(typeof(T));
        }
    }
}

internal static class SampleCustomResolverGetFormatterHelper
{
    // If type is concrete type, use type-formatter map
    static readonly Dictionary<Type, object> formatterMap = new Dictionary<Type, object>()
    {
        {typeof(FileInfo), new FileInfoFormatter()}
        // add more your own custom serializers.
    };

    internal static object GetFormatter(Type t)
    {
        object formatter;
        if (formatterMap.TryGetValue(t, out formatter))
        {
            return formatter;
        }

        // If target type is generics, use MakeGenericType.
        if (t.IsGenericParameter && t.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
        {
            return Activator.CreateInstance(typeof(ValueTupleFormatter<,>).MakeGenericType(t.GenericTypeArguments));
        }

        // If type can not get, must return null for fallback mecanism.
        return null;
    }
}
```

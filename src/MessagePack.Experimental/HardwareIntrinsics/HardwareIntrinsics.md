# Abstract

`Hardware Intrinsics` is a feature in order to utilize maximum power of the cpu.
You can serialize/deserialize primitive type array much faster than current implementation!

Supported types

- `sbyte[]`
- `short[]`
- `int[]`
- `bool[]`
- `float[]`
- `double[]`

# Usage

```csharp
var resolver = MessagePack.Resolvers.CompositeResolver.Create(new[] { PrimitiveArrayResolver.Instance, MessagePack.Resolvers.StandardResolver.Instance });
```

# When will this feature become official?

- The MessagePack-CSharp's lowest target framework is greater or equals to `.NET Core 3.1`.
- The current very complex and hard to understand implementation is rewritten.
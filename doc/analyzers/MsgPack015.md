# MsgPack015 MessagePackObjectAttribute.AllowPrivate should be set

When a `[MessagePackObject]`-attributed type cannot be serialized without access to non-public members, the `AllowPrivate` property should be set on the `MessagePackObjectAttribute` so that:

- Dynamically generated formatters can serialize the type, even if `DynamicObjectResolver` is used instead of `DynamicObjectResolverAllowPrivate`.
- Analyzers can help ensure proper annotation of non-public members.

## Examples of patterns that are flagged by this analyzer

### Internal members

```cs
[MessagePackObject]
public class MyData {
    [Key(0)]
    internal int Foo { get; set; }
}
```

### Internal type

```cs
[MessagePackObject]
internal class MyData {
    [Key(0)]
    public int Foo { get; set; }
}
```

## Typical fix

Simply add the `AllowPrivate = true` syntax to the `[MessagePackObject]` attribute:

```cs
[MessagePackObject(AllowPrivate = true)]
internal class MyData {
    [Key(0)]
    internal int Foo { get; set; }
}
```

An automated code fix is offered for this diagnostic.

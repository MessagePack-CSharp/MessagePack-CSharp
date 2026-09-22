# MsgPack003 Use MessagePackObjectAttribute

Types referenced by serializable fields and properties must themselves be attributed with the `[MessagePackObject]`.

## Examples of patterns that are flagged by this analyzer

```cs
[MessagePackObject]
public class A {
    [Key(0)]
    public B b;
}

public class B {
    public int Count;
}
```

## Example fix

Add the required attributes to the `B` class so that a dynamic formatter can be generated for it:

```cs
[MessagePackObject]
public class A {
    [Key(0)]
    public B b;
}

[MessagePackObject]
public class B {
    [Key(0)]
    public int Count;
}
```

An automated code fix is offered for this.

## Alternative fix

When `B` is formattable via a custom formatter, the diagnostic may be suppressed by declaring this attribute:

```cs
[assembly: MessagePackAssumedFormattable(typeof(B))]
```

When doing so, it becomes your responsibility to ensure that the custom formatter for `B` is discoverable via the `IFormatterResolver` object accessible through `MessagePackSerializerOptions.Resolver`.

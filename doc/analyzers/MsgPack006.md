# MsgPack006 Type must be of `IMessagePackFormatter`

This diganostic appears when the type passed to `MesssagePackFormatterAttribute` does not actually implement some `IMessagePackFormatter<T>`.

## Examples of patterns that are flagged by this analyzer

```cs
[MessagePackObject]
public class A {
    [Key(0), MessagePackFormatter(typeof(CustomBFormatter))]
    public B b;
}

public class CustomBFormatter {}
```

## Typical fix

Change the attribute to point to a valid formatter, or update the referenced class to be a valid formatter.
The following example takes the latter approach.

```cs
[MessagePackObject]
public class A {
    [Key(0), MessagePackFormatter(typeof(CustomBFormatter))]
    public B b;
}

public class CustomBFormatter : IMessagePackFormatter<B> {
    void Serialize(ref MessagePackWriter writer, B value, MessagePackSerializerOptions options)
        => throw new NotImplementedException();

    B Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        => throw new NotImplementedException();
}
```

# MsgPack013 Inaccessible formatter instance

A custom `IMessagePackFormatter<T>` implementation must have an accessible instance to be included in the source generated resolver.

This instance can be exposed by any of the following:

1. A public default constructor.
1. A public static readonly field called `Instance`.

## Examples of patterns that are flagged by this analyzer

```cs
class CustomBFormatter : IMessagePackFormatter<B> { // MsgPack013
    private CustomBFormatter { } // hides the default public constructor

    void Serialize(ref MessagePackWriter writer, B value, MessagePackSerializerOptions options)
        => throw new NotImplementedException();

    B Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        => throw new NotImplementedException();
}
```

## Typical fix

Delete the non-public default constructor so that C# can access it:

```cs
class CustomBFormatter : IMessagePackFormatter<B> {
    void Serialize(ref MessagePackWriter writer, B value, MessagePackSerializerOptions options)
        => throw new NotImplementedException();

    B Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        => throw new NotImplementedException();
}
```

Or make a singleton instance available:

```cs
class CustomBFormatter : IMessagePackFormatter<B> {
    public static readonly CustomBFormatter Instance = new();

    private CustomBFormatter { } // hides the default public constructor

    void Serialize(ref MessagePackWriter writer, B value, MessagePackSerializerOptions options)
        => throw new NotImplementedException();

    B Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        => throw new NotImplementedException();
}
```

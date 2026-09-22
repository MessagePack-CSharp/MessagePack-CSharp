# MsgPack010 Inaccessible Formatter

Formatters must be declared with `internal` or `public` visibility so that the source generated resolver can access them.

This tends to happen when a formatter is declared as a nested type, where C# defaults to `private` visibility.

## Examples of patterns that are flagged by this analyzer

```cs
class Outer {
    /*private*/ class CustomBFormatter : IMessagePackFormatter<B> { // MsgPack010
        void Serialize(ref MessagePackWriter writer, B value, MessagePackSerializerOptions options)
            => throw new NotImplementedException();

        B Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            => throw new NotImplementedException();
    }
}
```

## Typical fix

Add the `internal` or `public` modifier to the nested formatter:

```cs
class Outer {
    internal class CustomBFormatter : IMessagePackFormatter<B> {
        void Serialize(ref MessagePackWriter writer, B value, MessagePackSerializerOptions options)
            => throw new NotImplementedException();

        B Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            => throw new NotImplementedException();
    }
}
```

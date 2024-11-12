# MsgPack014 Nullable reference type formatter

Custom formatters of reference types should be prepared to handle `null` references.

## Examples of patterns that are flagged by this analyzer

```cs
class B { }

class CustomBFormatter : IMessagePackFormatter<B> { // MsgPack014
    void Serialize(ref MessagePackWriter writer, B value, MessagePackSerializerOptions options) {
        writer.WriteArrayHeader(0);
    }

    B Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        int count = reader.ReadArrayHeader();
        for (int i = 0; i < count; i++)
            reader.Skip();
    }
}
```

## Typical fix


```cs
class B { }

class CustomBFormatter : IMessagePackFormatter<B?> {
    void Serialize(ref MessagePackWriter writer, B? value, MessagePackSerializerOptions options) {
        if (value is null) {
            writer.WriteNil();
            return;
        }

        writer.WriteArrayHeader(0);
    }

    B? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
        if (reader.TryReadNil()) return null;

        int count = reader.ReadArrayHeader();
        for (int i = 0; i < count; i++)
            reader.Skip();
    }
}
```

An automated code fix will add the nullable ref annotations.
But it does not automatically add the null handling to the method bodies.
This must be done by hand.

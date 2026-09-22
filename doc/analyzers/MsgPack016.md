# MsgPack016 KeyAttribute-derived attributes are not supported by AOT formatters

When a `[MessagePackObject]`-attributed type attributes its fields and properties with attributes that derive from `KeyAttribute` rather than from `KeyAttribute` itself, that type is incompatible with AOT source generated formatters.

## Examples of patterns that are flagged by this analyzer

```cs
[MessagePackObject]
public class A
{
    [CompositeKey(0, 1)]
    public string Prop1 { get; set; }

    [CompositeKey(0, 2)]
    public string Prop2 { get; set; }
}

public class CompositeKeyAttribute : KeyAttribute
{
    public CompositeKeyAttribute(byte level, int index)
        : base(CreateKey(level, index)) { }

    private static string CreateKey(byte level, int index)
    {
        var c = (char)('A' + level);
        return c + index.ToString("x");
    }
}
```

## Typical fix

If you intend to keep using the derived attributes type, suppress the warning by turning off source generated formatter generation for that type:

```diff
-[MessagePackObject]
+[MessagePackObject(SuppressSourceGeneration = true)]
 public class A
```

Alternatively, enable propery AOT formatter source generation by switching to the standard `KeyAttribute`:

```cs
[MessagePackObject]
public class A
{
    [Key("A1")]
    public string Prop1 { get; set; }

    [Key("A2")]
    public string Prop2 { get; set; }
}
```


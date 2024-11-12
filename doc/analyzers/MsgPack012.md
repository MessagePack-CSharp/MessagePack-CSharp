# MsgPack012 Inaccessible data type

Serializable data types must have at least `internal` accessibility so that a source generated formatter elsewhere in the compilation can access it.
C# defaults to `internal` visibility except for nested types, which receive `private` visibility by default.

## Examples of patterns that are flagged by this analyzer

```cs
class Outer {
    [MessagePackObject]
    /*private*/ class DataType { }
}
```

## Typical fix

Add the `internal` or `public` modifier to the nested data type:

```cs
class Outer {
    [MessagePackObject]
    internal class DataType { }
}
```

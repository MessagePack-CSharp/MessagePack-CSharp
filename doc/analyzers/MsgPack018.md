# MsgPack018 Unique names required in force map mode

`[MessagePackObject]`-attributed types may omit attributing each member with a `[Key]` attribute using forced map mode.
In that mode, all serialized members *must* have unique names or a key collision would result in the serialized object.

## Examples of patterns that are flagged by this analyzer

```cs
[MessagePackObject]
public class A
{
    public string Prop1 { get; set; }
}

[MessagePackObject]
public class B : A
{
    public new string Prop1 { get; set; } // Diagnostic reported here due to redefinition of Prop1
}
```

## Typical fix

Rename one of the colliding properties:

```cs
[MessagePackObject]
public class A
{
    public string Prop1 { get; set; }
}

[MessagePackObject]
public class B : A
{
    public string Prop2 { get; set; }
}
```

Or add a `[Key]` attribute that assigns a unique serialized key to that member:


```cs
[MessagePackObject]
public class A
{
    public string Prop1 { get; set; }
}

[MessagePackObject]
public class B : A
{
    [Key("B_Prop1")]
    public new string Prop1 { get; set; }
}
```

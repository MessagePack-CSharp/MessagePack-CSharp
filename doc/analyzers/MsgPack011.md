# MsgPack011 Partial type required

When a `[MessagePackObject]` includes serializable members with less than `internal` visibility, the declaring type must use the `partial` modifier, so that the source generated formatter can be emitted as a member of that type and thus gain access to its private members.

## Examples of patterns that are flagged by this analyzer

```cs
[MessagePackObject]
public class A {
    [Key(0)]
    private B b;
}
```

## Typical fix

Add the `partial` keyword:

```cs
[MessagePackObject]
public partial class A {
    [Key(0)]
    private B b;
}
```

An automated code fix is available for this.

## Alternative fix

Alternatively, make all private/protected serializable members `internal` instead so that a formatter declared elsewhere in the compilation can reach them:

```cs
[MessagePackObject]
public class A {
    [Key(0)]
    internal B b;
}
```

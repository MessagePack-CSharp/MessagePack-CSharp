# MsgPack002 Avoid using a mutable static value for MessagePackSerializerOptions

Providing a static value for MessagePackSerializerOptions from a mutable member can lead to malfunction when that member is mutated by another party.

## Examples of patterns that are flagged by this analyzer

```cs
MessagePackSerializer.Serialize(obj, MessagePackSerializer.DefaultOptions);
```

or consuming your own static mutable member:

```cs
public class Foo
{
    public static MessagePackSerializerOptions MyOptions = MessagePackSerializerOptions.Standard;

    void Bar()
    {
        MessagePackSerializer.Serialize(obj, MyOptions); // diagnostic flagged here
    }
}
```

or *consuming* a static mutable member from MessagePack to define your own:

```cs
public class Foo
{
    public static readonly MessagePackSerializerOptions MyOptions = MessagePackSerializer.DefaultOptions;
```

## Solution

Use a `MessagePackSerializerOptions` value that comes from an *immutable* static property or field,
or any instance member or local variable.

```cs
MessagePackSerializer.Serialize(obj, MessagePackSerializerOptions.Standard);
```

or mark your own static field/property as readonly:

```cs
public class Foo
{
    public static readonly MessagePackSerializerOptions MyOptions = MessagePackSerializerOptions.Standard;

    void Bar()
    {
        MessagePackSerializer.Serialize(obj, MyOptions);
    }
}
```

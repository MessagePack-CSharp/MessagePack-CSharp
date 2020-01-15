# MsgPack001 Avoid static default for MessagePackSerializerOptions

Without an explicit MessagePackSerializerOptions argument, a shared/static default will be assumed which any other code in the entire AppDomain or process can alter, causing malfunction in your invocation.

## Examples of patterns that are flagged by this analyzer

```cs
MessagePackSerializer.Serialize(obj);
```

## Solution

Specify a `MessagePackSerializerOptions` value that comes from an immutable static property, field or local variable.

```cs
MessagePackSerializer.Serialize(obj, MessagePackSerializerOptions.Standard);
```

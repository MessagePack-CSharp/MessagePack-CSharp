# MsgPack017 Property with init accessor and initializer

When a `[MessagePackObject]`-attributed type declares a serialized property with an `init` setter (as opposed to a typical `set` setter) and
defines an initializer on that property, the default assigned by that initializer will be replaced with the default value for the type upon deserialization.

## Examples of patterns that are flagged by this analyzer

```cs
[MessagePackObject]
public class A
{
    [Key("Prop1")]
    public string Prop1 { get; init; } = "This is the default."; // Diagnostic emitted here

    [Key("Prop2")]
    public string Prop2 { get; set; } = "Another default."

    [Key("Prop3")]
    public string Prop3 { get; set; }
}
```

Deserializing this class may have unexpected behavior when a value is not given for `Prop1` during deserialization.
Consider the case of deserializing the msgpack equivalent of the following data payload:

```json
{ "Prop3": "hello" }
```

One might expect that when `A` is deserialized, it will have the following data:

Property | Value
--|--
Prop1 | "This is the default."
Prop2 | "Another default.
Prop3 | "hello"

But in fact it will have the following values:

Property | Value
--|--
Prop1 | null
Prop2 | "Another default.
Prop3 | "hello"

This is because as an `init` property, the only way the AOT source generated formatter can set the property's value is in an object initializer, like this:

```cs
// Values here are replaced if a value is given in deserialized data.
string deserializedProp1Value = default(string);
string deserializedProp2Value = default(string);

// Keep track of which values are actually specified.
bool deserializedProp2Value_specified = false; 

// process msgpack data here, possibly replacing deserializedProp1Value with an actual value.

A deserializedResult = new A
{
    Prop1 = deserializedProp1Value;
}

if (deserializedProp2Value_specified)
    deserializedResult.Prop2 = deserializedProp2Value;

return deserializedResult;
```

Notice how a property with a `set` setter may be conditionally set, and therefore it is only set when a value was specified by the data.
But the property with an `init` setter may only be set in the object initializer, for which C# provides no means to set conditionally.

## Typical fix

If it is important to specify a default value that is different from the default value for the type (e.g. `null` for reference types or `0` for integers), you should declare a `set` accessor instead of an `init` accessor.

```diff
-    public string Prop1 { get; init; } = "This is the default.";
+    public string Prop1 { get; set; } = "This is the default.";
```

Alternatively, disable source generated formatters for this type, forcing a dynamic one to be created at runtime which *does* have the ability to conditionally set the property.

```diff
-[MessagePackObject]
+[MessagePackObject(SuppressSourceGeneration = true)]
 public class A
```

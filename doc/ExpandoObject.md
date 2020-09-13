# Using `ExpandoObject` for Javascript-like discovery of messagepack structures

In Javascript an arbitrary JSON object can be parsed into an object graph and then explored with Javascript as easily as a native Javascript object,
since in Javascript all property access is late-bound.
In C# we can do the same thing using the `dynamic` keyword and the `ExpandoObject` type.

By default, deserializing untyped maps results in a `Dictionary<object, object>` being created to store the map.
If you would like to use C# `dynamic` to explore the deserialized object graph more naturally (i.e. the way Javascript would allow),
you can deserialize these maps into .NET `ExpandoObject` and use the C# dynamic keyword:

```cs
dynamic expando = new ExpandoObject();
expando.Name = "George";
expando.Age = 18;
expando.Other = new { OtherProperty = "foo" };

byte[] bin = MessagePackSerializer.Serialize(expando, MessagePackSerializerOptions.Standard);
this.logger.WriteLine(MessagePackSerializer.ConvertToJson(bin)); // {"Name":"George","Age":18,"Other":{"OtherProperty":"foo"}}

dynamic expando2 = MessagePackSerializer.Deserialize<ExpandoObject>(bin, ExpandoObjectResolver.Options);
Assert.Equal(expando.Name, expando2.Name);
Assert.Equal(expando.Age, expando2.Age);
Assert.NotNull(expando2.Other);
Assert.Equal(expando.Other.OtherProperty, expando2.Other.OtherProperty);
```

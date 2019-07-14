# Quick Start

Define the class to be serialized and attribute the class with `[MessagePackObject]`.
Attribute public members (property or field) with `[Key]`.

```csharp
[MessagePackObject]
public class MyClass
{
    // Key is serialization index, it is important for versioning.
    [Key(0)]
    public int Age { get; set; }

    [Key(1)]
    public string FirstName { get; set; }

    [Key(2)]
    public string LastName { get; set; }

    // public members and does not serialize target, mark IgnoreMemberttribute
    [IgnoreMember]
    public string FullName { get { return FirstName + LastName; } }
}
```

Call `MessagePackSerializer.Serialize<T>/Deserialize<T>` to serialize/deserialize your object graph.
You can also use the `ConvertToJson` method to see a human readable representation of the msgpack that was written.

```csharp
class Program
{
    static void Main(string[] args)
    {
        var mc = new MyClass
        {
            Age = 99,
            FirstName = "hoge",
            LastName = "huga",
        };

        // call Serialize/Deserialize, that's all.
        byte[] bytes = MessagePackSerializer.Serialize(mc);
        MyClass mc2 = MessagePackSerializer.Deserialize<MyClass>(bytes);

        // you can dump msgpack binary to human readable json.
        // In default, MeesagePack for C# reduce property name information.
        // [99,"hoge","huga"]
        var json = MessagePackSerializer.ConvertToJson(bytes);
        Console.WriteLine(json);
    }
}
```

By default the attribute is required. Optionally it can be unnecessary, see [Object Serialization section](serialization.md) and [Formatter Resolver section](resolvers.md) for details.

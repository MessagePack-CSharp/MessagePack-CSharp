MessagePack for C#(.NET, .NET Core, Unity)
===
Brainchild of [ZeroFormatter](https://github.com/neuecc/ZeroFormatter/), fastest MessagePack serializer on .NET.
Work in progress, stay tuned.

![image](https://cloud.githubusercontent.com/assets/46207/23337257/9bf22222-fc2a-11e6-888f-20d0451a526e.png)

Extremely fast, x10~20 faster than MsgPack-Cli.

Install
---
Beta is relased(please enable include pre-release package).

Standard library for .NET, .NET Core

```
Install-Package MessagePack -Pre
```

for Unity, download from releases page(not yet).

Extension Packages(info is see detail section).

```
Install-Package MessagePack.ImmutableCollections -Pre
Install-Package MessagePack.ReactiveProperty -Pre
Install-Package MessagePack.Unity -Pre
```

Quick Start
---
Define class and mark as `[MessagePackObject]` and public properties mark `[Key]`, call `MessagePackSerializer.Serialize<T>/Deserialize<T>`. 

```csharp
// mark MessagePackObjectAttribute
[MessagePackObject]
public class MyClass
{
    // Key is serialization index, it is important for versioning.
    [Key(0)]
    public virtual int Age { get; set; }

    [Key(1)]
    public virtual string FirstName { get; set; }

    [Key(2)]
    public virtual string LastName { get; set; }

    // If does not mark KeyAttribute, the property don't serialize/deserialize it.
    public string FullName { get { return FirstName + LastName; } }

    [Key(3)]
    public virtual IList<int> List { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        var mc = new MyClass
        {
            Age = 99,
            FirstName = "hoge",
            LastName = "huga",
            List = new List<int> { 1, 10, 100 }
        };

		// call Serialize/Deserialize, that's all.
        var bytes = MessagePackSerializer.Serialize(mc);
        var mc2 = MessagePackSerializer.Deserialize<MyClass>(bytes);
    }
}
---

 Union
---


Extensions
---
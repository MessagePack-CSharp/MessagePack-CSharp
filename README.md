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
Install-Package MessagePack.ImmutableCollection -Pre
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
    public int Age { get; set; }

    [Key(1)]
    public string FirstName { get; set; }

    [Key(2)]
    public string LastName { get; set; }

    // If does not mark KeyAttribute, the property don't serialize/deserialize it.
    public string FullName { get { return FirstName + LastName; } }
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
        };

		// call Serialize/Deserialize, that's all.
        var bytes = MessagePackSerializer.Serialize(mc);
        var mc2 = MessagePackSerializer.Deserialize<MyClass>(bytes);
    }
}
```

 Union
---
TODO:

Extensions
---
TODO:


Build
---
Open `MessagePack.sln` on Visual Studio 2017. `T4Templates` project is dummy project so unload-project. 

Unity Project is using symbolic link. At first, run `make_unity_symlink.bat` so linked under Unity project. You can open `src\MessagePack.UnityClient` on Unity Editor.

CodeGenerator(`mpc.exe`) is merged single exe to many dll by [LibZ](https://github.com/MiloszKrajewski/LibZ). run `build_libz.bat`, you can combine it.
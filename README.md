MessagePack for C#(.NET, .NET Core, Unity, Xamarin)
===

> TODO:Writing document now.

Extremely fast [MessagePack](http://msgpack.org/) serializer for C#, x10~20 faster than MsgPack-Cli and acquires best performance compared with all the other C# serializers.

![image](https://cloud.githubusercontent.com/assets/46207/23487810/e263277a-ff2b-11e6-81a6-6b4ca7acd8e3.png)

![image](https://cloud.githubusercontent.com/assets/46207/23487813/e7b22e1a-ff2b-11e6-8eeb-386c6a305628.png)

MessagePack has compact binary size and full set of general purpose expression. Please see the comparison with JSON, protobuf, ZeroFormatter section. If you want to know why MessagePack C# is fastest, please see performance section.

Install
---
The library provides in NuGet except for Unity.

Standard library for .NET, .NET Core, Xamarin.

```
Install-Package MessagePack
```

Visual Studio Analyzer to help object definition

```
Install-Package MessagePackAnalyzer
```

Extension Packages(info is see extension section).

```
Install-Package MessagePack.ImmutableCollection
Install-Package MessagePack.ReactiveProperty
Install-Package MessagePack.UnityShims
```

for Unity, download from [releases](https://github.com/neuecc/MessagePack-CSharp/releases) page, providing `.unitypackage`. Unity IL2CPP or Xamarin AOT Environment, check the pre-code generation section.

Quick Start
---
Define class and mark as `[MessagePackObject]` and public members(property or field) mark `[Key]`, call `MessagePackSerializer.Serialize<T>/Deserialize<T>`. 

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

    // public members and does not serialize target, mark IgnoreAttribute
	[Ignore]
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

By default the attribute is required. Optionally it can be unnecessary, see Object Serialization Format section and Formatter Resolver section for details.

Analyzer
---
MessagePackAnalyzer helps object definition. Attributes, accessibility etc are detected and it becomes a compiler error.

![zeroformatteranalyzer](https://cloud.githubusercontent.com/assets/46207/20078766/3ea54f14-a585-11e6-9873-b99cb5d9efe5.gif)

If you want to allow a specific type (for example, when registering a custom type), put `MessagePackAnalyzer.json` at the project root and make the Build Action to `AdditionalFiles`.

![image](https://cloud.githubusercontent.com/assets/46207/20149311/0e6f73d6-a6f4-11e6-91cb-44c771c267cb.png)

This is a sample of the contents of MessagePackAnalyzer.json. 

```
[ "System.Uri" ]
```

Built-in support types
---
TODO:


Object Serialization Format
---
TODO:



Union
---
TODO:


Note:Versioning....


Performance
---
TODO:



FormatterResolver
---
TODO:



MessagePackSerializer API
---




Extensions
---
TODO:

Pre Code Generation(mpc.exe)
---





RPC
---
TODO:

How to Build
---
Open `MessagePack.sln` on Visual Studio 2017.

Unity Project is using symbolic link. At first, run `make_unity_symlink.bat` so linked under Unity project. You can open `src\MessagePack.UnityClient` on Unity Editor.

Author Info
---
Yoshifumi Kawai(a.k.a. neuecc) is a software developer in Japan.  
He is the Director/CTO at Grani, Inc.  
Grani is a top social game developer in Japan.  
He is awarding Microsoft MVP for Visual C# since 2011.  
He is known as the creator of [UniRx](http://github.com/neuecc/UniRx/)(Reactive Extensions for Unity)  

Blog: https://medium.com/@neuecc (English)  
Blog: http://neue.cc/ (Japanese)  
Twitter: https://twitter.com/neuecc (Japanese)   

License
---
This library is under the MIT License.
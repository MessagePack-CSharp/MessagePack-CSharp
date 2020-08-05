# MessagePack Compiler via MSBuild Task

Cold startup performance and AOT environments can benefit by pre-compiling the specialized code
for serializing and deserializing your custom types.

Install the `MessagePack.MSBuild.Tasks` NuGet package in your project:
 [![NuGet](https://img.shields.io/nuget/v/MessagePack.MSBuild.Tasks.svg)](https://www.nuget.org/packages/MessagePack.MSBuild.Tasks)

This package automatically gets the MessagePack Compiler (mpc) to run during the build to produce a source file in the intermediate directory and adds it to the compilation, consumable in the normal way:

```cs
using System;
using MessagePack;
using MessagePack.Resolvers;

class Program
{
    static void Main(string[] args)
    {
        var o = new SomeObject { SomeMember = "hi" };

        var options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                GeneratedResolver.Instance,
                StandardResolver.Instance
            ));
        byte[] b = MessagePackSerializer.Serialize(o, options);
        var o2 = MessagePackSerializer.Deserialize<SomeObject>(b, options);
        Console.WriteLine(o2.SomeMember);
    }
}

[MessagePackObject]
public class SomeObject
{
    [Key(0)]
    public string SomeMember { get; set; }
}
```

## Customizations

A few MSBuild properties can be set in your project to customize mpc:

Property | Purpose | Default value
--|--|--
`MessagePackGeneratedResolverNamespace` | The prefix for the namespace under which code will be generated. `.Formatters` is always appended to this value. | `MessagePack`
`MessagePackGeneratedResolverName` | The name of the generated type. | `GeneratedResolver`
`MessagePackGeneratedUsesMapMode` | A boolean value that indicates whether all formatters should use property maps instead of more compact arrays. | `false`

For example you could add this xml to your project file to set each of the above properties (in this example, to their default values):

```xml
<PropertyGroup>
 <MessagePackGeneratedResolverNamespace>MessagePack</MessagePackGeneratedResolverNamespace>
 <MessagePackGeneratedResolverName>GeneratedResolver</MessagePackGeneratedResolverName>
 <MessagePackGeneratedUsesMapMode>false</MessagePackGeneratedUsesMapMode>
</PropertyGroup>
```

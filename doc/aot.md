# AOT Code Generation (to support Unity/Xamarin)

By default, MessagePack for C# serializes custom objects by using [generating IL](https://msdn.microsoft.com/en-us/library/system.reflection.emit.ilgenerator.aspx) at runtime for custom, highly tuned formatters for each type. This code generation has a minor upfront perf cost.
Because strict-AOT environments such as Xamarin and Unity IL2CPP forbid runtime code generation, MessagePack provides a way for you to run a code generator ahead of time as well.

> Note: When Unity targets the PC it allows dynamic code generation, so AOT is not required.

If you want to avoid the upfront dynamic generation cost or you need to run on Xamarin or Unity, you need AOT code generation. `mpc.exe`(MessagePackCompiler) is the code generator of MessagePack for C#. You can download mpc from the [releases](https://github.com/neuecc/MessagePack-CSharp/releases/) page, `mpc.zip`. mpc uses [Roslyn](https://github.com/dotnet/roslyn) to analyze source code.

```
mpc arguments help:
-i, --input              [required]Input path of analyze csproj
-o, --output             [required]Output file path
-c, --conditionalsymbol  [optional, default=empty]conditional compiler symbol
-r, --resolvername       [optional, default=GeneratedResolver]Set resolver name
-n, --namespace          [optional, default=MessagePack]Set namespace root name
-m, --usemapmode         [optional, default=false]Force use map mode serialization
```

```
// Simple Sample:
mpc.exe -i "..\src\Sandbox.Shared.csproj" -o "MessagePackGenerated.cs"

// Use force map simulate DynamicContractlessObjectResolver
mpc.exe -i "..\src\Sandbox.Shared.csproj" -o "MessagePackGenerated.cs" -m
```

If you create DLL by msbuild project, you can use Pre/Post build event.

```xml
<PropertyGroup>
    <PreBuildEvent>
        mpc.exe, here is useful for analyze/generate target is self project.
    </PreBuildEvent>
    <PostBuildEvent>
        mpc.exe, here is useful for analyze target is another project.
    </PostBuildEvent>
</PropertyGroup>
```

By default, `mpc.exe` generates resolver to `MessagePack.Resolvers.GeneratedResolver` and formatters generates to `MessagePack.Formatters.***`. You must specify this resolver each time you invoke the `MessagePackSerializer`.

```csharp
// Do this once and store it for reuse.
var resolver = MessagePack.Resolvers.CompositeResolver.CreateForAot(MessagePack.Resolvers.GeneratedResolver.Instance);
var options = MessagePackSerializerOptions.Default.WithResolver(resolver);

// Each time you serialize/deserialize, specify the options:
byte[] msgpackBytes = MessagePackSerializer.Serialize(myObject, options);
T myObject2 = MessagePackSerializer.Deserialize<MyObject>(msgpackBytes, options);
```

> Note: mpc.exe currently only supports running on Windows.
You can run on [Mono](http://www.mono-project.com/), that supports Mac and Linux.

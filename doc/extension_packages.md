# Extensions

MessagePack for C# has extension point and you can add external type's serialization support. There are official extension support.

```
Install-Package MessagePack.ImmutableCollection
Install-Package MessagePack.ReactiveProperty
Install-Package MessagePack.UnityShims
Install-Package MessagePack.AspNetCoreMvcFormatter
```

`MessagePack.ImmutableCollection` package add support for [System.Collections.Immutable](https://www.nuget.org/packages/System.Collections.Immutable/) library. It adds `ImmutableArray<>`, `ImmutableList<>`, `ImmutableDictionary<,>`, `ImmutableHashSet<>`, `ImmutableSortedDictionary<,>`, `ImmutableSortedSet<>`, `ImmutableQueue<>`, `ImmutableStack<>`, `IImmutableList<>`, `IImmutableDictionary<,>`, `IImmutableQueue<>`, `IImmutableSet<>`, `IImmutableStack<>` serialization support.

`MessagePack.ReactiveProperty` package add support for [ReactiveProperty](https://github.com/runceel/ReactiveProperty) library. It adds `ReactiveProperty<>`, `IReactiveProperty<>`, `IReadOnlyReactiveProperty<>`, `ReactiveCollection<>`, `Unit` serialization support. It is useful for save viewmodel state.

`MessagePack.UnityShims` package provides shim of [Unity](https://unity3d.com/)'s standard struct(`Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`, `Bounds`, `Rect`, `AnimationCurve`, `Keyframe`, `Matrix4x4`, `Gradient`, `Color32`, `RectOffset`, `LayerMask`, `Vector2Int`, `Vector3Int`, `RangeInt`, `RectInt`, `BoundsInt`) and their formatters. It can enable to communicate between server and Unity client.

After install, extension package must enable by configuration. Here is sample of enable all extension.

```csharp
// set extensions to default resolver.
MessagePack.Resolvers.CompositeResolver.RegisterAndSetAsDefault(
    // enable extension packages first
    ImmutableCollectionResolver.Instance,
    ReactivePropertyResolver.Instance,
    MessagePack.Unity.Extension.UnityBlitResolver.Instance,
    MessagePack.Unity.UnityResolver.Instance,

    // finaly use standard(default) resolver
    StandardResolver.Instance);
);
```

Configuration details, see:[Extension Point section](https://github.com/neuecc/MessagePack-CSharp#extension-pointiformatterresolver).

`MessagePack.AspNetCoreMvcFormatter` is add-on of [ASP.NET Core MVC](https://github.com/aspnet/Mvc)'s serialization to boostup performance. This is configuration sample.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc().AddMvcOptions(option =>
    {
        option.OutputFormatters.Clear();
        option.OutputFormatters.Add(new MessagePackOutputFormatter(ContractlessStandardResolver.Instance));
        option.InputFormatters.Clear();
        option.InputFormatters.Add(new MessagePackInputFormatter(ContractlessStandardResolver.Instance));
    });
}
```

Author is creating other extension packages, too.

* [MasterMemory](https://github.com/neuecc/MasterMemory) - Embedded Readonly In-Memory Document Database
* [MagicOnion](https://github.com/neuecc/MagicOnion) - gRPC based HTTP/2 RPC Streaming Framework
* [DatadogSharp](https://github.com/neuecc/DatadogSharp) - C# Datadog client

You can make your own extension serializers or integrate with framework, let's create them and share it!

* [MessagePack.FSharpExtensions](https://github.com/pocketberserker/MessagePack.FSharpExtensions) - supports F# list,set,map,unit,option,discriminated union
* [MessagePack.NodaTime](https://github.com/ARKlab/MessagePack) -
Support for NodaTime types to MessagePack C#
* [WebApiContrib.Core.Formatter.MessagePack](https://github.com/WebApiContrib/WebAPIContrib.Core#formatters) - supports ASP.NET Core MVC([details in blog post](https://www.strathweb.com/2017/06/using-messagepack-with-asp-net-core-mvc/))
* [MessagePack.MediaTypeFormatter](https://github.com/sketch7/MessagePack.MediaTypeFormatter) - MessagePack MediaTypeFormatter

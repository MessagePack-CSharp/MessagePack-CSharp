# Unity support

You can install by package and includes source code. If build target as PC, you can use as is but if build target uses IL2CPP, you can not use `Dynamic***Resolver` so use pre-code generation. Please see [pre-code generation section](aot.md).

In Unity, MessagePackSerializer can serialize `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`, `Bounds`, `Rect`, `AnimationCurve`, `Keyframe`, `Matrix4x4`, `Gradient`, `Color32`, `RectOffset`, `LayerMask`, `Vector2Int`, `Vector3Int`, `RangeInt`, `RectInt`, `BoundsInt` and there nullable, there array, there list by built-in extension `UnityResolver`. It is included StandardResolver by default.

MessagePack for C# has additional unsafe extension.  `UnsafeBlitResolver` is special resolver for extremely fast unsafe serialization/deserialization for struct array.

![image](https://cloud.githubusercontent.com/assets/46207/23837633/76589924-07ce-11e7-8b26-e50eab548938.png)

x20 faster Vector3[] serialization than native JsonUtility. If use `UnsafeBlitResolver`, serialize special format(ext:typecode 30~39)  `Vector2[]`, `Vector3[]`, `Quaternion[]`, `Color[]`, `Bounds[]`, `Rect[]`. If use `UnityBlitWithPrimitiveArrayResolver`, supports `int[]`, `float[]`, `double[]` too. This special feature is useful for serialize Mesh(many Vector3[]) or many transform position.

If you want to use unsafe resolver, you must enables unsafe option and define additional symbols. At first, write `-unsafe` on `smcs.rsp`, `gmcs.rsp` etc. And define `ENABLE_UNSAFE_MSGPACK` symbol.

![image](https://cloud.githubusercontent.com/assets/46207/23837456/fc01c828-07cb-11e7-92bf-f23eb2575115.png)

Here is sample of configuration.

```csharp
Resolvers.CompositeResolver.RegisterAndSetAsDefault(
    MessagePack.Unity.UnityResolver.Instance,
    MessagePack.Unity.Extension.UnityBlitWithPrimitiveArrayResolver.Instance,

    // If PC, use StandardResolver
    // MessagePack.Resolvers.StandardResolver.Instance,

    // If IL2CPP, Builtin + GeneratedResolver.
    // MessagePack.Resolvers.BuiltinResolver.Instance,
);
```

`MessagePack.UnityShims` NuGet package is for .NET ServerSide serialization support to communicate with Unity. It includes shim of Vector3 etc and Safe/Unsafe serialization extension.

If you want to share class between Unity and Server, you can use `SharedProject` or `Reference as Link` or new MSBuild(VS2017)'s wildcard reference etc. Anyway you need to source-code level share. This is sample project structure of use SharedProject.

- SharedProject(source code sharing)
  - Source codes of server-client shared
- ServerProject(.NET 4.6/.NET Core/.NET Standard)
  - [SharedProject]
  - [MessagePack]
  - [MessagePack.UnityShims]
- ClientDllProject(.NET 3.5)
  - [SharedProject]
  - [MessagePack](not dll, use MessagePack.unitypackage's sourcecodes)
- Unity
  - [Builded ClientDll]

Other ways, use plain POCO by `DataContract`/`DataMember` can use.

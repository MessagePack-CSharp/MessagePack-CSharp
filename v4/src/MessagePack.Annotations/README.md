# MessagePack.Annotations (migration shim)

In MessagePack v4 the attributes (`[MessagePackObject]`, `[Key]`, `[IgnoreMember]`, `[SerializationConstructor]`, `[MessagePackFormatter]`) and `IMessagePackSerializationCallbackReceiver` live in the **MessagePack** package itself. The source generator ships there too and runs on the assembly that declares the annotated types, so that assembly has to reference MessagePack anyway. An annotations-only reference is no longer a supported setup.

This package exists so that projects coming from v3 keep working:

- The surviving types are type-forwarded to MessagePack, so `using MessagePack;` code compiles unchanged and pre-built v3 assemblies still load.
- The package depends on MessagePack, so bumping its version brings the v4 runtime and source generator in.
- Having a 4.x version keeps NuGet from leaving MessagePack.Annotations 3.x next to MessagePack 4.x, which would define every attribute twice (CS0433).
- The types v4 renamed or deleted (`[Union]`, `[MessagePackKnownFormatter]`, `[MessagePackAssumedFormattable]`, `[ExcludeFormatterFromSourceGeneratedResolver]`) still exist here in their v3 shape, marked `[Obsolete]` with `error: true`. The compiler error text tells you the replacement.

Once your code compiles, remove this package and reference MessagePack directly.

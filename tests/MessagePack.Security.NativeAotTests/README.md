# Security NativeAOT tests

This executable checks the default and explicitly opted-in object comparer
contracts. It is not a complete library NativeAOT test suite.
See [the security documentation](../../README.md#non-reflective-object-comparers-for-nativeaot)
for the application-wide publish-time switch, annotated library asset
requirement, and custom/boxed-enum override migration.

## Running

From the repository root, with the supported SDK:

```text
dotnet run --project tests/MessagePack.Security.NativeAotTests -c Release -p:PublishAot=false -p:NonReflectiveSecurity=false -- legacy
dotnet run --project tests/MessagePack.Security.NativeAotTests -c Release -p:PublishAot=false -p:NonReflectiveSecurity=true -- opt-in
```

With the platform's NativeAOT prerequisites installed:

```text
dotnet publish tests/MessagePack.Security.NativeAotTests -c Release -r win-x64 -p:NonReflectiveSecurity=true -warnaserror -o bin/security-optin
```

Run `bin/security-optin/MessagePack.Security.NativeAotTests.exe opt-in` on Windows.
For Linux, publish with `-r linux-x64` and run
`./bin/security-optin/MessagePack.Security.NativeAotTests opt-in`.
CI runs both JIT modes and publishes/runs the opt-in native executable on
Windows and Linux, treating native build warnings as errors.

The fixture defaults to net9.0, matching the upstream test framework.
`-p:SecurityTestTargetFramework=net10.0` can select a newer executable target
without retargeting the library. The mode argument is an assertion, not a
mechanism for changing the switch.

For the legacy native negative control, publish with
`-p:NonReflectiveSecurity=false`, omit `-warnaserror`, and run with `legacy`.
That build intentionally still diagnoses the original IL2060/IL3050 path.
To inspect reachability, add `-p:IlcGenerateDgmlFile=true`: the opt-in scan
graph should contain no `ObjectFallbackEqualityComparer` nodes.
No suppression attributes, warning filters, linker roots, or dependency
annotations are used.

The checks cover typed and boxed primitives/enums, an enum with no typed
enum comparer call, normalization, collision fixtures, custom and enum
overrides, cache reuse, clone isolation, unsupported-type rejection, both
comparer interfaces, and an exact-wire-format untrusted map round trip.
The legacy checks make ordinary typed calls before testing reflection
dispatch, demonstrating the existing NativeAOT contract when those native
generic instantiations are available. This is not a claim that arbitrary
unseen types work under legacy NativeAOT.

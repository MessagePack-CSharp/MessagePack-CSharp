# Migrating from MessagePack v2 to v3

The most significant change in v3 is that AOT source generation of formatters is on by default.
These new formatters are comparable to the dynamic formatters generated at runtime in prior versions, but they have some limitations not shared by their dynamically generated counterparts.
These limitation and other migration requirements and considerations are enumerated below or are called out by analyzers.

The `MessagePackAnalyzer` nuget package, which was optional in v2, is now a nuget dependency, which means diagnostics may appear in your compilation after upgrading to v3 that you have not seen before.
v3 adds many new diagnostic providers to the set of analyzers as well, with general help and specific help for handling the new source generated formatters.

## Breaking Changes

- `MessagePackAnalyzer.json` is no longer used to configure the analyzer.
  Use `GeneratedMessagePackResolverAttribute`, `MessagePackKnownFormatterAttribute` and `MessagePackAssumedFormattableAttribute` instead.
- The `mpc` CLI tool is no longer used to generate ahead-of-time (AOT) formatters and resolver.
  AOT code generation is "on by default" in v3 courtesy of our roslyn source generator.
- Custom implementations of `IMessagePackFormatter<T>` should be `internal` for automatic inclusion in our source generated resolver.
- Types annotated with `[MessagePackObject]` should be declared as `partial` to grant the source generated formatter access to private/protected members, when applicable.
- Unity users:
  - Use NuGetForUnity to acquire the `MessagePack` nuget package instead of acquiring source code via the .zip file on our Releases page.
  - Unity 2021.3 is no longer supported. The minimum required version is 2022.3.12f1.

## Adapting to breaking changes

### Migrate your `MessagePackAnalyzer.json` file

1. Add `[assembly: MessagePackAssumedFormattable(typeof(MyType1))]` to your project for each type that appears inside your `MessagePackAnalyzer.json` file.
1. Delete the `MessagePackAnalyzer.json` file.

### Migrate from `mpc`

1. Remove any scripts that invoked `mpc` from your build.
1. Follow the instructions in [the AOT section of the README](../README.md#aot) to create a source generated resolver (with formatters).

Be sure to build with .NET SDK 6.0 or later.

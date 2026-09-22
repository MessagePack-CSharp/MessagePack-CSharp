# MsgPack009 Colliding Formatters

All formatters in a compilation are automatically added to a source generated resolver so that it can be found at runtime.

When two formatters implement `IMessagePackFormatter<T>` for the same `T`, it cannot be statically determined which formatter should be used, and this diagnostic results.

## Typical fix

Either remove all but one of the conflicting formatters, or exclude all but one from inclusion in the source generated resolver by applying the `[ExcludeFormatterFromSourceGeneratedResolver]` attribute to some of the formatters.

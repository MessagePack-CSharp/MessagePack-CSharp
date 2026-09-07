namespace MessagePack;

// Design notes:
// Both conversions are instance members so the interface compiles on every target framework (no static abstract members),
// and a constrained instance call on a struct is resolved and inlined even inside shared generic code,
// whereas a static virtual call from a formatter instantiated over a class target needs a runtime dictionary lookup.
// ToSurrogate is the creation direction, which no instance can own, so the formatter invokes it on default(TSurrogate).
// Surrogates are structs by constraint. The conversion is a one-shot projection with a bounded copy count
// (one per direction), so a struct costs a small memcpy where a class would cost an allocation per value,
// and a value-type argument fully specializes the conversion calls.
// A recursive target still works, because the surrogate references the target type in its members
// (e.g. Node[]? Children) and resolution recurses through the registered surrogate formatter one level at a time.
// Shapes the generator cannot register (generic surrogates, MsgPack019) compose SurrogateFormatterFactory into an explicit chain instead.

/// <summary>
/// Declares <typeparamref name="TSurrogate"/> as the serialized stand-in for <typeparamref name="TTarget"/>,
/// for types whose own shape cannot be serialized directly, such as third-party types or types with constructor-enforced invariants.
/// Implement it on the surrogate, a struct marked with <see cref="MessagePackObjectAttribute"/>.
/// The source generator discovers the implementation and registers a formatter for the target, which needs no attribute of its own.
/// </summary>
/// <typeparam name="TTarget">Type being serialized.</typeparam>
/// <typeparam name="TSurrogate">Struct whose members form the serialized representation.</typeparam>
public interface IMessagePackSurrogate<TTarget, TSurrogate>
    where TSurrogate : struct, IMessagePackSurrogate<TTarget, TSurrogate>
{
    /// <summary>
    /// Converts a non-null target value into its surrogate.
    /// Called on a default instance, so build the result from <paramref name="value"/> alone.
    /// </summary>
    TSurrogate ToSurrogate(TTarget value);

    /// <summary>Reconstructs the target value from this deserialized surrogate.</summary>
    TTarget ToTarget();
}

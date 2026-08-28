#if NET
namespace MessagePack;

/// <summary>
/// Declares TSurrogate as the serialized stand-in for TTarget: the wire carries
/// TSurrogate's shape, and the two static conversions bridge it to TTarget. This is the
/// declarative alternative to a hand-written formatter for types whose natural shape
/// cannot ride the wire directly (constructor-enforced invariants, third-party types,
/// computed state): mark the surrogate itself [MessagePackObject] and implement this
/// interface ON the surrogate (TSurrogate is the implementing type) — that implementation
/// is the whole declaration, discovered by the source generator and auto-registered, so
/// the target needs no attribute at all. Deserialization always flows through
/// <see cref="FromSurrogate"/>, so the target's constructor validation stays in force.
/// </summary>
/// <remarks>
/// Surrogates are STRUCTS by constraint: the conversion is a one-shot projection whose
/// copy count is bounded (one per direction), so a struct costs a small memcpy where a
/// class would cost an allocation per value plus shared-generics dictionary dispatch for
/// the static abstract calls — and a value-type argument fully specializes the formatter
/// instantiation. Nothing is lost: a RECURSIVE target still works, because the surrogate
/// references the TARGET type in its members (e.g. Node[]? Children) and resolution
/// recurses through the registered surrogate formatter one level at a time. Hand-write a
/// formatter when even the one projection copy shows up in a profile.
/// net10.0+ only (static abstract interface members): downlevel targets keep using
/// hand-written formatters. Shapes the generator cannot register (generic surrogates,
/// MsgPack019) compose <see cref="Formatters.SurrogateFormatterFactory{TTarget, TSurrogate}"/>
/// into an explicit chain instead.
/// </remarks>
public interface IMessagePackSurrogate<TTarget, TSurrogate>
    where TSurrogate : struct, IMessagePackSurrogate<TTarget, TSurrogate>
{
    /// <summary>Projects a (non-null) target value into its wire stand-in.</summary>
    static abstract TSurrogate ToSurrogate(TTarget value);

    /// <summary>Reconstructs the target value from its wire stand-in.</summary>
    static abstract TTarget FromSurrogate(TSurrogate surrogate);
}
#endif

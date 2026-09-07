namespace MessagePack.Formatters;

// The manual TSurrogate constraint opts this type out of the BufferConstraints generator,
// so the buffer constraints are spelled here by hand, downlevel split included.

public sealed class SurrogateFormatter<TWriteBuffer, TReadBuffer, TTarget, TSurrogate> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, TTarget?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TSurrogate : struct, IMessagePackSurrogate<TTarget, TSurrogate>
{
    // "does TTarget have a null representation" (reference types AND Nullable<S>), the
    // predicate the nil branch actually needs. A JIT-time constant per instantiation.
    // typeof(TTarget).IsValueType would misclassify Nullable<S> (whose nil must read as
    // null), and patching that with Nullable.GetUnderlyingType is not an intrinsic, so
    // this is the only spelling of the exact predicate that still constant-folds.
    static bool TargetCanBeNull => default(TTarget) is null;

    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TSurrogate> surrogateFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        surrogateFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TSurrogate>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, TTarget? value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }
        surrogateFormatter.Serialize(ref buffer, ref state, default(TSurrogate).ToSurrogate(value));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref TTarget? value)
    {
        // a nil where a non-nullable TTarget sits falls through to the surrogate formatter's own error
        if (TargetCanBeNull && buffer.TryReadNil())
        {
            value = default;
            return;
        }
        TSurrogate surrogate = default;
        surrogateFormatter.Deserialize(ref buffer, ref state, ref surrogate);
        value = surrogate.ToTarget();
    }
}

/// <summary>
/// Serves TTarget through <see cref="SurrogateFormatter{TWriteBuffer, TReadBuffer, TTarget, TSurrogate}"/>.
/// Implementing IMessagePackSurrogate already auto-registers the target, so reaching for
/// this factory directly is only needed for shapes the generator skips (MsgPack019:
/// generic surrogates), composed into an explicit chain. Reflection-free, so Native AOT safe.
/// </summary>
public sealed partial class SurrogateFormatterFactory<TTarget, TSurrogate> : MessagePackFormatterFactory
    where TSurrogate : struct, IMessagePackSurrogate<TTarget, TSurrogate>
{
    // one method, two signatures: net9+ overrides the base virtual (constraints inherited);
    // downlevel has no base member, so the constraints are spelled out
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(TTarget))
        {
            return new SurrogateFormatter<TWriteBuffer, TReadBuffer, TTarget, TSurrogate>();
        }
        return null;
    }
}

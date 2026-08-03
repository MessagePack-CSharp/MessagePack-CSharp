using SerializerFoundation;

namespace UltraMessagePack;

public interface IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
{
    void Initialize(MessagePackFormatterResolver resolver);
    void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T value);
    void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value);
}

// TODO: implement object-graph guarding (cycle detection) and depth limiting.

[StructLayout(LayoutKind.Auto)]
public struct SerializeState
{
}

[StructLayout(LayoutKind.Auto)]
public struct DeserializeState
{
    public int Depth;
}

// The factory IS the resolution unit: it receives the requested runtime Type and either
// creates a formatter for it or returns null ("not mine"). What used to be a separate
// factory-RESOLVER layer (Type -> factory) collapses into the factory itself, and
// composition is just CompositeFormatterFactory — a factory over factories. The
// MessagePackFormatterResolver above all this is nothing but the per-instance formatter
// cache.
//
// Chain discipline: a factory placed DIRECTLY into a chain must check `type` and return
// null for types it does not serve; a factory registered under a Type key (e.g. via
// FormatterRegistry.RegisterFactory&lt;T&gt;) is only ever asked for that type and
// may ignore the parameter.
public interface IMessagePackFormatterFactory
{
    /// <summary>
    /// Creates an IMessagePackFormatter&lt;TWriteBuffer, TReadBuffer, T&gt; for the
    /// requested type (returned as object; the resolver validates the shape), or null
    /// when this factory does not serve the type.
    ///
    /// CONTRACT: must return a fresh instance per call. A formatter that captures resolver
    /// state in Initialize (e.g. nested formatter fields) would be re-Initialized by a
    /// second resolver and route calls into the wrong formatter graph if shared. Only
    /// fully stateless formatters may safely return a cached singleton.
    ///
    /// MULTI-TARGETING SHAPE (resolves the GVM landmine verified 2026-07: `#if`-ing
    /// `allows ref struct` on an abstract generic interface METHOD makes downlevel-compiled
    /// implementations fail to LOAD against the with-flag build — "weaker type parameter
    /// constraints" TypeLoadException). The generic member therefore exists only on
    /// `allows ref struct` TFMs and carries a DEFAULT IMPLEMENTATION bridging to the
    /// Type-based overload below — the same versioning technique as
    /// INumberBase&lt;T&gt;.MultiplyAddEstimate: an implementation that cannot see this
    /// member still loads, and calls route through the bridge into its Type-based
    /// implementation. Implementations that CAN see it should override for direct,
    /// reflection-free construction (UMP102 nudges this).
    /// </summary>

#if NET9_0_OR_GREATER

    object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return CreateFormatter(typeof(TWriteBuffer), typeof(TReadBuffer), type);
    }

#endif

    /// <summary>
    /// Type-based flavor — the only ABSTRACT member, identical on every TFM (this is
    /// what downlevel factories implement and what the downlevel resolver calls).
    /// Implementations dispatch known buffer-type pairs back into generic construction
    /// (the source generator emits that dispatch for partial factories); unknown pairs
    /// return null.
    /// </summary>
    object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType);
}

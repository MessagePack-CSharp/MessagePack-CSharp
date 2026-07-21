using SerializerFoundation;

namespace UltraMessagePack;

public interface IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    void Initialize(MessagePackFormatterResolver resolver);
    void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref T value);
    void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value);
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
// DynamicFormatterFactory.RegisterFactory&lt;T&gt;) is only ever asked for that type and
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
    /// MULTI-TARGETING LANDMINE (verified empirically, 2026-07): this GENERIC METHOD is the
    /// one place `#if`-ing `allows ref struct` per TFM breaks. An implementation compiled
    /// against a no-flag TFM build fails to LOAD against a with-flag build with
    /// TypeLoadException ("implicitly implement an interface method with weaker type
    /// parameter constraints") — and consumer assemblies targeting a lower TFM than the app
    /// hit exactly that skew. Type-LEVEL generic parameters (IMessagePackFormatter
    /// implementations) are safe across the skew. Before shipping multi-TFM, restructure
    /// this factory so the buffer type parameters sit on a TYPE (e.g. an open-generic
    /// factory type closed by the resolver via MakeGenericType) instead of on a method.
    /// </summary>
    object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct;
}

[StructLayout(LayoutKind.Auto)]
public struct SerializeState
{
}

[StructLayout(LayoutKind.Auto)]
public struct DeserializeState
{
    public int Depth;
}

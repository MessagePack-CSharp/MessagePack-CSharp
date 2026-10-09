using MessagePack.Formatters;

namespace MessagePack;

// Global per-instantiation id for the resolvers' formatter tables.
// The id is only the slot index. The tables (and the formatters in them) are per-resolver instance state,
// so different resolvers holding different formatters for the same T never cross paths.

static class FormatterTypeIdCounter
{
    internal static int Next;
}

static class FormatterTypeId<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
{
    public static readonly int Value = Interlocked.Increment(ref FormatterTypeIdCounter.Next) - 1;
}

/// <summary>
/// Hands out the formatters of a factory chain and caches them per type.
/// A formatter is created once per resolver and shared afterwards, so a resolver is meant to live as long as the options that hold it.
/// </summary>
public sealed class MessagePackFormatterResolver
{
    // Accumulates the in-progress construction while nested formatters resolve, then registers the whole graph
    // at once when control returns to the root.
    readonly Lock gate = new Lock();
    readonly GraphConstruction construction = new();

    sealed class GraphConstruction
    {
        public readonly Dictionary<int, object> ConstructingFormatters = new();

        public bool FoundLegacyFormatter;

        public bool IsConstructing => ConstructingFormatters.Count != 0;

        public void Clear()
        {
            ConstructingFormatters.Clear();
            FoundLegacyFormatter = false;
        }
    }

    readonly MessagePackFormatterFactory factory;

    // Cache of the registered IMessagePackFormatters, indexed by FormatterTypeId.
    // A non-null slot holds an actual formatter created by the factory (including MissingMessagePackFormatter,
    // which throws at use) and never the compat sentinel. That invariant lets the hot path get by with a null check
    // alone, no type test.
    object?[] formatterTable = [];

    // CompatiblePairRequiredFormatter sentinels, same indexing as formatterTable.
    // Keeping them out of formatterTable moves the sentinel probe to the cold path. Both tables are copy-on-write
    // and lock-free to read.
    object?[] compatRequiredTable = [];

    /// <summary>Whether formatters that build hash-based collections such as Dictionary and HashSet use an equality comparer resistant to hash-flooding attacks.</summary>
    public bool HashFloodingResistant { get; init; } = true;

    /// <summary>Whether a formatter built without ref struct buffer support makes serialization throw instead of falling back to the compatible buffers.</summary>
    public bool ThrowOnLegacyFormatter { get; init; }

    /// <summary>
    /// Whether deserialization fails when the payload carries no value for a required member, meaning one declared with the C# <c>required</c> modifier or a constructor parameter without a default value.
    /// Turn it off to accept such payloads, for example data written by an older schema; the member then keeps its default.
    /// </summary>
    public bool ValidateRequiredMembers { get; init; } = true;

    /// <summary>
    /// Whether deserialization fails when the payload assigns nil to a member declared as a non-nullable reference type.
    /// Off by default. Only directly declared member types are checked; generic type arguments and collection elements are not, since their nullability is erased at runtime.
    /// </summary>
    public bool ValidateNullableAnnotations { get; init; }

    /// <summary>Raised with the root type when serialization fell back to the compatible buffers because a formatter built without ref struct buffer support was involved.</summary>
    public event Action<Type>? CompatibilityFallback;

    /// <summary>
    /// Creates a resolver over <paramref name="factory"/>. The settings (<see cref="HashFloodingResistant"/> and the other init-only
    /// properties) are chosen in the object initializer and fixed for the lifetime of the resolver, since the cached formatters capture them.
    /// </summary>
    public MessagePackFormatterResolver(MessagePackFormatterFactory factory)
    {
        this.factory = factory;
    }

    /// <summary>Creates a resolver over <paramref name="factories"/> combined into one chain, where the first factory that serves a type wins.</summary>
    public MessagePackFormatterResolver(MessagePackFormatterFactory[] factories)
        : this(MessagePackFormatterFactory.Combine(factories))
    {
    }

#if NET9_0_OR_GREATER

    // Top-level acquisition for the serializer entry points.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetFormatter<TWriteBuffer, TReadBuffer, T>(out IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        var id = FormatterTypeId<TWriteBuffer, TReadBuffer, T>.Value;
        var table = formatterTable;
        if ((uint)id < (uint)table.Length && table[id] is { } f)
        {
            formatter = Unsafe.As<IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>>(f);
            return true;
        }

        return TryGetFormatterSlow(id, out formatter);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    bool TryGetFormatterSlow<TWriteBuffer, TReadBuffer, T>(int id, out IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        // On net9+, a formatter provided by a netstandard2.0-built library only works over the Compatible buffers.
        // Returning false tells the caller to re-acquire via GetFormatter over the Compatible buffer pair.
        // The sentinel ledger keeps that per-call detection lock-free.
        var compat = compatRequiredTable;
        if ((uint)id < (uint)compat.Length && compat[id] != null)
        {
            formatter = null!;
            return false;
        }

        formatter = CreateAndRegisterFormatter<TWriteBuffer, TReadBuffer, T>(id);
        return formatter is not CompatiblePairRequiredFormatter;
    }

#endif

    /// <summary>
    /// Returns the formatter for <typeparamref name="T"/> over the given buffer types, creating and caching it on first use.
    /// A type no factory serves gets a formatter that throws when used.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> GetFormatter<TWriteBuffer, TReadBuffer, T>()
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var id = FormatterTypeId<TWriteBuffer, TReadBuffer, T>.Value;
        var table = formatterTable;
        if ((uint)id < (uint)table.Length)
        {
            // The slot id is unique to this instantiation, so the stored object is always an
            // IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>. The compatible-pair sentinel lives in
            // compatRequiredTable and is found on the cold path below.
            var f = table[id];
            if (f != null)
            {
                return Unsafe.As<IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>>(f);
            }
        }

        return CreateAndRegisterFormatter<TWriteBuffer, TReadBuffer, T>(id);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> CreateAndRegisterFormatter<TWriteBuffer, TReadBuffer, T>(int id)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        lock (gate)
        {
            var isRoot = !construction.IsConstructing;

            // 1. double-check, since another thread may have registered it while we were entering the lock
            var table = formatterTable;
            if ((uint)id < (uint)table.Length && table[id] is { } published)
            {
                return (IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>)published;
            }
            var compat = compatRequiredTable;
            if ((uint)id < (uint)compat.Length && compat[id] is { } knownSentinel)
            {
                if (!isRoot)
                {
                    construction.FoundLegacyFormatter = true;
                }
                return (IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>)knownSentinel;
            }

            // 2. check for a recursive reference (created but not yet initialized)
            if (!isRoot && construction.ConstructingFormatters.TryGetValue(id, out var cycle))
            {
                return (IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>)cycle;
            }

            try
            {
                // 3. create and initialize
                object result;
#if NET9_0_OR_GREATER
                var created = factory.CreateFormatter<TWriteBuffer, TReadBuffer>(typeof(T));
#else
                var created = factory.CreateFormatter(typeof(TWriteBuffer), typeof(TReadBuffer), typeof(T));
#endif
                if (created != null)
                {
                    // the factory's product is not statically typed, so validate it here
                    if (created is not IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter)
                    {
                        throw new InvalidOperationException($"The factory asked for type '{typeof(T).FullName}' created '{created.GetType().FullName}', which is not an IMessagePackFormatter for that type.");
                    }

                    construction.ConstructingFormatters[id] = formatter;
                    formatter.Initialize(this); // recurses into GetFormatter for nested types, so still constructing here
                    result = formatter;
                }
                else
                {
                    // a legacy-TFM formatter can return null here yet still be creatable over the Compatible pair,
                    // so check for that
                    var servableByCompatiblePair = CanCreateCompatiblePair(factory, typeof(TWriteBuffer), typeof(TReadBuffer), typeof(T));
                    if (servableByCompatiblePair && !ThrowOnLegacyFormatter)
                    {
                        construction.FoundLegacyFormatter = true;
                    }

                    result = new MissingMessagePackFormatter<TWriteBuffer, TReadBuffer, T>(factory.GetType(), servableByCompatiblePair);
                    construction.ConstructingFormatters[id] = result;
                }

                // 4. once control returns to the root, register everything
                if (isRoot)
                {
                    if (construction.FoundLegacyFormatter)
                    {
                        // the object graph contains a legacy-TFM formatter, so discard it all and mark only the root
                        // with CompatiblePairRequired
                        var sentinel = new CompatiblePairRequiredFormatter<TWriteBuffer, TReadBuffer, T>(factory.GetType());
                        RegisterInto(ref compatRequiredTable, id, sentinel);
                        construction.Clear();
                        CompatibilityFallback?.Invoke(typeof(T));
                        return sentinel;
                    }

                    // register every type in the graph
                    foreach (var kv in construction.ConstructingFormatters)
                    {
                        RegisterFormatter(kv.Key, kv.Value);
                    }
                    construction.Clear();
                }

                return (IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>)result;
            }
            catch when (isRoot)
            {
                construction.Clear();
                throw;
            }
        }
    }

    /// <summary>
    /// Creates, and initializes, the formatter a member-level <c>[MessagePackFormatter]</c> factory provides for
    /// <typeparamref name="T"/>. Called from a formatter's <c>Initialize</c> (the generated and the reflection
    /// formatters do), it treats the factory like one of the chain: a factory that serves the type only over the
    /// Compatible (non-ref-struct) buffers, one compiled against a netstandard build of MessagePack, makes the whole
    /// object graph under construction reroute to those buffers, as a chain factory's formatter would, unless
    /// <see cref="ThrowOnLegacyFormatter"/> turns that into an error.
    /// </summary>
    public IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> CreateFormatterWith<TWriteBuffer, TReadBuffer, T>(MessagePackFormatterFactory factory)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        ArgumentNullException.ThrowIfNull(factory);
#if NET9_0_OR_GREATER
        var created = factory.CreateFormatter<TWriteBuffer, TReadBuffer>(typeof(T));
#else
        var created = factory.CreateFormatter(typeof(TWriteBuffer), typeof(TReadBuffer), typeof(T));
#endif
        if (created is not null)
        {
            if (created is not IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter)
            {
                throw new InvalidOperationException($"The [MessagePackFormatter] factory '{factory.GetType().FullName}' asked for type '{typeof(T).FullName}' created '{created.GetType().FullName}', which is not an IMessagePackFormatter for that type.");
            }
            formatter.Initialize(this);
            return formatter;
        }

        if (CanCreateCompatiblePair(factory, typeof(TWriteBuffer), typeof(TReadBuffer), typeof(T)))
        {
            if (ThrowOnLegacyFormatter)
            {
                throw new MessagePackSerializationException($"The [MessagePackFormatter] factory '{factory.GetType().FullName}' can create a formatter for '{typeof(T).FullName}' only over the Compatible (non-ref-struct) buffer pair (requested TWriteBuffer: {typeof(TWriteBuffer).FullName}, TReadBuffer: {typeof(TReadBuffer).FullName}): it is likely provided by a library compiled against a netstandard build of MessagePack, and ThrowOnLegacyFormatter is enabled. Multi-target that library with net9.0 for full speed, or turn ThrowOnLegacyFormatter off to run the object graph over the compatibility path.");
            }
            lock (gate)
            {
                if (construction.IsConstructing)
                {
                    // the graph is discarded when control returns to the root, which is then marked CompatiblePairRequired
                    // and re-resolved over the Compatible pair, where this same Initialize runs again and the factory's
                    // Type-based member serves it; this placeholder is never used
                    construction.FoundLegacyFormatter = true;
                    return new CompatiblePairRequiredFormatter<TWriteBuffer, TReadBuffer, T>(factory.GetType());
                }
            }
            // outside a graph construction (Initialize called by hand) there is no root to reroute
            throw new MessagePackSerializationException($"The [MessagePackFormatter] factory '{factory.GetType().FullName}' can create a formatter for '{typeof(T).FullName}' only over the Compatible (non-ref-struct) buffer pair, and this formatter is being initialized outside the resolver, which cannot reroute it. Resolve the owning type through MessagePackFormatterResolver.GetFormatter instead.");
        }

        throw new MessagePackSerializationException($"The [MessagePackFormatter] factory '{factory.GetType().FullName}' did not create a formatter for '{typeof(T).FullName}'.");
    }

    // Asks a factory whether a type that could not be created for the requested (ref struct) pair can be created
    // over the Compatible pair. For example, a formatter from a netstandard2.0-built library cannot serve ref struct
    // pairs but can serve Compatible ones.
    static bool CanCreateCompatiblePair(MessagePackFormatterFactory factory, Type writeBufferType, Type readBufferType, Type valueType)
    {
#if NETSTANDARD2_0
        // ns2.0 has no Type.IsByRefLike, but it can never request a ref struct pair either, so there is nothing
        // to probe in the first place
        return false;
#else
        if (!writeBufferType.IsByRefLike && !readBufferType.IsByRefLike)
        {
            // the request itself was a Compatible pair, so the type is simply not registered
            return false;
        }

        try
        {
            // registration happens later, when GetFormatter runs over the Compatible pair; here we only check
            // whether creation would succeed
#pragma warning disable CA2263 // the non-generic overload is the compat tier that downlevel-built factories override; that tier is exactly what this probes
            var compatibleFormatter = factory.CreateFormatter(typeof(CompatibleArrayPoolListWriteBuffer), typeof(CompatibleReadOnlySpanReadBuffer), valueType);
#pragma warning restore CA2263
            return compatibleFormatter != null;
        }
        catch
        {
            return false;
        }
#endif
    }

    void RegisterFormatter(int id, object formatter)
    {
        RegisterInto(ref formatterTable, id, formatter);
    }

    static void RegisterInto(ref object?[] tableField, int id, object value)
    {
        var table = tableField;
        if (id >= table.Length)
        {
            var grown = new object?[Math.Max(table.Length * 2, id + 1)];
            Array.Copy(table, grown, table.Length);
            table = grown;
        }
        table[id] = value;
        Volatile.Write(ref tableField, table);
    }
}

// Non-generic marker base so the cold-path checks (`is CompatiblePairRequiredFormatter`) never need
// a generic-dictionary lookup under shared generics.
internal abstract class CompatiblePairRequiredFormatter
{
}

internal sealed partial class CompatiblePairRequiredFormatter<TWriteBuffer, TReadBuffer, T> : CompatiblePairRequiredFormatter, IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
{
    readonly string factoryType;

    public CompatiblePairRequiredFormatter(Type factoryType)
    {
        this.factoryType = factoryType.FullName ?? "";
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T value)
    {
        throw Error();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        throw Error();
    }

    InvalidOperationException Error()
    {
        return new InvalidOperationException(
            $"Type '{typeof(T).FullName}' in {factoryType} can only be served over the Compatible (non-ref-struct) buffer pair (requested TWriteBuffer: {typeof(TWriteBuffer).FullName}, TReadBuffer: {typeof(TReadBuffer).FullName}): its formatter comes from a library compiled against a netstandard build of MessagePack. Serialize through the MessagePackSerializer entry points (which reroute automatically), or resolve with the Compatible buffer types directly. Multi-target that library with net9.0 for full speed.");
    }
}

internal sealed partial class MissingMessagePackFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
{
    readonly string resolverType;
    readonly bool servableByCompatiblePair;

    public MissingMessagePackFormatter(Type resolverType, bool servableByCompatiblePair)
    {
        this.resolverType = resolverType.FullName ?? "";
        this.servableByCompatiblePair = servableByCompatiblePair;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T value)
    {
        throw new InvalidOperationException(BuildMessage());
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        throw new InvalidOperationException(BuildMessage());
    }

    string BuildMessage()
    {
        var message = $"Type '{typeof(T).FullName}' is not found in {resolverType} (TWriteBuffer: {typeof(TWriteBuffer).FullName}, TReadBuffer: {typeof(TReadBuffer).FullName}).";
        if (servableByCompatiblePair)
        {
            // only reachable when ThrowOnLegacyFormatter is enabled
            message += " The factory can create this formatter for the Compatible (non-ref-struct) buffer pair: it is likely provided by a library compiled against a netstandard build of MessagePack, which cannot serve ref struct buffer pairs. Multi-target that library with net9.0 for full speed, or serialize through the MessagePackSerializer entry points (with ThrowOnLegacyFormatter off) to run it over the compatibility path.";
        }
        else if (typeof(T).IsArray || typeof(T).IsConstructedGenericType)
        {
            // collection-shaped misses under a source-generated-only chain are usually roots the member-graph harvest
            // cannot see (Person[] passed straight to Serialize), so point at the declaration that closes them
            message += " If this type appears only as a serialization root under a source-generated-only chain (DefaultAot / Native AOT), declare it on a partial factory class with [MessagePackSerializable<" + typeof(T).Name + ">] so the source generator registers its formatter.";
        }
        return message;
    }
}
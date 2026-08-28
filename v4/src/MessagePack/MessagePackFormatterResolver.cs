using MessagePack.Formatters;

namespace MessagePack;

// Global per-instantiation id for the resolvers' formatter tables.
// The id is only the slot INDEX; the tables (and the formatters in them) are per-resolver instance state,
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
/// A resolver that caches the IMessagePackFormatter.
/// </summary>
public sealed class MessagePackFormatterResolver
{
    // Accumulates the in-progress construction while nested formatters resolve, then
    // registers the whole graph at once when control returns to the root.
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
    // A non-null slot holds an actual formatter created by the factory (including
    // MissingMessagePackFormatter, which throws at use), NEVER the compat sentinel:
    // that invariant lets the hot path get by with a null check alone, no type test.
    object?[] formatterTable = [];

    // CompatiblePairRequiredFormatter sentinels, same indexing as formatterTable.
    // Keeping them out of formatterTable moves the sentinel probe to the cold path;
    // both tables are copy-on-write and lock-free to read.
    object?[] compatRequiredTable = [];

    /// <summary>
    /// When true, formatters that build hash-based collections (Dictionary/HashSet/...)
    /// use an EqualityComparer resistant to hash-flooding attacks.
    /// </summary>
    public bool HashFloodingResistant { get; }

    /// <summary>
    /// When true, if the graph contains a legacy formatter that does not allow ref struct
    /// buffers, it is NOT routed to the compatible buffers; serialization throws instead.
    /// </summary>
    public bool ThrowOnLegacyFormatter { get; }

    /// <summary>
    /// When true (the default), deserialization fails when the payload does not carry a value
    /// for a required member: one declared with the C# <c>required</c> modifier, or one fed
    /// through a deserialization-constructor parameter that has no default value.
    /// Pass <c>validateRequiredMembers: false</c> to accept such payloads (the member keeps its default),
    /// e.g. when reading data written by an older schema.
    /// </summary>
    public bool ValidateRequiredMembers { get; }

    /// <summary>
    /// When true, deserialization fails when the payload assigns nil to a member whose declared
    /// type is a non-nullable reference type. Off by default; enable it together with required
    /// member validation when deserializing untrusted input.
    /// The check covers directly declared member types only: generic type parameters and
    /// collection elements are not validated (their nullability is erased at runtime).
    /// </summary>
    public bool ValidateNullableAnnotations { get; }

    /// <summary>
    /// Raised when a legacy formatter that does not allow ref struct buffers was involved
    /// and serialization fell back to the compatible buffers.
    /// </summary>
    public event Action<Type>? CompatibilityFallback;

    public MessagePackFormatterResolver(MessagePackFormatterFactory factory, bool hashFloodingResistant = true, bool throwOnLegacyFormatter = false, bool validateRequiredMembers = true, bool validateNullableAnnotations = false)
    {
        this.factory = factory;
        this.HashFloodingResistant = hashFloodingResistant;
        this.ThrowOnLegacyFormatter = throwOnLegacyFormatter;
        this.ValidateRequiredMembers = validateRequiredMembers;
        this.ValidateNullableAnnotations = validateNullableAnnotations;
    }

    /// <summary>
    /// Composes the factories into one chain; first non-null wins, so put overrides before defaults.
    /// </summary>
    public MessagePackFormatterResolver(MessagePackFormatterFactory[] factories, bool hashFloodingResistant = true, bool throwOnLegacyFormatter = false, bool validateRequiredMembers = true, bool validateNullableAnnotations = false)
        : this(MessagePackFormatterFactory.Combine(factories), hashFloodingResistant, throwOnLegacyFormatter, validateRequiredMembers, validateNullableAnnotations)
    {
    }

#if NET9_0_OR_GREATER

    /// <summary>
    /// Top-level acquisition for the serializer entry points.
    /// </summary>
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
        // On net10, a formatter provided by a netstandard2.0-built library only works
        // over the Compatible buffers; returning false tells the caller to re-acquire
        // via GetFormatter over the Compatible buffer pair. The sentinel ledger keeps
        // that per-call detection lock-free.
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
            // slot id is unique to this instantiation, so the stored object is always an
            // IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>; the compatible-pair
            // sentinel lives in compatRequiredTable and is found on the cold path below.
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

            // 1. double-check: another thread may have registered it while we were
            //    entering the lock
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
                    // a legacy-TFM formatter can return null here yet still be creatable
                    // over the Compatible pair — check for that
                    var servableByCompatiblePair = CanCreateCompatiblePair(typeof(TWriteBuffer), typeof(TReadBuffer), typeof(T));
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
                        // the object graph contains a legacy-TFM formatter: discard it all
                        // and mark only the root with CompatiblePairRequired
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

    // Asks the factory whether a type that could not be created for the requested
    // (ref struct) pair can be created over the Compatible pair — e.g. a formatter from a
    // netstandard2.0-built library cannot serve ref struct pairs but can serve Compatible ones.
    bool CanCreateCompatiblePair(Type writeBufferType, Type readBufferType, Type valueType)
    {
#if NETSTANDARD2_0
        // ns2.0 has no Type.IsByRefLike — but it can never request a ref struct pair
        // either, so there is nothing to probe in the first place
        return false;
#else
        if (!writeBufferType.IsByRefLike && !readBufferType.IsByRefLike)
        {
            // the request itself was a Compatible pair: the type is simply not registered
            return false;
        }

        try
        {
            // registration happens later, when GetFormatter runs over the Compatible
            // pair; here we only check whether creation would succeed
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

// Non-generic marker base so the cold-path checks (`is CompatiblePairRequiredFormatter`)
// never need a generic-dictionary lookup under shared generics.
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
            $"Type '{typeof(T).FullName}' in {factoryType} can only be served over the Compatible (non-ref-struct) buffer pair (requested TWriteBuffer: {typeof(TWriteBuffer).FullName}, TReadBuffer: {typeof(TReadBuffer).FullName}): its formatter comes from a library compiled against a netstandard build of MessagePack. Serialize through the MessagePackSerializer entry points (which reroute automatically), or resolve with the Compatible buffer types directly. Multi-target that library with net10.0 for full speed.");
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
            message += " The factory can create this formatter for the Compatible (non-ref-struct) buffer pair: it is likely provided by a library compiled against a netstandard build of MessagePack, which cannot serve ref struct buffer pairs. Multi-target that library with net10.0 for full speed, or serialize through the MessagePackSerializer entry points (with ThrowOnLegacyFormatter off) to run it over the compatibility path.";
        }
        else if (typeof(T).IsArray || typeof(T).IsConstructedGenericType)
        {
            // collection-shaped misses under a source-generated-only chain are usually
            // ROOTS the member-graph harvest cannot see (Person[] passed straight to
            // Serialize); point at the declaration that closes them
            message += " If this type appears only as a serialization root under a source-generated-only chain (DefaultAot / Native AOT), declare it on a partial factory class with [MessagePackSerializable<" + typeof(T).Name + ">] so the source generator registers its formatter.";
        }
        return message;
    }
}
using System.ComponentModel;

namespace MessagePack;

/// <summary>
/// Converts values of type <typeparamref name="T"/> to and from MessagePack.
/// Instances are created by a <see cref="MessagePackFormatterResolver"/>, which calls <see cref="Initialize"/> once before sharing them.
/// </summary>
/// <typeparam name="TWriteBuffer">Buffer type the formatter writes to.</typeparam>
/// <typeparam name="TReadBuffer">Buffer type the formatter reads from.</typeparam>
/// <typeparam name="T">Type this formatter handles.</typeparam>
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
    /// <summary>Called once by the resolver after construction. Acquire the formatters of nested types from <paramref name="resolver"/> here.</summary>
    void Initialize(MessagePackFormatterResolver resolver);

    /// <summary>
    /// Writes <paramref name="value"/> to <paramref name="buffer"/>.
    /// Surround every call into another formatter with <see cref="SerializeState.Enter"/> and <see cref="SerializeState.Exit"/>.
    /// </summary>
    void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T value);

    /// <summary>
    /// Reads a value from <paramref name="buffer"/> into <paramref name="value"/>.
    /// An existing instance may be populated in place, otherwise a new one is assigned.
    /// Surround every call into another formatter with <see cref="DeserializeState.Enter"/> and <see cref="DeserializeState.Exit"/>.
    /// </summary>
    void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value);
}

/// <summary>
/// State carried through every formatter during one serialization.
/// Tracks nesting depth against <see cref="MessagePackSerializerOptions.MaxDepth"/>.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct SerializeState
{
    // Depth budget counting down to 0. Charged (Enter/Exit) by every formatter that descends into another formatter,
    // never by flat shapes; DepthTrackingAnalyzer enforces the pairing.
    int remainingDepth;

    // Circular-reference identity table ([MessagePackObject(AllowCircularReferences = true)]).
    // Only the generated formatters of annotated types call TrackCircularReference, so untracked graphs never allocate it.
    Dictionary<object, uint>? circularReferences;

    /// <summary>Creates state with the given depth limit. Zero means unlimited.</summary>
    public SerializeState(int maxDepth)
    {
        this.remainingDepth = maxDepth == 0 ? 0 : maxDepth + 1; // 0 is unlimited
    }

    /// <summary>Enters one nesting level before calling another formatter. Throws when the depth limit is exceeded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enter()
    {
        if (--remainingDepth == 0)
        {
            MessagePackSerializationException.ThrowSerializeDepthExceeded();
        }
    }

    /// <summary>Leaves the level entered by <see cref="Enter"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Exit() => remainingDepth++;

    /// <summary>
    /// Records <paramref name="value"/> for circular-reference tracking.
    /// Returns true when it was already written in this operation, with its id in <paramref name="referenceId"/>.
    /// Otherwise assigns a new id and returns false.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TrackCircularReference(object value, out uint referenceId)
    {
        var map = circularReferences ??= new Dictionary<object, uint>(ReferenceEqualityComparer.Instance);
        if (map.TryGetValue(value, out referenceId))
        {
            return true;
        }
        referenceId = (uint)map.Count;
        map.Add(value, referenceId);
        return false;
    }
}

/// <summary>
/// State carried through every formatter during one deserialization.
/// Tracks nesting depth against <see cref="MessagePackSerializerOptions.MaxDepth"/>.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct DeserializeState
{
    // Same countdown scheme as SerializeState.
    int remainingDepth;

    // Declared-element budget, counting down from the message length.
    // Every array element, map key and map value is a distinct msgpack value that occupies at least one byte of
    // the message, and no two of them share a byte, so the sum of the counts declared by every array/map header
    // in the message can never exceed the message length. ReadArrayHeader/ReadMapHeader(ref state) charge each
    // header's count here, so the first header that pushes the sum past the length is provably a lie and throws
    // before anyone preallocates from it.
    // The per-header guard in ReadBufferExtensions (count <= BytesRemaining) cannot see this on its own:
    // nested headers all measure the same tail of the message, so 500 headers can each claim the whole tail
    // and pass, while their preallocations sum to 500 times the message.
    long remainingDeclaredElements;

    /// <summary>
    /// Creates state with the given depth limit and no declared-element budget. Zero means unlimited.
    /// Prefer <see cref="DeserializeState(int, long)"/> when the length of the message is known.
    /// </summary>
    public DeserializeState(int maxDepth)
        : this(maxDepth, long.MaxValue)
    {
    }

    /// <summary>
    /// Creates state with the given depth limit (zero means unlimited) and a declared-element budget of
    /// <paramref name="messageLength"/>, the number of bytes the message occupies.
    /// <see cref="ReadBufferExtensions.ReadArrayHeader{TReadBuffer}(ref TReadBuffer, ref DeserializeState)"/> and
    /// <see cref="ReadBufferExtensions.ReadMapHeader{TReadBuffer}(ref TReadBuffer, ref DeserializeState)"/> charge every
    /// declared element against it, so a message whose headers claim more elements than it has bytes is rejected
    /// before the formatters allocate for the claim.
    /// </summary>
    public DeserializeState(int maxDepth, long messageLength)
    {
        this.remainingDepth = maxDepth == 0 ? 0 : maxDepth + 1; // 0 is unlimited
        this.remainingDeclaredElements = messageLength;
    }

    /// <summary>
    /// Charges <paramref name="charge"/> declared elements (a map charges two per entry) against the budget.
    /// Throws when the message has declared more elements than it has bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ChargeDeclaredElements(string kind, int count, long charge)
    {
        var remaining = remainingDeclaredElements - charge;
        if (remaining < 0)
        {
            MessagePackSerializationException.ThrowDeclaredElementsExceedMessage(kind, count, remainingDeclaredElements);
        }
        remainingDeclaredElements = remaining;
    }

    /// <summary>Enters one nesting level before calling another formatter. Throws when the depth limit is exceeded.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enter()
    {
        if (--remainingDepth == 0)
        {
            MessagePackSerializationException.ThrowDeserializeDepthExceeded();
        }
    }

    /// <summary>Leaves the level entered by <see cref="Enter"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Exit() => remainingDepth++;

    // Read-side circular-reference table from id to instance.
    // A Dictionary rather than a List on purpose. A definition skipped by version-tolerant reading leaves a hole,
    // which stays harmless until a back-reference actually targets it (loud unknown-id failure),
    // and payload-supplied ids can never force a sparse allocation.
    // The ids are payload-chosen, so the table takes the same hash-flooding-resistant comparer as every deserialized integer-keyed dictionary.
    Dictionary<uint, object>? circularReferences;

    /// <summary>
    /// Registers the instance created for a definition under its id.
    /// Call it before populating the members, so that back-references inside them resolve to this instance.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RegisterCircularReference(uint referenceId, object value)
    {
        var map = circularReferences ??= new Dictionary<uint, object>(HashFloodingResistantEqualityComparer.Get<uint>());
        if (!map.TryAdd(referenceId, value))
        {
            MessagePackSerializationException.ThrowDuplicateCircularReferenceId(referenceId);
        }
    }

    /// <summary>Returns the instance registered under a back-reference id. Unknown ids and instances of another type throw.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public T ResolveCircularReference<T>(uint referenceId)
        where T : class
    {
        if (circularReferences is { } map && map.TryGetValue(referenceId, out var value))
        {
            return value as T ?? throw MessagePackSerializationException.ThrowCircularReferenceTypeMismatch(referenceId, value.GetType(), typeof(T));
        }
        throw MessagePackSerializationException.ThrowUnknownCircularReferenceId(referenceId);
    }
}
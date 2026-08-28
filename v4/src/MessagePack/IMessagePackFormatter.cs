using SerializerFoundation;
using System.ComponentModel;

namespace MessagePack;

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

[StructLayout(LayoutKind.Auto)]
public struct SerializeState
{
    // Depth budget counting down to 0
    int remainingDepth;

    // Circular-reference identity table ([MessagePackObject(AllowCircularReferences = true)])
    // only the generated formatters of annotated types call TrackCircularReference, so untracked graphs never allocate it.
    Dictionary<object, uint>? circularReferences;

    public SerializeState(int maxDepth)
    {
        this.remainingDepth = maxDepth == 0 ? 0 : maxDepth + 1; // 0 is unlimited
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enter()
    {
        if (--remainingDepth == 0)
        {
            MessagePackSerializationException.ThrowSerializeDepthExceeded();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Exit() => remainingDepth++;

    /// <summary>
    /// True: <paramref name="value"/> was already written in this operation — write a
    /// back-reference carrying <paramref name="referenceId"/> instead of the object.
    /// False: first encounter — <paramref name="referenceId"/> was newly assigned; write
    /// the definition envelope [id, body].
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

[StructLayout(LayoutKind.Auto)]
public struct DeserializeState
{
    // Same countdown scheme as SerializeState.
    int remainingDepth;

    public DeserializeState(int maxDepth)
    {
        this.remainingDepth = maxDepth == 0 ? 0 : maxDepth + 1; // 0 is unlimited
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enter()
    {
        if (--remainingDepth == 0)
        {
            MessagePackSerializationException.ThrowDeserializeDepthExceeded();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Exit() => remainingDepth++;

    // Read-side circular-reference table: id -> instance.
    // A Dictionary rather than a List on purpose: a definition skipped by version-tolerant reading leaves a hole,
    // which stays harmless until a back-reference actually targets it (loud unknown-id failure),
    // and payload-supplied ids can never force a sparse allocation.
    Dictionary<uint, object>? circularReferences;

    /// <summary>
    /// Registers a definition's instance under its wire id. Call BEFORE populating the
    /// instance's members: that is what lets back-references inside those members (a cycle) resolve to the in-progress instance.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void RegisterCircularReference(uint referenceId, object value)
    {
        var map = circularReferences ??= new Dictionary<uint, object>();
        if (!map.TryAdd(referenceId, value))
        {
            MessagePackSerializationException.ThrowDuplicateCircularReferenceId(referenceId);
        }
    }

    /// <summary>Resolves a back-reference id to its registered instance; unknown ids and instances of an unexpected type throw.</summary>
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
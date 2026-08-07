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

// Object-graph guarding = depth limiting only (MessagePackSerializerOptions.MaxDepth).
// True cycle DETECTION (reference tracking) is deliberately not implemented: it would
// cost a hash lookup per object on the hot path; a cyclic graph instead runs into the
// depth limit and surfaces as a clean MessagePackSerializationException rather than a
// process-killing StackOverflowException.
//
// Container formatters (collections, object formatters — anything that recurses into
// nested formatters) call Enter() after their null/nil handling and
// Exit() on the way out. Leaf formatters skip both. No try/finally: a throw
// abandons the whole (de)serialization and the state with it.

[StructLayout(LayoutKind.Auto)]
public struct SerializeState
{
    // Depth budget counting down to 0. A raw-constructed state starts at 0: the first
    // Enter takes it negative and it can never reach 0 again (nesting is bounded by the
    // call stack), which is exactly the documented "unlimited" behavior.
    int remainingDepth;
    readonly int maxDepth; // only for the exception message

    /// <summary>A raw-constructed state (maxDepth 0, e.g. direct formatter tests) is unlimited.</summary>
    public SerializeState(int maxDepth)
    {
        this.maxDepth = maxDepth;
        this.remainingDepth = maxDepth == 0 ? 0 : maxDepth + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enter()
    {
        if (--remainingDepth == 0)
        {
            MessagePackSerializationException.ThrowSerializeDepthExceeded(maxDepth);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Exit() => remainingDepth++;
}

[StructLayout(LayoutKind.Auto)]
public struct DeserializeState
{
    // Same countdown scheme as SerializeState.
    int remainingDepth;
    readonly int maxDepth; // only for the exception message

    /// <summary>A raw-constructed state (maxDepth 0, e.g. direct formatter tests) is unlimited.</summary>
    public DeserializeState(int maxDepth)
    {
        this.maxDepth = maxDepth;
        this.remainingDepth = maxDepth == 0 ? 0 : maxDepth + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enter()
    {
        if (--remainingDepth == 0)
        {
            MessagePackSerializationException.ThrowDeserializeDepthExceeded(maxDepth);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Exit() => remainingDepth++;
}
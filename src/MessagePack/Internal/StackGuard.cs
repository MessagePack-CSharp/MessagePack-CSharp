namespace MessagePack;

/// <summary>
/// The stack half of the depth guard. MaxDepth counts levels, but a level's frames differ by an order of magnitude:
/// object / object[] / Typeless on .NET Framework x64 take about 2.4 KB per level, a wide self-recursive generated type
/// 1.6-2.7 KB, so a payload well within MaxDepth 500 overflowed a 512 KB or 1 MB thread (an uncatchable crash).
/// SerializeState and DeserializeState.Enter ask the runtime for stack room every 16th level: the runtime guarantees
/// 128 KB (64-bit) past a successful probe, which covers the 16 unprobed levels at the heaviest shape measured.
/// </summary>
static class StackGuard
{
    public const int ProbeMask = 15;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Probe()
    {
        if (!HasSufficientStack())
        {
            MessagePackSerializationException.ThrowInsufficientStack();
        }
    }

    public static bool HasSufficientStack()
    {
#if NETSTANDARD2_0
        // only the throwing form exists (an InsufficientExecutionStackException is an ordinary, catchable exception)
        try
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            return true;
        }
        catch (InsufficientExecutionStackException)
        {
            return false;
        }
#else
        return RuntimeHelpers.TryEnsureSufficientExecutionStack();
#endif
    }
}

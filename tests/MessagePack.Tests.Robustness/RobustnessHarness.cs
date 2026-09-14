using MessagePack;

namespace MessagePack.Tests.Robustness;

// The deserializer's security contract: feeding ANY byte sequence to Deserialize must
// terminate, and must fail (if it fails) ONLY through the sanctioned exceptions below.
// A raw IndexOutOfRange/NullReference/AccessViolation, an OutOfMemory from an
// unchecked length, a StackOverflow from unbounded recursion, or a hang are all contract
// breaks - the guards (allocation-bomb count-vs-BytesRemaining, depth-iterative skip,
// implausible-header) exist to convert every such input into MessagePackSerializationException.
// This harness runs one attempt and classifies the outcome; the callers (RobustnessTests) drive
// it with mutation strategies under a time budget. Public because it is compile-linked into
// MessagePack.Tests.Fuzz.Target: the SharpFuzz harness reuses IsSanctioned as its crash oracle
// (RunAttempt's thread-per-attempt timeout is NOT used there - libFuzzer's own -timeout/-rss_limit_mb
// cover hangs and OOM without the per-exec thread cost).
public static class RobustnessHarness
{
    // exceptions a malformed payload is ALLOWED to produce
    public static bool IsSanctioned(Exception ex) => ex switch
    {
        MessagePackSerializationException => true,
        // typeless/contractless type-resolution and populate-shape rejections surface as these
        InvalidOperationException => true,
        // async framing / truncated stream boundary
        EndOfStreamException => true,
        // Deserialize(Type,...) argument validation for API misuse is not reachable here
        // (we always pass well-formed generic args), so ArgumentException is NOT sanctioned
        _ => false,
    };

    // Runs one deserialize attempt on a background thread with a hard wall-clock deadline,
    // so a hypothetical infinite loop in a guard is caught as a contract break rather than
    // hanging the whole run. Returns a description of the violation, or null when the
    // attempt is contract-conformant (clean success OR a sanctioned throw).
    public static string? RunAttempt(string label, Action attempt, int timeoutMs = 5000)
    {
        Exception? thrown = null;
        var thread = new Thread(() =>
        {
            try
            {
                attempt();
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
        })
        {
            IsBackground = true,
            Name = "robustness-attempt",
        };
        thread.Start();
        if (!thread.Join(timeoutMs))
        {
            // cannot safely abort a managed thread on modern .NET; it is background, so it
            // dies with the process. Report the hang - the input bytes are in `label`.
            return $"TIMEOUT (>{timeoutMs}ms, possible infinite loop) on {label}";
        }
        if (thrown != null && !IsSanctioned(thrown))
        {
            return $"UNSANCTIONED {thrown.GetType().Name}: {thrown.Message} on {label}";
        }
        return null;
    }

    public static string Hex(ReadOnlySpan<byte> bytes)
    {
        const int cap = 64;
        var shown = bytes.Length <= cap ? bytes : bytes[..cap];
        var suffix = bytes.Length <= cap ? "" : $"...(+{bytes.Length - cap}B)";
        return $"[{Convert.ToHexString(shown)}{suffix}] (len={bytes.Length})";
    }
}

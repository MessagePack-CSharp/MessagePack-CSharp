using BenchmarkDotNet.Attributes;
using System.Text;
using System.Text.Unicode;

// Read-side mirror of the write round's Utf8.FromUtf16-vs-GetBytes finding: the current
// TryReadString materializes via Encoding.UTF8.GetString, which walks the payload TWICE
// (GetCharCount, then GetChars decoding straight into the allocated string). The
// candidate walks ONCE with Utf8.ToUtf16 into a stackalloc'd char window (utf8
// byteCount >= charCount, so byteCount chars always suffice) and pays a char memcpy in
// the string ctor instead of the second walk. Prediction: wash on ASCII (the count walk
// is a vectorized scan ~ memcpy speed), candidate ahead on multibyte text where a walk
// costs real decode work.
//   Ascii8  - 1..8 ascii chars (property-value scale, alloc-dominated)
//   Ascii24 - 16..32 ascii chars
//   JP8     - 4..8 hiragana chars (12..24 utf8 bytes, all 3-byte sequences)
//
// MEASURED (Zen 5, ShortRun):
//   Ascii8:  Ultra 14.0 vs Stack 21.6 ns (candidate 1.55x SLOWER)
//   Ascii24: 28.7 vs 26.3 (noisy iteration, band)   JP8: 27.1 vs 29.1 (band)
// VERDICT: GetString stays. The write-side finding (Utf8.FromUtf16 beating GetBytes)
// does NOT mirror to reads: GetString's "two walks" land in a specialized
// count-then-widen-into-the-allocated-string path (no intermediate buffer, internal
// FastAllocateString) that the manual stackalloc+ToUtf16+ctor route cannot match —
// the candidate pays transcode machinery plus a second char copy and loses decisively
// at the dominant short-ASCII scale. Structure around the call is already converged:
// nil peek (1 cmp), fixstr header inline, exact header+payload tokenSize.
public class TryReadStringBenchmark
{
    const int Count = 10_000;

    [Params("Ascii8", "Ascii24", "JP8")]
    public string Class = "Ascii8";

    byte[] buffer = default!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        string Next()
        {
            int len = Class switch
            {
                "Ascii8" => rand.Next(1, 9),
                "Ascii24" => rand.Next(16, 33),
                _ => rand.Next(4, 9),
            };
            var chars = new char[len];
            for (int i = 0; i < len; i++)
            {
                chars[i] = Class == "JP8" ? (char)rand.Next(0x3042, 0x3094) : (char)rand.Next('a', 'z' + 1);
            }
            return new string(chars);
        }

        buffer = new byte[Count * 101 + 8];
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            offset += UltraMessagePack.MessagePackPrimitives.UnsafeWriteString(ref buffer[offset], Next());
        }

        // both implementations must agree on every message
        int o0 = 0, o1 = 0;
        for (int n = 0; n < Count; n++)
        {
            var r0 = UltraMessagePack.MessagePackPrimitives.TryReadString(buffer.AsSpan(o0), out var v0, out var c0);
            var r1 = TryReadStringStack(buffer.AsSpan(o1), out var v1, out var c1);
            if (r0 != UltraMessagePack.DecodeResult.Success || r1 != r0 || v0 != v1 || c0 != c1)
            {
                throw new InvalidOperationException($"verify failed at message {n}");
            }
            o0 += c0; o1 += c1;
        }
    }

    // candidate: single-pass transcode into a stack window, string ctor copies out
    static UltraMessagePack.DecodeResult TryReadStringStack(ReadOnlySpan<byte> source, out string? value, out int tokenSize)
    {
        value = null;
        if (!source.IsEmpty && source[0] == 0xc0) // nil
        {
            tokenSize = 1;
            return UltraMessagePack.DecodeResult.Success;
        }
        var r = UltraMessagePack.MessagePackPrimitives.TryReadStringHeader(source, out int byteCount, out int headerSize);
        if (r != UltraMessagePack.DecodeResult.Success)
        {
            tokenSize = headerSize;
            return r;
        }
        tokenSize = headerSize + byteCount;
        if (source.Length - headerSize < byteCount)
        {
            return UltraMessagePack.DecodeResult.InsufficientBuffer;
        }
        var payload = source.Slice(headerSize, byteCount);
        if (byteCount <= 256)
        {
            Span<char> chars = stackalloc char[byteCount]; // utf8 byteCount >= charCount
            Utf8.ToUtf16(payload, chars, out _, out int charsWritten, replaceInvalidSequences: true);
            value = new string(chars[..charsWritten]);
            return UltraMessagePack.DecodeResult.Success;
        }
        value = Encoding.UTF8.GetString(payload);
        return UltraMessagePack.DecodeResult.Success;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int Ultra()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (UltraMessagePack.MessagePackPrimitives.TryReadString(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                sum += v!.Length;
            }
            offset += consumed;
        }
        return sum;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int StackTranscode()
    {
        ReadOnlySpan<byte> buf = buffer;
        int sum = 0;
        int offset = 0;
        for (int n = 0; n < Count; n++)
        {
            if (TryReadStringStack(buf.Slice(offset), out var v, out var consumed) == UltraMessagePack.DecodeResult.Success)
            {
                sum += v!.Length;
            }
            offset += consumed;
        }
        return sum;
    }
}

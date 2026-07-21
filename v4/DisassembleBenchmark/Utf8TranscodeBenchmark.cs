using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using BenchmarkDotNet.Attributes;
using UltraMessagePack;

// Transcode-call shootout inside the WriteString shape: Encoding.UTF8.GetBytes vs
// Encoding.UTF8.TryGetBytes vs Utf8.FromUtf16. All three bottom out in
// Utf8Utility.TranscodeToUtf8, so this measures only wrapper/prologue overhead
// (virtual dispatch vs static call + OperationStatus/out-param bookkeeping),
// which can only matter on short strings. Everything but the encode call is
// identical to UltraMessagePack.MessagePackPrimitives.UnsafeWriteString.
public class Utf8TranscodeBenchmark
{
    string value = default!;
    byte[] buffer = default!;

    [Params(3, 8, 60, 1000)]
    public int Length;

    [Params("Ascii", "Japanese")]
    public string Content = "Ascii";

    [GlobalSetup]
    public void Setup()
    {
        value = Content == "Ascii" ? new string('x', Length) : new string('あ', Length);
        buffer = new byte[3 * Length + 5];
    }

    [Benchmark(Baseline = true)]
    public int GetBytes() => WriteStringGetBytes(ref MemoryMarshal.GetArrayDataReference(buffer), value);

    [Benchmark]
    public int TryGetBytes() => WriteStringTryGetBytes(ref MemoryMarshal.GetArrayDataReference(buffer), value);

    [Benchmark]
    public int FromUtf16() => WriteStringFromUtf16(ref MemoryMarshal.GetArrayDataReference(buffer), value);

    public static int WriteStringGetBytes(ref byte destination, string value)
    {
        int length = value.Length;
        int headerSize = length <= 31 ? 1 : length <= 255 ? 2 : length <= 65535 ? 3 : 5;
        int byteCount = Encoding.UTF8.GetBytes(value, MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination, headerSize), 3 * length));
        return FinishString(ref destination, headerSize, byteCount);
    }

    public static int WriteStringTryGetBytes(ref byte destination, string value)
    {
        int length = value.Length;
        int headerSize = length <= 31 ? 1 : length <= 255 ? 2 : length <= 65535 ? 3 : 5;
        Encoding.UTF8.TryGetBytes(value, MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination, headerSize), 3 * length), out int byteCount);
        return FinishString(ref destination, headerSize, byteCount);
    }

    public static int WriteStringFromUtf16(ref byte destination, string value)
    {
        int length = value.Length;
        int headerSize = length <= 31 ? 1 : length <= 255 ? 2 : length <= 65535 ? 3 : 5;
        Utf8.FromUtf16(value, MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination, headerSize), 3 * length), out _, out int byteCount);
        return FinishString(ref destination, headerSize, byteCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int FinishString(ref byte destination, int headerSize, int byteCount)
    {
        int actualHeaderSize = byteCount <= 31 ? 1 : byteCount <= 255 ? 2 : byteCount <= 65535 ? 3 : 5;
        if (actualHeaderSize != headerSize)
        {
            MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination, headerSize), byteCount)
                .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref destination, actualHeaderSize), byteCount));
        }
        switch (actualHeaderSize)
        {
            case 1:
                destination = (byte)(MessagePackCode.MinFixStr | byteCount);
                break;
            case 2:
                destination = MessagePackCode.Str8;
                Unsafe.Add(ref destination, 1) = (byte)byteCount;
                break;
            case 3:
                destination = MessagePackCode.Str16;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((ushort)byteCount));
                break;
            default:
                destination = MessagePackCode.Str32;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 1), BinaryPrimitives.ReverseEndianness((uint)byteCount));
                break;
        }
        return actualHeaderSize + byteCount;
    }
}

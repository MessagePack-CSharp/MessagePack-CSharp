using MessagePack; // attributes only ([MessagePackObject]/[Key]); never invoked at runtime
using System.Buffers;
using UltraMessagePack;
using Ultra = UltraMessagePack.MessagePackSerializer;

// Native AOT smoke test: the AOT-supported configuration is "explicit options over the
// source-generated factory + primitives". Everything below runs through that chain; the
// [RequiresDynamicCode] surface (options-less entries, MessagePackSerializerOptions.Default,
// GenericFormatterFactory) is deliberately never referenced, so `dotnet publish` must
// complete with zero IL warnings — treat any IL warning in the publish log as a failure.

var options = new UltraMessagePack.MessagePackSerializerOptions(
    UltraMessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
    PrimitiveFormatterFactory.Instance);

//options = UltraMessagePack.MessagePackSerializerOptions.Default;

int failures = 0;
void Check(bool condition, string name)
{
    if (!condition)
    {
        failures++;
        Console.WriteLine($"NG {name}");
    }
}

Console.WriteLine($"RuntimeFeature.IsDynamicCodeSupported = {System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}");

// ---- known-answer vectors (hand-computed msgpack, oracle-compat formats) ----

// [0x93, 1, uint16 300, fixstr "abc"]
{
    var bytes = Ultra.Serialize(new SmokeIntKeyPoco { Id = 1, Count = 300, Name = "abc" }, options);
    byte[] expected = [0x93, 0x01, 0xcd, 0x01, 0x2c, 0xa3, (byte)'a', (byte)'b', (byte)'c'];
    Check(bytes.AsSpan().SequenceEqual(expected), $"intkey known-answer: [{Convert.ToHexString(bytes)}]");
}

// fixmap2 {"id":1, "name":"ab"}
{
    var bytes = Ultra.Serialize(new SmokeStringKeyPoco { Id = 1, Name = "ab" }, options);
    byte[] expected = [0x82, 0xa2, (byte)'i', (byte)'d', 0x01, 0xa4, (byte)'n', (byte)'a', (byte)'m', (byte)'e', 0xa2, (byte)'a', (byte)'b'];
    Check(bytes.AsSpan().SequenceEqual(expected), $"stringkey known-answer: [{Convert.ToHexString(bytes)}]");
}

// ---- roundtrips across the member-type matrix ----

// int format-class boundaries through a generated formatter
foreach (var v in (int[])[0, 1, 127, 128, 255, 256, 65535, 65536, int.MaxValue, -1, -32, -33, -128, -129, -32768, -32769, int.MinValue])
{
    var back = Ultra.Deserialize<SmokeIntKeyPoco>(Ultra.Serialize(new SmokeIntKeyPoco { Id = v, Count = v, Name = null }, options), options)!;
    Check(back.Id == v && back.Count == v && back.Name == null, $"int boundary roundtrip {v}");
}

// nested poco + DateTime + byte[] + int[] + double/bool/long, including the >1KB-scratch
// string (pooled segment growth) and a Japanese string (safe WriteString path)
{
    var value = new SmokeNestedPoco
    {
        Inner = new SmokeIntKeyPoco { Id = 42, Count = -70000, Name = "山岡士郎" },
        Numbers = [1, 300, -70000, int.MaxValue, int.MinValue, 0],
        Stamp = new DateTime(2026, 7, 28, 1, 2, 3, 456, DateTimeKind.Utc),
        Blob = [0, 1, 2, 254, 255],
        Score = -1.5,
        Flag = true,
        Big = 5_000_000_000L,
        Long = new string('あ', 5000),
    };
    var bytes = Ultra.Serialize(value, options);
    var back = Ultra.Deserialize<SmokeNestedPoco>(bytes, options)!;
    Check(back.Inner!.Id == 42 && back.Inner.Count == -70000 && back.Inner.Name == "山岡士郎", "nested inner roundtrip");
    Check(back.Numbers.AsSpan().SequenceEqual(value.Numbers), "int[] member roundtrip");
    Check(back.Stamp.Ticks == value.Stamp.Ticks, "DateTime member roundtrip");
    Check(back.Blob.AsSpan().SequenceEqual(value.Blob), "byte[] member roundtrip");
    Check(back.Score == value.Score && back.Flag == value.Flag && back.Big == value.Big, "double/bool/long roundtrip");
    Check(back.Long == value.Long, "5000-char string roundtrip (pooled segments)");

    // null members
    var empty = Ultra.Deserialize<SmokeNestedPoco>(Ultra.Serialize(new SmokeNestedPoco(), options), options)!;
    Check(empty.Inner == null && empty.Numbers == null && empty.Blob == null && empty.Long == null, "null members roundtrip");

    // IBufferWriter entry produces identical bytes
    var writer = new ArrayBufferWriter<byte>();
    Ultra.Serialize(writer, value, options);
    Check(writer.WrittenSpan.SequenceEqual(bytes), "IBufferWriter entry byte-identical");

    // ReadOnlySequence entry: contiguous and split at every byte of the small poco payload
    var small = Ultra.Serialize(value.Inner, options);
    for (int splitAt = 0; splitAt <= small.Length; splitAt++)
    {
        var sequence = splitAt == small.Length
            ? new ReadOnlySequence<byte>(small)
            : SmokeSegment.CreateSplit(small, splitAt);
        var fromSeq = Ultra.Deserialize<SmokeIntKeyPoco>(sequence, options)!;
        if (fromSeq.Id != 42 || fromSeq.Count != -70000 || fromSeq.Name != "山岡士郎")
        {
            Check(false, $"sequence roundtrip splitAt={splitAt}");
            break;
        }
    }

    // populate overload reuses the instance
    var reusable = new SmokeIntKeyPoco();
    Ultra.Deserialize(ref reusable!, small, options);
    Check(reusable.Id == 42 && reusable.Name == "山岡士郎", "populate roundtrip");
}

// struct poco (value-type T through the generic entry — the classic AOT instantiation case)
{
    var back = Ultra.Deserialize<SmokeStructPoco>(Ultra.Serialize(new SmokeStructPoco { X = -129, Y = long.MaxValue }, options), options);
    Check(back.X == -129 && back.Y == long.MaxValue, "struct poco roundtrip");
}

// unregistered type must fail with the resolver's exception, not an AOT crash
try
{
    Ultra.Serialize(new List<int> { 1 }, options);
    Check(false, "List<int> should not resolve without GenericFormatterFactory");
}
catch (InvalidOperationException)
{
}

Console.WriteLine(failures == 0 ? "AOT smoke: all OK" : $"AOT smoke: {failures} FAILURES");
return failures == 0 ? 0 : 1;

[MessagePackObject]
public class SmokeIntKeyPoco
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public int Count { get; set; }
    [Key(2)] public string? Name { get; set; }
}

[MessagePackObject]
public class SmokeStringKeyPoco
{
    [Key("id")] public int Id { get; set; }
    [Key("name")] public string? Name { get; set; }
}

[MessagePackObject]
public class SmokeNestedPoco
{
    [Key(0)] public SmokeIntKeyPoco? Inner { get; set; }
    [Key(1)] public int[]? Numbers { get; set; }
    [Key(2)] public DateTime Stamp { get; set; }
    [Key(3)] public byte[]? Blob { get; set; }
    [Key(4)] public double Score { get; set; }
    [Key(5)] public bool Flag { get; set; }
    [Key(6)] public long Big { get; set; }
    [Key(7)] public string? Long { get; set; }
}

[MessagePackObject]
public struct SmokeStructPoco
{
    [Key(0)] public int X { get; set; }
    [Key(1)] public long Y { get; set; }
}

// two-segment ReadOnlySequence builder for the split-deserialize checks
sealed class SmokeSegment : ReadOnlySequenceSegment<byte>
{
    SmokeSegment(ReadOnlyMemory<byte> memory, SmokeSegment? next, long runningIndex)
    {
        Memory = memory;
        Next = next;
        RunningIndex = runningIndex;
    }

    public static ReadOnlySequence<byte> CreateSplit(byte[] data, int splitAt)
    {
        var second = new SmokeSegment(data.AsMemory(splitAt), null, splitAt);
        var first = new SmokeSegment(data.AsMemory(0, splitAt), second, 0);
        return new ReadOnlySequence<byte>(first, 0, second, data.Length - splitAt);
    }
}

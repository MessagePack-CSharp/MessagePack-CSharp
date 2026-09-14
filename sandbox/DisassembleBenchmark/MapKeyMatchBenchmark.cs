using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;

// Map-key string matching strategies for the source generator's string-key deserialization loop (ObjectEmitter.EmitReadLoop).
// The generated code today is a linear if/else SequenceEqual chain in member declaration order; the candidates explore embedded-automata shapes and a declaration-order hint.
//   LinearChain         - the current generated shape: 16 SequenceEqual tests in declaration order
//   ChunkFirstAutomata  - mpc-style embedded automata: compose up to 8 bytes into a ulong sized by the remaining length, switch on the chunk, descend into the next chunk.
//                         Leaves re-check the total length because short-chunk composition zero-pads (the shipped mpc form omits this and cannot tell "id" from "id\0"); the tail chunk pays a runtime width-compose.
//   LengthFirstAutomata - switch on key length first, then exact-width constant compares inside the bucket (nested ulong switch where several members share a length).
//                         Tails read overlapped or narrow at compile-time-constant widths, so there is no runtime composition, and lengths absent from the member set exit at the root switch with zero compares.
//   HintThenLinear      - declaration-order hint: same-writer payloads carry keys in declaration order, so try member[hint] with one SequenceEqual and fall back to the linear chain on a miss
//   HintThenLengthFirst - the same hint, falling back to LengthFirstAutomata
//
// Member table (idx, name, utf8 length):
//   0 id(2)  1 age(3)  2 name(4)  3 tags(4)  4 email(5)  5 score(5)  6 address(7)
//   7 isActive(8)  8 userName(8)  9 userMail(8)  10 createdAt(9)  11 updatedAt(9)
//   12 description(11)  13 phoneNumber(11)  14 countryCode(11)  15 profileImage(12)
// Length buckets: 4 and 5 hold two members, 8 holds three, 9 two, 11 three; 6/10/13 are member-free lengths the unknown-key pool exploits.
//
// Distributions (100k+ keys per CLAUDE.md's predictor pitfall; matching is branch-bound, so small repeated buffers would be memorized):
//   InOrder   - every object carries all 16 keys in declaration order: the same-writer payload the hint targets
//   Shuffled  - per-object random key order: foreign-writer / reordered worst case
//   Unknown25 - in-order plus ~24% unknown keys (5 random inserts per object, lengths both colliding and member-free): the version-tolerance cost
//
// Setup() is self-verifying: every candidate's idx sequence over the generated stream must equal the LinearChain oracle.
// VerifyCandidates() (wired into --verify) adds adversarial keys: NUL paddings, every truncation, last-byte mutations, extensions, and all hint states.
public class MapKeyMatchBenchmark
{
    [Params("InOrder", "Shuffled", "Unknown25")]
    public string Distribution = "InOrder";

    static readonly string[] MemberNames =
    [
        "id", "age", "name", "tags", "email", "score", "address", "isActive",
        "userName", "userMail", "createdAt", "updatedAt", "description", "phoneNumber", "countryCode", "profileImage",
    ];

    byte[] blob = null!;
    int[] offsets = null!;
    int[] lengths = null!;
    int[] objectKeyCounts = null!;
    long expectedSum;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        var memberUtf8 = MemberNames.Select(Encoding.UTF8.GetBytes).ToArray();
        const int ObjectCount = 6250; // x16 known keys = 100,000

        var objects = new List<byte[][]>(ObjectCount);
        for (int o = 0; o < ObjectCount; o++)
        {
            switch (Distribution)
            {
                case "InOrder":
                    objects.Add(memberUtf8);
                    break;
                case "Shuffled":
                    var shuffled = (byte[][])memberUtf8.Clone();
                    rand.Shuffle(shuffled);
                    objects.Add(shuffled);
                    break;
                case "Unknown25":
                    var withUnknown = new List<byte[]>(memberUtf8);
                    for (int u = 0; u < 5; u++)
                    {
                        withUnknown.Insert(rand.Next(withUnknown.Count + 1), MakeUnknownKey(rand));
                    }
                    objects.Add([.. withUnknown]);
                    break;
                default:
                    throw new InvalidOperationException(Distribution);
            }
        }

        var totalKeys = objects.Sum(static o => o.Length);
        var totalBytes = objects.Sum(static o => o.Sum(static k => k.Length));
        blob = new byte[totalBytes];
        offsets = new int[totalKeys];
        lengths = new int[totalKeys];
        objectKeyCounts = new int[objects.Count];
        int k = 0, pos = 0;
        for (int o = 0; o < objects.Count; o++)
        {
            objectKeyCounts[o] = objects[o].Length;
            foreach (var key in objects[o])
            {
                key.CopyTo(blob, pos);
                offsets[k] = pos;
                lengths[k] = key.Length;
                pos += key.Length;
                k++;
            }
        }

        expectedSum = LinearChain();
        if (ChunkFirstAutomata() != expectedSum || LengthFirstAutomata() != expectedSum
            || HintThenLinear() != expectedSum || HintThenLengthFirst() != expectedSum)
        {
            throw new InvalidOperationException($"candidate sum mismatch on {Distribution}");
        }
    }

    static byte[] MakeUnknownKey(Random rand)
    {
        // lengths overlap the member buckets (3/4/5/8/9/11) and hit member-free ones (6/10/13) so both rejection paths get exercised
        int[] lens = [3, 4, 5, 6, 8, 9, 10, 11, 13];
        while (true)
        {
            var b = new byte[lens[rand.Next(lens.Length)]];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = (byte)('a' + rand.Next(26));
            }
            if (Array.IndexOf(MemberNames, Encoding.ASCII.GetString(b)) < 0)
            {
                return b;
            }
        }
    }

    // ---- candidates -------------------------------------------------------

    [Benchmark(Baseline = true)]
    public long LinearChain()
    {
        long sum = 0;
        int k = 0;
        for (int o = 0; o < objectKeyCounts.Length; o++)
        {
            int count = objectKeyCounts[o];
            for (int i = 0; i < count; i++, k++)
            {
                sum += MatchLinear(blob.AsSpan(offsets[k], lengths[k]));
            }
        }
        return sum;
    }

    [Benchmark]
    public long ChunkFirstAutomata()
    {
        long sum = 0;
        int k = 0;
        for (int o = 0; o < objectKeyCounts.Length; o++)
        {
            int count = objectKeyCounts[o];
            for (int i = 0; i < count; i++, k++)
            {
                sum += MatchChunkFirst(blob.AsSpan(offsets[k], lengths[k]));
            }
        }
        return sum;
    }

    [Benchmark]
    public long LengthFirstAutomata()
    {
        long sum = 0;
        int k = 0;
        for (int o = 0; o < objectKeyCounts.Length; o++)
        {
            int count = objectKeyCounts[o];
            for (int i = 0; i < count; i++, k++)
            {
                sum += MatchLengthFirst(blob.AsSpan(offsets[k], lengths[k]));
            }
        }
        return sum;
    }

    [Benchmark]
    public long HintThenLinear()
    {
        long sum = 0;
        int k = 0;
        for (int o = 0; o < objectKeyCounts.Length; o++)
        {
            int count = objectKeyCounts[o];
            int hint = 0;
            for (int i = 0; i < count; i++, k++)
            {
                var key = blob.AsSpan(offsets[k], lengths[k]);
                int idx = MatchHint(key, hint);
                if (idx < 0)
                {
                    idx = MatchLinear(key);
                }
                if (idx >= 0)
                {
                    hint = idx + 1;
                }
                sum += idx;
            }
        }
        return sum;
    }

    [Benchmark]
    public long HintThenLengthFirst()
    {
        long sum = 0;
        int k = 0;
        for (int o = 0; o < objectKeyCounts.Length; o++)
        {
            int count = objectKeyCounts[o];
            int hint = 0;
            for (int i = 0; i < count; i++, k++)
            {
                var key = blob.AsSpan(offsets[k], lengths[k]);
                int idx = MatchHint(key, hint);
                if (idx < 0)
                {
                    idx = MatchLengthFirst(key);
                }
                if (idx >= 0)
                {
                    hint = idx + 1;
                }
                sum += idx;
            }
        }
        return sum;
    }

    // ---- matchers ---------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int MatchLinear(ReadOnlySpan<byte> key)
    {
        if (key.SequenceEqual("id"u8)) return 0;
        if (key.SequenceEqual("age"u8)) return 1;
        if (key.SequenceEqual("name"u8)) return 2;
        if (key.SequenceEqual("tags"u8)) return 3;
        if (key.SequenceEqual("email"u8)) return 4;
        if (key.SequenceEqual("score"u8)) return 5;
        if (key.SequenceEqual("address"u8)) return 6;
        if (key.SequenceEqual("isActive"u8)) return 7;
        if (key.SequenceEqual("userName"u8)) return 8;
        if (key.SequenceEqual("userMail"u8)) return 9;
        if (key.SequenceEqual("createdAt"u8)) return 10;
        if (key.SequenceEqual("updatedAt"u8)) return 11;
        if (key.SequenceEqual("description"u8)) return 12;
        if (key.SequenceEqual("phoneNumber"u8)) return 13;
        if (key.SequenceEqual("countryCode"u8)) return 14;
        if (key.SequenceEqual("profileImage"u8)) return 15;
        return -1;
    }

    // mpc's GetKey: a full 8-byte chunk when possible, otherwise a width-cascade compose of the remainder (the part LengthFirst avoids by pinning the length first)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ulong FetchChunk(ReadOnlySpan<byte> key, int offset)
    {
        int rest = key.Length - offset;
        if (rest >= 8)
        {
            return BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(offset));
        }
        ulong v = 0;
        int shift = 0;
        if ((rest & 4) != 0)
        {
            v = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(offset));
            offset += 4;
            shift = 32;
        }
        if ((rest & 2) != 0)
        {
            v |= (ulong)BinaryPrimitives.ReadUInt16LittleEndian(key.Slice(offset)) << shift;
            offset += 2;
            shift += 16;
        }
        if ((rest & 1) != 0)
        {
            v |= (ulong)key[offset] << shift;
        }
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int MatchChunkFirst(ReadOnlySpan<byte> key)
    {
        switch (FetchChunk(key, 0))
        {
            case 0x6469UL: return key.Length == 2 ? 0 : -1; // "id"
            case 0x656761UL: return key.Length == 3 ? 1 : -1; // "age"
            case 0x656d616eUL: return key.Length == 4 ? 2 : -1; // "name"
            case 0x73676174UL: return key.Length == 4 ? 3 : -1; // "tags"
            case 0x6c69616d65UL: return key.Length == 5 ? 4 : -1; // "email"
            case 0x65726f6373UL: return key.Length == 5 ? 5 : -1; // "score"
            case 0x73736572646461UL: return key.Length == 7 ? 6 : -1; // "address"
            case 0x6576697463417369UL: return key.Length == 8 ? 7 : -1; // "isActive"
            case 0x656d614e72657375UL: return key.Length == 8 ? 8 : -1; // "userName"
            case 0x6c69614d72657375UL: return key.Length == 8 ? 9 : -1; // "userMail"
            case 0x4164657461657263UL: return key.Length == 9 && FetchChunk(key, 8) == 0x74UL ? 10 : -1; // "createdA" + "t"
            case 0x4164657461647075UL: return key.Length == 9 && FetchChunk(key, 8) == 0x74UL ? 11 : -1; // "updatedA" + "t"
            case 0x7470697263736564UL: return key.Length == 11 && FetchChunk(key, 8) == 0x6e6f69UL ? 12 : -1; // "descript" + "ion"
            case 0x6d754e656e6f6870UL: return key.Length == 11 && FetchChunk(key, 8) == 0x726562UL ? 13 : -1; // "phoneNum" + "ber"
            case 0x437972746e756f63UL: return key.Length == 11 && FetchChunk(key, 8) == 0x65646fUL ? 14 : -1; // "countryC" + "ode"
            case 0x49656c69666f7270UL: return key.Length == 12 && FetchChunk(key, 8) == 0x6567616dUL ? 15 : -1; // "profileI" + "mage"
            default: return -1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int MatchLengthFirst(ReadOnlySpan<byte> key)
    {
        switch (key.Length)
        {
            case 2:
                if (BinaryPrimitives.ReadUInt16LittleEndian(key) == 0x6469) return 0; // "id"
                break;
            case 3:
                if (BinaryPrimitives.ReadUInt16LittleEndian(key) == 0x6761 && key[2] == 0x65) return 1; // "age"
                break;
            case 4:
                switch (BinaryPrimitives.ReadUInt32LittleEndian(key))
                {
                    case 0x656d616e: return 2; // "name"
                    case 0x73676174: return 3; // "tags"
                }
                break;
            case 5:
                switch (BinaryPrimitives.ReadUInt32LittleEndian(key))
                {
                    case 0x69616d65: if (key[4] == 0x6c) return 4; break; // "emai" + "l"
                    case 0x726f6373: if (key[4] == 0x65) return 5; break; // "scor" + "e"
                }
                break;
            case 7: // "address": overlapped uints [0..4) + [3..7)
                if (BinaryPrimitives.ReadUInt32LittleEndian(key) == 0x72646461
                    && BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(3)) == 0x73736572) return 6;
                break;
            case 8:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x6576697463417369UL: return 7; // "isActive"
                    case 0x656d614e72657375UL: return 8; // "userName"
                    case 0x6c69614d72657375UL: return 9; // "userMail"
                }
                break;
            case 9:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x4164657461657263UL: if (key[8] == 0x74) return 10; break; // "createdA" + "t"
                    case 0x4164657461647075UL: if (key[8] == 0x74) return 11; break; // "updatedA" + "t"
                }
                break;
            case 11: // tails as overlapped ulongs [3..11)
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x7470697263736564UL: // "descript"
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x6e6f697470697263UL) return 12;
                        break;
                    case 0x6d754e656e6f6870UL: // "phoneNum"
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x7265626d754e656eUL) return 13;
                        break;
                    case 0x437972746e756f63UL: // "countryC"
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x65646f437972746eUL) return 14;
                        break;
                }
                break;
            case 12: // "profileImage": ulong [0..8) + uint [8..12)
                if (BinaryPrimitives.ReadUInt64LittleEndian(key) == 0x49656c69666f7270UL
                    && BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(8)) == 0x6567616d) return 15;
                break;
        }
        return -1;
    }

    // one compare against the declaration-order expectation; the caller falls back to a full matcher on miss and advances the hint on any hit
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int MatchHint(ReadOnlySpan<byte> key, int hint)
    {
        switch (hint)
        {
            case 0: return key.SequenceEqual("id"u8) ? 0 : -1;
            case 1: return key.SequenceEqual("age"u8) ? 1 : -1;
            case 2: return key.SequenceEqual("name"u8) ? 2 : -1;
            case 3: return key.SequenceEqual("tags"u8) ? 3 : -1;
            case 4: return key.SequenceEqual("email"u8) ? 4 : -1;
            case 5: return key.SequenceEqual("score"u8) ? 5 : -1;
            case 6: return key.SequenceEqual("address"u8) ? 6 : -1;
            case 7: return key.SequenceEqual("isActive"u8) ? 7 : -1;
            case 8: return key.SequenceEqual("userName"u8) ? 8 : -1;
            case 9: return key.SequenceEqual("userMail"u8) ? 9 : -1;
            case 10: return key.SequenceEqual("createdAt"u8) ? 10 : -1;
            case 11: return key.SequenceEqual("updatedAt"u8) ? 11 : -1;
            case 12: return key.SequenceEqual("description"u8) ? 12 : -1;
            case 13: return key.SequenceEqual("phoneNumber"u8) ? 13 : -1;
            case 14: return key.SequenceEqual("countryCode"u8) ? 14 : -1;
            case 15: return key.SequenceEqual("profileImage"u8) ? 15 : -1;
            default: return -1;
        }
    }

    // ---- verification -----------------------------------------------------

    public static void VerifyCandidates()
    {
        // adversarial keys first (they pinpoint the failing matcher and key), cross-checked per key against the linear SequenceEqual oracle
        var adversarial = new List<byte[]> { Array.Empty<byte>(), new byte[40] };
        foreach (var name in MemberNames)
        {
            var utf8 = Encoding.UTF8.GetBytes(name);
            adversarial.Add(utf8);
            for (int cut = 0; cut < utf8.Length; cut++)
            {
                adversarial.Add(utf8[..cut]); // every truncation
            }
            for (int pad = 1; pad <= 8; pad++)
            {
                var padded = new byte[utf8.Length + pad]; // NUL padding: the chunk-compose collision class
                utf8.CopyTo(padded, 0);
                adversarial.Add(padded);
            }
            var mutated = (byte[])utf8.Clone();
            mutated[^1] ^= 1;
            adversarial.Add(mutated);
            adversarial.Add([.. utf8, (byte)'x']); // extension
        }

        foreach (var key in adversarial)
        {
            var expected = MatchLinear(key);
            if (MatchChunkFirst(key) != expected)
            {
                throw new InvalidOperationException($"ChunkFirst mismatch on [{Convert.ToHexString(key)}]: {MatchChunkFirst(key)} (expected {expected})");
            }
            if (MatchLengthFirst(key) != expected)
            {
                throw new InvalidOperationException($"LengthFirst mismatch on [{Convert.ToHexString(key)}]: {MatchLengthFirst(key)} (expected {expected})");
            }
            for (int hint = 0; hint <= 16; hint++)
            {
                var hintExpected = hint < 16 && key.AsSpan().SequenceEqual(Encoding.UTF8.GetBytes(MemberNames[hint])) ? hint : -1;
                if (MatchHint(key, hint) != hintExpected)
                {
                    throw new InvalidOperationException($"MatchHint mismatch on [{Convert.ToHexString(key)}] hint={hint}");
                }
            }
        }

        // dataset cross-check (Setup throws on candidate sum mismatch) for every distribution
        foreach (var distribution in new[] { "InOrder", "Shuffled", "Unknown25" })
        {
            new MapKeyMatchBenchmark { Distribution = distribution }.Setup();
        }
    }
}

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;

// Multi-chunk key compare inside ObjectEmitter's length-first automata: the shipped && chain (one branch per remaining
// chunk) against an XOR-OR fold ((a ^ C1) | (b ^ C2) | ...) == 0 that tests every remaining chunk with one branch.
// Only keys needing two or more reads after the bucket dispatch change shape: in the Short set that is age(3) /
// address(7) / profileImage(12); in the Long set every member of 19+ bytes (three to four ulong reads).
// The matchers are emitted by a script that mirrors EmitLengthBucket / PlanKeyReads, so both rows are the generator's
// own shape and differ only in the compare fold.
//
// Result (net11.0 rc.1, x64, 10 iterations; ns per key): the fold never lost and ObjectEmitter.KeyCondition adopted it.
//   Short InOrder 1.40 -> 1.40 | NearMiss25 5.05 -> 4.69 (0.93) | Shuffled 10.8 -> 10.3 (0.95)
//   Long  InOrder 1.49 -> 1.38 (0.92) | NearMiss25 6.32 -> 5.78 (0.92) | Shuffled 11.2 -> 10.8 (0.96)
// Shuffled stays ~7.5x InOrder in both: the length and first-chunk switches mispredict, which the fold cannot touch.
//
// Key sets (16 members each, same table size as MapKeyMatchBenchmark):
//   Short - MapKeyMatchBenchmark's camelCase set, lengths 2..12
//   Long  - descriptive member names, lengths 11..26; buckets 20 and 21 share first chunks ("shipping", "...edAtUtc")
// Distributions (100k+ keys):
//   InOrder    - every object carries all 16 keys in declaration order
//   Shuffled   - per-object random key order
//   NearMiss25 - in-order plus 5 per object of a member key with one byte flipped: passes the length switch and
//                usually the first-chunk switch, then fails in the compares this benchmark is about (where && can exit
//                early and the fold cannot)
public class MapKeyCompareFoldBenchmark
{
    [Params("Short", "Long")]
    public string KeySet = "Short";

    [Params("InOrder", "Shuffled", "NearMiss25")]
    public string Distribution = "InOrder";

    internal static readonly string[] ShortNames =
    [
        "id", "age", "name", "tags", "email", "score", "address", "isActive",
        "userName", "userMail", "createdAt", "updatedAt", "description", "phoneNumber", "countryCode", "profileImage",
    ];

    internal static readonly string[] LongNames =
    [
        "createdAtUtcTimestamp", "updatedAtUtcTimestamp", "lastModifiedByUserId", "primaryEmailAddress",
        "secondaryEmailAddress", "shippingAddressLine1", "shippingAddressLine2", "billingAddressPostalCode",
        "preferredLanguageCode", "notificationPreferences", "accountVerificationStatus", "externalIdentityProviderId",
        "displayName", "phoneNumber", "isEmailConfirmed", "twoFactorEnabled",
    ];

    byte[] blob = null!;
    int[] offsets = null!;
    int[] lengths = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);
        var names = KeySet == "Short" ? ShortNames : KeySet == "Long" ? LongNames : throw new InvalidOperationException(KeySet);
        var memberUtf8 = names.Select(Encoding.UTF8.GetBytes).ToArray();
        const int ObjectCount = 6250; // x16 known keys = 100,000

        var keys = new List<byte[]>(ObjectCount * 21);
        for (int o = 0; o < ObjectCount; o++)
        {
            switch (Distribution)
            {
                case "InOrder":
                    keys.AddRange(memberUtf8);
                    break;
                case "Shuffled":
                    var shuffled = (byte[][])memberUtf8.Clone();
                    rand.Shuffle(shuffled);
                    keys.AddRange(shuffled);
                    break;
                case "NearMiss25":
                    var withMiss = new List<byte[]>(memberUtf8);
                    for (int u = 0; u < 5; u++)
                    {
                        var miss = (byte[])memberUtf8[rand.Next(memberUtf8.Length)].Clone();
                        miss[rand.Next(miss.Length)] ^= 0x20; // a case flip: still a plausible identifier
                        withMiss.Insert(rand.Next(withMiss.Count + 1), miss);
                    }
                    keys.AddRange(withMiss);
                    break;
                default:
                    throw new InvalidOperationException(Distribution);
            }
        }

        blob = new byte[keys.Sum(static k => k.Length)];
        offsets = new int[keys.Count];
        lengths = new int[keys.Count];
        int pos = 0;
        for (int k = 0; k < keys.Count; k++)
        {
            keys[k].CopyTo(blob, pos);
            offsets[k] = pos;
            lengths[k] = keys[k].Length;
            pos += keys[k].Length;
        }

        var oracle = 0L;
        foreach (var key in keys)
        {
            oracle += Array.IndexOf(names, Encoding.UTF8.GetString(key));
        }
        if (AndChain() != oracle || XorOr() != oracle)
        {
            throw new InvalidOperationException($"candidate sum mismatch on {KeySet}/{Distribution}");
        }
    }

    [Benchmark(Baseline = true)]
    public long AndChain()
    {
        long sum = 0;
        var blob = this.blob;
        if (KeySet == "Short")
        {
            for (int k = 0; k < offsets.Length; k++) sum += MatchShortAndChain(blob.AsSpan(offsets[k], lengths[k]));
        }
        else
        {
            for (int k = 0; k < offsets.Length; k++) sum += MatchLongAndChain(blob.AsSpan(offsets[k], lengths[k]));
        }
        return sum;
    }

    [Benchmark]
    public long XorOr()
    {
        long sum = 0;
        var blob = this.blob;
        if (KeySet == "Short")
        {
            for (int k = 0; k < offsets.Length; k++) sum += MatchShortXorOr(blob.AsSpan(offsets[k], lengths[k]));
        }
        else
        {
            for (int k = 0; k < offsets.Length; k++) sum += MatchLongXorOr(blob.AsSpan(offsets[k], lengths[k]));
        }
        return sum;
    }

    // every truncation, NUL padding, single-byte flip at every position and extension of every member, cross-checked
    // against the string oracle, then each distribution's Setup gate
    public static void VerifyCandidates()
    {
        foreach (var (names, andChain, xorOr) in new (string[], Func<byte[], int>, Func<byte[], int>)[]
        {
            (ShortNames, static k => MatchShortAndChain(k), static k => MatchShortXorOr(k)),
            (LongNames, static k => MatchLongAndChain(k), static k => MatchLongXorOr(k)),
        })
        {
            var adversarial = new List<byte[]> { Array.Empty<byte>(), new byte[40] };
            foreach (var name in names)
            {
                var utf8 = Encoding.UTF8.GetBytes(name);
                adversarial.Add(utf8);
                adversarial.Add([.. utf8, (byte)'x']);
                for (int cut = 0; cut < utf8.Length; cut++)
                {
                    adversarial.Add(utf8[..cut]);
                }
                for (int pad = 1; pad <= 8; pad++)
                {
                    adversarial.Add([.. utf8, .. new byte[pad]]);
                }
                for (int at = 0; at < utf8.Length; at++)
                {
                    foreach (var bit in new byte[] { 0x01, 0x20, 0x80 })
                    {
                        var flipped = (byte[])utf8.Clone();
                        flipped[at] ^= bit;
                        adversarial.Add(flipped);
                    }
                }
            }
            foreach (var key in adversarial)
            {
                var expected = Array.IndexOf(names, Encoding.UTF8.GetString(key));
                if (andChain(key) != expected || xorOr(key) != expected)
                {
                    throw new InvalidOperationException($"mismatch on [{Convert.ToHexString(key)}]: and={andChain(key)} xor={xorOr(key)} (expected {expected})");
                }
            }
        }

        foreach (var keySet in new[] { "Short", "Long" })
        {
            foreach (var distribution in new[] { "InOrder", "Shuffled", "NearMiss25" })
            {
                new MapKeyCompareFoldBenchmark { KeySet = keySet, Distribution = distribution }.Setup();
            }
        }
    }

    // ---- generated matchers (EmitLengthBucket shape; AndChain = shipped fold, XorOr = candidate) ----
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int MatchShortAndChain(ReadOnlySpan<byte> key)
    {
        switch (key.Length)
        {
            case 2:
                if (BinaryPrimitives.ReadUInt16LittleEndian(key) == 0x6469) return 0; // "id"
                break;
            case 3:
                if (BinaryPrimitives.ReadUInt16LittleEndian(key) == 0x6761 && BinaryPrimitives.ReadUInt16LittleEndian(key.Slice(1)) == 0x6567) return 1; // "age"
                break;
            case 4:
                switch (BinaryPrimitives.ReadUInt32LittleEndian(key))
                {
                    case 0x656d616eU:
                        return 2; // "name"
                    case 0x73676174U:
                        return 3; // "tags"
                }
                break;
            case 5:
                switch (BinaryPrimitives.ReadUInt32LittleEndian(key))
                {
                    case 0x69616d65U:
                        if (BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(1)) == 0x6c69616dU) return 4; // "email"
                        break;
                    case 0x726f6373U:
                        if (BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(1)) == 0x65726f63U) return 5; // "score"
                        break;
                }
                break;
            case 7:
                if (BinaryPrimitives.ReadUInt32LittleEndian(key) == 0x72646461U && BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(3)) == 0x73736572U) return 6; // "address"
                break;
            case 8:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x6576697463417369UL:
                        return 7; // "isActive"
                    case 0x656d614e72657375UL:
                        return 8; // "userName"
                    case 0x6c69614d72657375UL:
                        return 9; // "userMail"
                }
                break;
            case 9:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x4164657461657263UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(1)) == 0x7441646574616572UL) return 10; // "createdAt"
                        break;
                    case 0x4164657461647075UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(1)) == 0x7441646574616470UL) return 11; // "updatedAt"
                        break;
                }
                break;
            case 11:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x7470697263736564UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x6e6f697470697263UL) return 12; // "description"
                        break;
                    case 0x6d754e656e6f6870UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x7265626d754e656eUL) return 13; // "phoneNumber"
                        break;
                    case 0x437972746e756f63UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x65646f437972746eUL) return 14; // "countryCode"
                        break;
                }
                break;
            case 12:
                if (BinaryPrimitives.ReadUInt64LittleEndian(key) == 0x49656c69666f7270UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(4)) == 0x6567616d49656c69UL) return 15; // "profileImage"
                break;
        }
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int MatchShortXorOr(ReadOnlySpan<byte> key)
    {
        switch (key.Length)
        {
            case 2:
                if (BinaryPrimitives.ReadUInt16LittleEndian(key) == 0x6469) return 0; // "id"
                break;
            case 3:
                if (((uint)(BinaryPrimitives.ReadUInt16LittleEndian(key) ^ 0x6761) | (uint)(BinaryPrimitives.ReadUInt16LittleEndian(key.Slice(1)) ^ 0x6567)) == 0) return 1; // "age"
                break;
            case 4:
                switch (BinaryPrimitives.ReadUInt32LittleEndian(key))
                {
                    case 0x656d616eU:
                        return 2; // "name"
                    case 0x73676174U:
                        return 3; // "tags"
                }
                break;
            case 5:
                switch (BinaryPrimitives.ReadUInt32LittleEndian(key))
                {
                    case 0x69616d65U:
                        if (BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(1)) == 0x6c69616dU) return 4; // "email"
                        break;
                    case 0x726f6373U:
                        if (BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(1)) == 0x65726f63U) return 5; // "score"
                        break;
                }
                break;
            case 7:
                if (((BinaryPrimitives.ReadUInt32LittleEndian(key) ^ 0x72646461U) | (BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(3)) ^ 0x73736572U)) == 0) return 6; // "address"
                break;
            case 8:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x6576697463417369UL:
                        return 7; // "isActive"
                    case 0x656d614e72657375UL:
                        return 8; // "userName"
                    case 0x6c69614d72657375UL:
                        return 9; // "userMail"
                }
                break;
            case 9:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x4164657461657263UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(1)) == 0x7441646574616572UL) return 10; // "createdAt"
                        break;
                    case 0x4164657461647075UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(1)) == 0x7441646574616470UL) return 11; // "updatedAt"
                        break;
                }
                break;
            case 11:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x7470697263736564UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x6e6f697470697263UL) return 12; // "description"
                        break;
                    case 0x6d754e656e6f6870UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x7265626d754e656eUL) return 13; // "phoneNumber"
                        break;
                    case 0x437972746e756f63UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x65646f437972746eUL) return 14; // "countryCode"
                        break;
                }
                break;
            case 12:
                if (((BinaryPrimitives.ReadUInt64LittleEndian(key) ^ 0x49656c69666f7270UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(4)) ^ 0x6567616d49656c69UL)) == 0) return 15; // "profileImage"
                break;
        }
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int MatchLongAndChain(ReadOnlySpan<byte> key)
    {
        switch (key.Length)
        {
            case 11:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x4e79616c70736964UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x656d614e79616c70UL) return 12; // "displayName"
                        break;
                    case 0x6d754e656e6f6870UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x7265626d754e656eUL) return 13; // "phoneNumber"
                        break;
                }
                break;
            case 16:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x436c69616d457369UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x64656d7269666e6fUL) return 14; // "isEmailConfirmed"
                        break;
                    case 0x6f746361466f7774UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x64656c62616e4572UL) return 15; // "twoFactorEnabled"
                        break;
                }
                break;
            case 19:
                if (BinaryPrimitives.ReadUInt64LittleEndian(key) == 0x457972616d697270UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x726464416c69616dUL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(11)) == 0x737365726464416cUL) return 3; // "primaryEmailAddress"
                break;
            case 20:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x69646f4d7473616cUL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x7355794264656966UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(12)) == 0x6449726573557942UL) return 2; // "lastModifiedByUserId"
                        break;
                    case 0x676e697070696873UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x4c73736572646441UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(12)) == 0x31656e694c737365UL) return 5; // "shippingAddressLine1"
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x4c73736572646441UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(12)) == 0x32656e694c737365UL) return 6; // "shippingAddressLine2"
                        break;
                }
                break;
            case 21:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x4164657461657263UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x656d695463745574UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(13)) == 0x706d617473656d69UL) return 0; // "createdAtUtcTimestamp"
                        break;
                    case 0x4164657461647075UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x656d695463745574UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(13)) == 0x706d617473656d69UL) return 1; // "updatedAtUtcTimestamp"
                        break;
                    case 0x7261646e6f636573UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x64416c69616d4579UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(13)) == 0x737365726464416cUL) return 4; // "secondaryEmailAddress"
                        break;
                    case 0x6572726566657270UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x676175676e614c64UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(13)) == 0x65646f4365676175UL) return 8; // "preferredLanguageCode"
                        break;
                }
                break;
            case 23:
                if (BinaryPrimitives.ReadUInt64LittleEndian(key) == 0x6163696669746f6eUL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x666572506e6f6974UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(15)) == 0x7365636e65726566UL) return 9; // "notificationPreferences"
                break;
            case 24:
                if (BinaryPrimitives.ReadUInt64LittleEndian(key) == 0x41676e696c6c6962UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x6f50737365726464UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(16)) == 0x65646f436c617473UL) return 7; // "billingAddressPostalCode"
                break;
            case 25:
                if (BinaryPrimitives.ReadUInt64LittleEndian(key) == 0x56746e756f636361UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x7461636966697265UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(16)) == 0x75746174536e6f69UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(17)) == 0x7375746174536e6fUL) return 10; // "accountVerificationStatus"
                break;
            case 26:
                if (BinaryPrimitives.ReadUInt64LittleEndian(key) == 0x6c616e7265747865UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x797469746e656449UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(16)) == 0x72656469766f7250UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(18)) == 0x644972656469766fUL) return 11; // "externalIdentityProviderId"
                break;
        }
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int MatchLongXorOr(ReadOnlySpan<byte> key)
    {
        switch (key.Length)
        {
            case 11:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x4e79616c70736964UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x656d614e79616c70UL) return 12; // "displayName"
                        break;
                    case 0x6d754e656e6f6870UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(3)) == 0x7265626d754e656eUL) return 13; // "phoneNumber"
                        break;
                }
                break;
            case 16:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x436c69616d457369UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x64656d7269666e6fUL) return 14; // "isEmailConfirmed"
                        break;
                    case 0x6f746361466f7774UL:
                        if (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) == 0x64656c62616e4572UL) return 15; // "twoFactorEnabled"
                        break;
                }
                break;
            case 19:
                if (((BinaryPrimitives.ReadUInt64LittleEndian(key) ^ 0x457972616d697270UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) ^ 0x726464416c69616dUL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(11)) ^ 0x737365726464416cUL)) == 0) return 3; // "primaryEmailAddress"
                break;
            case 20:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x69646f4d7473616cUL:
                        if (((BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) ^ 0x7355794264656966UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(12)) ^ 0x6449726573557942UL)) == 0) return 2; // "lastModifiedByUserId"
                        break;
                    case 0x676e697070696873UL:
                        if (((BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) ^ 0x4c73736572646441UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(12)) ^ 0x31656e694c737365UL)) == 0) return 5; // "shippingAddressLine1"
                        if (((BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) ^ 0x4c73736572646441UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(12)) ^ 0x32656e694c737365UL)) == 0) return 6; // "shippingAddressLine2"
                        break;
                }
                break;
            case 21:
                switch (BinaryPrimitives.ReadUInt64LittleEndian(key))
                {
                    case 0x4164657461657263UL:
                        if (((BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) ^ 0x656d695463745574UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(13)) ^ 0x706d617473656d69UL)) == 0) return 0; // "createdAtUtcTimestamp"
                        break;
                    case 0x4164657461647075UL:
                        if (((BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) ^ 0x656d695463745574UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(13)) ^ 0x706d617473656d69UL)) == 0) return 1; // "updatedAtUtcTimestamp"
                        break;
                    case 0x7261646e6f636573UL:
                        if (((BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) ^ 0x64416c69616d4579UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(13)) ^ 0x737365726464416cUL)) == 0) return 4; // "secondaryEmailAddress"
                        break;
                    case 0x6572726566657270UL:
                        if (((BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) ^ 0x676175676e614c64UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(13)) ^ 0x65646f4365676175UL)) == 0) return 8; // "preferredLanguageCode"
                        break;
                }
                break;
            case 23:
                if (((BinaryPrimitives.ReadUInt64LittleEndian(key) ^ 0x6163696669746f6eUL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) ^ 0x666572506e6f6974UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(15)) ^ 0x7365636e65726566UL)) == 0) return 9; // "notificationPreferences"
                break;
            case 24:
                if (((BinaryPrimitives.ReadUInt64LittleEndian(key) ^ 0x41676e696c6c6962UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) ^ 0x6f50737365726464UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(16)) ^ 0x65646f436c617473UL)) == 0) return 7; // "billingAddressPostalCode"
                break;
            case 25:
                if (((BinaryPrimitives.ReadUInt64LittleEndian(key) ^ 0x56746e756f636361UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) ^ 0x7461636966697265UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(16)) ^ 0x75746174536e6f69UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(17)) ^ 0x7375746174536e6fUL)) == 0) return 10; // "accountVerificationStatus"
                break;
            case 26:
                if (((BinaryPrimitives.ReadUInt64LittleEndian(key) ^ 0x6c616e7265747865UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(8)) ^ 0x797469746e656449UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(16)) ^ 0x72656469766f7250UL) | (BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(18)) ^ 0x644972656469766fUL)) == 0) return 11; // "externalIdentityProviderId"
                break;
        }
        return -1;
    }
}

using System.Diagnostics;
using System.Text;

namespace MessagePack;

/// <summary>
/// Holds the members of a payload that the deserialized type does not declare, and writes them back on serialization,
/// so that an older schema in the middle of a pipeline does not strip what a newer writer produced.
/// Declare one settable member of this type on a <see cref="MessagePackObjectAttribute"/> type, without <see cref="KeyAttribute"/>. The source-generated formatter fills it.
/// Values are kept as raw MessagePack bytes and replayed unchanged.
/// A packet holds either string-keyed members (<see cref="IsMap"/>) or array elements by index (<see cref="IsArray"/>), decided at construction.
/// <see cref="ToMapDictionary"/> and <see cref="ToArrayDictionary"/> decode the contents for inspection.
/// </summary>
public sealed class MessagePackUnknownMembers
{
    // String-keyed entries carry the key's utf8 bytes (Index -1), int-keyed entries carry the array index
    // (KeyUtf8 null). Values are verbatim raw msgpack.
    internal readonly record struct Entry(int Index, byte[]? KeyUtf8, byte[] Value);

    internal readonly List<Entry> Entries = new();
    readonly bool isMap;

    /// <summary>
    /// Creates an empty packet, for string-keyed members when <paramref name="isMap"/> is true and for array elements otherwise.
    /// Entries are only added by deserialization, so constructing one is rarely needed beyond resetting a member.
    /// </summary>
    public MessagePackUnknownMembers(bool isMap) => this.isMap = isMap;

    /// <summary>Number of captured members.</summary>
    public int Count => Entries.Count;

    /// <summary>Whether this packet holds string-keyed members. Fixed at construction, so it is meaningful while empty.</summary>
    public bool IsMap => isMap;

    /// <summary>Whether this packet holds array elements keyed by index. Fixed at construction, so it is meaningful while empty.</summary>
    public bool IsArray => !isMap;

    // Duplicate-key index for wide maps. Entries keeps the raw data in payload order; past a handful of
    // keys the linear scan would make capture quadratic in the key count (a schema relay with thousands of
    // unknown members is ordinary input), so a keyed set takes over. Keys are payload-chosen, hence the
    // hash-flooding-resistant comparer like every other input-keyed table here.
    const int LinearScanLimit = 8;
    HashSet<byte[]>? keyIndex;

    internal void AddStringKeyed(byte[] keyUtf8, byte[] value)
    {
        Debug.Assert(isMap, "capture created this packet for the map mode it runs in");
        // The generated formatter rejects duplicate declared keys, and captured keys keep the same policy.
        // Otherwise a hostile duplicate would be preserved and replayed into a payload that every strict reader
        // downstream rejects.
        if (keyIndex is null)
        {
            foreach (var entry in Entries)
            {
                if (entry.KeyUtf8 is { } existing && existing.AsSpan().SequenceEqual(keyUtf8))
                {
                    ThrowDuplicateKey();
                }
            }
            if (Entries.Count >= LinearScanLimit)
            {
                keyIndex = new HashSet<byte[]>(Utf8KeyComparer.Instance);
                foreach (var entry in Entries)
                {
                    keyIndex.Add(entry.KeyUtf8!);
                }
            }
        }
        if (keyIndex is not null && !keyIndex.Add(keyUtf8))
        {
            ThrowDuplicateKey();
        }
        Entries.Add(new Entry(-1, keyUtf8, value));
    }

    static void ThrowDuplicateKey() => throw new MessagePackSerializationException("The map in the payload defines the same key more than once");

    sealed class Utf8KeyComparer : IEqualityComparer<byte[]>
    {
        public static readonly Utf8KeyComparer Instance = new();

        public bool Equals(byte[]? x, byte[]? y) => x.AsSpan().SequenceEqual(y);

        public int GetHashCode(byte[] key) => (int)SipHash.Hash64(key, HashFloodingResistantEqualityComparer.sipHashKey0, HashFloodingResistantEqualityComparer.sipHashKey1);
    }

    internal void AddIntKeyed(int index, byte[] value)
    {
        Debug.Assert(!isMap, "capture created this packet for the array mode it runs in");
        Entries.Add(new Entry(index, null, value));
    }

    // Array header count when this packet extends a wire array whose declared members occupy [0, declaredCount).
    // Entries ascend by index (the capture loop's own counter), so the last entry is the packet's max, and Math.Max
    // covers a capture whose entries all sit at key holes inside the declared range.
    internal int GetArrayCount(int declaredCount)
    {
        var count = Entries.Count;
        return count == 0 ? declaredCount : Math.Max(declaredCount, Entries[count - 1].Index + 1);
    }

    /// <summary>
    /// Decodes the string-keyed members for inspection. Values are read the way <c>object</c> deserializes, so maps become dictionaries and arrays become object arrays.
    /// The result is a snapshot; serialization replays the raw bytes, not this view.
    /// Throws when the packet holds array elements.
    /// </summary>
    public Dictionary<string, object?> ToMapDictionary(MessagePackSerializerOptions? options = null)
    {
        if (IsArray)
        {
            throw new InvalidOperationException("This packet holds trailing array elements keyed by index; use ToArrayDictionary.");
        }
        // DefaultAot, not Default. Object deserialization is the builtin tier's PrimitiveObjectFormatter in both
        // presets, and this one keeps the method clean for trimming/Native AOT (no reflection tail, no MakeGenericType tier).
        options ??= MessagePackSerializerOptions.DefaultAot;
        var result = options.Resolver.HashFloodingResistant
            ? new Dictionary<string, object?>(Entries.Count, HashFloodingResistantEqualityComparer.Get<string>())
            : new Dictionary<string, object?>(Entries.Count);
        foreach (var entry in Entries)
        {
            result[Encoding.UTF8.GetString(entry.KeyUtf8!)] = MessagePackSerializer.Deserialize<object>(entry.Value, options);
        }
        return result;
    }

    /// <summary>
    /// Decodes the array elements for inspection, keyed by their original index. Values are read as in <see cref="ToMapDictionary"/>, and a captured nil becomes null.
    /// Throws when the packet holds string-keyed members.
    /// </summary>
    public Dictionary<int, object?> ToArrayDictionary(MessagePackSerializerOptions? options = null)
    {
        if (IsMap)
        {
            throw new InvalidOperationException("This packet holds string-keyed members; use ToMapDictionary.");
        }
        options ??= MessagePackSerializerOptions.DefaultAot;
        // No flooding-resistant comparer here, because the keys are loop-counter indices the capture assigned in
        // ascending order, not attacker-chosen values.
        var result = new Dictionary<int, object?>(Entries.Count);
        foreach (var entry in Entries)
        {
            result[entry.Index] = MessagePackSerializer.Deserialize<object>(entry.Value, options);
        }
        return result;
    }
}

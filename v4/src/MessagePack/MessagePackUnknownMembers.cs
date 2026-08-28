using System.Diagnostics;
using System.Text;
using MessagePack.Internal;

namespace MessagePack;

/// <summary>
/// Round-trip retention for schema evolution: captures the members of a payload that the deserializing type does not declare, and writes them back on serialization, so an older schema in the middle of a pipeline no longer silently strips what a newer writer produced.
/// Declare exactly one settable member of this type on a [MessagePackObject] type; it carries no [Key] and is served by the source-generated formatter only.
/// The captured data is opaque by design: values are held and replayed as raw msgpack bytes, byte-for-byte (keys re-encode with canonical string headers), which is what keeps the round trip faithful and keeps unvalidated data uninterpreted.
/// One packet holds one kind of entry, fixed at construction: string keys (<see cref="IsMap"/>) or array indices (<see cref="IsArray"/>), never both. Each deserialization assigns a freshly captured packet in its own wire mode.
/// <see cref="ToMapDictionary"/> and <see cref="ToArrayDictionary"/> offer read-only materialized views for diagnostics; there is deliberately no write access, because edited entries would have to be re-encoded and the fidelity guarantee would be gone.
/// Replay order: string-keyed entries append after the declared members in capture order; int-keyed entries rejoin the array at their original indices, both inside the declared key range (filling the key holes they were captured from) and beyond it (gaps become nil).
/// A nil at a key hole is not captured: it is the padding serialization regenerates for every hole anyway, so the type's own payloads round trip packet-free. A non-nil hole value (a wider schema still writing a key this schema retired by deleting the member) is captured and replayed in place.
/// A deserialization always replaces the whole packet on the target member, never merges.
/// </summary>
public sealed class MessagePackUnknownMembers
{
    // string-keyed entries carry the key's utf8 bytes (Index -1); int-keyed entries carry
    // the array index (KeyUtf8 null). Values are verbatim raw msgpack.
    internal readonly record struct Entry(int Index, byte[]? KeyUtf8, byte[] Value);

    internal readonly List<Entry> Entries = new();
    readonly bool isMap;

    /// <summary>Creates an empty packet fixed to one wire mode: <paramref name="isMap"/> true for string-keyed map capture, false for int-keyed array capture. Entries are only ever added by deserialization, so constructing one is rarely useful beyond resetting a member.</summary>
    public MessagePackUnknownMembers(bool isMap) => this.isMap = isMap;

    /// <summary>Number of captured members.</summary>
    public int Count => Entries.Count;

    /// <summary>True when this packet captures string-keyed members (map-format types). Fixed at construction, meaningful even while empty.</summary>
    public bool IsMap => isMap;

    /// <summary>True when this packet captures trailing array elements keyed by index (int-keyed array-format types). Fixed at construction, meaningful even while empty.</summary>
    public bool IsArray => !isMap;

    internal void AddStringKeyed(byte[] keyUtf8, byte[] value)
    {
        Debug.Assert(isMap, "capture created this packet for the map mode it runs in");
        // the generated formatter rejects duplicate DECLARED keys; captured keys keep the
        // same policy, otherwise a hostile duplicate would be preserved and replayed into
        // a payload that every strict reader downstream rejects
        foreach (var entry in Entries)
        {
            if (entry.KeyUtf8 is { } existing && existing.AsSpan().SequenceEqual(keyUtf8))
            {
                throw new MessagePackSerializationException("The map in the payload defines the same key more than once");
            }
        }
        Entries.Add(new Entry(-1, keyUtf8, value));
    }

    internal void AddIntKeyed(int index, byte[] value)
    {
        Debug.Assert(!isMap, "capture created this packet for the array mode it runs in");
        Entries.Add(new Entry(index, null, value));
    }

    // array header count when this packet extends a wire array whose declared members
    // occupy [0, declaredCount): entries ascend by index (the capture loop's own
    // counter), so the last entry is the packet's max and Math.Max covers a capture
    // whose entries all sit at key holes inside the declared range
    internal int GetArrayCount(int declaredCount)
    {
        var count = Entries.Count;
        return count == 0 ? declaredCount : Math.Max(declaredCount, Entries[count - 1].Index + 1);
    }

    /// <summary>
    /// Materializes a string-keyed capture for inspection: values deserialize the way <see cref="PrimitiveObjectFormatter{TWriteBuffer, TReadBuffer}"/> reads <c>object</c> (maps become dictionaries, arrays become object[], the timestamp extension becomes DateTime; any other extension throws).
    /// The view is a snapshot; serialization always replays the raw captured bytes, never this materialization.
    /// Throws when the packet is an array-format capture (<see cref="IsArray"/>), empty or not.
    /// </summary>
    public Dictionary<string, object?> ToMapDictionary(MessagePackSerializerOptions? options = null)
    {
        if (IsArray)
        {
            throw new InvalidOperationException("This packet holds trailing array elements keyed by index; use ToArrayDictionary.");
        }
        // DefaultAot, not Default: object deserialization is the builtin tier's
        // PrimitiveObjectFormatter in both presets, and this one keeps the method clean
        // for trimming/Native AOT (no reflection tail, no MakeGenericType tier)
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
    /// Materializes an array-format capture for inspection, keyed by the original array index; values deserialize as in <see cref="ToMapDictionary"/> (a captured nil, the wider schema's key padding, becomes null).
    /// Throws when the packet is a map-format capture (<see cref="IsMap"/>), empty or not.
    /// </summary>
    public Dictionary<int, object?> ToArrayDictionary(MessagePackSerializerOptions? options = null)
    {
        if (IsMap)
        {
            throw new InvalidOperationException("This packet holds string-keyed members; use ToMapDictionary.");
        }
        options ??= MessagePackSerializerOptions.DefaultAot;
        // no flooding-resistant comparer here: the keys are loop-counter indices the
        // capture assigned in ascending order, not attacker-chosen values
        var result = new Dictionary<int, object?>(Entries.Count);
        foreach (var entry in Entries)
        {
            result[entry.Index] = MessagePackSerializer.Deserialize<object>(entry.Value, options);
        }
        return result;
    }
}

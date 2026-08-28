using System.Buffers.Binary;
using System.ComponentModel;

namespace MessagePack;

/// <summary>
/// Support surface for source-generated formatters, not part of the general-purpose API:
/// the circular-reference back-reference token (ext type
/// <see cref="MessagePackCode.CircularReferenceExtensionTypeCode"/>) emitted for
/// <see cref="MessagePackObjectAttribute.AllowCircularReferences"/> types, and the
/// capture/replay calls behind <see cref="MessagePackUnknownMembers"/>. A hand-written
/// formatter for such a type may use these alongside the SerializeState/DeserializeState
/// tracking methods.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SourceGeneratorHelper
{
    // fixext4 + type + 4-byte id: the widest back-reference token
    const int MaxCircularReferenceBackReferenceLength = 6;

    extension<TWriteBuffer>(ref TWriteBuffer buffer)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        /// <summary>Writes a circular-reference back-reference: fixext1/2/4 carrying the
        /// id as raw big-endian in the smallest width.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void WriteCircularReferenceBackReference(uint referenceId)
        {
            ref var destination = ref buffer.GetReference(MaxCircularReferenceBackReferenceLength);
            Unsafe.Add(ref destination, 1) = unchecked((byte)MessagePackCode.CircularReferenceExtensionTypeCode);
            if (referenceId <= byte.MaxValue)
            {
                destination = MessagePackCode.FixExt1;
                Unsafe.Add(ref destination, 2) = (byte)referenceId;
                buffer.Advance(3);
                return;
            }
            if (referenceId <= ushort.MaxValue)
            {
                destination = MessagePackCode.FixExt2;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 2), MessagePackEndian.ToBigEndian((ushort)referenceId));
                buffer.Advance(4);
                return;
            }
            destination = MessagePackCode.FixExt4;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref destination, 2), MessagePackEndian.ToBigEndian(referenceId));
            buffer.Advance(6);
        }

        /// <summary>Replays captured string-keyed unknown members after the declared members: each key as a canonical str header plus the original utf8, each value verbatim. The map header count written earlier must already include <see cref="MessagePackUnknownMembers.Count"/>.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void WriteUnknownStringKeyed(MessagePackUnknownMembers members)
        {
            // a packet is settable and therefore transplantable across types; an
            // array-mode packet replayed into a map would silently corrupt the wire
            if (!members.IsMap)
            {
                throw new MessagePackSerializationException("This MessagePackUnknownMembers packet was constructed for array-format (int-keyed) capture and cannot be replayed by a map-format (string-keyed) type.");
            }
            foreach (var entry in members.Entries)
            {
                buffer.WriteString(entry.KeyUtf8!);
                buffer.WriteRaw(entry.Value);
            }
        }

        /// <summary>Replays the captured entry for one key hole (an index inside the declared key range with no member): the packet's next unreplayed entry when it sits at <paramref name="index"/> (advancing <paramref name="replayedCount"/>), the hole's nil padding otherwise. Captured entries ascend by index, so one cursor walks every hole in key order.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void WriteUnknownIntKeyedHole(MessagePackUnknownMembers? members, ref int replayedCount, int index)
        {
            if (members is not null && replayedCount < members.Entries.Count)
            {
                var entry = members.Entries[replayedCount];
                if (entry.Index == index)
                {
                    buffer.WriteRaw(entry.Value);
                    replayedCount++;
                    return;
                }
                // ascending entries make a lower index a passed-over one: a map-mode
                // packet (string-keyed entries carry Index -1) or a transplanted packet
                // whose entry sits at a declared key of this type. Fail loud either way.
                if (entry.Index < index)
                {
                    throw members.IsMap
                        ? new MessagePackSerializationException("This MessagePackUnknownMembers packet was constructed for map-format (string-keyed) capture and cannot be replayed by an array-format (int-keyed) type.")
                        : new MessagePackSerializationException("This MessagePackUnknownMembers packet holds entries at indices that are declared keys of the type replaying it.");
                }
            }
            buffer.WriteNil();
        }

        /// <summary>Replays captured int-keyed unknown members as the array's trailing elements starting at <paramref name="declaredCount"/>: gaps between captured indices become nil. <paramref name="replayedCount"/> skips the leading entries <see cref="WriteUnknownIntKeyedHole"/> already replayed into this type's key holes. The array header count written earlier must be <see cref="GetUnknownArrayCount"/>.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void WriteUnknownIntKeyed(MessagePackUnknownMembers members, int declaredCount, int replayedCount = 0)
        {
            if (members.IsMap)
            {
                throw new MessagePackSerializationException("This MessagePackUnknownMembers packet was constructed for map-format (string-keyed) capture and cannot be replayed by an array-format (int-keyed) type.");
            }
            var next = declaredCount;
            var entries = members.Entries;
            for (var i = replayedCount; i < entries.Count; i++)
            {
                var entry = entries[i];
                // an entry still unreplayed below the declared width sits at a DECLARED
                // key of this type (every hole was offered the cursor first): a
                // transplanted packet colliding with a member value fails loud here
                // instead of corrupting the wire
                if (entry.Index < next)
                {
                    throw new MessagePackSerializationException("This MessagePackUnknownMembers packet holds entries at indices that are declared keys of the type replaying it.");
                }
                for (; next < entry.Index; next++)
                {
                    buffer.WriteNil();
                }
                buffer.WriteRaw(entry.Value);
                next++;
            }
        }
    }

    /// <summary>Array header count for a type carrying int-keyed unknown members: the declared width, extended to cover the captured indices.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static int GetUnknownArrayCount(MessagePackUnknownMembers? members, int declaredCount) =>
        members is null ? declaredCount : members.GetArrayCount(declaredCount);

    // no-box bridges to explicit IMessagePackSerializationCallbackReceiver implementations on
    // structs: the constrained call through the type parameter dispatches on the value's address,
    // so callback mutations land in the caller's local without the box round-trip

    /// <summary>Invokes <see cref="IMessagePackSerializationCallbackReceiver.OnBeforeSerialize"/> on a struct without boxing.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnBeforeSerialize<T>(ref T value)
        where T : struct, IMessagePackSerializationCallbackReceiver
        => value.OnBeforeSerialize();

    /// <summary>Invokes <see cref="IMessagePackSerializationCallbackReceiver.OnAfterDeserialize"/> on a struct without boxing.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnAfterDeserialize<T>(ref T value)
        where T : struct, IMessagePackSerializationCallbackReceiver
        => value.OnAfterDeserialize();

    extension<TReadBuffer>(ref TReadBuffer buffer)
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        /// <summary>Consumes a circular-reference back-reference and returns its id;
        /// returns false consuming nothing when the next token is something else. A matched
        /// token with a payload width other than 1/2/4 bytes is malformed and throws.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool TryReadCircularReferenceBackReference(out uint referenceId)
        {
            if (!buffer.TryReadExtHeader(MessagePackCode.CircularReferenceExtensionTypeCode, out var dataLength))
            {
                referenceId = 0;
                return false;
            }
            // TryReadExtHeader proved dataLength fits in BytesRemaining, so TryGetSpan
            // (which stitches straddles) can only fail on a broken buffer implementation
            if (!buffer.TryGetSpan(dataLength, out var span))
            {
                throw new MessagePackSerializationException("Unexpected end of data while reading a circular-reference back-reference");
            }
            referenceId = dataLength switch
            {
                1 => span[0],
                2 => BinaryPrimitives.ReadUInt16BigEndian(span),
                4 => BinaryPrimitives.ReadUInt32BigEndian(span),
                _ => throw MessagePackSerializationException.ThrowMalformedCircularReferenceWidth(dataLength),
            };
            buffer.Advance(dataLength);
            return true;
        }

        /// <summary>Captures one unknown string-keyed member into <paramref name="members"/> (created on first capture): the key's utf8 bytes, copied before the buffer advances past the (possibly pooled) window they live in, then the value verbatim via ReadRaw. Duplicate captured keys throw, matching the declared-key policy.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void CaptureUnknownStringKeyed(ref MessagePackUnknownMembers? members, scoped ReadOnlySpan<byte> keyUtf8)
        {
            var key = keyUtf8.ToArray();
            buffer.Advance(keyUtf8.Length);
            (members ??= new MessagePackUnknownMembers(isMap: true)).AddStringKeyed(key, buffer.ReadRaw());
        }

        /// <summary>Captures one unknown int-keyed member (an array element beyond the declared keys) into <paramref name="members"/> (created on first capture).</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void CaptureUnknownIntKeyed(ref MessagePackUnknownMembers? members, int index)
        {
            (members ??= new MessagePackUnknownMembers(isMap: false)).AddIntKeyed(index, buffer.ReadRaw());
        }

        /// <summary>Captures one unknown int-keyed member at a key hole (an index inside the declared key range with no member) into <paramref name="members"/> (created on first capture). Nil is consumed without capturing: it is the padding serialization regenerates for every hole, so the type's own payloads round trip packet-free. Anything else is data a wider schema wrote at a key this type retired, held for replay at the same index.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void CaptureUnknownIntKeyedHole(ref MessagePackUnknownMembers? members, int index)
        {
            if (buffer.TryReadNil())
            {
                return;
            }
            (members ??= new MessagePackUnknownMembers(isMap: false)).AddIntKeyed(index, buffer.ReadRaw());
        }
    }
}

namespace UltraMessagePack.Formatters;

/// <summary>
/// The default formatter for <see cref="object"/>.
/// serializes by RUNTIME type (primitives, string, byte[], DateTime, enums as their
/// underlying integer, then IDictionary as a map / ICollection as an array of objects)
/// and deserializes by WIRE type (integers come back as the exact width the code names,
/// maps as Dictionary&lt;object, object?&gt;, arrays as object[]).
/// </summary>
public sealed partial class PrimitiveObjectFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?>
{
    IEqualityComparer<object>? comparer;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        if (resolver.HashFloodingResistant)
        {
            comparer = HashFloodingResistantEqualityComparer.Get<object>();
        }
    }
    
    // Everything is written/read via DIRECT buffer calls, deliberately bypassing the resolver even for string/DateTime.

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, object? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var t = value.GetType();

        // GetTypeCode reports an enum as its underlying type., folding v3's separate enum
        // Integer types use the FORCED-width writers so the wire code
        // names the exact .NET type and the value roundtrips as that type.
        switch (Type.GetTypeCode(t))
        {
            case TypeCode.Boolean: buffer.WriteBoolean((bool)value); return;
            case TypeCode.Char: buffer.WriteChar((char)value); return;
            case TypeCode.SByte: buffer.WriteForcedInt8((sbyte)value); return;
            case TypeCode.Byte: buffer.WriteForcedUInt8((byte)value); return;
            case TypeCode.Int16: buffer.WriteForcedInt16((short)value); return;
            case TypeCode.UInt16: buffer.WriteForcedUInt16((ushort)value); return;
            case TypeCode.Int32: buffer.WriteForcedInt32((int)value); return;
            case TypeCode.UInt32: buffer.WriteForcedUInt32((uint)value); return;
            case TypeCode.Int64: buffer.WriteForcedInt64((long)value); return;
            case TypeCode.UInt64: buffer.WriteForcedUInt64((ulong)value); return;
            case TypeCode.Single: buffer.WriteSingle((float)value); return;
            case TypeCode.Double: buffer.WriteDouble((double)value); return;
            case TypeCode.DateTime: buffer.WriteTimestamp((DateTime)value); return;
            case TypeCode.String: buffer.WriteString((string)value); return;
            default: break;
        }

        if (t == typeof(byte[]))
        {
            buffer.WriteBinary((byte[])value);
            return;
        }

        if (value is System.Collections.IDictionary dictionary) // IDictionary before ICollection: it implements both
        {
            state.Enter();
            buffer.WriteMapHeader(dictionary.Count);
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                Serialize(ref buffer, ref state, entry.Key);
                Serialize(ref buffer, ref state, entry.Value);
            }
            state.Exit();
            return;
        }

        // string-keyed generic dictionaries without the non-generic interface(ExpandoObject) and friends.
        if (value is IDictionary<string, object?> stringMap)
        {
            state.Enter();
            buffer.WriteMapHeader(stringMap.Count);
            foreach (var entry in stringMap)
            {
                buffer.WriteString(entry.Key);
                Serialize(ref buffer, ref state, entry.Value);
            }
            state.Exit();
            return;
        }

        if (value is System.Collections.ICollection collection)
        {
            state.Enter();
            buffer.WriteArrayHeader(collection.Count);
            foreach (var item in collection)
            {
                Serialize(ref buffer, ref state, item);
            }
            state.Exit();
            return;
        }

        throw new MessagePackSerializationException("Not supported primitive object resolver. type:" + t.Name);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref object? value)
    {
        if (!buffer.TryPeek(out var code))
        {
            throw new MessagePackSerializationException("Truncated MessagePack data reading object.");
        }

        if (code <= MessagePackCode.MaxFixInt)
        {
            value = buffer.ReadByte(); // positive fixint reads as byte (v3 rule)
            return;
        }
        if (code >= MessagePackCode.MinNegativeFixInt)
        {
            value = buffer.ReadSByte();
            return;
        }
        if (code < MessagePackCode.MinFixArray)
        {
            value = DeserializeMap(ref buffer, ref state); // fixmap
            return;
        }
        if (code < MessagePackCode.MinFixStr)
        {
            value = DeserializeArray(ref buffer, ref state); // fixarray
            return;
        }
        if (code < MessagePackCode.Nil)
        {
            value = buffer.ReadString(); // fixstr
            return;
        }

        switch (code)
        {
            case MessagePackCode.Nil:
                buffer.TryReadNil();
                value = null;
                return;
            case MessagePackCode.False:
            case MessagePackCode.True:
                value = buffer.ReadBoolean();
                return;
            case MessagePackCode.Int8: value = buffer.ReadSByte(); return;
            case MessagePackCode.Int16: value = buffer.ReadInt16(); return;
            case MessagePackCode.Int32: value = buffer.ReadInt32(); return;
            case MessagePackCode.Int64: value = buffer.ReadInt64(); return;
            case MessagePackCode.UInt8: value = buffer.ReadByte(); return;
            case MessagePackCode.UInt16: value = buffer.ReadUInt16(); return;
            case MessagePackCode.UInt32: value = buffer.ReadUInt32(); return;
            case MessagePackCode.UInt64: value = buffer.ReadUInt64(); return;
            case MessagePackCode.Float32: value = buffer.ReadSingle(); return;
            case MessagePackCode.Float64: value = buffer.ReadDouble(); return;
            case MessagePackCode.Str8:
            case MessagePackCode.Str16:
            case MessagePackCode.Str32:
                value = buffer.ReadString();
                return;
            case MessagePackCode.Bin8:
            case MessagePackCode.Bin16:
            case MessagePackCode.Bin32:
                value = buffer.ReadBinary();
                return;
            case MessagePackCode.FixExt1:
            case MessagePackCode.FixExt2:
            case MessagePackCode.FixExt4:
            case MessagePackCode.FixExt8:
            case MessagePackCode.FixExt16:
            case MessagePackCode.Ext8:
            case MessagePackCode.Ext16:
            case MessagePackCode.Ext32:
                // timestamp is the only ext this formatter understands; ReadTimestamp
                // rejects any other type code as malformed data
                value = buffer.ReadTimestamp();
                return;
            case MessagePackCode.Array16:
            case MessagePackCode.Array32:
                value = DeserializeArray(ref buffer, ref state);
                return;
            case MessagePackCode.Map16:
            case MessagePackCode.Map32:
                value = DeserializeMap(ref buffer, ref state);
                return;
            default: // 0xc1 (never used)
                throw new MessagePackSerializationException($"Unrecognized MessagePack code 0x{code:x2} reading object.");
        }
    }

    object DeserializeArray(ref TReadBuffer buffer, ref DeserializeState state)
    {
        var length = buffer.ReadArrayHeader();
        if (length == 0)
        {
            return Array.Empty<object?>();
        }
        var array = new object?[length];
        state.Enter();
        for (int i = 0; i < array.Length; i++)
        {
            Deserialize(ref buffer, ref state, ref array[i]);
        }
        state.Exit();
        return array;
    }

    object DeserializeMap(ref TReadBuffer buffer, ref DeserializeState state)
    {
        var length = buffer.ReadMapHeader();
        var dictionary = comparer == null
            ? new Dictionary<object, object?>(length)
            : new Dictionary<object, object?>(length, comparer);
        state.Enter();
        for (int i = 0; i < length; i++)
        {
            object? key = null;
            object? val = null;
            Deserialize(ref buffer, ref state, ref key);
            Deserialize(ref buffer, ref state, ref val);
            try
            {
                dictionary.Add(key!, val); // nil or duplicate keys are data errors, wrapped below
            }
            catch (ArgumentException ex)
            {
                throw new MessagePackSerializationException("Invalid map key in object payload (nil or duplicate).", ex);
            }
        }
        state.Exit();
        return dictionary;
    }
}

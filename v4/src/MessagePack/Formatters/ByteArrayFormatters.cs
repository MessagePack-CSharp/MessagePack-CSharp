namespace MessagePack.Formatters;

// bin-format views over byte payloads: PrimitiveFormatterFactory claims these BEFORE the
// generic collection tier resolves Array<>/ArraySegment<>/Memory<>/ReadOnlyMemory<>/ReadOnlySequence<>,
// so they keep v3's bin wire format instead of becoming an array of integers.

public sealed partial class ByteArrayFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, byte[]?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, byte[]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteBinary(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref byte[]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        // compatible-path: v3 also read byte[] from the array-of-integers wire form
        // (payloads from producers that wrote byte arrays as arrays); each element must
        // fit a byte - ReadByte rejects anything wider, same as v3
        if (buffer.TryPeek(out var code) &&
            ((code >= MessagePackCode.MinFixArray && code <= MessagePackCode.MinFixArray + MessagePackRange.MaxFixArrayCount) || code == MessagePackCode.Array16 || code == MessagePackCode.Array32))
        {
            var count = buffer.ReadArrayHeader();
            if (count == 0)
            {
                value = []; // the singleton empty array, same as v3
                return;
            }
            var result = new byte[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = buffer.ReadByte();
            }
            value = result;
            return;
        }
        value = buffer.ReadBinary();
    }
}

public sealed partial class ByteArraySegmentFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ArraySegment<byte>>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ArraySegment<byte> value)
    {
        if (value.Array == null) // default segment = nil (v3 wire behavior)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteBinary(value.AsSpan());
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ArraySegment<byte> value)
    {
        value = buffer.TryReadNil() ? default : new ArraySegment<byte>(buffer.ReadBinary());
    }
}

public sealed partial class ByteMemoryFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Memory<byte>>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Memory<byte> value)
    {
        buffer.WriteBinary(value.Span); // Memory has no null: default = empty bin
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Memory<byte> value)
    {
        value = buffer.TryReadNil() ? default : buffer.ReadBinary();
    }
}

public sealed partial class ByteReadOnlyMemoryFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ReadOnlyMemory<byte>>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ReadOnlyMemory<byte> value)
    {
        buffer.WriteBinary(value.Span);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ReadOnlyMemory<byte> value)
    {
        value = buffer.TryReadNil() ? default : buffer.ReadBinary();
    }
}

public sealed partial class ByteReadOnlySequenceFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ReadOnlySequence<byte>>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ReadOnlySequence<byte> value)
    {
        // struct with no null state: default = empty bin (same contract as Memory<byte>)
        buffer.WriteBinary(value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ReadOnlySequence<byte> value)
    {
        value = buffer.TryReadNil() ? default : new ReadOnlySequence<byte>(buffer.ReadBinary());
    }
}

// special formatter to maintain compatibility due to bugs, https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues/2134
// List<byte> is written in array-format from v1, not bin.
// This is a workaround to read data that was mistakenly written in bin-format as List<byte> during a certain period in v3
// Also, to keep compatibility with the serialize format up to v3, we will continue to use array-format.
public sealed partial class ByteListFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, List<byte>?>
{
    readonly ListFormatter<TWriteBuffer, TReadBuffer, byte> inner = new();

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        inner.Initialize(resolver);
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, List<byte>? value)
    {
        inner.Serialize(ref buffer, ref state, value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref List<byte>? value)
    {
        // compatible-path: if the data was written in bin-format, read it as such and convert to List<byte>
        if (buffer.TryPeek(out var code) && code is MessagePackCode.Bin8 or MessagePackCode.Bin16 or MessagePackCode.Bin32)
        {
            var bytes = buffer.ReadBinary();
#if NET9_0_OR_GREATER
            var result = value ?? new List<byte>(bytes.Length);
            CollectionsMarshal.SetCount(result, bytes.Length);
            bytes.CopyTo(CollectionsMarshal.AsSpan(result));
#else
            List<byte> result;
            if (value != null)
            {
                result = value;
                result.Clear();
                result.AddRange(bytes);
            }
            else
            {
                result = new List<byte>(bytes);
            }
#endif
            value = result;
            return;
        }
        else
        {
            // regular-path: if the data was written in array-format
            inner.Deserialize(ref buffer, ref state, ref value);
        }
    }
}
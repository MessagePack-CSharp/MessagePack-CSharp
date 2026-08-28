// v3-parity multi-dimensional arrays: a (rank+1)-element outer array holding each
// dimension length followed by the flat element array in row-major order —
// T[,]   → [len0, len1, [elements]]
// T[,,]  → [len0, len1, len2, [elements]]
// T[,,,] → [len0, len1, len2, len3, [elements]]
// Rank > 4 stays unsupported (v3-parity). The element-array header is bomb-guarded by
// ReadArrayHeader and must equal the checked product of the dimension lengths, so the
// multi-dim allocation is always bounded by actual payload bytes.

namespace MessagePack.Formatters;

public sealed partial class TwoDimensionalArrayFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T[,]?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T[,]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var len0 = value.GetLength(0);
        var len1 = value.GetLength(1);

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(3);
        buffer.WriteInt32(len0);
        buffer.WriteInt32(len1);
        buffer.WriteArrayHeader(value.Length);

        for (int x = 0; x < len0; x++)
        {
            for (int y = 0; y < len1; y++)
            {
                f.Serialize(ref buffer, ref state, value[x, y]);
            }
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T[,]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var header = buffer.ReadArrayHeader();
        if (header != 3)
        {
            throw new MessagePackSerializationException($"Invalid T[,] format: expected a 3-element array, got {header}.");
        }

        var len0 = buffer.ReadInt32();
        var len1 = buffer.ReadInt32();
        var count = buffer.ReadArrayHeader(); // bomb-guarded against BytesRemaining
        if (len0 < 0 || len1 < 0 || count != checked(len0 * len1))
        {
            throw new MessagePackSerializationException($"Invalid T[,] format: {len0}x{len1} does not match {count} elements.");
        }

        // Populate contract: reuse only when every dimension matches
        var result = (value != null && value.GetLength(0) == len0 && value.GetLength(1) == len1)
            ? value
            : new T[len0, len1];

        var f = formatter;
        state.Enter();

        for (int x = 0; x < len0; x++)
        {
            for (int y = 0; y < len1; y++)
            {
                f.Deserialize(ref buffer, ref state, ref result[x, y]);
            }
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class TwoDimensionalArrayFormatterFactory<T> : MessagePackFormatterFactory
{
    // one method, two signatures: net9+ overrides the base virtual (constraints
    // inherited); downlevel has no base member, so the constraints are spelled out
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(T[,]))
        {
            return new TwoDimensionalArrayFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class ThreeDimensionalArrayFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T[,,]?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T[,,]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var len0 = value.GetLength(0);
        var len1 = value.GetLength(1);
        var len2 = value.GetLength(2);

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(4);
        buffer.WriteInt32(len0);
        buffer.WriteInt32(len1);
        buffer.WriteInt32(len2);
        buffer.WriteArrayHeader(value.Length);

        for (int x = 0; x < len0; x++)
        {
            for (int y = 0; y < len1; y++)
            {
                for (int z = 0; z < len2; z++)
                {
                    f.Serialize(ref buffer, ref state, value[x, y, z]);
                }
            }
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T[,,]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var header = buffer.ReadArrayHeader();
        if (header != 4)
        {
            throw new MessagePackSerializationException($"Invalid T[,,] format: expected a 4-element array, got {header}.");
        }

        var len0 = buffer.ReadInt32();
        var len1 = buffer.ReadInt32();
        var len2 = buffer.ReadInt32();
        var count = buffer.ReadArrayHeader();
        if (len0 < 0 || len1 < 0 || len2 < 0 || count != checked(len0 * len1 * len2))
        {
            throw new MessagePackSerializationException($"Invalid T[,,] format: {len0}x{len1}x{len2} does not match {count} elements.");
        }

        var result = (value != null && value.GetLength(0) == len0 && value.GetLength(1) == len1 && value.GetLength(2) == len2)
            ? value
            : new T[len0, len1, len2];

        var f = formatter;
        state.Enter();

        for (int x = 0; x < len0; x++)
        {
            for (int y = 0; y < len1; y++)
            {
                for (int z = 0; z < len2; z++)
                {
                    f.Deserialize(ref buffer, ref state, ref result[x, y, z]);
                }
            }
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class ThreeDimensionalArrayFormatterFactory<T> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(T[,,]))
        {
            return new ThreeDimensionalArrayFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public sealed partial class FourDimensionalArrayFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T[,,,]?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T[,,,]? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var len0 = value.GetLength(0);
        var len1 = value.GetLength(1);
        var len2 = value.GetLength(2);
        var len3 = value.GetLength(3);

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(5);
        buffer.WriteInt32(len0);
        buffer.WriteInt32(len1);
        buffer.WriteInt32(len2);
        buffer.WriteInt32(len3);
        buffer.WriteArrayHeader(value.Length);

        for (int x = 0; x < len0; x++)
        {
            for (int y = 0; y < len1; y++)
            {
                for (int z = 0; z < len2; z++)
                {
                    for (int w = 0; w < len3; w++)
                    {
                        f.Serialize(ref buffer, ref state, value[x, y, z, w]);
                    }
                }
            }
        }

        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T[,,,]? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var header = buffer.ReadArrayHeader();
        if (header != 5)
        {
            throw new MessagePackSerializationException($"Invalid T[,,,] format: expected a 5-element array, got {header}.");
        }

        var len0 = buffer.ReadInt32();
        var len1 = buffer.ReadInt32();
        var len2 = buffer.ReadInt32();
        var len3 = buffer.ReadInt32();
        var count = buffer.ReadArrayHeader();
        if (len0 < 0 || len1 < 0 || len2 < 0 || len3 < 0 || count != checked(len0 * len1 * len2 * len3))
        {
            throw new MessagePackSerializationException($"Invalid T[,,,] format: {len0}x{len1}x{len2}x{len3} does not match {count} elements.");
        }

        var result = (value != null && value.GetLength(0) == len0 && value.GetLength(1) == len1 && value.GetLength(2) == len2 && value.GetLength(3) == len3)
            ? value
            : new T[len0, len1, len2, len3];

        var f = formatter;
        state.Enter();

        for (int x = 0; x < len0; x++)
        {
            for (int y = 0; y < len1; y++)
            {
                for (int z = 0; z < len2; z++)
                {
                    for (int w = 0; w < len3; w++)
                    {
                        f.Deserialize(ref buffer, ref state, ref result[x, y, z, w]);
                    }
                }
            }
        }

        value = result;
        state.Exit();
    }
}

public sealed partial class FourDimensionalArrayFormatterFactory<T> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(T[,,,]))
        {
            return new FourDimensionalArrayFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

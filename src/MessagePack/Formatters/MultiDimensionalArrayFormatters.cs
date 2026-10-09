// v3-parity multi-dimensional arrays: a (rank+1)-element outer array holding each
// dimension length followed by the flat element array in row-major order
// T[,]   → [len0, len1, [elements]]
// T[,,]  → [len0, len1, len2, [elements]]
// T[,,,] → [len0, len1, len2, len3, [elements]]

namespace MessagePack.Formatters;

// The element count must equal the product of the dimension lengths.
static class DimensionProduct
{
    // A cheap pre-filter, not the authority. It mirrors CoreCLR's rule for what it will construct (probed on
    // .NET 11): every dimension at most Array.MaxLength (0x7FFFFFC7) and the running product, taken left to right, at
    // most uint.MaxValue, a zero dimension zeroing it. So int[50000, 50000, 0] and int[2147483591, 0] exist there
    // and read back, while int[70000, 70000, 0] and int[2147483647, 0] do not and are rejected here as a format error
    // before `new T[...]`. The rule is an implementation detail that differs per runtime (Native AOT refuses an
    // intermediate product above int.MaxValue, Mono and IL2CPP have their own), so whatever passes here is still
    // constructed through New, which turns the runtime's own refusal into the same format error.
    const long MaxDimension = 0x7FFFFFC7;

    // `new T[...]` for dimensions the pre-filter accepted: a runtime that refuses the shape throws OutOfMemoryException
    // (or an overflow / range error) synchronously for the request itself, before any memory is used, and that is a
    // data error here, not a memory condition. Only element-free shapes can reach this with large dimensions (a shape
    // with elements is bounded by the message's element and byte budgets first).
    public static T[,] New<T>(int len0, int len1)
    {
        try
        {
            return new T[len0, len1];
        }
        catch (Exception e) when (e is OutOfMemoryException or OverflowException or ArgumentOutOfRangeException)
        {
            throw Unconstructible(e, len0, len1);
        }
    }

    public static T[,,] New<T>(int len0, int len1, int len2)
    {
        try
        {
            return new T[len0, len1, len2];
        }
        catch (Exception e) when (e is OutOfMemoryException or OverflowException or ArgumentOutOfRangeException)
        {
            throw Unconstructible(e, len0, len1, len2);
        }
    }

    public static T[,,,] New<T>(int len0, int len1, int len2, int len3)
    {
        try
        {
            return new T[len0, len1, len2, len3];
        }
        catch (Exception e) when (e is OutOfMemoryException or OverflowException or ArgumentOutOfRangeException)
        {
            throw Unconstructible(e, len0, len1, len2, len3);
        }
    }

    static MessagePackSerializationException Unconstructible(Exception inner, params int[] lengths)
        => new($"Invalid multi-dimensional array format: this runtime cannot construct an array of dimensions {string.Join("x", lengths)} ({inner.GetType().Name}).", inner);

    static bool Matches(int count, ReadOnlySpan<int> lengths)
    {
        long product = 1;
        foreach (var length in lengths)
        {
            if (length < 0 || length > MaxDimension)
            {
                return false;
            }
            product *= length;
            if (product > uint.MaxValue)
            {
                return false;
            }
        }
        return count == product;
    }

    public static bool Matches(int count, int len0, int len1)
        => Matches(count, [len0, len1]);

    public static bool Matches(int count, int len0, int len1, int len2)
        => Matches(count, [len0, len1, len2]);

    public static bool Matches(int count, int len0, int len1, int len2, int len3)
        => Matches(count, [len0, len1, len2, len3]);
}

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
        var lb0 = value.GetLowerBound(0); // a COM / Excel array can start at 1; the wire carries lengths only, and the loops add the bound to an offset so an upper bound of int.MaxValue cannot overflow
        var len1 = value.GetLength(1);
        var lb1 = value.GetLowerBound(1); // a COM / Excel array can start at 1; the wire carries lengths only, and the loops add the bound to an offset so an upper bound of int.MaxValue cannot overflow

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(3);
        buffer.WriteInt32(len0);
        buffer.WriteInt32(len1);
        buffer.WriteArrayHeader(value.Length);

        // an empty array can still have huge non-zero dimensions next to a zero one ([2147483591, 0, []] reads
        // instantly), which the loops below would walk in full: nothing to write, so skip them
        if (value.Length == 0)
        {
            state.Exit();
            return;
        }

        for (int x = 0; x < len0; x++)
        {
            for (int y = 0; y < len1; y++)
            {
                f.Serialize(ref buffer, ref state, value[lb0 + x, lb1 + y]);
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

        var header = buffer.ReadArrayHeader(ref state);
        if (header != 3)
        {
            throw new MessagePackSerializationException($"Invalid T[,] format: expected a 3-element array, got {header}.");
        }

        var len0 = buffer.ReadInt32();
        var len1 = buffer.ReadInt32();
        var count = buffer.ReadArrayHeader(ref state); // bomb-guarded against BytesRemaining
        if (len0 < 0 || len1 < 0 || !DimensionProduct.Matches(count, len0, len1))
        {
            throw new MessagePackSerializationException($"Invalid T[,] format: {len0}x{len1} does not match {count} elements.");
        }
        state.Enter();

        // Populate contract: reuse only when every dimension matches (then nothing is allocated and the elements are
        // read into in place below)
        var reusable = value != null && value.GetLength(0) == len0 && value.GetLength(1) == len1;

        // a fresh array whose count passes the byte guards can still claim more memory than its bytes justify (see
        // PresizeCapacity): read the elements through a growing flat array first, then place them, so the allocation
        // follows the bytes
        if (!reusable && ReadBufferExtensions.PresizeCapacity<T>(count, buffer.BytesRemaining) < count)
        {
            var flat = CollectionReads<TWriteBuffer, TReadBuffer, T>.ReadArray(ref buffer, ref state, formatter, count);
            var placed = DimensionProduct.New<T>(len0, len1);
            var k = 0;
            for (int x = 0; x < len0; x++)
            {
                for (int y = 0; y < len1; y++)
                {
                    placed[x, y] = flat[k++];
                }
            }
            value = placed;
            state.Exit();
            return;
        }

        var result = reusable ? value! : DimensionProduct.New<T>(len0, len1);
        var lb0 = result.GetLowerBound(0); // a reused array keeps its own lower bounds (a fresh one is zero-based)
        var lb1 = result.GetLowerBound(1); // a reused array keeps its own lower bounds (a fresh one is zero-based)

        // a zero-element array can still declare a huge non-zero dimension next to a zero one, which the loops below would
        // walk in full: nothing to read, so skip them
        if (count == 0)
        {
            value = result;
            state.Exit();
            return;
        }

        var f = formatter;

        for (int x = 0; x < len0; x++)
        {
            for (int y = 0; y < len1; y++)
            {
                f.Deserialize(ref buffer, ref state, ref result[lb0 + x, lb1 + y]);
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
        var lb0 = value.GetLowerBound(0); // a COM / Excel array can start at 1; the wire carries lengths only, and the loops add the bound to an offset so an upper bound of int.MaxValue cannot overflow
        var len1 = value.GetLength(1);
        var lb1 = value.GetLowerBound(1); // a COM / Excel array can start at 1; the wire carries lengths only, and the loops add the bound to an offset so an upper bound of int.MaxValue cannot overflow
        var len2 = value.GetLength(2);
        var lb2 = value.GetLowerBound(2); // a COM / Excel array can start at 1; the wire carries lengths only, and the loops add the bound to an offset so an upper bound of int.MaxValue cannot overflow

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(4);
        buffer.WriteInt32(len0);
        buffer.WriteInt32(len1);
        buffer.WriteInt32(len2);
        buffer.WriteArrayHeader(value.Length);

        // an empty array can still have huge non-zero dimensions next to a zero one ([2147483591, 0, []] reads
        // instantly), which the loops below would walk in full: nothing to write, so skip them
        if (value.Length == 0)
        {
            state.Exit();
            return;
        }

        for (int x = 0; x < len0; x++)
        {
            for (int y = 0; y < len1; y++)
            {
                for (int z = 0; z < len2; z++)
                {
                    f.Serialize(ref buffer, ref state, value[lb0 + x, lb1 + y, lb2 + z]);
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

        var header = buffer.ReadArrayHeader(ref state);
        if (header != 4)
        {
            throw new MessagePackSerializationException($"Invalid T[,,] format: expected a 4-element array, got {header}.");
        }

        var len0 = buffer.ReadInt32();
        var len1 = buffer.ReadInt32();
        var len2 = buffer.ReadInt32();
        var count = buffer.ReadArrayHeader(ref state);
        if (len0 < 0 || len1 < 0 || len2 < 0 || !DimensionProduct.Matches(count, len0, len1, len2))
        {
            throw new MessagePackSerializationException($"Invalid T[,,] format: {len0}x{len1}x{len2} does not match {count} elements.");
        }
        state.Enter();

        // Populate contract: reuse only when every dimension matches (then nothing is allocated and the elements are
        // read into in place below)
        var reusable = value != null && value.GetLength(0) == len0 && value.GetLength(1) == len1 && value.GetLength(2) == len2;

        // a fresh array whose count passes the byte guards can still claim more memory than its bytes justify (see
        // PresizeCapacity): read the elements through a growing flat array first, then place them, so the allocation
        // follows the bytes
        if (!reusable && ReadBufferExtensions.PresizeCapacity<T>(count, buffer.BytesRemaining) < count)
        {
            var flat = CollectionReads<TWriteBuffer, TReadBuffer, T>.ReadArray(ref buffer, ref state, formatter, count);
            var placed = DimensionProduct.New<T>(len0, len1, len2);
            var k = 0;
            for (int x = 0; x < len0; x++)
            {
                for (int y = 0; y < len1; y++)
                {
                    for (int z = 0; z < len2; z++)
                    {
                        placed[x, y, z] = flat[k++];
                    }
                }
            }
            value = placed;
            state.Exit();
            return;
        }

        var result = reusable ? value! : DimensionProduct.New<T>(len0, len1, len2);
        var lb0 = result.GetLowerBound(0); // a reused array keeps its own lower bounds (a fresh one is zero-based)
        var lb1 = result.GetLowerBound(1); // a reused array keeps its own lower bounds (a fresh one is zero-based)
        var lb2 = result.GetLowerBound(2); // a reused array keeps its own lower bounds (a fresh one is zero-based)

        if (count == 0)
        {
            value = result;
            state.Exit();
            return;
        }

        var f = formatter;

        for (int x = 0; x < len0; x++)
        {
            for (int y = 0; y < len1; y++)
            {
                for (int z = 0; z < len2; z++)
                {
                    f.Deserialize(ref buffer, ref state, ref result[lb0 + x, lb1 + y, lb2 + z]);
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
        var lb0 = value.GetLowerBound(0); // a COM / Excel array can start at 1; the wire carries lengths only, and the loops add the bound to an offset so an upper bound of int.MaxValue cannot overflow
        var len1 = value.GetLength(1);
        var lb1 = value.GetLowerBound(1); // a COM / Excel array can start at 1; the wire carries lengths only, and the loops add the bound to an offset so an upper bound of int.MaxValue cannot overflow
        var len2 = value.GetLength(2);
        var lb2 = value.GetLowerBound(2); // a COM / Excel array can start at 1; the wire carries lengths only, and the loops add the bound to an offset so an upper bound of int.MaxValue cannot overflow
        var len3 = value.GetLength(3);
        var lb3 = value.GetLowerBound(3); // a COM / Excel array can start at 1; the wire carries lengths only, and the loops add the bound to an offset so an upper bound of int.MaxValue cannot overflow

        var f = formatter;
        state.Enter();

        buffer.WriteArrayHeader(5);
        buffer.WriteInt32(len0);
        buffer.WriteInt32(len1);
        buffer.WriteInt32(len2);
        buffer.WriteInt32(len3);
        buffer.WriteArrayHeader(value.Length);

        // an empty array can still have huge non-zero dimensions next to a zero one ([2147483591, 0, []] reads
        // instantly), which the loops below would walk in full: nothing to write, so skip them
        if (value.Length == 0)
        {
            state.Exit();
            return;
        }

        for (int x = 0; x < len0; x++)
        {
            for (int y = 0; y < len1; y++)
            {
                for (int z = 0; z < len2; z++)
                {
                    for (int w = 0; w < len3; w++)
                    {
                        f.Serialize(ref buffer, ref state, value[lb0 + x, lb1 + y, lb2 + z, lb3 + w]);
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

        var header = buffer.ReadArrayHeader(ref state);
        if (header != 5)
        {
            throw new MessagePackSerializationException($"Invalid T[,,,] format: expected a 5-element array, got {header}.");
        }

        var len0 = buffer.ReadInt32();
        var len1 = buffer.ReadInt32();
        var len2 = buffer.ReadInt32();
        var len3 = buffer.ReadInt32();
        var count = buffer.ReadArrayHeader(ref state);
        if (len0 < 0 || len1 < 0 || len2 < 0 || len3 < 0 || !DimensionProduct.Matches(count, len0, len1, len2, len3))
        {
            throw new MessagePackSerializationException($"Invalid T[,,,] format: {len0}x{len1}x{len2}x{len3} does not match {count} elements.");
        }
        state.Enter();

        // Populate contract: reuse only when every dimension matches (then nothing is allocated and the elements are
        // read into in place below)
        var reusable = value != null && value.GetLength(0) == len0 && value.GetLength(1) == len1 && value.GetLength(2) == len2 && value.GetLength(3) == len3;

        // a fresh array whose count passes the byte guards can still claim more memory than its bytes justify (see
        // PresizeCapacity): read the elements through a growing flat array first, then place them, so the allocation
        // follows the bytes
        if (!reusable && ReadBufferExtensions.PresizeCapacity<T>(count, buffer.BytesRemaining) < count)
        {
            var flat = CollectionReads<TWriteBuffer, TReadBuffer, T>.ReadArray(ref buffer, ref state, formatter, count);
            var placed = DimensionProduct.New<T>(len0, len1, len2, len3);
            var k = 0;
            for (int x = 0; x < len0; x++)
            {
                for (int y = 0; y < len1; y++)
                {
                    for (int z = 0; z < len2; z++)
                    {
                        for (int w = 0; w < len3; w++)
                        {
                            placed[x, y, z, w] = flat[k++];
                        }
                    }
                }
            }
            value = placed;
            state.Exit();
            return;
        }

        var result = reusable ? value! : DimensionProduct.New<T>(len0, len1, len2, len3);
        var lb0 = result.GetLowerBound(0); // a reused array keeps its own lower bounds (a fresh one is zero-based)
        var lb1 = result.GetLowerBound(1); // a reused array keeps its own lower bounds (a fresh one is zero-based)
        var lb2 = result.GetLowerBound(2); // a reused array keeps its own lower bounds (a fresh one is zero-based)
        var lb3 = result.GetLowerBound(3); // a reused array keeps its own lower bounds (a fresh one is zero-based)

        if (count == 0)
        {
            value = result;
            state.Exit();
            return;
        }

        var f = formatter;

        for (int x = 0; x < len0; x++)
        {
            for (int y = 0; y < len1; y++)
            {
                for (int z = 0; z < len2; z++)
                {
                    for (int w = 0; w < len3; w++)
                    {
                        f.Deserialize(ref buffer, ref state, ref result[lb0 + x, lb1 + y, lb2 + z, lb3 + w]);
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

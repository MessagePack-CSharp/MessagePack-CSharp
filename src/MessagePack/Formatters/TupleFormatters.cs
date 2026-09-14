namespace MessagePack.Formatters;

// nil for null, otherwise array[N] of items.
// Tuple items are get-only, so Deserialize collects locals and constructs.
// Created by GenericFormatterFactory via TupleFormatterFactory<...>.

public sealed partial class TupleFormatter<TWriteBuffer, TReadBuffer, T1> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Tuple<T1>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T1> formatter1 = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter1 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T1>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Tuple<T1>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteFixArrayHeader(1);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Tuple<T1>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        if (buffer.ReadArrayHeader() != 1)
        {
            throw new MessagePackSerializationException("Invalid Tuple count");
        }
        state.Enter();
        T1 item1 = default!;
        formatter1.Deserialize(ref buffer, ref state, ref item1);
        value = new Tuple<T1>(item1);
        state.Exit();
    }
}

// one method, two signatures: net9+ overrides the base virtual (constraints inherited);
// downlevel has no base member, so the constraints are spelled out (same for every
// factory in this file)
public sealed partial class TupleFormatterFactory<T1> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(Tuple<T1>) ? new TupleFormatter<TWriteBuffer, TReadBuffer, T1>() : null;
    }
}

public sealed partial class TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Tuple<T1, T2>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T1> formatter1 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T2> formatter2 = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter1 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T1>();
        formatter2 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T2>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Tuple<T1, T2>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteFixArrayHeader(2);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        formatter2.Serialize(ref buffer, ref state, value.Item2);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Tuple<T1, T2>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        if (buffer.ReadArrayHeader() != 2)
        {
            throw new MessagePackSerializationException("Invalid Tuple count");
        }
        state.Enter();
        T1 item1 = default!;
        T2 item2 = default!;
        formatter1.Deserialize(ref buffer, ref state, ref item1);
        formatter2.Deserialize(ref buffer, ref state, ref item2);
        value = new Tuple<T1, T2>(item1, item2);
        state.Exit();
    }
}

public sealed partial class TupleFormatterFactory<T1, T2> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(Tuple<T1, T2>) ? new TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2>() : null;
    }
}

public sealed partial class TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Tuple<T1, T2, T3>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T1> formatter1 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T2> formatter2 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T3> formatter3 = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter1 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T1>();
        formatter2 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T2>();
        formatter3 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T3>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Tuple<T1, T2, T3>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteFixArrayHeader(3);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        formatter2.Serialize(ref buffer, ref state, value.Item2);
        formatter3.Serialize(ref buffer, ref state, value.Item3);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Tuple<T1, T2, T3>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        if (buffer.ReadArrayHeader() != 3)
        {
            throw new MessagePackSerializationException("Invalid Tuple count");
        }
        state.Enter();
        T1 item1 = default!;
        T2 item2 = default!;
        T3 item3 = default!;
        formatter1.Deserialize(ref buffer, ref state, ref item1);
        formatter2.Deserialize(ref buffer, ref state, ref item2);
        formatter3.Deserialize(ref buffer, ref state, ref item3);
        value = new Tuple<T1, T2, T3>(item1, item2, item3);
        state.Exit();
    }
}

public sealed partial class TupleFormatterFactory<T1, T2, T3> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(Tuple<T1, T2, T3>) ? new TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3>() : null;
    }
}

public sealed partial class TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Tuple<T1, T2, T3, T4>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T1> formatter1 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T2> formatter2 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T3> formatter3 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T4> formatter4 = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter1 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T1>();
        formatter2 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T2>();
        formatter3 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T3>();
        formatter4 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T4>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Tuple<T1, T2, T3, T4>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteFixArrayHeader(4);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        formatter2.Serialize(ref buffer, ref state, value.Item2);
        formatter3.Serialize(ref buffer, ref state, value.Item3);
        formatter4.Serialize(ref buffer, ref state, value.Item4);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Tuple<T1, T2, T3, T4>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        if (buffer.ReadArrayHeader() != 4)
        {
            throw new MessagePackSerializationException("Invalid Tuple count");
        }
        state.Enter();
        T1 item1 = default!;
        T2 item2 = default!;
        T3 item3 = default!;
        T4 item4 = default!;
        formatter1.Deserialize(ref buffer, ref state, ref item1);
        formatter2.Deserialize(ref buffer, ref state, ref item2);
        formatter3.Deserialize(ref buffer, ref state, ref item3);
        formatter4.Deserialize(ref buffer, ref state, ref item4);
        value = new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        state.Exit();
    }
}

public sealed partial class TupleFormatterFactory<T1, T2, T3, T4> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(Tuple<T1, T2, T3, T4>) ? new TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4>() : null;
    }
}

public sealed partial class TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Tuple<T1, T2, T3, T4, T5>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T1> formatter1 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T2> formatter2 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T3> formatter3 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T4> formatter4 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T5> formatter5 = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter1 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T1>();
        formatter2 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T2>();
        formatter3 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T3>();
        formatter4 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T4>();
        formatter5 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T5>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Tuple<T1, T2, T3, T4, T5>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteFixArrayHeader(5);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        formatter2.Serialize(ref buffer, ref state, value.Item2);
        formatter3.Serialize(ref buffer, ref state, value.Item3);
        formatter4.Serialize(ref buffer, ref state, value.Item4);
        formatter5.Serialize(ref buffer, ref state, value.Item5);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Tuple<T1, T2, T3, T4, T5>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        if (buffer.ReadArrayHeader() != 5)
        {
            throw new MessagePackSerializationException("Invalid Tuple count");
        }
        state.Enter();
        T1 item1 = default!;
        T2 item2 = default!;
        T3 item3 = default!;
        T4 item4 = default!;
        T5 item5 = default!;
        formatter1.Deserialize(ref buffer, ref state, ref item1);
        formatter2.Deserialize(ref buffer, ref state, ref item2);
        formatter3.Deserialize(ref buffer, ref state, ref item3);
        formatter4.Deserialize(ref buffer, ref state, ref item4);
        formatter5.Deserialize(ref buffer, ref state, ref item5);
        value = new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        state.Exit();
    }
}

public sealed partial class TupleFormatterFactory<T1, T2, T3, T4, T5> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(Tuple<T1, T2, T3, T4, T5>) ? new TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5>() : null;
    }
}

public sealed partial class TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5, T6> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Tuple<T1, T2, T3, T4, T5, T6>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T1> formatter1 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T2> formatter2 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T3> formatter3 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T4> formatter4 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T5> formatter5 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T6> formatter6 = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter1 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T1>();
        formatter2 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T2>();
        formatter3 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T3>();
        formatter4 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T4>();
        formatter5 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T5>();
        formatter6 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T6>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Tuple<T1, T2, T3, T4, T5, T6>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteFixArrayHeader(6);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        formatter2.Serialize(ref buffer, ref state, value.Item2);
        formatter3.Serialize(ref buffer, ref state, value.Item3);
        formatter4.Serialize(ref buffer, ref state, value.Item4);
        formatter5.Serialize(ref buffer, ref state, value.Item5);
        formatter6.Serialize(ref buffer, ref state, value.Item6);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Tuple<T1, T2, T3, T4, T5, T6>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        if (buffer.ReadArrayHeader() != 6)
        {
            throw new MessagePackSerializationException("Invalid Tuple count");
        }
        state.Enter();
        T1 item1 = default!;
        T2 item2 = default!;
        T3 item3 = default!;
        T4 item4 = default!;
        T5 item5 = default!;
        T6 item6 = default!;
        formatter1.Deserialize(ref buffer, ref state, ref item1);
        formatter2.Deserialize(ref buffer, ref state, ref item2);
        formatter3.Deserialize(ref buffer, ref state, ref item3);
        formatter4.Deserialize(ref buffer, ref state, ref item4);
        formatter5.Deserialize(ref buffer, ref state, ref item5);
        formatter6.Deserialize(ref buffer, ref state, ref item6);
        value = new Tuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
        state.Exit();
    }
}

public sealed partial class TupleFormatterFactory<T1, T2, T3, T4, T5, T6> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(Tuple<T1, T2, T3, T4, T5, T6>) ? new TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5, T6>() : null;
    }
}

public sealed partial class TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5, T6, T7> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Tuple<T1, T2, T3, T4, T5, T6, T7>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T1> formatter1 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T2> formatter2 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T3> formatter3 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T4> formatter4 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T5> formatter5 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T6> formatter6 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T7> formatter7 = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter1 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T1>();
        formatter2 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T2>();
        formatter3 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T3>();
        formatter4 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T4>();
        formatter5 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T5>();
        formatter6 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T6>();
        formatter7 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T7>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Tuple<T1, T2, T3, T4, T5, T6, T7>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteFixArrayHeader(7);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        formatter2.Serialize(ref buffer, ref state, value.Item2);
        formatter3.Serialize(ref buffer, ref state, value.Item3);
        formatter4.Serialize(ref buffer, ref state, value.Item4);
        formatter5.Serialize(ref buffer, ref state, value.Item5);
        formatter6.Serialize(ref buffer, ref state, value.Item6);
        formatter7.Serialize(ref buffer, ref state, value.Item7);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Tuple<T1, T2, T3, T4, T5, T6, T7>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        if (buffer.ReadArrayHeader() != 7)
        {
            throw new MessagePackSerializationException("Invalid Tuple count");
        }
        state.Enter();
        T1 item1 = default!;
        T2 item2 = default!;
        T3 item3 = default!;
        T4 item4 = default!;
        T5 item5 = default!;
        T6 item6 = default!;
        T7 item7 = default!;
        formatter1.Deserialize(ref buffer, ref state, ref item1);
        formatter2.Deserialize(ref buffer, ref state, ref item2);
        formatter3.Deserialize(ref buffer, ref state, ref item3);
        formatter4.Deserialize(ref buffer, ref state, ref item4);
        formatter5.Deserialize(ref buffer, ref state, ref item5);
        formatter6.Deserialize(ref buffer, ref state, ref item6);
        formatter7.Deserialize(ref buffer, ref state, ref item7);
        value = new Tuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
        state.Exit();
    }
}

public sealed partial class TupleFormatterFactory<T1, T2, T3, T4, T5, T6, T7> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(Tuple<T1, T2, T3, T4, T5, T6, T7>) ? new TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5, T6, T7>() : null;
    }
}

// arity 8 needs `where TRest : notnull` to match the BCL declaration — a manual
// constraint opts out of BufferConstraintsGenerator, so ALL constraints are spelled out
public sealed class TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5, T6, T7, TRest> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>?>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TRest : notnull
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T1> formatter1 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T2> formatter2 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T3> formatter3 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T4> formatter4 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T5> formatter5 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T6> formatter6 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T7> formatter7 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, TRest> formatterRest = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter1 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T1>();
        formatter2 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T2>();
        formatter3 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T3>();
        formatter4 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T4>();
        formatter5 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T5>();
        formatter6 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T6>();
        formatter7 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T7>();
        formatterRest = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TRest>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        buffer.WriteFixArrayHeader(8);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        formatter2.Serialize(ref buffer, ref state, value.Item2);
        formatter3.Serialize(ref buffer, ref state, value.Item3);
        formatter4.Serialize(ref buffer, ref state, value.Item4);
        formatter5.Serialize(ref buffer, ref state, value.Item5);
        formatter6.Serialize(ref buffer, ref state, value.Item6);
        formatter7.Serialize(ref buffer, ref state, value.Item7);
        formatterRest.Serialize(ref buffer, ref state, value.Rest);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        if (buffer.ReadArrayHeader() != 8)
        {
            throw new MessagePackSerializationException("Invalid Tuple count");
        }
        state.Enter();
        T1 item1 = default!;
        T2 item2 = default!;
        T3 item3 = default!;
        T4 item4 = default!;
        T5 item5 = default!;
        T6 item6 = default!;
        T7 item7 = default!;
        TRest rest = default!;
        formatter1.Deserialize(ref buffer, ref state, ref item1);
        formatter2.Deserialize(ref buffer, ref state, ref item2);
        formatter3.Deserialize(ref buffer, ref state, ref item3);
        formatter4.Deserialize(ref buffer, ref state, ref item4);
        formatter5.Deserialize(ref buffer, ref state, ref item5);
        formatter6.Deserialize(ref buffer, ref state, ref item6);
        formatter7.Deserialize(ref buffer, ref state, ref item7);
        formatterRest.Deserialize(ref buffer, ref state, ref rest);
        value = new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
        state.Exit();
    }
}

public sealed partial class TupleFormatterFactory<T1, T2, T3, T4, T5, T6, T7, TRest> : MessagePackFormatterFactory
    where TRest : notnull
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>) ? new TupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5, T6, T7, TRest>() : null;
    }
}

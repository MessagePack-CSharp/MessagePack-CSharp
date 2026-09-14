namespace MessagePack.Formatters;

// array[N] of items, nil is rejected.
// Created by GenericFormatterFactory via TupleFormatterFactory<...>.

public sealed partial class ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ValueTuple<T1>>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T1> formatter1 = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter1 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T1>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ValueTuple<T1> value)
    {
        state.Enter();
        buffer.WriteFixArrayHeader(1);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ValueTuple<T1> value)
    {
        if (buffer.ReadArrayHeader() != 1)
        {
            throw new MessagePackSerializationException("Invalid ValueTuple count");
        }
        state.Enter();
        formatter1.Deserialize(ref buffer, ref state, ref value.Item1);
        state.Exit();
    }
}

// one method, two signatures: net9+ overrides the base virtual (constraints inherited);
// downlevel has no base member, so the constraints are spelled out (same for every
// factory in this file)
public sealed partial class ValueTupleFormatterFactory<T1> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(ValueTuple<T1>) ? new ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1>() : null;
    }
}

public sealed partial class ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ValueTuple<T1, T2>>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T1> formatter1 = null!;
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T2> formatter2 = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter1 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T1>();
        formatter2 = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T2>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ValueTuple<T1, T2> value)
    {
        state.Enter();
        buffer.WriteFixArrayHeader(2);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        formatter2.Serialize(ref buffer, ref state, value.Item2);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ValueTuple<T1, T2> value)
    {
        if (buffer.ReadArrayHeader() != 2)
        {
            throw new MessagePackSerializationException("Invalid ValueTuple count");
        }
        state.Enter();

        formatter1.Deserialize(ref buffer, ref state, ref value.Item1);
        formatter2.Deserialize(ref buffer, ref state, ref value.Item2);
        state.Exit();
    }
}

public sealed partial class ValueTupleFormatterFactory<T1, T2> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(ValueTuple<T1, T2>) ? new ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2>() : null;
    }
}

public sealed partial class ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ValueTuple<T1, T2, T3>>
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ValueTuple<T1, T2, T3> value)
    {
        state.Enter();
        buffer.WriteFixArrayHeader(3);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        formatter2.Serialize(ref buffer, ref state, value.Item2);
        formatter3.Serialize(ref buffer, ref state, value.Item3);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ValueTuple<T1, T2, T3> value)
    {
        if (buffer.ReadArrayHeader() != 3)
        {
            throw new MessagePackSerializationException("Invalid ValueTuple count");
        }
        state.Enter();
        formatter1.Deserialize(ref buffer, ref state, ref value.Item1);
        formatter2.Deserialize(ref buffer, ref state, ref value.Item2);
        formatter3.Deserialize(ref buffer, ref state, ref value.Item3);
        state.Exit();
    }
}

public sealed partial class ValueTupleFormatterFactory<T1, T2, T3> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(ValueTuple<T1, T2, T3>) ? new ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3>() : null;
    }
}

public sealed partial class ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ValueTuple<T1, T2, T3, T4>>
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ValueTuple<T1, T2, T3, T4> value)
    {
        state.Enter();
        buffer.WriteFixArrayHeader(4);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        formatter2.Serialize(ref buffer, ref state, value.Item2);
        formatter3.Serialize(ref buffer, ref state, value.Item3);
        formatter4.Serialize(ref buffer, ref state, value.Item4);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ValueTuple<T1, T2, T3, T4> value)
    {
        if (buffer.ReadArrayHeader() != 4)
        {
            throw new MessagePackSerializationException("Invalid ValueTuple count");
        }
        state.Enter();
        formatter1.Deserialize(ref buffer, ref state, ref value.Item1);
        formatter2.Deserialize(ref buffer, ref state, ref value.Item2);
        formatter3.Deserialize(ref buffer, ref state, ref value.Item3);
        formatter4.Deserialize(ref buffer, ref state, ref value.Item4);
        state.Exit();
    }
}

public sealed partial class ValueTupleFormatterFactory<T1, T2, T3, T4> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(ValueTuple<T1, T2, T3, T4>) ? new ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4>() : null;
    }
}

public sealed partial class ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ValueTuple<T1, T2, T3, T4, T5>>
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ValueTuple<T1, T2, T3, T4, T5> value)
    {
        state.Enter();
        buffer.WriteFixArrayHeader(5);
        formatter1.Serialize(ref buffer, ref state, value.Item1);
        formatter2.Serialize(ref buffer, ref state, value.Item2);
        formatter3.Serialize(ref buffer, ref state, value.Item3);
        formatter4.Serialize(ref buffer, ref state, value.Item4);
        formatter5.Serialize(ref buffer, ref state, value.Item5);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ValueTuple<T1, T2, T3, T4, T5> value)
    {
        if (buffer.ReadArrayHeader() != 5)
        {
            throw new MessagePackSerializationException("Invalid ValueTuple count");
        }
        state.Enter();
        formatter1.Deserialize(ref buffer, ref state, ref value.Item1);
        formatter2.Deserialize(ref buffer, ref state, ref value.Item2);
        formatter3.Deserialize(ref buffer, ref state, ref value.Item3);
        formatter4.Deserialize(ref buffer, ref state, ref value.Item4);
        formatter5.Deserialize(ref buffer, ref state, ref value.Item5);
        state.Exit();
    }
}

public sealed partial class ValueTupleFormatterFactory<T1, T2, T3, T4, T5> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(ValueTuple<T1, T2, T3, T4, T5>) ? new ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5>() : null;
    }
}

public sealed partial class ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5, T6> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ValueTuple<T1, T2, T3, T4, T5, T6>>
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ValueTuple<T1, T2, T3, T4, T5, T6> value)
    {
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ValueTuple<T1, T2, T3, T4, T5, T6> value)
    {
        if (buffer.ReadArrayHeader() != 6)
        {
            throw new MessagePackSerializationException("Invalid ValueTuple count");
        }
        state.Enter();
        formatter1.Deserialize(ref buffer, ref state, ref value.Item1);
        formatter2.Deserialize(ref buffer, ref state, ref value.Item2);
        formatter3.Deserialize(ref buffer, ref state, ref value.Item3);
        formatter4.Deserialize(ref buffer, ref state, ref value.Item4);
        formatter5.Deserialize(ref buffer, ref state, ref value.Item5);
        formatter6.Deserialize(ref buffer, ref state, ref value.Item6);
        state.Exit();
    }
}

public sealed partial class ValueTupleFormatterFactory<T1, T2, T3, T4, T5, T6> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(ValueTuple<T1, T2, T3, T4, T5, T6>) ? new ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5, T6>() : null;
    }
}

public sealed partial class ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5, T6, T7> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ValueTuple<T1, T2, T3, T4, T5, T6, T7> value)
    {
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ValueTuple<T1, T2, T3, T4, T5, T6, T7> value)
    {
        if (buffer.ReadArrayHeader() != 7)
        {
            throw new MessagePackSerializationException("Invalid ValueTuple count");
        }
        state.Enter();
        formatter1.Deserialize(ref buffer, ref state, ref value.Item1);
        formatter2.Deserialize(ref buffer, ref state, ref value.Item2);
        formatter3.Deserialize(ref buffer, ref state, ref value.Item3);
        formatter4.Deserialize(ref buffer, ref state, ref value.Item4);
        formatter5.Deserialize(ref buffer, ref state, ref value.Item5);
        formatter6.Deserialize(ref buffer, ref state, ref value.Item6);
        formatter7.Deserialize(ref buffer, ref state, ref value.Item7);
        state.Exit();
    }
}

public sealed partial class ValueTupleFormatterFactory<T1, T2, T3, T4, T5, T6, T7> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(ValueTuple<T1, T2, T3, T4, T5, T6, T7>) ? new ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5, T6, T7>() : null;
    }
}

// arity 8 needs `where TRest : struct` to name the BCL type — a manual constraint opts
// out of BufferConstraintsGenerator, so ALL constraints are spelled out here (EnumFormatter style)
public sealed class ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5, T6, T7, TRest> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TRest : struct
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> value)
    {
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

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> value)
    {
        if (buffer.ReadArrayHeader() != 8)
        {
            throw new MessagePackSerializationException("Invalid ValueTuple count");
        }
        state.Enter();
        formatter1.Deserialize(ref buffer, ref state, ref value.Item1);
        formatter2.Deserialize(ref buffer, ref state, ref value.Item2);
        formatter3.Deserialize(ref buffer, ref state, ref value.Item3);
        formatter4.Deserialize(ref buffer, ref state, ref value.Item4);
        formatter5.Deserialize(ref buffer, ref state, ref value.Item5);
        formatter6.Deserialize(ref buffer, ref state, ref value.Item6);
        formatter7.Deserialize(ref buffer, ref state, ref value.Item7);
        formatterRest.Deserialize(ref buffer, ref state, ref value.Rest);
        state.Exit();
    }
}

public sealed partial class ValueTupleFormatterFactory<T1, T2, T3, T4, T5, T6, T7, TRest> : MessagePackFormatterFactory
    where TRest : struct
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>) ? new ValueTupleFormatter<TWriteBuffer, TReadBuffer, T1, T2, T3, T4, T5, T6, T7, TRest>() : null;
    }
}

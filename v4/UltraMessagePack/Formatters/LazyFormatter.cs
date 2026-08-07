using System.Diagnostics.CodeAnalysis;

namespace UltraMessagePack.Formatters;

/// <summary>
/// Serializes <see cref="Lazy{T}"/> transparently as its value.
/// Serializing FORCES the lazy evaluation, and deserializing produces an already-evaluated instance.
/// </summary>
public sealed partial class LazyFormatter<TWriteBuffer, TReadBuffer, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Lazy<T>?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Lazy<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        formatter.Serialize(ref buffer, ref state, value.Value);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Lazy<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        state.Enter();
        T item = default!;
        formatter.Deserialize(ref buffer, ref state, ref item);
#if NETSTANDARD2_0
        // ns2.0 has no Lazy<T>(T value) ctor: the closure allocation is downlevel-only
        value = new Lazy<T>(() => item);
#else
        value = new Lazy<T>(item);
#endif
        state.Exit();
    }
}

// one method, two signatures: net9+ overrides the base virtual (constraints inherited);
// downlevel has no base member, so the constraints are spelled out
public sealed partial class LazyFormatterFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(Lazy<T>) ? new LazyFormatter<TWriteBuffer, TReadBuffer, T>() : null;
    }
}
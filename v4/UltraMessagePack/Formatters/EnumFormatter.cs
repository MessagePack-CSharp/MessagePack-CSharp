using SerializerFoundation;
using System.Runtime.CompilerServices;

namespace UltraMessagePack.Formatters;

// Enums serialize as their underlying integer value (MessagePack-CSharp compatible:
// smallest-format signed/unsigned int per the underlying type's writer). One formatter
// per underlying type so the hot path is a reinterpret + direct write with no per-call
// switch; EnumFormatterFactory<T> picks the variant once at resolve time.

public sealed class EnumByteFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where T : struct, Enum
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref T value)
    {
        buffer.WriteByte(Unsafe.As<T, byte>(ref value));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        var underlying = buffer.ReadByte();
        value = Unsafe.As<byte, T>(ref underlying);
    }
}

public sealed class EnumSByteFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where T : struct, Enum
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref T value)
    {
        buffer.WriteSByte(Unsafe.As<T, sbyte>(ref value));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        var underlying = buffer.ReadSByte();
        value = Unsafe.As<sbyte, T>(ref underlying);
    }
}

public sealed class EnumInt16Formatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where T : struct, Enum
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref T value)
    {
        buffer.WriteInt16(Unsafe.As<T, short>(ref value));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        var underlying = buffer.ReadInt16();
        value = Unsafe.As<short, T>(ref underlying);
    }
}

public sealed class EnumUInt16Formatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where T : struct, Enum
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref T value)
    {
        buffer.WriteUInt16(Unsafe.As<T, ushort>(ref value));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        var underlying = buffer.ReadUInt16();
        value = Unsafe.As<ushort, T>(ref underlying);
    }
}

public sealed class EnumInt32Formatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where T : struct, Enum
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref T value)
    {
        buffer.WriteInt32(Unsafe.As<T, int>(ref value));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        var underlying = buffer.ReadInt32();
        value = Unsafe.As<int, T>(ref underlying);
    }
}

public sealed class EnumUInt32Formatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where T : struct, Enum
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref T value)
    {
        buffer.WriteUInt32(Unsafe.As<T, uint>(ref value));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        var underlying = buffer.ReadUInt32();
        value = Unsafe.As<uint, T>(ref underlying);
    }
}

public sealed class EnumInt64Formatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where T : struct, Enum
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref T value)
    {
        buffer.WriteInt64(Unsafe.As<T, long>(ref value));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        var underlying = buffer.ReadInt64();
        value = Unsafe.As<long, T>(ref underlying);
    }
}

public sealed class EnumUInt64Formatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
    where T : struct, Enum
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ref T value)
    {
        buffer.WriteUInt64(Unsafe.As<T, ulong>(ref value));
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        var underlying = buffer.ReadUInt64();
        value = Unsafe.As<ulong, T>(ref underlying);
    }
}

public sealed class EnumFormatterFactory<T> : IMessagePackFormatterFactory
    where T : struct, Enum
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.Byte => new EnumByteFormatter<TWriteBuffer, TReadBuffer, T>(),
            TypeCode.SByte => new EnumSByteFormatter<TWriteBuffer, TReadBuffer, T>(),
            TypeCode.Int16 => new EnumInt16Formatter<TWriteBuffer, TReadBuffer, T>(),
            TypeCode.UInt16 => new EnumUInt16Formatter<TWriteBuffer, TReadBuffer, T>(),
            TypeCode.Int32 => new EnumInt32Formatter<TWriteBuffer, TReadBuffer, T>(),
            TypeCode.UInt32 => new EnumUInt32Formatter<TWriteBuffer, TReadBuffer, T>(),
            TypeCode.Int64 => new EnumInt64Formatter<TWriteBuffer, TReadBuffer, T>(),
            TypeCode.UInt64 => new EnumUInt64Formatter<TWriteBuffer, TReadBuffer, T>(),
            _ => (object?)null,
        };
    }
}

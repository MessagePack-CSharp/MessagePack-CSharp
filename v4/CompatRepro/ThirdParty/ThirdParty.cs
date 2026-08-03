
using SerializerFoundation;
using System;
using UltraMessagePack;

namespace ThirdPartyLib;


public class ThirdPartyFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
    where TWriteBuffer : struct, IWriteBuffer
    where TReadBuffer : struct, IReadBuffer
{

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value)
    {
        Console.WriteLine("Call OK");
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value)
    {
        Console.WriteLine("Call OK");
    }
}

public class ThirdPartyFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        if (writeBufferType == typeof(SerializerFoundation.ArrayPoolListWriteBuffer) && readBufferType == typeof(SerializerFoundation.ReadOnlySpanReadBuffer) && valueType == typeof(int))
        {

        }
        return null;
    }
}

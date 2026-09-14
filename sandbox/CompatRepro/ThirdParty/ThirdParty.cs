
using SerializerFoundation;
using System;
using MessagePack;

namespace ThirdPartyLib;

public class MyPerson
{
    public int MyProperty { get; set; }
}

public class ThirdPartyFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, MyPerson>
    where TWriteBuffer : struct, IWriteBuffer
    where TReadBuffer : struct, IReadBuffer
{

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, MyPerson value)
    {
        Console.WriteLine("Call OK");
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref MyPerson value)
    {
        Console.WriteLine("Call OK");
    }
}

public class ThirdPartyFactory : MessagePackFormatterFactory
{
    public override object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        // generated-factory style: a closed set of known pairs, no MakeGenericType.
        // A downlevel build can only enumerate the non-ref-struct (Compatible*) pairs,
        // so modern ref struct pairs fall through to null.
        if (writeBufferType == typeof(CompatibleArrayPoolListWriteBuffer) && readBufferType == typeof(CompatibleReadOnlySpanReadBuffer) && valueType == typeof(MyPerson))
        {
            return new ThirdPartyFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer>();
        }
        return null;
    }
}
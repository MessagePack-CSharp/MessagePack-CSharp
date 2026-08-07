using System.Net;

namespace UltraMessagePack.Formatters;

// System.Net types.
// IPAddress travels as its canonical string, which — unlike GetAddressBytes — keeps the
// IPv6 scope id ("%1") lossless. IPEndPoint is [addressString, port]: two typed tokens
// instead of one "addr:port" string, so no bracket-parsing on any TFM.

/// <summary>Serializes <see cref="IPAddress"/> as its canonical string (IPv6 scope id included).</summary>
public sealed partial class IPAddressFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IPAddress?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IPAddress? value)
    {
        buffer.WriteString(value?.ToString());
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IPAddress? value)
    {
        var text = buffer.ReadString();
        if (text == null)
        {
            value = null;
            return;
        }
        if (!IPAddress.TryParse(text, out var address))
        {
            throw new MessagePackSerializationException("Can't parse to IPAddress, input string was not in a correct format.");
        }
        value = address;
    }
}

/// <summary>Serializes <see cref="IPEndPoint"/> as [addressString, port].</summary>
public sealed partial class IPEndPointFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, IPEndPoint?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, IPEndPoint? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        buffer.WriteFixArrayHeader(2);
        buffer.WriteString(value.Address.ToString());
        buffer.WriteInt32(value.Port);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref IPEndPoint? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }
        if (buffer.ReadArrayHeader() != 2)
        {
            throw new MessagePackSerializationException("Invalid IPEndPoint format.");
        }
        var text = buffer.ReadString();
        if (text == null || !IPAddress.TryParse(text, out var address))
        {
            throw new MessagePackSerializationException("Can't parse to IPAddress, input string was not in a correct format.");
        }
        var port = buffer.ReadInt32();
        // the ctor rejects out-of-range ports with ArgumentOutOfRangeException; a forged
        // payload can carry any int, so validate as a data error
        if ((uint)port > IPEndPoint.MaxPort)
        {
            throw new MessagePackSerializationException("Invalid IPEndPoint port.");
        }
        value = new IPEndPoint(address, port);
    }
}

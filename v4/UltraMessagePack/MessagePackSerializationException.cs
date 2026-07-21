namespace UltraMessagePack;

// Shape-identical to MessagePack.MessagePackSerializationException (verified against
// MessagePack 3.1.8 via reflection: non-sealed, : Exception, [Serializable], the three
// public constructors plus the protected legacy serialization one). UltraMessagePack is
// slated to become MessagePack for C# v4, so the exception surface must match exactly —
// user code catching by this type must keep working unchanged.
[Serializable]
public class MessagePackSerializationException : Exception
{
    public MessagePackSerializationException()
    {
    }

    public MessagePackSerializationException(string message)
        : base(message)
    {
    }

    public MessagePackSerializationException(string message, Exception inner)
        : base(message, inner)
    {
    }

#pragma warning disable SYSLIB0051 // legacy serialization ctor kept for exact shape parity
    protected MessagePackSerializationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
    {
    }
#pragma warning restore SYSLIB0051
}

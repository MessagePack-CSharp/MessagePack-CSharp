using System.Diagnostics.CodeAnalysis;

namespace MessagePack.Formatters;

// MsgPack104: the AssemblyQualifiedName str is this formatter's wire form for Type, not a string member of the object model.
#pragma warning disable MsgPack104

/// <summary>
/// Serializes <see cref="Type"/> by its <see cref="Type.AssemblyQualifiedName"/>.
/// Opt in via <see cref="TypeFormatterFactory"/> only for trusted data.
/// </summary>
[Obsolete("Deserializing System.Type executes Type.GetType over payload-provided names, which can load assemblies and permanently grow the process. This exists for v3 compatibility with trusted data only; suppress this warning to accept that risk.")]
public sealed partial class TypeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Type?>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Type? value)
    {
        buffer.WriteString(value?.AssemblyQualifiedName);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Type round-trips by AssemblyQualifiedName carried in the payload; trimmed apps must keep the named types rooted themselves (inherent to serializing Type, same as v3)")]
    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Type? value)
    {
        var name = buffer.ReadString();
        if (name == null)
        {
            value = null;
            return;
        }

        // no Try variant exists for Type.GetType, so this is the one catch-based wrap
        Type? type;
        try
        {
            type = Type.GetType(name, throwOnError: false);
        }
        catch (Exception ex)
        {
            throw new MessagePackSerializationException($"Can't load type '{name}'.", ex);
        }
        value = type ?? throw new MessagePackSerializationException($"Can't load type '{name}'.");
    }
}

/// <summary>
/// Opt-in factory for <see cref="TypeFormatter{TWriteBuffer, TReadBuffer}"/> (trusted data only)
/// compose it BEFORE the default chain, e.g. <c>new MessagePackSerializerOptions(new MessagePackFormatterResolver([new TypeFormatterFactory(), MessagePackFormatterFactory.Default]))</c>.
/// </summary>
[Obsolete("Deserializing System.Type executes Type.GetType over payload-provided names, which can load assemblies and permanently grow the process. This exists for v3 compatibility with trusted data only; suppress this warning to accept that risk.")]
public sealed partial class TypeFormatterFactory : MessagePackFormatterFactory
{
    // one method, two signatures: net9+ overrides the base virtual (constraints inherited);
    // downlevel has no base member, so the constraints are spelled out
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(Type) ? new TypeFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}

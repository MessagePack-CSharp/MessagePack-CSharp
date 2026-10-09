using MessagePack.Formatters;
using System.Diagnostics.CodeAnalysis;

namespace MessagePack;

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

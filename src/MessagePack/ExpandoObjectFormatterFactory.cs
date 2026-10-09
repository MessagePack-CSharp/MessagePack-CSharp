using MessagePack.Formatters;
using System.Dynamic;

namespace MessagePack;

/// <summary>
/// Serves <see cref="ExpandoObject"/>, which no default chain does (the [Obsolete] message
/// carries the reasons). Compose it anywhere in a chain, nothing else claims the type:
/// <c>MessagePackFormatterFactory.Combine(new ExpandoObjectFormatterFactory(), MessagePackFormatterFactory.Default)</c>.
/// </summary>
[Obsolete(ExpandoObjectMessages.Deprecated)]
public sealed partial class ExpandoObjectFormatterFactory : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(ExpandoObject) ? new ExpandoObjectFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}

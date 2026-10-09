using MessagePack.Formatters;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using static MessagePack.MessagePackPrimitives;

namespace MessagePack;

/// <summary>
/// Serves <see cref="object"/> with Typeless semantics: concrete type names embedded in the payload.
/// Compose it FIRST (before the tiers that would claim object).
/// </summary>
public sealed partial class TypelessFormatterFactory : MessagePackFormatterFactory
{
    readonly TypelessTypeLoader typeLoader;
    readonly bool omitAssemblyVersion;

    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(TypelessMessages.RequiresUnreferencedCode)]
    public TypelessFormatterFactory(TypelessTypeLoader typeLoader, bool omitAssemblyVersion = false)
    {
        ArgumentNullException.ThrowIfNull(typeLoader);
        this.typeLoader = typeLoader;
        this.omitAssemblyVersion = omitAssemblyVersion;
    }

#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(object))
        {
            return new TypelessFormatter<TWriteBuffer, TReadBuffer>(typeLoader, omitAssemblyVersion);
        }
        return null;
    }
}

using MessagePack.Formatters;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using static MessagePack.MessagePackPrimitives;

namespace MessagePack;

/// <summary>
/// The tail half of typeless composition: claims interface and abstract static types with
/// <see cref="ForceTypelessFormatter{TWriteBuffer, TReadBuffer, T}"/>. Compose it LAST —
/// v3's TypelessObjectResolver sat at the end of its chain for the same reason: the
/// collection interfaces (IList&lt;T&gt;, IDictionary&lt;K,V&gt;, ...) belong to BuiltIn's
/// interface formatters and union roots to their generated formatters; only interfaces and
/// abstract bases nothing else serves fall through to the typeless envelope.
/// </summary>
public sealed class ForceTypelessFormatterFactory : GenericFormatterFactoryBase
{
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(TypelessMessages.RequiresUnreferencedCode)]
    public ForceTypelessFormatterFactory()
    {
    }

    protected override Type? GetOpenFactoryType(Type type, out Type[] typeArguments, out object?[]? constructorArguments)
    {
        typeArguments = [type];
        constructorArguments = null;
        return type.IsInterface || type.IsAbstract ? typeof(ForceTypelessFormatterFactory<>) : null;
    }
}

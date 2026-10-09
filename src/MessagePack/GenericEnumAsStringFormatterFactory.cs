using MessagePack.Formatters;
using SerializerFoundation;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace MessagePack;

public sealed class GenericEnumAsStringFormatterFactory : GenericFormatterFactoryBase
{
    readonly bool ignoreCase;

    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    public GenericEnumAsStringFormatterFactory(bool ignoreCase = false)
    {
        this.ignoreCase = ignoreCase;
    }

    protected override Type? GetOpenFactoryType(Type type, out Type[] typeArguments, out object?[]? constructorArguments)
    {
        typeArguments = [type];
        constructorArguments = [ignoreCase];
        return type.IsEnum ? typeof(EnumAsStringFormatterFactory<>) : null;
    }
}

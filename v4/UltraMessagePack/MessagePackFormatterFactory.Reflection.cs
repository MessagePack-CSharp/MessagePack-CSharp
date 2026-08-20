using System.Diagnostics.CodeAnalysis;
using UltraMessagePack.Formatters;

namespace UltraMessagePack;

/// <summary>
/// Reflection-driven object tier: the runtime equivalent of v3's dynamic resolvers,
/// without whole-formatter Emit. A [MessagePackObject]-annotated type serializes in its
/// keyed wire form (int keys: array with nil holes; string keys or MessagePackObject(true):
/// map), byte-compatible with the source-generated formatter for the same type.
/// With annotatedOnly the tier serves ONLY annotated types — the Default chain's tail,
/// covering annotated types the source generator did not (v3 DynamicObjectResolver
/// semantics). Without it the tier is a catch-all that additionally serves attribute-free
/// objects as maps of member name to value (v3 contractless); compose that form LAST, it
/// claims any concrete class/struct.
/// </summary>
public sealed class ReflectionFormatterFactory : GenericFormatterFactoryBase
{
    // `new`: the inherited MessagePackFormatterFactory const describes the Default chain's
    // annotated tail; this one covers the factory in any composition (WithContractless too)
    internal new const string RequiresUnreferencedCodeMessage =
        "Reflection-based object serialization discovers members, attributes and constructors via reflection at runtime; " +
        "trimming can remove them silently. For trimmed or Native AOT apps use source-generated formatters.";

    readonly bool annotatedOnly;
    readonly bool allowPrivate;

    /// <summary>
    /// annotatedOnly restricts the tier to [MessagePackObject] types (the Default chain's
    /// fallback form); without it attribute-free types are claimed too (contractless).
    /// allowPrivate widens discovery to non-public members, accessors and constructors
    /// (v3's AllowPrivate resolvers). The member names still become part of the wire
    /// contract, so renaming a private field is a breaking change for stored payloads.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public ReflectionFormatterFactory(bool annotatedOnly = false, bool allowPrivate = false)
    {
        this.annotatedOnly = annotatedOnly;
        this.allowPrivate = allowPrivate;
    }

    protected override Type? GetOpenFactoryType(Type type, out Type[] typeArguments, out object?[]? constructorArguments)
    {
        typeArguments = [type];
        constructorArguments = [allowPrivate];

        // decline everything that can never be a reflection-served object; types another
        // tier serves better are expected to be claimed earlier in the chain
        if (type.IsPrimitive
            || type.IsEnum
            || type.IsArray
            || type.IsInterface
            || type.IsAbstract // also covers static classes
            || type.IsPointer
            || type.IsByRef
            || type.ContainsGenericParameters
            || typeof(Delegate).IsAssignableFrom(type)
            || Nullable.GetUnderlyingType(type) != null
            || type == typeof(string)
            || type == typeof(object)
#if !NETSTANDARD2_0
            || type.IsByRefLike
#endif
            )
        {
            return null;
        }

        if (annotatedOnly && !HasMessagePackObjectAttribute(type))
        {
            return null;
        }

        return typeof(ReflectionObjectFormatterFactory<>);
    }

    static bool HasMessagePackObjectAttribute(Type type)
    {
        // attribute DATA is enough here (no instantiation); base-chain walk mirrors the
        // formatter's derived-annotation tolerance
        foreach (var attribute in type.CustomAttributes)
        {
            for (var current = attribute.AttributeType; current is not null; current = current.BaseType)
            {
                if (current.FullName == MessagePackAttributeNames.MessagePackObject)
                {
                    return true;
                }
            }
        }
        return false;
    }
}

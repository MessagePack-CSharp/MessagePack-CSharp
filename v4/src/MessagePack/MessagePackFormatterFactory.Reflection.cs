using System.Diagnostics.CodeAnalysis;
using MessagePack.Formatters;

namespace MessagePack;

/// <summary>
/// Serializes objects through reflection at runtime, the equivalent of v3's dynamic resolvers.
/// A <see cref="MessagePackObjectAttribute"/> type produces the same bytes as its source-generated formatter would.
/// With <c>annotatedOnly</c> the factory serves only annotated types, including <c>[DataContract]</c>.
/// Without it, objects without attributes are also served as maps of member name to value; compose that form last, because it claims any concrete class or struct.
/// </summary>
public sealed class ReflectionFormatterFactory : GenericFormatterFactoryBase
{
    // `new` because the inherited MessagePackFormatterFactory const describes the Default chain's annotated tail,
    // whereas this one covers the factory in any composition (WithContractless too).
    internal new const string RequiresUnreferencedCodeMessage =
        "Reflection-based object serialization discovers members, attributes and constructors via reflection at runtime; " +
        "trimming can remove them silently. For trimmed or Native AOT apps use source-generated formatters.";

    readonly bool annotatedOnly;
    readonly bool allowPrivate;

    /// <summary>
    /// <paramref name="annotatedOnly"/> restricts the factory to annotated types; otherwise types without attributes are served too.
    /// <paramref name="allowPrivate"/> includes non-public members, accessors and constructors.
    /// Private member names still become part of the serialized contract, so renaming one breaks stored data.
    /// </summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    public ReflectionFormatterFactory(bool annotatedOnly = false, bool allowPrivate = false)
    {
        this.annotatedOnly = annotatedOnly;
        this.allowPrivate = allowPrivate;
    }

    /// <inheritdoc/>
    protected override Type? GetOpenFactoryType(Type type, out Type[] typeArguments, out object?[]? constructorArguments)
    {
        typeArguments = [type];
        constructorArguments = [allowPrivate];

        // A [UnionTag]-annotated interface/abstract root (v3's [Union] attribute name is matched too) gets runtime
        // polymorphic dispatch, v3 DynamicUnionResolver's role, for assemblies the source generator never saw,
        // pre-built v3 DLLs included. Claimed before the interface/abstract decline below, and before the typeless
        // tail can wrap it (v3 kept the same order).
        if ((type.IsInterface || type.IsAbstract) && !type.ContainsGenericParameters && HasUnionAnnotation(type))
        {
            typeArguments = [type];
            constructorArguments = null;
            return typeof(ReflectionUnionFormatterFactory<>);
        }

        // Decline everything that can never be a reflection-served object. Types another tier serves better
        // are expected to be claimed earlier in the chain.
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
            // Not "served better elsewhere" but deliberately unserved. A contractless claim would write ExpandoObject
            // (no public members) as an empty map, silent data loss, so declining keeps the miss loud until
            // ExpandoObjectFormatterFactory is composed in.
            || type == typeof(System.Dynamic.ExpandoObject)
#if !NETSTANDARD2_0
            || type.IsByRefLike
#endif
            )
        {
            return null;
        }

        // A type-level [MessagePackFormatter] (v3's attribute name matches too) that got this far was not compiled by
        // the source generator, and no runtime tier serves it. Serving the type as a reflection map instead would
        // silently change its format (the attribute outranks the object formatter), so name the actual problem.
        if (AttributeFormatterActivator.FindAttribute(type.GetCustomAttributes(inherit: true), MessagePackAttributeNames.Formatter) is { } formatterAttribute)
        {
            throw AttributeFormatterActivator.TypeLevelNotCompiled(formatterAttribute, type);
        }

        if (annotatedOnly && !HasSerializableAnnotation(type))
        {
            return null;
        }

        return typeof(ReflectionObjectFormatterFactory<>);
    }

    static bool HasUnionAnnotation(Type type)
    {
        foreach (var attribute in type.CustomAttributes)
        {
            for (var current = attribute.AttributeType; current is not null; current = current.BaseType)
            {
                if (current.FullName is MessagePackAttributeNames.UnionTag or MessagePackAttributeNames.V3Union)
                {
                    return true;
                }
            }
        }
        return false;
    }

    static bool HasSerializableAnnotation(Type type)
    {
        // Attribute data is enough here (no instantiation), and the attribute's own base chain is walked so derived
        // annotation types count. [DataContract] counts on the type itself, since the v3 dynamic resolvers honored it
        // as an alternative annotation. [MessagePackObject] also counts from base types (it declares Inherited = true,
        // in v3 and v4 alike), so an unannotated derived type is claimed like v3 did, whereas [DataContract] declares
        // Inherited = false and stays declared-only.
        for (var declaring = type; declaring is not null && declaring != typeof(object); declaring = declaring.BaseType)
        {
            foreach (var attribute in declaring.CustomAttributes)
            {
                for (var current = attribute.AttributeType; current is not null; current = current.BaseType)
                {
                    if (current.FullName is MessagePackAttributeNames.MessagePackObject
                        || (declaring == type && current.FullName is MessagePackAttributeNames.DataContract))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}

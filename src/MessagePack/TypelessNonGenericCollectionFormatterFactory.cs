using MessagePack.Formatters;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace MessagePack;

/// <summary>
/// The part of typeless composition that keeps v3's wire for collection classes. v3's DynamicGenericResolver asked a
/// non-generic class for its non-generic views before its generic ones: <c>class Orders : Collection&lt;Order&gt;</c>
/// (or one derived from List&lt;T&gt;, ObservableCollection&lt;T&gt;, KeyedCollection&lt;K,T&gt;, Dictionary&lt;K,V&gt;)
/// implements IList / IDictionary, so its elements went through the <c>object</c> formatter. Under a typeless chain
/// that is the typeless formatter: every element carries its concrete type name, and a derived element comes back as
/// itself. The default chain asks for the generic views first (elements by their declared type, which is what makes
/// such a collection round-trip without typeless at all; v3 could write it there and not read it back), so the same
/// class has another wire there, and payloads v3 wrote in typeless mode do not read through it.
/// This factory sits ahead of the chain <see cref="MessagePackFormatterFactory.WithTypeless"/> extends, like
/// <see cref="NativeDateTimeFormatterFactory"/>, because the source-generated factory registers the generic-view
/// formatter for such a member type. It claims exactly the types the two orders disagree on; a type-level
/// [MessagePackFormatter] (or [MessagePackObject] / [DataContract]) keeps its claim.
/// </summary>
internal sealed class TypelessNonGenericCollectionFormatterFactory : GenericFormatterFactoryBase
{
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(TypelessMessages.RequiresUnreferencedCode)]
    public TypelessNonGenericCollectionFormatterFactory()
    {
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "a claim check for a tier gated by RequiresDynamicCode / RequiresUnreferencedCode at construction; a trimmed-away constructor or interface only means the type falls through to the chain behind")]
    protected override Type? GetOpenFactoryType(Type type, out Type[] typeArguments, out object?[]? constructorArguments)
    {
        typeArguments = [type];
        constructorArguments = null;

        // v3 took this branch for a type that is not generic (a class nested in a generic one counts as generic), and
        // its formatters construct through new()
        if (type.IsGenericType || type.IsValueType || type.IsAbstract || type.IsArray
            || type.GetConstructor(Type.EmptyTypes) == null
            || ReflectionFormatterFactory.HasObjectClaim(type))
        {
            return null;
        }

        var list = typeof(IList).IsAssignableFrom(type);
        if (!list && !typeof(IDictionary).IsAssignableFrom(type))
        {
            return null;
        }

        // without a generic view the chain reaches the same two formatters by itself, behind whatever claims the type
        // by name first; only a type the generic-view rules would take is decided here
        var hasGenericView = false;
        foreach (var implemented in type.GetInterfaces())
        {
            if (implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                hasGenericView = true;
                break;
            }
        }
        if (!hasGenericView)
        {
            return null;
        }

        return list ? typeof(NonGenericListFormatterFactory<>) : typeof(NonGenericDictionaryFormatterFactory<>);
    }
}

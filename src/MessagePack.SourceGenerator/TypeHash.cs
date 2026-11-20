using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator;

/// <summary>
/// The key a generated factory buckets its types by. A factory used to answer CreateFormatter with one chain of
/// <c>if (type == typeof(X)) return new XFormatter();</c> over every type it serves, and a runtime that resolves a
/// method's type references when the method first runs (IL2CPP initializes them all on entry, the JIT loads them to
/// compile the method) paid for every formatter of the assembly on the first lookup of any one type. The chain is
/// split into small methods, and this hash picks the one to run.
///
/// It has to be computable on both sides: here from the symbol, in the generated code from the
/// <see cref="System.Type"/> (<see cref="EmitRuntimeMethod"/>, which must stay this method's mirror). Both hash the
/// type's metadata name (<c>Type.Name</c>: no namespace, no containing type, the arity suffix included) with FNV-1a,
/// then mix in the generic arguments (the containing types' first, the order <c>Type.GetGenericArguments</c> returns)
/// and, for an array, its element type and rank. Collisions only share a bucket, and a type that is not found in its
/// own bucket is looked for in every other one, so a disagreement between the two sides costs the laziness, never
/// the formatter.
/// </summary>
static class TypeHash
{
    /// <summary>The name of the emitted method.</summary>
    public const string RuntimeMethodName = "MessagePackTypeHash";

    public static uint Of(ITypeSymbol type)
    {
        unchecked
        {
            switch (type)
            {
                case IArrayTypeSymbol array:
                    return Of(array.ElementType) * 31u + (uint)array.Rank;
                case INamedTypeSymbol named:
                    var hash = OfName(named.MetadataName);
                    // an open definition has no arguments at run time (Type.IsConstructedGenericType is false)
                    if (!SymbolEqualityComparer.Default.Equals(named, named.OriginalDefinition))
                    {
                        MixTypeArguments(named, ref hash);
                    }
                    return hash;
                case IDynamicTypeSymbol:
                    return OfName("Object");
                default:
                    return OfName(type.MetadataName);
            }
        }
    }

    static void MixTypeArguments(INamedTypeSymbol named, ref uint hash)
    {
        if (named.ContainingType is { } container)
        {
            MixTypeArguments(container, ref hash);
        }
        foreach (var argument in named.TypeArguments)
        {
            hash = unchecked(hash * 31u + Of(argument));
        }
    }

    static uint OfName(string name)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in name)
            {
                hash = (hash ^ c) * 16777619u;
            }
            return hash;
        }
    }

    /// <summary>
    /// Emits the run-time twin of <see cref="Of"/> as a private static method of the generated factory. Fully qualified
    /// and explicitly unchecked: it is also emitted into a user's partial class, in a compilation whose usings and
    /// overflow checking are not ours.
    /// </summary>
    public static void EmitRuntimeMethod(CodeWriter writer)
    {
        writer.Line("// the bucket key of a type: FNV-1a over Type.Name, then the generic arguments and the array");
        writer.Line("// shape. The generator computed the same value for every type the buckets hold.");
        writer.Line($"static uint {RuntimeMethodName}(global::System.Type type)");
        using (writer.OpenScope())
        {
            writer.Line("unchecked");
            using (writer.OpenScope())
            {
                writer.Line("if (type.IsArray)");
                using (writer.OpenScope())
                {
                    writer.Line($"return {RuntimeMethodName}(type.GetElementType()!) * 31u + (uint)type.GetArrayRank();");
                }
                writer.Line("var name = type.Name;");
                writer.Line("var hash = 2166136261u;");
                writer.Line("for (int i = 0; i < name.Length; i++)");
                using (writer.OpenScope())
                {
                    writer.Line("hash = (hash ^ name[i]) * 16777619u;");
                }
                writer.Line("if (type.IsConstructedGenericType)");
                using (writer.OpenScope())
                {
                    writer.Line("var arguments = type.GetGenericArguments();");
                    writer.Line("for (int i = 0; i < arguments.Length; i++)");
                    using (writer.OpenScope())
                    {
                        writer.Line($"hash = hash * 31u + {RuntimeMethodName}(arguments[i]);");
                    }
                }
                writer.Line("return hash;");
            }
        }
    }
}

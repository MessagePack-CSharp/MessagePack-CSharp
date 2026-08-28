using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator;

/// <summary>
/// Parses a [MessagePackSerializable&lt;T&gt;]-annotated partial factory class into a
/// <see cref="SerializableFactoryModel"/>. Each declared root type runs through
/// <see cref="ObjectParser.HarvestSerializedType"/> — exactly the pipeline serialized
/// members use — so a root declaration serves the whole reachable closure (the array or
/// collection wrapper, nested collections, enums, Nullable, closed user generics), not
/// just the named type. The class shape mirrors JsonSerializerContext: a partial,
/// non-generic class whose generated half derives MessagePackFormatterFactory.
/// The generic and non-generic attribute forms arrive through separate FAWMN pipelines
/// (exact-name matching); the factory node merges results targeting the same class.
/// </summary>
static class SerializableParser
{
    public static SerializableParseResult Parse(GeneratorAttributeSyntaxContext context)
    {
        var diagnostics = new List<DiagnosticInfo>();
        var type = (INamedTypeSymbol)context.TargetSymbol;
        var typeLocation = LocationInfo.From(type);
        var typeName = type.ToDisplayString();

        if (type.TypeKind != TypeKind.Class || type.IsRecord || type.IsGenericType || type.IsStatic || type.IsAbstract)
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack016", $"'{typeName}': [MessagePackSerializable] requires a non-generic, non-abstract, non-record class (the generated half derives MessagePackFormatterFactory and constructs an Instance).", typeLocation));
            return Empty(diagnostics);
        }
        if (!ObjectParser.IsPartialChain(type))
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack016", $"'{typeName}': [MessagePackSerializable] requires the class (and every containing type) to be declared partial: the factory implementation is generated as the class's other half.", typeLocation));
            return Empty(diagnostics);
        }
        for (var accessible = type; accessible is not null; accessible = accessible.ContainingType)
        {
            if (accessible.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack016", $"'{typeName}': [MessagePackSerializable] requires the class (and every containing type) to be public or internal — the module-initializer registration must be reachable from the module.", typeLocation));
                return Empty(diagnostics);
            }
        }
        // the generated partial declares the base, so the user half may omit it; an
        // explicit MessagePackFormatterFactory base is redundant-but-legal, anything else
        // would collide (CS0263) — diagnose it with intent instead
        if (type.BaseType is { SpecialType: not SpecialType.System_Object } baseType
            && baseType.ToDisplayString() != "MessagePack.MessagePackFormatterFactory")
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack016", $"'{typeName}': [MessagePackSerializable] generates a partial deriving MessagePackFormatterFactory, so the class cannot also derive '{baseType.ToDisplayString()}'.", typeLocation));
            return Empty(diagnostics);
        }

        var compilation = context.SemanticModel.Compilation;
        var harvestedGenerics = new Dictionary<string, HarvestedGenericModel>();
        var harvestedBuiltIns = new Dictionary<string, HarvestedBuiltInModel>();
        foreach (var attribute in context.Attributes)
        {
            // generic form: the root is the attribute's type argument (metadata carries no
            // constructor arguments there); typeof form: the single constructor argument
            var root = attribute.AttributeClass is { IsGenericType: true } attributeClass
                ? attributeClass.TypeArguments[0]
                : attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as ITypeSymbol : null;
            if (root is null || root.TypeKind == TypeKind.Error)
            {
                continue; // unresolvable typeof already carries a compiler error
            }
            if (ContainsTypeParameterDeep(root))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack017", $"'{typeName}': [MessagePackSerializable] root '{root.ToDisplayString()}' is not a closed type — a serialization root must be fully constructed (open generics and type parameters cannot be registered).", typeLocation));
                continue;
            }
            ObjectParser.HarvestSerializedType(root, compilation, harvestedGenerics, harvestedBuiltIns);
        }

        var fullTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var model = new SerializableFactoryModel(
            FullTypeName: fullTypeName,
            HintName: ObjectParser.Sanitize(fullTypeName),
            Declarations: ObjectParser.BuildNestedDeclarations(type),
            HarvestedGenerics: new EquatableArray<HarvestedGenericModel>([.. harvestedGenerics.Values.OrderBy(static h => h.ClosedTypeName, StringComparer.Ordinal)]),
            HarvestedBuiltIns: new EquatableArray<HarvestedBuiltInModel>([.. harvestedBuiltIns.Values.OrderBy(static h => h.ClosedTypeName, StringComparer.Ordinal)]));
        return new SerializableParseResult(model, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
    }

    static bool ContainsTypeParameterDeep(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol => true,
        IArrayTypeSymbol array => ContainsTypeParameterDeep(array.ElementType),
        INamedTypeSymbol named => named.IsUnboundGenericType || named.TypeArguments.Any(ContainsTypeParameterDeep),
        _ => false,
    };

    static SerializableParseResult Empty(List<DiagnosticInfo> diagnostics) =>
        new(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
}

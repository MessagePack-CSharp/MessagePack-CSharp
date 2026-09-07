using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator;

/// <summary>
/// Parses a type-level [MessagePackFormatter] into an <see cref="AttributeFormatterTypeModel"/>: the generated factory
/// constructs the attribute-directed formatter with compiled code and registers the type into
/// SourceGeneratedFormatterFactory through the module initializer, so the annotation rides the same registry tier as
/// generated object formatters. There is no runtime attribute tier, a type the generator cannot compile a registration
/// for is served by nothing, which MsgPack014 surfaces instead of a silent stand-down.
/// Binding of the attribute itself is shared with the member-level path
/// (<see cref="ObjectParser.TryBuildCustomFormatter"/>), so the expression-string argument form is compile-time checked
/// here too.
/// </summary>
static class AttributeFormatterParser
{
    const string MessagePackObjectAttributeName = "MessagePack.MessagePackObjectAttribute";

    public static AttributeFormatterParseResult Parse(GeneratorAttributeSyntaxContext context)
    {
        var diagnostics = new List<DiagnosticInfo>();
        var type = (INamedTypeSymbol)context.TargetSymbol;
        var typeLocation = LocationInfo.From(type);
        var typeName = type.ToDisplayString();

        if (type.IsGenericType)
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack014", $"'{typeName}': type-level [MessagePackFormatter] is not generated for generic types (the registration cannot be pre-closed); register the factory in the resolver chain instead.", typeLocation));
            return Empty(diagnostics);
        }
        for (var accessible = type; accessible is not null; accessible = accessible.ContainingType)
        {
            if (accessible.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack014", $"'{typeName}': type-level [MessagePackFormatter] needs the type (and every containing type) to be public or internal; register the factory in the resolver chain instead.", typeLocation));
                return Empty(diagnostics);
            }
        }
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == MessagePackObjectAttributeName
                && ObjectParser.ReadNamedBool(attribute, "SuppressSourceGeneration"))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack014", $"'{typeName}': SuppressSourceGeneration also suppresses the type-level [MessagePackFormatter] registration; register the factory in the resolver chain instead.", typeLocation));
                return Empty(diagnostics);
            }
        }

        var fullTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (!ObjectParser.TryBuildCustomFormatter(context.Attributes[0], type, typeName, typeLocation, context, diagnostics, out var custom))
        {
            return Empty(diagnostics);
        }
        return new AttributeFormatterParseResult(new AttributeFormatterTypeModel(fullTypeName, custom!), new EquatableArray<DiagnosticInfo>([.. diagnostics]));
    }

    static AttributeFormatterParseResult Empty(List<DiagnosticInfo> diagnostics) =>
        new(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
}

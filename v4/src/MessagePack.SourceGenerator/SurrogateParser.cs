using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.SourceGenerator;

/// <summary>
/// Discovers IMessagePackSurrogate&lt;TTarget, TSurrogate&gt; implementations: the implementation itself is the
/// declaration ("TSurrogate is the wire stand-in for TTarget"), so the generated factory auto-registers the target with
/// a statically closed SurrogateFormatter, no attribute on the target,
/// which may be a third-party type the user cannot annotate. A target whose own declaration already claims it
/// ([MessagePackObject] or type-level [MessagePackFormatter]) wins and the surrogate is skipped with MsgPack019;
/// shapes the factory cannot close statically (open generics, inaccessible types) are skipped the same way,
/// with explicit chain composition of SurrogateFormatterFactory as the fallback.
/// Duplicate targets across the compilation are MsgPack018 errors, detected over the collected
/// <see cref="SurrogateTargetSite"/>s.
/// </summary>
static class SurrogateParser
{
    const string FormatterAttributeName = "MessagePack.MessagePackFormatterAttribute";

    /// <summary>Cheap syntax gate: a base list mentioning IMessagePackSurrogate&lt;...&gt;.</summary>
    public static bool IsCandidate(Microsoft.CodeAnalysis.SyntaxNode node)
    {
        if (node is not TypeDeclarationSyntax { BaseList.Types: var baseTypes })
        {
            return false;
        }
        foreach (var baseType in baseTypes)
        {
            var name = baseType.Type;
            while (true)
            {
                if (name is QualifiedNameSyntax qualified)
                {
                    name = qualified.Right;
                }
                else if (name is AliasQualifiedNameSyntax aliasQualified)
                {
                    name = aliasQualified.Name;
                }
                else
                {
                    break;
                }
            }
            if (name is GenericNameSyntax { Identifier.ValueText: "IMessagePackSurrogate" })
            {
                return true;
            }
        }
        return false;
    }

    public static SurrogateParseResult? Parse(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)context.Node, cancellationToken) is not INamedTypeSymbol type)
        {
            return null;
        }
        if (type.TypeKind != TypeKind.Struct)
        {
            // the interface's struct constraint makes a class implementation CS0453 at the user's declaration;
            // skipping here keeps the error from cascading into generated code
            return null;
        }

        var targets = new List<INamedTypeSymbol>();
        foreach (var implemented in type.AllInterfaces)
        {
            // only the self-shaped implementation declares an association: a type implementing
            // IMessagePackSurrogate<TTarget, SomeOtherType> is not that surrogate
            if (implemented.OriginalDefinition is { MetadataName: "IMessagePackSurrogate`2" } definition
                && definition.ContainingNamespace.ToDisplayString() == "MessagePack"
                && SymbolEqualityComparer.Default.Equals(implemented.TypeArguments[1], type))
            {
                targets.Add(implemented);
            }
        }
        if (targets.Count == 0)
        {
            return null;
        }

        var diagnostics = new List<DiagnosticInfo>();
        var sites = new List<SurrogateTargetSite>();
        var typeLocation = LocationInfo.From(type);
        var typeName = type.ToDisplayString();
        var compilation = context.SemanticModel.Compilation;

        if (ObjectParser.ContainsTypeParameter(type))
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack019", $"'{typeName}': a generic surrogate cannot be auto-registered (the factory needs a closed instantiation); compose SurrogateFormatterFactory into an explicit chain instead.", typeLocation));
            return Result(sites, diagnostics);
        }
        if (!ObjectParser.IsAccessibleToGeneratedCode(type, compilation))
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack019", $"'{typeName}': the surrogate type must be public or internal for the generated factory to construct it; compose SurrogateFormatterFactory into an explicit chain instead.", typeLocation));
            return Result(sites, diagnostics);
        }

        var surrogateTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        foreach (var implemented in targets)
        {
            var target = implemented.TypeArguments[0];
            var targetDisplay = target.ToDisplayString();
            if (ObjectParser.ContainsTypeParameter(target))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack019", $"'{typeName}': the surrogate target '{targetDisplay}' is not a closed type, so it cannot be auto-registered.", typeLocation));
                continue;
            }
            if (!ObjectParser.IsAccessibleToGeneratedCode(target, compilation))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack019", $"'{typeName}': the surrogate target '{targetDisplay}' must be public or internal for the generated factory to serve it; compose SurrogateFormatterFactory into an explicit chain instead.", typeLocation));
                continue;
            }
            if (target is INamedTypeSymbol namedTarget
                && (ObjectParser.HasMessagePackObjectAttribute(namedTarget.OriginalDefinition) || ObjectParser.HasAttribute(namedTarget.OriginalDefinition, FormatterAttributeName)))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack019", $"'{typeName}': the surrogate target '{targetDisplay}' carries its own serialization declaration ([MessagePackObject] or a type-level [MessagePackFormatter]), which wins; the surrogate is not registered. Remove the target's declaration to serialize it through the surrogate.", typeLocation));
                continue;
            }
            sites.Add(new SurrogateTargetSite(
                TargetTypeName: target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                SurrogateTypeName: surrogateTypeName,
                Location: typeLocation));
        }
        return Result(sites, diagnostics);
    }

    static SurrogateParseResult? Result(List<SurrogateTargetSite> sites, List<DiagnosticInfo> diagnostics) =>
        sites.Count == 0 && diagnostics.Count == 0
            ? null
            : new SurrogateParseResult(
                new EquatableArray<SurrogateTargetSite>([.. sites]),
                new EquatableArray<DiagnosticInfo>([.. diagnostics]));
}

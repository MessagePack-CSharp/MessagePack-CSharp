using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// A closed class confines its hierarchy to the declaring assembly, so a [UnionTag] root
/// that is closed has a knowable case universe: every concrete type deriving from it in
/// the compilation must carry a tag on the root, or serializing it through the root
/// silently writes nil — the open-hierarchy version tolerance, which a closed root has no
/// excuse for. MsgPack106 reports the missing tag on the derived type as an error (a
/// deliberately unserialized case can #pragma-suppress).
/// Detection walks each concrete type's base chain: closedness reads the `closed`
/// modifier token off the declaration (the declaring compilation never surfaces the
/// lowered [IsClosedType] attribute on the symbol) and falls back to the attribute for
/// metadata symbols.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ClosedUnionCoverageAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MsgPack106";

    const string IsClosedTypeAttributeName = "System.Runtime.CompilerServices.IsClosedTypeAttribute";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Closed union root must tag every case",
        "'{0}' is a closed [UnionTag] root but derived type '{1}' has no tag; serializing it through the root writes nil",
        "MessagePack.SourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A closed class confines its derived types to the declaring assembly, so the union case universe of a closed [UnionTag] root is known at compile time. A concrete derived type without a [UnionTag] entry serializes as nil through the root — silent data loss that the closed declaration makes detectable.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class || type.IsAbstract)
        {
            return; // only concrete classes are union cases
        }

        for (var baseType = type.BaseType; baseType is not null && baseType.SpecialType != SpecialType.System_Object; baseType = baseType.BaseType)
        {
            var hasUnionTag = false;
            var tagged = false;
            foreach (var attribute in baseType.GetAttributes())
            {
                if (!UnionParser.IsUnionTagAttribute(attribute.AttributeClass))
                {
                    continue;
                }
                hasUnionTag = true;
                if (UnionParser.ResolveCaseType(attribute, baseType) is { } caseType
                    && SymbolEqualityComparer.Default.Equals(caseType, type))
                {
                    tagged = true;
                    break;
                }
            }
            if (hasUnionTag && !tagged && IsClosed(baseType, context.CancellationToken))
            {
                // the fix adds the missing [UnionTag] on the ROOT, so hand it both ends
                var properties = ImmutableDictionary<string, string?>.Empty
                    .Add("RootDocId", DocumentationCommentId.CreateDeclarationId(baseType))
                    .Add("CaseDocId", DocumentationCommentId.CreateDeclarationId(type));
                context.ReportDiagnostic(Diagnostic.Create(Rule, PickLocation(type), properties, baseType.ToDisplayString(), type.ToDisplayString()));
            }
        }
    }

    static bool IsClosed(INamedTypeSymbol type, System.Threading.CancellationToken cancellationToken)
    {
        // metadata symbols carry the lowered attribute
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == IsClosedTypeAttributeName)
            {
                return true;
            }
        }
        // source symbols only show the modifier; the token text is readable without the
        // union-aware Roslyn API surface (this analyzer compiles against the older floor)
        foreach (var syntaxReference in type.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is TypeDeclarationSyntax declaration)
            {
                foreach (var modifier in declaration.Modifiers)
                {
                    if (modifier.ValueText == "closed")
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    static Location PickLocation(ISymbol symbol)
    {
        foreach (var location in symbol.Locations)
        {
            if (location.SourceTree is not null)
            {
                return location;
            }
        }
        return Location.None;
    }
}

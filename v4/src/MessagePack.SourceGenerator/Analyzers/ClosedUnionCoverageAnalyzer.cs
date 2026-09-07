using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// A closed class confines its hierarchy to the declaring assembly, so a [UnionTag] root
/// that is closed has a knowable case universe: every concrete type deriving from it in
/// the compilation must carry a tag on the root, or serializing it through the root
/// silently writes nil, the open-hierarchy version tolerance, which a closed root has no
/// excuse for. MsgPack106 reports each missing case as an error on the root, where the
/// [UnionTag] list lives and where the fix lands (a deliberately unserialized case can
/// #pragma-suppress there).
/// Detection starts from the root: closedness reads the `closed` modifier token off the
/// declaration (the declaring compilation never surfaces the lowered [IsClosedType]
/// attribute on the symbol) and falls back to the attribute for metadata symbols; the
/// concrete descendants are enumerated from the compilation's own assembly, which is the
/// whole universe a closed class permits.
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
        description: "A closed class confines its derived types to the declaring assembly, so the union case universe of a closed [UnionTag] root is known at compile time. A concrete derived type without a [UnionTag] entry serializes as nil through the root, a silent data loss that the closed declaration makes detectable.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeRoot, SymbolKind.NamedType);
    }

    static void AnalyzeRoot(SymbolAnalysisContext context)
    {
        var root = (INamedTypeSymbol)context.Symbol;
        if (root.TypeKind != TypeKind.Class)
        {
            return;
        }

        HashSet<ISymbol>? taggedCases = null;
        Location? tagLocation = null;
        foreach (var attribute in root.GetAttributes())
        {
            if (!UnionParser.IsUnionTagAttribute(attribute.AttributeClass))
            {
                continue;
            }
            taggedCases ??= new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            if (UnionParser.ResolveCaseType(attribute, root) is { } caseType)
            {
                taggedCases.Add(caseType);
            }
            // the partial declaration that carries the [UnionTag] list is where the error belongs
            tagLocation ??= attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken)
                .FirstAncestorOrSelf<TypeDeclarationSyntax>()?.Identifier.GetLocation();
        }
        if (taggedCases is null || !IsClosed(root, context.CancellationToken))
        {
            return;
        }

        var location = tagLocation ?? PickLocation(root);
        var rootDocId = DocumentationCommentId.CreateDeclarationId(root);
        foreach (var candidate in EnumerateTypes(context.Compilation.Assembly.GlobalNamespace))
        {
            if (candidate.TypeKind != TypeKind.Class || candidate.IsAbstract
                || !DerivesFrom(candidate, root) || taggedCases.Contains(candidate))
            {
                continue; // only concrete descendants are union cases
            }

            // the fix adds the missing [UnionTag] on the root, so hand it both ends
            var properties = ImmutableDictionary<string, string?>.Empty
                .Add("RootDocId", rootDocId)
                .Add("CaseDocId", DocumentationCommentId.CreateDeclarationId(candidate));
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, properties, root.ToDisplayString(), candidate.ToDisplayString()));
        }
    }

    static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol root)
    {
        for (var baseType = type.BaseType; baseType is not null && baseType.SpecialType != SpecialType.System_Object; baseType = baseType.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, root))
            {
                return true;
            }
        }
        return false;
    }

    static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceOrTypeSymbol container)
    {
        foreach (var member in container.GetMembers())
        {
            if (member is INamespaceSymbol nestedNamespace)
            {
                foreach (var type in EnumerateTypes(nestedNamespace))
                {
                    yield return type;
                }
            }
            else if (member is INamedTypeSymbol type)
            {
                yield return type;
                foreach (var nested in EnumerateTypes(type))
                {
                    yield return nested;
                }
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
        // source symbols only show the modifier; the token text is readable without the union-aware Roslyn API surface
        // (this analyzer compiles against the older floor)
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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// A pattern union enumerates its complete case universe in its own creation members (the spec defines the case types
/// as the parameter types of the single-parameter constructors, or of the provider's static Create methods),
/// so a [UnionTag]-tagged union with an untagged case is knowably incomplete: serializing that case throws at runtime.
/// MsgPack107 reports each untagged case on the root as an error (a deliberately unserialized case can
/// #pragma-suppress). An object-typed creation parameter is a catch-all, not a case declaration,
/// and demands nothing, coverage of what flows through it stays a runtime concern.
/// The closed-hierarchy counterpart is MsgPack106.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PatternUnionCoverageAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MsgPack107";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Union must tag every declared case",
        "'{0}' declares union case '{1}' without a [UnionTag]; serializing it throws at runtime",
        "MessagePack.SourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A union declaration (or a hand-written [Union]-pattern type) lists its complete case set in its creation members. A case without a [UnionTag] entry cannot be serialized (serialization throws when it holds that case), so the missing tag is detectable at compile time.");

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
        if (type.TypeKind is not (TypeKind.Struct or TypeKind.Class)
            || (type.TypeKind == TypeKind.Class && type.IsAbstract))
        {
            return; // inheritance roots are MsgPack106's (closed) territory
        }

        HashSet<ISymbol>? taggedCases = null;
        foreach (var attribute in type.GetAttributes())
        {
            if (UnionParser.IsUnionTagAttribute(attribute.AttributeClass)
                && UnionParser.ResolveCaseType(attribute, type) is { } caseType)
            {
                (taggedCases ??= new HashSet<ISymbol>(SymbolEqualityComparer.Default)).Add(caseType);
            }
        }
        if (taggedCases is null || !UnionParser.HasUnionMarker(type))
        {
            return;
        }

        foreach (var declaredCase in DeclaredCases(type))
        {
            if (!taggedCases.Contains(declaredCase))
            {
                // the fix adds the missing [UnionTag] on the root, so hand it both ends
                var properties = ImmutableDictionary<string, string?>.Empty
                    .Add("RootDocId", DocumentationCommentId.CreateDeclarationId(type));
                if (declaredCase is INamedTypeSymbol { IsGenericType: false } namedCase && DocumentationCommentId.CreateDeclarationId(namedCase) is { } caseId)
                {
                    properties = properties.Add("CaseDocId", caseId);
                }
                context.ReportDiagnostic(Diagnostic.Create(Rule, PickLocation(type), properties, type.ToDisplayString(), declaredCase.ToDisplayString()));
            }
        }
    }

    // the spec's case universe: parameter types of the union creation members, the single-parameter constructors,
    // or the provider's static Create methods. An object parameter (catch-all)
    // and the union type itself are not cases; a type parameter is one (taggable by name since the ("T", tag)
    // form exists).
    static IEnumerable<ITypeSymbol> DeclaredCases(INamedTypeSymbol type)
    {
        var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        if (UnionParser.FindUnionMemberProvider(type) is { } provider)
        {
            foreach (var member in provider.GetMembers("Create"))
            {
                if (member is IMethodSymbol { IsStatic: true, Parameters.Length: 1 } factory
                    && SymbolEqualityComparer.Default.Equals(factory.ReturnType, type)
                    && IsCaseParameter(factory.Parameters[0].Type, type)
                    && seen.Add(factory.Parameters[0].Type))
                {
                    yield return factory.Parameters[0].Type;
                }
            }
            yield break;
        }
        foreach (var constructor in type.InstanceConstructors)
        {
            if (constructor.DeclaredAccessibility is (Accessibility.Public or Accessibility.Internal)
                && constructor.Parameters.Length == 1
                && IsCaseParameter(constructor.Parameters[0].Type, type)
                && seen.Add(constructor.Parameters[0].Type))
            {
                yield return constructor.Parameters[0].Type;
            }
        }
    }

    static bool IsCaseParameter(ITypeSymbol parameterType, INamedTypeSymbol unionType) =>
        parameterType.SpecialType != SpecialType.System_Object
        && !SymbolEqualityComparer.Default.Equals(parameterType, unionType);

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

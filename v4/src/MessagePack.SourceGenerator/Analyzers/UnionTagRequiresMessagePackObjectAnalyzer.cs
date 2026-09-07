using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// Union discovery is driven by [MessagePackObject] alone: the generator's single ForAttributeWithMetadataName pipeline
/// routes a [MessagePackObject] type with [UnionTag] attributes to the union parser,
/// and never sees a type that carries only [UnionTag], no formatter is generated and serialization fails at runtime
/// with formatter-not-found. MsgPack103 turns that silent miss into a compile-time error: there is no situation where a
/// lone [UnionTag] does something useful.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnionTagRequiresMessagePackObjectAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MsgPack103";

    const string UnionTagAttributeName = "MessagePack.UnionTagAttribute";
    const string MessagePackObjectAttributeName = "MessagePack.MessagePackObjectAttribute";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "UnionTag requires MessagePackObject",
        "'{0}' has [UnionTag] but no [MessagePackObject]; union discovery is driven by [MessagePackObject], so no formatter is generated for this base",
        "MessagePack.SourceGenerator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A [UnionTag] polymorphic base participates in serialization only when it also carries [MessagePackObject]: the source generator discovers types through that single attribute. Without it the union base silently gets no formatter and fails at runtime with formatter-not-found.");

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
        var hasUnionTag = false;
        var hasMessagePackObject = false;
        foreach (var attribute in type.GetAttributes())
        {
            // base-chain walk: a derived annotation still counts, matching the parsers
            for (var attributeType = attribute.AttributeClass; attributeType is not null; attributeType = attributeType.BaseType)
            {
                var name = attributeType.ToDisplayString();
                if (name == UnionTagAttributeName)
                {
                    hasUnionTag = true;
                    break;
                }
                if (name == MessagePackObjectAttributeName)
                {
                    hasMessagePackObject = true;
                    break;
                }
            }
        }

        if (hasUnionTag && !hasMessagePackObject)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, PickLocation(type), type.ToDisplayString()));
        }
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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SerializerFoundation.Analyzers;

/// <summary>
/// A virtual marked [RequireOverride] is conceptually abstract: its body is a
/// compatibility bridge for assemblies compiled against a TFM where the member does not
/// exist (making it abstract there would fail type LOADING, not compilation). Code that
/// can see the member has no reason to ride the bridge, so SF003 requires the override
/// wherever the attribute is visible. Downlevel compilations never see the member, so
/// enforcement automatically stops exactly at the capability boundary.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RequireOverrideAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SF003";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Virtuals marked [RequireOverride] must be overridden",
        "'{0}' does not override '{1}', which is marked [RequireOverride]; the base body is a compatibility bridge for assemblies that cannot see the member, not for code compiled against this surface",
        "SerializerFoundation.Design",
        DiagnosticSeverity.Error, // "require" means require: the bridge is never the intended path for new code
        isEnabledByDefault: true,
        description: "A virtual member marked [RequireOverride] is conceptually abstract; it is virtual only so that assemblies compiled against TFMs where the member does not exist keep loading. Every non-abstract derived type compiled where the member is visible must override it (directly or through a base).");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static startContext =>
        {
            var attribute = startContext.Compilation.GetTypeByMetadataName("SerializerFoundation.CodeAnalysis.RequireOverrideAttribute");
            if (attribute is null)
            {
                return;
            }

            startContext.RegisterSymbolAction(
                context => AnalyzeType(context, attribute),
                SymbolKind.NamedType);
        });
    }

    static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol attribute)
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false } type)
        {
            return;
        }

        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            foreach (var member in baseType.GetMembers())
            {
                if (member is not IMethodSymbol { IsVirtual: true } required || !HasRequireOverride(required, attribute))
                {
                    continue;
                }
                if (!HasOverrideBelow(type, baseType, required))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, PickLocation(type), type.Name, required.Name));
                }
            }
        }
    }

    static bool HasRequireOverride(IMethodSymbol method, INamedTypeSymbol attribute)
    {
        foreach (var data in method.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(data.AttributeClass, attribute))
            {
                return true;
            }
        }
        return false;
    }

    // an override anywhere strictly below the declaring base satisfies the requirement:
    // an intermediate (possibly abstract) base overriding the member covers all its leaves
    static bool HasOverrideBelow(INamedTypeSymbol type, INamedTypeSymbol declaringBase, IMethodSymbol required)
    {
        for (var current = type;
             current is not null && !SymbolEqualityComparer.Default.Equals(current, declaringBase);
             current = current.BaseType)
        {
            foreach (var member in current.GetMembers(required.Name))
            {
                if (member is not IMethodSymbol { IsOverride: true } candidate)
                {
                    continue;
                }
                for (var overridden = candidate.OverriddenMethod; overridden is not null; overridden = overridden.OverriddenMethod)
                {
                    if (SymbolEqualityComparer.Default.Equals(overridden.OriginalDefinition, required.OriginalDefinition))
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
            if (location.SourceTree is { } tree && !tree.FilePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            {
                return location;
            }
        }
        return symbol.Locations.IsDefaultOrEmpty ? Location.None : symbol.Locations[0];
    }
}

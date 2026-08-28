using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// The generic CreateFormatter&lt;TWriteBuffer, TReadBuffer&gt; is a base-class virtual
/// whose body bridges to the Type-based overload — that keeps downlevel-compiled
/// factories loading, but a factory COMPILED against the modern surface has no reason
/// to ride the bridge: forgetting the override is silent and costs direct construction
/// (the Type-based path typically means reflection, which Native AOT cannot follow).
/// MsgPack102 makes the omission visible.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FactoryGenericOverrideAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MsgPack102";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Factory relies on the CreateFormatter bridge instead of direct construction",
        "'{0}' does not implement CreateFormatter<TWriteBuffer, TReadBuffer> and will route through the default-implementation bridge; implement the generic overload (and mark the type partial to get the Type-based dispatch generated) for direct, AOT-safe construction",
        "MessagePack.Compatibility",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "On TFMs with 'allows ref struct', MessagePackFormatterFactory's generic CreateFormatter is a virtual whose body bridges to the Type-based overload so that factories compiled against downlevel builds keep working. A factory compiled against the modern surface should override the generic member for direct construction; the bridge exists for assemblies that cannot see it, not for new code.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static startContext =>
        {
            var factoryInterface = startContext.Compilation.GetTypeByMetadataName("MessagePack.MessagePackFormatterFactory");
            if (factoryInterface is null)
            {
                return;
            }

            // the generic member only EXISTS on the modern surface — its absence means
            // this compilation is downlevel, where the Type-based member is the whole
            // contract and there is nothing to override
            IMethodSymbol? genericMember = null;
            foreach (var member in factoryInterface.GetMembers("CreateFormatter"))
            {
                if (member is IMethodSymbol { Arity: 2, Parameters.Length: 1 } method)
                {
                    genericMember = method;
                    break;
                }
            }
            if (genericMember is null)
            {
                return;
            }

            startContext.RegisterSymbolAction(
                context => AnalyzeType(context, factoryInterface, genericMember),
                SymbolKind.NamedType);
        });
    }

    static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol factoryBase, IMethodSymbol genericMember)
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false } type)
        {
            return;
        }

        var derivesFromFactory = false;
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, factoryBase))
            {
                derivesFromFactory = true;
                break;
            }
        }
        if (!derivesFromFactory)
        {
            return;
        }

        // only the base's bridge body runs when nothing between the type and the factory
        // base (other partials and generated parts included) overrides the virtual
        for (var current = type; current is not null && !SymbolEqualityComparer.Default.Equals(current, factoryBase); current = current.BaseType)
        {
            foreach (var member in current.GetMembers("CreateFormatter"))
            {
                if (member is IMethodSymbol { Arity: 2, Parameters.Length: 1, IsOverride: true })
                {
                    return; // rides its own override, not the bridge
                }
            }
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, PickLocation(type), type.Name));
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

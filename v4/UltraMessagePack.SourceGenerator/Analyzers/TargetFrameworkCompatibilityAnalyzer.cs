using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UltraMessagePack.SourceGenerator.Analyzers;

/// <summary>
/// Enforces the multi-targeting packaging rule: an assembly that compiles buffer-generic
/// code (type parameters constrained to IWriteBuffer/IReadBuffer) against a DOWNLEVEL
/// UltraMessagePack build (no `allows ref struct` on the buffer generics) MUST also ship
/// a net10.0 build. Without one, a .NET 10 app resolves the downlevel assembly, the
/// runtime unifies it against the net10.0 UltraMessagePack, and instantiating those
/// generics with a ref struct buffer throws a constraint violation at runtime — the
/// analyzer is the only fence in front of that cliff. "Downlevel build referenced" is
/// detected via the [ModernBufferSurface] assembly marker that only the net10.0 core
/// carries.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TargetFrameworkCompatibilityAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "UMP101";

    // the exact TFM the project must include: NuGet never selects a HIGHER TFM asset
    // (netstandard2.0;net11.0 hands net10.0 consumers the netstandard build), and
    // platform-specific flavors (net10.0-windows) do not cover platform-neutral
    // consumers — so nothing short of platform-neutral net10.0 itself closes the hole
    const string RequiredModernTfm = "net10.0";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Buffer-generic code compiled against a downlevel UltraMessagePack requires a net10.0 target",
        "'{0}' constrains type parameters to IWriteBuffer/IReadBuffer but compiles against an UltraMessagePack build without 'allows ref struct'; add net10.0 to TargetFrameworks so .NET 10 consumers resolve a compatible build instead of failing at runtime with a constraint violation",
        "UltraMessagePack.Compatibility",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "UltraMessagePack's net10.0 build declares 'allows ref struct' on the buffer type parameters (and carries the [ModernBufferSurface] assembly marker). An implementation compiled against a build without that flag — netstandard TFMs, but also net9.0, which resolves the netstandard2.1 asset — lacks the flag too, so when the assembly is loaded on a modern runtime together with the net10.0 UltraMessagePack, instantiating the formatter with a ref struct buffer throws at runtime. Multi-target the project and include net10.0 (the source generator emits the per-TFM constraints).");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        // generated code is analyzed too: the generator's emitted formatter partials carry
        // the hazardous constraints and shipping them downlevel-only is exactly the bug
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static startContext =>
        {
            var compilation = startContext.Compilation;
            var writeBuffer = compilation.GetTypeByMetadataName("SerializerFoundation.IWriteBuffer");
            var readBuffer = compilation.GetTypeByMetadataName("SerializerFoundation.IReadBuffer");
            if (writeBuffer is null && readBuffer is null)
            {
                return;
            }

            // The net10.0 core carries an assembly-level [ModernBufferSurface] marker;
            // compiling against a MARKER-LESS core means the referenced buffer generics
            // have no `allows ref struct`. Keying on the referenced SURFACE instead of
            // corelib capability (the old RuntimeFeature.ByRefLikeGenerics probe) closes
            // the net9.0 blind spot: a net9.0 project can express `allows ref struct`,
            // yet still resolves the netstandard2.1 core asset and compiles the
            // hazardous no-flag shape. (Works for the core's own source builds too: the
            // marker is #if'd into exactly the same builds it describes.)
            var coreAssembly = (writeBuffer ?? readBuffer)!.ContainingAssembly;
            foreach (var attribute in coreAssembly.GetAttributes())
            {
                if (attribute.AttributeClass is
                    {
                        Name: "ModernBufferSurfaceAttribute",
                        ContainingNamespace: { Name: "SerializerFoundation", ContainingNamespace.IsGlobalNamespace: true },
                    })
                {
                    return;
                }
            }

            if (ProjectAlsoTargetsModernTfm(startContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions))
            {
                return;
            }

            startContext.RegisterSymbolAction(
                context => AnalyzeSymbol(context, writeBuffer, readBuffer),
                SymbolKind.NamedType,
                SymbolKind.Method);
        });
    }

    static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol? writeBuffer, INamedTypeSymbol? readBuffer)
    {
        // type-level generics cover formatter implementations; method-level generics cover
        // factory implementations (CreateFormatter<TWriteBuffer, TReadBuffer>)
        var typeParameters = context.Symbol switch
        {
            INamedTypeSymbol type => type.TypeParameters,
            IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation } method => method.TypeParameters,
            _ => ImmutableArray<ITypeParameterSymbol>.Empty,
        };

        foreach (var typeParameter in typeParameters)
        {
            foreach (var constraint in typeParameter.ConstraintTypes)
            {
                if (IsBufferConstraint(constraint, writeBuffer, readBuffer))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, PickLocation(context.Symbol), context.Symbol.Name));
                    return;
                }
            }
        }
    }

    static Location PickLocation(ISymbol symbol)
    {
        // partial types get their buffer constraints from the BufferConstraintsGenerator's
        // emitted declaration; anchor the warning at the author's declaration, not there
        foreach (var location in symbol.Locations)
        {
            if (location.SourceTree is { } tree && !tree.FilePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            {
                return location;
            }
        }
        return symbol.Locations.IsDefaultOrEmpty ? Location.None : symbol.Locations[0];
    }

    static bool IsBufferConstraint(ITypeSymbol constraint, INamedTypeSymbol? writeBuffer, INamedTypeSymbol? readBuffer)
    {
        // AllInterfaces catches derived constraints (`where T : struct, ICustomWriteBuffer`
        // with `ICustomWriteBuffer : IWriteBuffer`) — same runtime hazard, and for type
        // parameters it also walks interfaces implied by their own constraints
        if (SymbolEqualityComparer.Default.Equals(constraint, writeBuffer) ||
            SymbolEqualityComparer.Default.Equals(constraint, readBuffer))
        {
            return true;
        }
        foreach (var implemented in constraint.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(implemented, writeBuffer) ||
                SymbolEqualityComparer.Default.Equals(implemented, readBuffer))
            {
                return true;
            }
        }
        return false;
    }

    static bool ProjectAlsoTargetsModernTfm(AnalyzerConfigOptions options)
    {
        // TargetFrameworks is NOT compiler-visible by default, and it cannot be passed
        // raw: ';' starts a comment in editorconfig syntax, so the generated global
        // config truncates "netstandard2.0;net10.0" to "netstandard2.0" (verified
        // empirically). The repo's Directory.Build.targets (and, once packaged, a
        // buildTransitive .targets — it must be a .targets: $(TargetFrameworks) is not
        // defined yet when .props import) therefore passes a comma-separated copy as
        // UltraMessagePackTargetFrameworks. When the properties are missing entirely we
        // still report: the compilation is provably downlevel and a false warning on a
        // misconfigured project beats silence on a broken package.
        options.TryGetValue("build_property.UltraMessagePackTargetFrameworks", out var escapedTargetFrameworks);
        options.TryGetValue("build_property.TargetFrameworks", out var rawTargetFrameworks);
        options.TryGetValue("build_property.TargetFramework", out var targetFramework);
        foreach (var candidate in $"{escapedTargetFrameworks};{rawTargetFrameworks};{targetFramework}".Split(';', ','))
        {
            if (string.Equals(candidate.Trim(), RequiredModernTfm, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

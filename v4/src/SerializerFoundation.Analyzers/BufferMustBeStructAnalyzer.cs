using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SerializerFoundation.Analyzers;

/// <summary>
/// IWriteBuffer/IReadBuffer implementations must be structs. Every consuming API is
/// constrained `where TBuffer : struct, I...Buffer` (with `allows ref struct` on modern
/// TFMs), so a class implementation compiles at its declaration and is then unusable at
/// every call site — a confusing dead end that surfaces far from its cause. It would
/// also be wrong even if it were accepted: reference semantics reintroduce exactly the
/// shared-mutable-buffer aliasing that the struct + by-ref contract (and SF002's
/// no-copy rule) exists to prevent. SF001 turns the mistake into an error at the
/// declaration.
///
/// Interfaces deriving from the buffer interfaces stay legal: they are abstractions a
/// struct can still implement, and the struct constraint is enforced where a concrete
/// type appears.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BufferMustBeStructAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SF001";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Buffer implementations must be structs",
        "'{0}' implements {1} but is not a struct; every serializer API constrains buffers to `struct`, so this type can never be used as a buffer",
        "SerializerFoundation.Correctness",
        DiagnosticSeverity.Error, // the class shape is a guaranteed dead end, not a style choice
        isEnabledByDefault: true,
        description: "Types implementing IWriteBuffer/IReadBuffer must be structs (ref structs included). All consuming APIs constrain buffer type parameters to `struct`, so a class implementation cannot be passed anywhere, and its reference semantics would break the single-owner buffer contract even if it could.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static startContext =>
        {
            var interfaces = ImmutableArray.CreateRange(
                new[]
                {
                    "SerializerFoundation.IWriteBuffer",
                    "SerializerFoundation.IReadBuffer",
                }
                .Select(startContext.Compilation.GetTypeByMetadataName)
                .Where(static symbol => symbol is not null)
                .Select(static symbol => symbol!));
            if (interfaces.IsEmpty)
            {
                return;
            }

            startContext.RegisterSymbolAction(
                context => AnalyzeType(context, interfaces),
                SymbolKind.NamedType);
        });
    }

    static void AnalyzeType(SymbolAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind is not TypeKind.Class || type.IsStatic)
        {
            return; // structs are the contract; derived buffer INTERFACES stay legal abstractions
        }

        var implemented = FindBufferInterface(type, interfaces);
        if (implemented is null)
        {
            return;
        }

        // a class hierarchy gets ONE error, at the type that introduced the interface;
        // subclasses inherit both the interface and the diagnosis
        if (type.BaseType is { } baseType && FindBufferInterface(baseType, interfaces) is not null)
        {
            return;
        }

        var location = type.Locations.FirstOrDefault(static l => l.SourceTree is not null) ?? Location.None;
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, type.Name, implemented.Name));
    }

    static INamedTypeSymbol? FindBufferInterface(INamedTypeSymbol type, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        foreach (var implemented in type.AllInterfaces)
        {
            foreach (var bufferInterface in interfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implemented, bufferInterface))
                {
                    return bufferInterface;
                }
            }
        }
        return null;
    }
}

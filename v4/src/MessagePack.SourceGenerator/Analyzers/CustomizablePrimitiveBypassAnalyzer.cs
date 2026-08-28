using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// string and DateTime are the two primitives whose wire behavior is resolver-configurable
/// (StringInterningFormatter, DotNetOptimizedDateTimeFormatter's native format), so a
/// formatter that calls buffer.WriteString(string)/ReadString()/WriteTimestamp/ReadTimestamp
/// directly silently bypasses that configuration.
/// For DateTime the bypass is a wire-compat bug (timestamp ext mixed into a graph the
/// resolver serializes natively); for string it silently defeats interning.
/// MsgPack104 steers hand-written formatters to the resolver-obtained formatter, the same route
/// generated formatters always take.
/// Protocol-structural writes (map keys, enum names, wire-representation strings of
/// non-string types) stay legal through the UTF-8 span overload and the header APIs, which
/// this rule deliberately does not match.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CustomizablePrimitiveBypassAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MsgPack104";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Formatter bypasses the resolver-configured string/DateTime formatter",
        "'{0}' inside a formatter bypasses the resolver's {1} formatter, so configurations like string interning or the .NET-native DateTime format silently do not apply; cache resolver.GetFormatter<TWriteBuffer, TReadBuffer, {1}>() in Initialize and serialize through it",
        "MessagePack.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "string and DateTime are the primitives whose serialization is resolver-configurable (StringInterningFormatter, DotNetOptimizedDateTimeFormatter), so a formatter calling the buffer primitive directly silently escapes that configuration; for DateTime it even mixes wire formats. Route the value through the formatter obtained from the resolver, exactly as generated formatters do. Writes that are protocol structure rather than object-model values (map keys, enum names, string renderings of non-string types) belong on the UTF-8 span overload or the header APIs, or suppress this rule with a justification.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static startContext =>
        {
            var formatterInterface = startContext.Compilation.GetTypeByMetadataName("MessagePack.IMessagePackFormatter`3");
            var writeExtensions = startContext.Compilation.GetTypeByMetadataName("MessagePack.WriteBufferExtensions");
            var readExtensions = startContext.Compilation.GetTypeByMetadataName("MessagePack.ReadBufferExtensions");
            if (formatterInterface is null || (writeExtensions is null && readExtensions is null))
            {
                return;
            }

            startContext.RegisterOperationAction(
                context => AnalyzeInvocation(context, formatterInterface, writeExtensions, readExtensions),
                OperationKind.Invocation);
        });
    }

    static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol formatterInterface, INamedTypeSymbol? writeExtensions, INamedTypeSymbol? readExtensions)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        // WriteString needs overload discrimination: the ReadOnlySpan<byte> overload is the
        // sanctioned route for structural strings (map keys, enum names) and must stay silent
        var valueTypeName = method.Name switch
        {
            "WriteString" when HasStringParameter(method) => "string",
            "ReadString" => "string",
            "WriteTimestamp" => "DateTime",
            "ReadTimestamp" => "DateTime",
            _ => null,
        };
        if (valueTypeName is null)
        {
            return;
        }

        // the extension members live in an extension block, so the invocation's method sits
        // in a synthesized container nested in the static class; walk up to the named owner
        if (!IsDeclaredIn(method.ReducedFrom ?? method, writeExtensions, readExtensions))
        {
            return;
        }

        if (!IsInsideFormatter(context.ContainingSymbol, formatterInterface))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), method.Name, valueTypeName));
    }

    static bool HasStringParameter(IMethodSymbol method)
    {
        foreach (var parameter in method.Parameters)
        {
            if (parameter.Type.SpecialType == SpecialType.System_String)
            {
                return true;
            }
        }
        return false;
    }

    static bool IsDeclaredIn(IMethodSymbol method, INamedTypeSymbol? writeExtensions, INamedTypeSymbol? readExtensions)
    {
        for (var type = method.ContainingType; type is not null; type = type.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(type, writeExtensions) ||
                SymbolEqualityComparer.Default.Equals(type, readExtensions))
            {
                return true;
            }
        }
        return false;
    }

    static bool IsInsideFormatter(ISymbol? containingSymbol, INamedTypeSymbol formatterInterface)
    {
        // nested helper classes (member slots and the like) count as long as any enclosing
        // type is a formatter: they serialize the formatter's values
        for (var type = containingSymbol?.ContainingType; type is not null; type = type.ContainingType)
        {
            foreach (var implemented in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, formatterInterface))
                {
                    return true;
                }
            }
        }
        return false;
    }
}

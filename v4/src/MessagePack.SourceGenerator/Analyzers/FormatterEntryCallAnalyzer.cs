using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// A formatter that calls a MessagePackSerializer entry point from inside its own
/// Serialize/Deserialize re-enters the serializer from the top: the active resolver and
/// options are replaced by whatever the entry call defaults to, the depth/security
/// tracking in SerializeState restarts from zero, and the buffer in flight is bypassed
/// entirely. The member's formatter should come from the Initialize-provided resolver
/// instead. MsgPack109 flags every entry invocation lexically inside an
/// IMessagePackFormatter implementation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FormatterEntryCallAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MsgPack109";

    const string SerializerTypeName = "MessagePack.MessagePackSerializer";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Formatters should not call serializer entry points",
        "'{0}' implements IMessagePackFormatter but calls MessagePackSerializer.{1}; an entry call restarts serialization with default options, bypassing the active resolver, the in-flight buffer, and the depth tracking — use the resolver handed to Initialize instead",
        "MessagePack.SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Entry points compose their own resolver, options and buffers. Inside a formatter the serialization is already in flight: nested values must go through formatters obtained from the Initialize-provided resolver, so options, custom tiers and the SerializeState depth/security accounting stay in effect.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var target = invocation.TargetMethod;
        if (!IsSerializerEntry(target))
        {
            return;
        }

        // lexical walk: lambdas and local functions inside a formatter method count too
        for (var containing = context.ContainingSymbol?.ContainingType; containing is not null; containing = containing.ContainingType)
        {
            if (ImplementsFormatterInterface(containing))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    invocation.Syntax.GetLocation(),
                    containing.ToDisplayString(),
                    target.Name));
                return;
            }
        }
    }

    static bool IsSerializerEntry(IMethodSymbol method)
    {
        for (var containing = method.ContainingType; containing is not null; containing = containing.ContainingType)
        {
            if (containing.ToDisplayString() == SerializerTypeName)
            {
                return true;
            }
        }
        return false;
    }

    static bool ImplementsFormatterInterface(INamedTypeSymbol type)
    {
        foreach (var implemented in type.AllInterfaces)
        {
            if (implemented.OriginalDefinition is { MetadataName: "IMessagePackFormatter`3" } original
                && original.ContainingNamespace.ToDisplayString() == "MessagePack")
            {
                return true;
            }
        }
        return false;
    }
}

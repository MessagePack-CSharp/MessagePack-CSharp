using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// DeserializeState carries a declared-element budget next to the depth budget: the message length, charged by every
/// array/map header a formatter reads through buffer.ReadArrayHeader(ref state) / buffer.ReadMapHeader(ref state).
/// Every element is a distinct msgpack value of at least one byte, so a message whose headers together declare more
/// elements than it has bytes is provably malformed, and the charging readers reject it before the formatter
/// preallocates from the claim. The parameterless ReadArrayHeader() / ReadMapHeader() only compare one header with the
/// bytes remaining after it, which nested headers all share: a payload of 500 nested headers each claiming the whole
/// tail passes that check at every level while the preallocations add up to 500 times the message. The parameterless
/// readers stay public for scanners that never allocate (Skip, envelope parsing), so nothing at compile time forces a
/// formatter onto the charging overload.
///
/// MsgPack115 reports a parameterless ReadArrayHeader() / ReadMapHeader() call in a body that has a DeserializeState
/// parameter to pass: the formatter is inside a deserialization with a budget and skips charging it.
/// The same read moved into a helper that is handed the buffer but not the state is reported at the call that leaves
/// the state behind: the helper itself has nothing to pass, so only its caller can be told.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DeclaredElementBudgetAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MsgPack115";

    const string DeserializeStateName = "MessagePack.DeserializeState";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Container header read without charging the declared-element budget",
        "{0}",
        "MessagePack.SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A formatter that reads an array or map header while a DeserializeState is in scope must read it through buffer.ReadArrayHeader(ref state) / buffer.ReadMapHeader(ref state), which charges the claimed count against the message-wide declared-element budget. The parameterless overloads compare the header only with the bytes that follow it, which nested headers share, so a malicious payload can make every nesting level preallocate for the whole message.");

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

        // the state that should have been passed: a DeserializeState parameter of the function whose body holds the
        // call (a local function or lambda has its own parameters; a ref parameter cannot be captured from the outside)
        var function = EnclosingFunction(invocation, context.ContainingSymbol);
        if (function is null || !HasDeserializeStateParameter(function))
        {
            return;
        }

        if (!IsStatelessHeaderRead(invocation))
        {
            if (StatelessHeaderReadIn(invocation, context.Compilation) is { } header)
            {
                var overload = header.Contains("Array") ? "ReadArrayHeader" : "ReadMapHeader";
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    invocation.Syntax.GetLocation(),
                    $"'{function.ContainingType.ToDisplayString()}.{function.Name}' hands its buffer to '{invocation.TargetMethod.Name}', which reads a container header with {header}() and is not given the DeserializeState in scope here; pass the state on and call buffer.{overload}(ref state) there so the claimed count is charged against the message's declared-element budget."));
            }
            return;
        }

        // the charging overload itself is implemented over the plain reader
        if (function.Name == invocation.TargetMethod.Name)
        {
            return;
        }

        var charging = invocation.TargetMethod.Name.Contains("Array") ? "ReadArrayHeader" : "ReadMapHeader";
        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.Syntax.GetLocation(),
            $"'{function.ContainingType.ToDisplayString()}.{function.Name}' reads a container header with {invocation.TargetMethod.Name}() while its DeserializeState is in scope; call buffer.{charging}(ref state) instead so the claimed count is charged against the message's declared-element budget."));
    }

    // buffer.ReadArrayHeader() / buffer.ReadMapHeader() with no DeserializeState argument, and the primitive decoders
    // MessagePackPrimitives.TryReadArrayHeader / TryReadMapHeader (a formatter decoding the header itself over a span and
    // advancing past it charges nothing either).
    // Matched by name, like the depth analyzer's container-header check, so the extension-member surface and
    // any buffer-tier twin both count; a call that already passes a DeserializeState is the charging overload.
    static bool IsStatelessHeaderRead(IInvocationOperation invocation)
    {
        if (invocation.TargetMethod.Name is "TryReadArrayHeader" or "TryReadMapHeader")
        {
            return invocation.TargetMethod.ContainingType?.Name == "MessagePackPrimitives";
        }
        if (invocation.TargetMethod.Name is not ("ReadArrayHeader" or "ReadMapHeader"))
        {
            return false;
        }
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Type.ToDisplayString() == DeserializeStateName)
            {
                return false;
            }
        }
        return true;
    }

    // A call that hands the buffer (a by-ref parameter of a type-parameter type, as the formatter interface declares
    // it) to a method or local function of this compilation that takes no DeserializeState and reads a container header
    // in its own body: the name of that header read. One level down, like the depth analyzer's helper look.
    static string? StatelessHeaderReadIn(IInvocationOperation invocation, Compilation compilation)
    {
        var target = invocation.TargetMethod;
        if (target.MethodKind is not (MethodKind.Ordinary or MethodKind.LocalFunction)
            || target.Name is "ReadArrayHeader" or "ReadMapHeader"
            || HasDeserializeStateParameter(target)
            || !ForwardsBuffer(invocation))
        {
            return null;
        }
        foreach (var reference in target.OriginalDefinition.DeclaringSyntaxReferences)
        {
            if (!compilation.ContainsSyntaxTree(reference.SyntaxTree))
            {
                continue; // a helper of another project: not this compilation's tree
            }
            var syntax = reference.GetSyntax();
            if (FormatterSymbols.SemanticModelOf(syntax.SyntaxTree, invocation, compilation).GetOperation(syntax) is not { } body)
            {
                continue;
            }
            foreach (var operation in body.Descendants())
            {
                if (operation is IInvocationOperation nested && IsStatelessHeaderRead(nested))
                {
                    return nested.TargetMethod.Name;
                }
            }
        }
        return null;
    }

    static bool ForwardsBuffer(IInvocationOperation invocation)
    {
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.RefKind == RefKind.Ref
                && argument.Value is IParameterReferenceOperation { Parameter: { RefKind: RefKind.Ref, Type: ITypeParameterSymbol } })
            {
                return true;
            }
        }
        return false;
    }

    static IMethodSymbol? EnclosingFunction(IOperation operation, ISymbol containingSymbol)
    {
        for (var current = operation.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case ILocalFunctionOperation localFunction:
                    return localFunction.Symbol;
                case IAnonymousFunctionOperation lambda:
                    return lambda.Symbol;
            }
        }
        return containingSymbol as IMethodSymbol;
    }

    static bool HasDeserializeStateParameter(IMethodSymbol method)
    {
        foreach (var parameter in method.Parameters)
        {
            if (parameter.Type.ToDisplayString() == DeserializeStateName)
            {
                return true;
            }
        }
        return false;
    }
}

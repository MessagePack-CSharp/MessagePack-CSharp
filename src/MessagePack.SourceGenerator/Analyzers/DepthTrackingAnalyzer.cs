using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// SerializeState/DeserializeState carry the depth budget as a countdown that state.Enter()
/// charges and state.Exit() refunds. The contract: a formatter charges one level when it descends into another
/// formatter, and flat shapes (primitive arrays, scalars written inline) never charge.
/// The pair is hand-written in every formatter that nests (the generated ones always emit it),
/// and a mistake is silent at runtime: an Exit skipped by an early return leaks budget for the rest of the operation
/// until a deep-but-legal graph throws, an unmatched Exit hands the budget back so a malicious payload recurses past
/// the limit, and a container that never Enters is invisible to the limit altogether.
///
/// MsgPack112 walks the control flow graph of every body that calls Enter or Exit and reports a depth that differs
/// between paths (a conditional Enter, an Enter inside a loop without its Exit), an Exit with nothing to refund,
/// and a return with an Enter still open. A throw ends the operation and is not a path.
/// Finally regions contribute their net effect to the branch that runs them; catch blocks are outside the analysis.
///
/// MsgPack113 covers the missing pair: an IMessagePackFormatter Serialize/Deserialize that writes or reads an array/map
/// header and descends into a nested formatter (passes its state by ref to a Serialize/Deserialize)
/// is a container level and must charge the budget. Wrappers that only delegate (Nullable, surrogates)
/// and flat shapes that write a header around primitives never descend, so they stay silent on purpose;
/// so does a header with a constant count of 0, which opens nothing.
/// The descent, or the whole container (header and descent), may sit in a helper the entry hands its buffer and
/// state to (one level down): the entry is reported unless it, or that helper, charges the budget. A helper's header
/// is never paired with a descent somewhere else in the entry: a wrapper that delegates on one path and reads a flat
/// shape through a helper on another is not a level. A body that opens a container and calls
/// itself with the state for the nested values is a level at every turn of the recursion, formatter entry or not,
/// and is reported on its own: nothing a caller charges once covers it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DepthTrackingAnalyzer : DiagnosticAnalyzer
{
    public const string UnbalancedDiagnosticId = "MsgPack112";
    public const string MissingEnterDiagnosticId = "MsgPack113";

    const string SerializeStateName = "MessagePack.SerializeState";
    const string DeserializeStateName = "MessagePack.DeserializeState";

    static readonly DiagnosticDescriptor UnbalancedRule = new(
        UnbalancedDiagnosticId,
        "state.Enter() and state.Exit() must pair on every path",
        "{0}",
        "MessagePack.SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The depth budget in SerializeState/DeserializeState is a countdown charged by Enter and refunded by Exit. Every path through a body that calls either must reach its end with the calls paired: a skipped Exit leaks budget until a legal graph throws, an extra Exit lets a payload recurse past the configured limit.");

    static readonly DiagnosticDescriptor MissingEnterRule = new(
        MissingEnterDiagnosticId,
        "Container formatter never charges the depth budget",
        "{0}",
        "MessagePack.SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A formatter that writes or reads an array/map header and then descends into nested formatters is one nesting level of the graph. It must call state.Enter() before descending and state.Exit() afterwards, otherwise the depth limit never sees this level.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UnbalancedRule, MissingEnterRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationBlockAction(AnalyzeBlocks);
    }

    static void AnalyzeBlocks(OperationBlockAnalysisContext context)
    {
        if (context.OwningSymbol is not IMethodSymbol method)
        {
            return;
        }

        foreach (var block in context.OperationBlocks)
        {
            // a method body arrives as the IBlockOperation of its BlockSyntax (or the expression body);
            // initializers and attribute arguments have no flow to balance
            if (block is not (IBlockOperation or IMethodBodyOperation or IConstructorBodyOperation))
            {
                continue;
            }

            var callsDepthTracking = false;
            var opensContainer = false;
            var descends = false;
            var recurses = false;
            var chargedByHelper = false;
            var containerInHelper = false;
            string? helperName = null;
            foreach (var operation in block.Descendants())
            {
                if (operation is not IInvocationOperation invocation)
                {
                    continue;
                }
                if (ClassifyDepthCall(invocation) != DepthCall.None)
                {
                    callsDepthTracking = true;
                }
                else if (IsContainerHeaderCall(invocation))
                {
                    opensContainer = true;
                }
                else if (ForwardsStateToFormatter(invocation))
                {
                    descends = true;
                }
                else if (ForwardsState(invocation) && IsCallToEnclosingFunction(invocation))
                {
                    recurses = true;
                }
                else if (ForwardsBufferAndState(invocation))
                {
                    var helper = SummarizeHelper(invocation, context.Compilation);
                    descends |= helper.Descends;
                    containerInHelper |= helper.OpensContainer && helper.Descends;
                    chargedByHelper |= helper.Charges;
                    if (helper.Descends)
                    {
                        helperName ??= invocation.TargetMethod.Name;
                    }
                }
            }

            if (callsDepthTracking)
            {
                AnalyzeGraph(context.GetControlFlowGraph(block), method.Locations[0], context.ReportDiagnostic);
            }
            else if (opensContainer && recurses)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingEnterRule,
                    method.Locations[0],
                    $"'{method.ContainingType.ToDisplayString()}.{method.Name}' writes or reads a container header and calls itself for the nested values without state.Enter(); every turn of the recursion is a nesting level, so wrap the nested call in state.Enter() / state.Exit() or the depth limit never sees it."));
            }
            else if (((opensContainer && descends) || containerInHelper) && !chargedByHelper && FormatterSymbols.IsFormatterEntry(method))
            {
                var through = helperName is null ? "" : $" (through its helper '{helperName}')";
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingEnterRule,
                    method.Locations[0],
                    $"'{method.ContainingType.ToDisplayString()}.{method.Name}' writes or reads a container header and descends into nested formatters{through} without state.Enter(); wrap the nested calls in state.Enter() / state.Exit() so the depth limit counts this level."));
            }
        }
    }

    // every body gets its own balance: the containing method, then each local function and lambda through their own
    // graphs (nested ones recurse)
    static void AnalyzeGraph(ControlFlowGraph graph, Location location, Action<Diagnostic> report)
    {
        if (CheckBalance(graph) is { } message)
        {
            report(Diagnostic.Create(UnbalancedRule, location, message));
        }

        foreach (var localFunction in graph.LocalFunctions)
        {
            AnalyzeGraph(graph.GetLocalFunctionControlFlowGraph(localFunction), localFunction.Locations[0], report);
        }

        foreach (var block in graph.Blocks)
        {
            foreach (var operation in EnumerateOperations(block))
            {
                foreach (var lambda in operation.DescendantsAndSelf().OfType<IFlowAnonymousFunctionOperation>())
                {
                    AnalyzeGraph(graph.GetAnonymousFunctionControlFlowGraph(lambda), lambda.Syntax.GetLocation(), report);
                }
            }
        }
    }

    // Forward dataflow of one integer (open Enter count) per basic block,
    // run to a fixpoint before anything is judged so the verdict does not depend on block order.
    // A block reached with two different depths is a conflict; the exit block instead remembers the deepest arrival,
    // so an early return that skips the Exit reads as "still open" rather than as a merge conflict.
    // A block's depth changes at most twice (set, then conflict), which bounds the walk.
    static string? CheckBalance(ControlFlowGraph graph)
    {
        const int Unvisited = int.MinValue;
        const int Conflict = int.MinValue + 1;

        var blocks = graph.Blocks;
        var depths = new int[blocks.Length];
        for (var i = 0; i < depths.Length; i++)
        {
            depths[i] = Unvisited;
        }

        // the graph lists its entry block first and its exit block last
        var entry = blocks[0];
        var exit = blocks[blocks.Length - 1];
        var work = new Queue<int>();
        var wentNegative = false;
        var deepestExit = Unvisited;
        depths[entry.Ordinal] = 0;
        work.Enqueue(entry.Ordinal);

        while (work.Count > 0)
        {
            var block = blocks[work.Dequeue()];
            var depth = depths[block.Ordinal];
            if (depth != Conflict)
            {
                foreach (var operation in EnumerateOperations(block))
                {
                    foreach (var invocation in operation.DescendantsAndSelf().OfType<IInvocationOperation>())
                    {
                        switch (ClassifyDepthCall(invocation))
                        {
                            case DepthCall.Enter:
                                depth++;
                                break;
                            case DepthCall.Exit:
                                depth--;
                                if (depth < 0)
                                {
                                    wentNegative = true;
                                }
                                break;
                        }
                    }
                }
            }

            Propagate(block.FallThroughSuccessor, depth);
            Propagate(block.ConditionalSuccessor, depth);
        }

        for (var i = 0; i < depths.Length; i++)
        {
            if (depths[i] == Conflict)
            {
                return "state.Enter() and state.Exit() are not paired the same way on every path: control flow merges with different depth-tracking counts (a conditional Enter without its Exit under the same condition, or an Enter inside a loop whose Exit is outside).";
            }
        }
        if (wentNegative)
        {
            return "state.Exit() is reachable without a preceding state.Enter() on the same path; the extra refund lets a payload recurse past the depth limit.";
        }
        if (deepestExit > 0)
        {
            return deepestExit == 1
                ? "a path returns with state.Enter() still open (no state.Exit() before the return); the leaked depth budget makes a later legal graph throw."
                : $"a path returns with {deepestExit} state.Enter() calls still open (no matching state.Exit() before the return); the leaked depth budget makes a later legal graph throw.";
        }
        return null;

        void Propagate(ControlFlowBranch? branch, int depth)
        {
            if (branch?.Destination is not { } destination)
            {
                return; // throw / rethrow: the operation ends, no path to balance
            }

            if (depth != Conflict)
            {
                foreach (var finallyRegion in branch.FinallyRegions)
                {
                    depth += NetEffect(blocks, finallyRegion);
                }
            }

            if (destination.Ordinal == exit.Ordinal)
            {
                if (depth != Conflict && depth > deepestExit)
                {
                    deepestExit = depth;
                }
                return;
            }

            var current = depths[destination.Ordinal];
            if (current == Unvisited)
            {
                depths[destination.Ordinal] = depth;
                work.Enqueue(destination.Ordinal);
            }
            else if (current != Conflict && current != depth)
            {
                depths[destination.Ordinal] = Conflict;
                work.Enqueue(destination.Ordinal);
            }
        }
    }

    // finally blocks have no regular predecessor edge in the graph; the branch that runs them carries the region,
    // and the region's Enter minus Exit count is its effect
    static int NetEffect(ImmutableArray<BasicBlock> blocks, ControlFlowRegion region)
    {
        var effect = 0;
        for (var ordinal = region.FirstBlockOrdinal; ordinal <= region.LastBlockOrdinal; ordinal++)
        {
            foreach (var operation in EnumerateOperations(blocks[ordinal]))
            {
                foreach (var invocation in operation.DescendantsAndSelf().OfType<IInvocationOperation>())
                {
                    effect += ClassifyDepthCall(invocation) switch
                    {
                        DepthCall.Enter => 1,
                        DepthCall.Exit => -1,
                        _ => 0,
                    };
                }
            }
        }
        return effect;
    }

    static IEnumerable<IOperation> EnumerateOperations(BasicBlock block)
    {
        foreach (var operation in block.Operations)
        {
            yield return operation;
        }
        if (block.BranchValue is { } branchValue)
        {
            yield return branchValue;
        }
    }

    enum DepthCall
    {
        None,
        Enter,
        Exit,
    }

    static DepthCall ClassifyDepthCall(IInvocationOperation invocation)
    {
        var target = invocation.TargetMethod;
        if (target.Parameters.Length != 0 || target.Name is not ("Enter" or "Exit"))
        {
            return DepthCall.None;
        }
        var containingType = target.ContainingType?.ToDisplayString();
        if (containingType is not (SerializeStateName or DeserializeStateName))
        {
            return DepthCall.None;
        }
        return target.Name == "Enter" ? DepthCall.Enter : DepthCall.Exit;
    }

    // (Try)(Write|Read)(Fix)?(Array|Map)Header...: the msgpack container openers of the buffer surface,
    // matched by name so extension-method and buffer-tier twins all count.
    // A header written with a constant count of 0 (a bare object as an empty map)
    // has no elements to descend into and is not a nesting level.
    static bool IsContainerHeaderCall(IInvocationOperation invocation)
    {
        if (invocation.Arguments.Length == 1
            && invocation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: 0 })
        {
            return false;
        }
        var name = invocation.TargetMethod.Name;
        name = TrimPrefix(name, "Try");
        if (TrimPrefix(name, "Write") is { } afterWrite && afterWrite != name)
        {
            name = afterWrite;
        }
        else if (TrimPrefix(name, "Read") is { } afterRead && afterRead != name)
        {
            name = afterRead;
        }
        else
        {
            return false;
        }
        name = TrimPrefix(name, "Fix");
        if (TrimPrefix(name, "Array") is { } afterArray && afterArray != name)
        {
            name = afterArray;
        }
        else if (TrimPrefix(name, "Map") is { } afterMap && afterMap != name)
        {
            name = afterMap;
        }
        else
        {
            return false;
        }
        return name.StartsWith("Header", StringComparison.Ordinal);
    }

    static string TrimPrefix(string name, string prefix) =>
        name.StartsWith(prefix, StringComparison.Ordinal) ? name.Substring(prefix.Length) : name;

    // A container split into a helper: the entry hands both its buffer and its state on by ref to a method of its own.
    // What that method's body does counts for the entry, one level down: a container header it reads or writes, a
    // descent (the state forwarded to a Serialize/Deserialize, or to itself), and a charge of its own (Enter/Exit there
    // covers the level, and MsgPack112 balances it in that body). A helper that only skips or reads scalars with the
    // state (version-tolerance tails, float readers) is none of the three.
    static HelperSummary SummarizeHelper(IInvocationOperation invocation, Compilation compilation)
    {
        var summary = default(HelperSummary);
        foreach (var reference in invocation.TargetMethod.DeclaringSyntaxReferences)
        {
            // a helper in another project is a source symbol of THAT compilation in the IDE (project references are
            // compilations there): its tree is not ours, and asking this compilation for its model throws (AD0001)
            if (!compilation.ContainsSyntaxTree(reference.SyntaxTree))
            {
                continue;
            }
            var syntax = reference.GetSyntax();
            if (FormatterSymbols.SemanticModelOf(syntax.SyntaxTree, invocation, compilation).GetOperation(syntax) is not { } body)
            {
                continue;
            }
            foreach (var operation in body.Descendants())
            {
                if (operation is not IInvocationOperation nested)
                {
                    continue;
                }
                if (ClassifyDepthCall(nested) != DepthCall.None)
                {
                    summary.Charges = true;
                }
                else if (IsContainerHeaderCall(nested))
                {
                    summary.OpensContainer = true;
                }
                else if (ForwardsStateToFormatter(nested) || (ForwardsState(nested) && IsCallToEnclosingFunction(nested)))
                {
                    summary.Descends = true;
                }
            }
        }
        return summary;
    }

    struct HelperSummary
    {
        public bool OpensContainer;
        public bool Descends;
        public bool Charges;
    }

    // a call written inside the body of the function it calls (a method or a local function): direct recursion
    static bool IsCallToEnclosingFunction(IInvocationOperation invocation)
    {
        var target = invocation.TargetMethod.OriginalDefinition;
        if (target.MethodKind is not (MethodKind.Ordinary or MethodKind.LocalFunction))
        {
            return false;
        }
        foreach (var reference in target.DeclaringSyntaxReferences)
        {
            if (reference.SyntaxTree == invocation.Syntax.SyntaxTree && reference.Span.Contains(invocation.Syntax.Span))
            {
                return true;
            }
        }
        return false;
    }

    // the state parameter handed on by ref to anything but a formatter's Serialize/Deserialize
    static bool ForwardsState(IInvocationOperation invocation)
    {
        if (invocation.TargetMethod.Name is "Serialize" or "Deserialize")
        {
            return false;
        }
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.RefKind == RefKind.Ref
                && argument.Value is IParameterReferenceOperation { Parameter.Type: { } type }
                && type.ToDisplayString() is SerializeStateName or DeserializeStateName)
            {
                return true;
            }
        }
        return false;
    }

    static bool ForwardsBufferAndState(IInvocationOperation invocation)
    {
        if (invocation.TargetMethod.Name is "Serialize" or "Deserialize" || invocation.TargetMethod.MethodKind != MethodKind.Ordinary)
        {
            return false;
        }
        var forwardsState = false;
        var forwardsBuffer = false;
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.RefKind != RefKind.Ref || argument.Value is not IParameterReferenceOperation { Parameter.Type: { } type })
            {
                continue;
            }
            if (type.ToDisplayString() is SerializeStateName or DeserializeStateName)
            {
                forwardsState = true;
            }
            else if (type is ITypeParameterSymbol)
            {
                forwardsBuffer = true; // the buffer: the entry's other by-ref parameter, of a type-parameter type
            }
        }
        return forwardsState && forwardsBuffer;
    }

    // a nested-formatter descent: the state parameter handed by ref to a Serialize/Deserialize
    static bool ForwardsStateToFormatter(IInvocationOperation invocation)
    {
        if (invocation.TargetMethod.Name is not ("Serialize" or "Deserialize"))
        {
            return false;
        }
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.RefKind == RefKind.Ref
                && argument.Value is IParameterReferenceOperation { Parameter.Type: { } type }
                && type.ToDisplayString() is SerializeStateName or DeserializeStateName)
            {
                return true;
            }
        }
        return false;
    }
}

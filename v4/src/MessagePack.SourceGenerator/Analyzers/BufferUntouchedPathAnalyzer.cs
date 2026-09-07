using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// A formatter's Serialize must put exactly one msgpack value on the wire and its Deserialize must consume exactly one;
/// the classic hand-written slip is a path that does neither, `if (value == null) return;` without WriteNil,
/// or a read side that returns a default without consuming the element. The stream then loses (or keeps)
/// one value and every element after it lands in the wrong slot, usually far from the bug.
/// MsgPack114 walks the control flow graph of each Serialize/Deserialize implementation and reports when some path can
/// reach the end without ever touching the buffer parameter. Any use of the buffer counts as touching it (a write,
/// a read, a header, a ref forward to a helper or a nested formatter),
/// so the analysis only ever flags a path that provably does nothing with the stream;
/// a throw ends the operation and is not a path.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BufferUntouchedPathAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MsgPack114";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Formatter path leaves the buffer untouched",
        "{0}",
        "MessagePack.SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Serialize must write exactly one msgpack value and Deserialize must consume exactly one. A path that returns without touching the buffer at all (a null early return without WriteNil, a read side that returns a default without reading) shifts every later element of the stream.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationBlockAction(AnalyzeBlocks);
    }

    static void AnalyzeBlocks(OperationBlockAnalysisContext context)
    {
        if (context.OwningSymbol is not IMethodSymbol { Parameters.Length: >= 2 } method
            || !FormatterSymbols.IsFormatterEntry(method))
        {
            return;
        }
        var bufferParameter = method.Parameters[0];

        foreach (var block in context.OperationBlocks)
        {
            if (block is not (IBlockOperation or IMethodBodyOperation))
            {
                continue;
            }
            if (!MayFinishUntouched(context.GetControlFlowGraph(block), bufferParameter))
            {
                continue;
            }

            var message = method.Name == "Serialize"
                ? $"'{method.ContainingType.ToDisplayString()}.Serialize' can return without writing anything to the buffer; the stream is then missing this value and every later element shifts (write nil for the null case, or throw)."
                : $"'{method.ContainingType.ToDisplayString()}.Deserialize' can return without reading anything from the buffer; this value stays in the stream and every later read shifts (consume the value, or throw).";
            context.ReportDiagnostic(Diagnostic.Create(Rule, method.Locations[0], message));
        }
    }

    // forward may-analysis over the graph: a block is "possibly untouched" when some path reaches it without a buffer
    // reference; merges or, so the fixpoint is reached once every block's flag has stopped rising
    static bool MayFinishUntouched(ControlFlowGraph graph, IParameterSymbol bufferParameter)
    {
        var blocks = graph.Blocks;
        var untouched = new bool[blocks.Length];
        var visited = new bool[blocks.Length];
        var exit = blocks[blocks.Length - 1];
        var work = new Queue<int>();
        untouched[0] = true;
        visited[0] = true;
        work.Enqueue(0);

        while (work.Count > 0)
        {
            var block = blocks[work.Dequeue()];
            var stillUntouched = untouched[block.Ordinal] && !Touches(block, bufferParameter);
            Propagate(block.FallThroughSuccessor, stillUntouched);
            Propagate(block.ConditionalSuccessor, stillUntouched);
        }

        return visited[exit.Ordinal] && untouched[exit.Ordinal];

        void Propagate(ControlFlowBranch? branch, bool stillUntouched)
        {
            if (branch?.Destination is not { } destination)
            {
                return; // throw / rethrow: the operation ends, no path to judge
            }
            if (stillUntouched)
            {
                foreach (var finallyRegion in branch.FinallyRegions)
                {
                    for (var ordinal = finallyRegion.FirstBlockOrdinal; ordinal <= finallyRegion.LastBlockOrdinal && stillUntouched; ordinal++)
                    {
                        stillUntouched = !Touches(blocks[ordinal], bufferParameter);
                    }
                }
            }
            if (!visited[destination.Ordinal])
            {
                visited[destination.Ordinal] = true;
                untouched[destination.Ordinal] = stillUntouched;
                work.Enqueue(destination.Ordinal);
            }
            else if (stillUntouched && !untouched[destination.Ordinal])
            {
                untouched[destination.Ordinal] = true;
                work.Enqueue(destination.Ordinal);
            }
        }
    }

    static bool Touches(BasicBlock block, IParameterSymbol bufferParameter)
    {
        foreach (var operation in block.Operations)
        {
            if (References(operation, bufferParameter))
            {
                return true;
            }
        }
        return block.BranchValue is { } branchValue && References(branchValue, bufferParameter);
    }

    static bool References(IOperation operation, IParameterSymbol bufferParameter)
    {
        foreach (var descendant in operation.DescendantsAndSelf())
        {
            if (descendant is IParameterReferenceOperation reference
                && SymbolEqualityComparer.Default.Equals(reference.Parameter, bufferParameter))
            {
                return true;
            }
        }
        return false;
    }
}

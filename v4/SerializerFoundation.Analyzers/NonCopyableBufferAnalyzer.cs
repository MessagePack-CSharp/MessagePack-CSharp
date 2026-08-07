using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SerializerFoundation.Analyzers;

/// <summary>
/// Every struct implementing IWriteBuffer/IReadBuffer is
/// single-owner mutable state — a copy diverges silently (two write indexes over one
/// window) and double-returns pooled arrays at Dispose. The BCL declined a general
/// [NonCopyable] (dotnet/runtime#50389), and for this library no attribute is needed
/// anyway: implementing a buffer interface IS the non-copyable marker, so the analyzer
/// keys directly on that.
///
/// v1 rules (move-friendly: fresh values — object creation, default, method returns —
/// may be assigned or passed by value, transferring ownership):
///  - assigning/initializing from an EXISTING buffer value (local, parameter, field)
///  - passing an existing buffer value as a by-value argument
///  - declaring a by-value buffer parameter (ref/in/out are the sanctioned shapes)
///  - boxing a buffer struct (interface/object conversion — the plain-struct fallback
///    tier compiles this happily and every mutation then hits a hidden copy)
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonCopyableBufferAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SF002";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Buffer structs are single-owner and must not be copied",
        "'{0}' implements a buffer interface and must not be {1}; buffers are single-owner — pass by ref or construct in place",
        "SerializerFoundation.Correctness",
        DiagnosticSeverity.Error, // this must not allow silent divergence of mutable state, so it's an error, not a warning
        isEnabledByDefault: true,
        description: "Structs implementing IWriteBuffer/IReadBuffer (or the async flavors) hold single-owner mutable state (write indexes, rented pool arrays, pinned windows). A copy diverges silently and double-disposes pooled state. Pass buffers by ref (the formatter contract), construct them in place, and never box them.");

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

            startContext.RegisterOperationAction(
                context => AnalyzeAssignment(context, interfaces),
                OperationKind.SimpleAssignment,
                OperationKind.VariableDeclarator);
            startContext.RegisterOperationAction(
                context => AnalyzeArgument(context, interfaces),
                OperationKind.Argument);
            startContext.RegisterOperationAction(
                context => AnalyzeConversion(context, interfaces),
                OperationKind.Conversion);
            startContext.RegisterOperationAction(
                context => AnalyzeParameters(context, ((ILocalFunctionOperation)context.Operation).Symbol, interfaces),
                OperationKind.LocalFunction);
            startContext.RegisterSymbolAction(
                context => AnalyzeParameters(context, (IMethodSymbol)context.Symbol, interfaces),
                SymbolKind.Method);
        });
    }

    static void AnalyzeAssignment(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var (target, value) = context.Operation switch
        {
            ISimpleAssignmentOperation assignment => ((ITypeSymbol?)assignment.Target.Type, assignment.Value),
            IVariableDeclaratorOperation { Initializer.Value: { } initializer } declarator => (declarator.Symbol.Type, initializer),
            _ => (null, null),
        };
        if (target is null || value is null || !IsBufferStruct(target, interfaces) || IsFreshValue(value))
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(Rule, value.Syntax.GetLocation(), target.Name, "copied by assignment"));
    }

    static void AnalyzeArgument(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var argument = (IArgumentOperation)context.Operation;
        if (argument.Parameter is not { RefKind: RefKind.None } ||
            argument.Value.Type is not { } type ||
            !IsBufferStruct(type, interfaces) ||
            IsFreshValue(argument.Value))
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Syntax.GetLocation(), type.Name, "passed by value"));
    }

    static void AnalyzeConversion(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var conversion = (IConversionOperation)context.Operation;
        if (conversion.Type is not { IsReferenceType: true } ||
            conversion.Operand.Type is not { } operandType ||
            !IsBufferStruct(operandType, interfaces))
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(Rule, conversion.Syntax.GetLocation(), operandType.Name, "boxed"));
    }

    static void AnalyzeParameters(DiagnosticReporter context, IMethodSymbol method, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.LocalFunction or MethodKind.Constructor))
        {
            return;
        }
        foreach (var parameter in method.Parameters)
        {
            if (parameter.RefKind == RefKind.None && IsBufferStruct(parameter.Type, interfaces))
            {
                var location = parameter.Locations.IsDefaultOrEmpty ? Location.None : parameter.Locations[0];
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, parameter.Type.Name, "received by value (declare the parameter ref/in/out)"));
            }
        }
    }

    // fresh values transfer ownership (move, not copy): construction, default, and
    // method returns; everything read from an existing storage location is a copy
    static bool IsFreshValue(IOperation value)
    {
        while (value is IConversionOperation conversion)
        {
            value = conversion.Operand;
        }
        return value is IObjectCreationOperation or IDefaultValueOperation or IInvocationOperation or ILiteralOperation;
    }

    static bool IsBufferStruct(ITypeSymbol type, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        if (type is ITypeParameterSymbol typeParameter)
        {
            foreach (var constraint in typeParameter.ConstraintTypes)
            {
                if (IsOrImplementsBufferInterface(constraint, interfaces))
                {
                    return true;
                }
            }
            return false;
        }
        return type.IsValueType && IsOrImplementsBufferInterface(type, interfaces);
    }

    static bool IsOrImplementsBufferInterface(ITypeSymbol type, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        foreach (var bufferInterface in interfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(type, bufferInterface))
            {
                return true;
            }
        }
        foreach (var implemented in type.AllInterfaces)
        {
            foreach (var bufferInterface in interfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implemented, bufferInterface))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>Adapter so parameter checks share code between symbol and operation contexts.</summary>
    readonly struct DiagnosticReporter
    {
        readonly Action<Diagnostic> report;

        DiagnosticReporter(Action<Diagnostic> report) => this.report = report;

        public void ReportDiagnostic(Diagnostic diagnostic) => report(diagnostic);

        public static implicit operator DiagnosticReporter(SymbolAnalysisContext context) => new(context.ReportDiagnostic);

        public static implicit operator DiagnosticReporter(OperationAnalysisContext context) => new(context.ReportDiagnostic);
    }
}

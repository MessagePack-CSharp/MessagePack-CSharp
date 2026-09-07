using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SerializerFoundation.Analyzers;

/// <summary>
/// Every struct implementing IWriteBuffer/IReadBuffer is single-owner mutable state,
/// a copy diverges silently (two write indexes over one window) and double-returns pooled arrays at Dispose.
/// The BCL declined a general [NonCopyable] (dotnet/runtime#50389),
/// and for this library no attribute is needed anyway: implementing a buffer interface IS the non-copyable marker,
/// so the analyzer keys directly on that.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonCopyableBufferAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SF002";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Buffer structs are single-owner and must not be copied",
        "'{0}' implements a buffer interface and must not be {1}; buffers are single-owner - pass by ref or construct in place",
        "SerializerFoundation.Correctness",
        DiagnosticSeverity.Error, // this must not allow silent divergence of mutable state, so it's an error, not a warning
        isEnabledByDefault: true,
        description: "Structs implementing IWriteBuffer/IReadBuffer (or the async flavors) hold single-owner mutable state (write indexes, rented pool arrays, pinned windows). A copy diverges silently and double-disposes pooled state. Pass buffers by ref (the formatter contract), construct them in place, and never box them. `in` and `ref readonly` parameters count as copies: the buffer's mutating members act on a defensive copy, and so does any non-readonly member called through a readonly field or a ref readonly reference.");

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
            startContext.RegisterOperationAction(
                context => AnalyzeParameters(context, ((IAnonymousFunctionOperation)context.Operation).Symbol, interfaces),
                OperationKind.AnonymousFunction);
            startContext.RegisterSymbolAction(
                context => AnalyzeParameters(context, (IMethodSymbol)context.Symbol, interfaces),
                SymbolKind.Method);
            // delegate Invoke is implicitly declared, so the Method symbol action never
            // sees it; the delegate TYPE is the source-declared symbol to hook
            startContext.RegisterSymbolAction(
                context =>
                {
                    if (((INamedTypeSymbol)context.Symbol).DelegateInvokeMethod is { } invoke)
                    {
                        AnalyzeParameters(context, invoke, interfaces);
                    }
                },
                SymbolKind.NamedType);
            startContext.RegisterSymbolAction(
                context => AnalyzeProperty(context, interfaces),
                SymbolKind.Property);
            startContext.RegisterOperationAction(
                context => AnalyzeReturn(context, interfaces),
                OperationKind.Return,
                OperationKind.YieldReturn);
            startContext.RegisterOperationAction(
                context => AnalyzeForEach(context, interfaces),
                OperationKind.Loop);
            startContext.RegisterOperationAction(
                context => AnalyzePattern(context, interfaces),
                OperationKind.DeclarationPattern,
                OperationKind.RecursivePattern);
            startContext.RegisterOperationAction(
                context => AnalyzeWith(context, interfaces),
                OperationKind.With);
            startContext.RegisterOperationAction(
                context => AnalyzeTuple(context, interfaces),
                OperationKind.Tuple);
            // KNOWN GAP: C#12 collection-expression elements ([a]) need
            // ICollectionExpressionOperation, which arrived after the deliberate
            // Microsoft.CodeAnalysis 4.8 floor (see the csproj comment); wire it up when
            // the floor moves to 4.10+
            startContext.RegisterOperationAction(
                context => AnalyzeCollectionElements(context, ((IArrayInitializerOperation)context.Operation).ElementValues, interfaces),
                OperationKind.ArrayInitializer);
            startContext.RegisterOperationAction(
                context => AnalyzeUsing(context, interfaces),
                OperationKind.Using);
            startContext.RegisterOperationAction(
                context => AnalyzeReadOnlyReceiver(context, interfaces),
                OperationKind.Invocation);
        });
    }

    static void AnalyzeAssignment(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var (target, value) = context.Operation switch
        {
            // `r = ref b` and `ref var r = ref b` rebind a reference, no value moves
            ISimpleAssignmentOperation { IsRef: false } assignment => ((ITypeSymbol?)assignment.Target.Type, assignment.Value),
            IVariableDeclaratorOperation { Symbol.IsRef: false, Initializer.Value: { } initializer } declarator => (declarator.Symbol.Type, initializer),
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
        if (argument.Parameter is not { } parameter || argument.Value.Type is not { } type || !IsBufferStruct(type, interfaces))
        {
            return;
        }
        if (parameter.RefKind is RefKind.In or RefKind.RefReadOnlyParameter)
        {
            // no copy at the call site, but the callee cannot mutate through a readonly reference:
            // every mutating member call inside acts on a hidden defensive copy, so the
            // caller's buffer never advances (flagged even for a fresh value, the callee is wrong either way)
            context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Syntax.GetLocation(), type.Name, "passed as a readonly reference (the callee's in/ref readonly parameter mutates a defensive copy)"));
            return;
        }
        if (parameter.RefKind != RefKind.None || IsFreshValue(argument.Value))
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Syntax.GetLocation(), type.Name, "passed by value"));
    }

    static void AnalyzeConversion(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var conversion = (IConversionOperation)context.Operation;
        if (conversion.Operand.Type is not { } operandType || !IsBufferStruct(operandType, interfaces))
        {
            return;
        }
        if (conversion.Type is { IsReferenceType: true })
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, conversion.Syntax.GetLocation(), operandType.Name, "boxed"));
        }
        else if (conversion.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
        {
            // not a box, but the same escape: the wrapper is not a buffer type, so the
            // copy inside it dodges every other rule from here on
            context.ReportDiagnostic(Diagnostic.Create(Rule, conversion.Syntax.GetLocation(), operandType.Name, "wrapped in a Nullable"));
        }
    }

    static void AnalyzeParameters(DiagnosticReporter context, IMethodSymbol method, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.LocalFunction or MethodKind.Constructor
            or MethodKind.AnonymousFunction or MethodKind.DelegateInvoke or MethodKind.UserDefinedOperator or MethodKind.Conversion))
        {
            return;
        }
        foreach (var parameter in method.Parameters)
        {
            // ref and out are the only sanctioned shapes: a buffer's members are mutating, so an
            // in / ref readonly parameter is a by-value parameter in disguise (each mutating
            // call acts on a hidden defensive copy and the caller's buffer never advances)
            if (parameter.RefKind is not (RefKind.None or RefKind.In or RefKind.RefReadOnlyParameter) || !IsBufferStruct(parameter.Type, interfaces))
            {
                continue;
            }
            var location = parameter.Locations.IsDefaultOrEmpty ? Location.None : parameter.Locations[0];
            var shape = parameter.RefKind == RefKind.None
                ? "received by value (declare the parameter ref)"
                : "received as a readonly reference (in/ref readonly: every mutating call acts on a hidden defensive copy; declare the parameter ref)";
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, parameter.Type.Name, shape));
        }
    }

    // the getter of a by-value property hands out a COPY on every access, so
    // `x.Buffer.Dispose()` silently disposes a temporary — no other rule can see that,
    // because no assignment/argument/conversion is involved at the use site
    static void AnalyzeProperty(SymbolAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var property = (IPropertySymbol)context.Symbol;
        if (property.RefKind != RefKind.None || !IsBufferStruct(property.Type, interfaces))
        {
            return;
        }
        var location = property.Locations.IsDefaultOrEmpty ? Location.None : property.Locations[0];
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, property.Type.Name, "exposed as a by-value property (use a ref-returning property or a field)"));
    }

    // returning a LOCAL or a fresh value is the factory move (the storage dies with the
    // frame); returning a field, a ref target, or a by-ref parameter duplicates storage
    // that stays alive behind the caller's back. Ref-returning members hand out the
    // storage itself and are exempt. A `using` local is the exception among locals: the
    // frame disposes it on exit, so the caller receives a copy of a dead buffer (and the
    // pooled state behind it is returned twice).
    static void AnalyzeReturn(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var returnOperation = (IReturnOperation)context.Operation;
        if (returnOperation.ReturnedValue is not { Type: { } type } value ||
            !IsBufferStruct(type, interfaces) ||
            context.ContainingSymbol is IMethodSymbol { RefKind: not RefKind.None })
        {
            return;
        }
        var unwrapped = Unwrap(value);
        if (unwrapped is ILocalReferenceOperation { Local: { IsRef: false, IsUsing: true } })
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, value.Syntax.GetLocation(), type.Name, "returned from a using local (the local is disposed when the method exits, so the caller gets a copy of a disposed buffer; drop the using and let the caller own it)"));
            return;
        }
        if (IsFreshValue(unwrapped) ||
            unwrapped is ILocalReferenceOperation { Local.IsRef: false } ||
            unwrapped is IParameterReferenceOperation { Parameter.RefKind: RefKind.None }) // the by-value parameter was already diagnosed at its declaration
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(Rule, value.Syntax.GetLocation(), type.Name, "returned by value from existing storage (return a fresh value or return by ref)"));
    }

    static void AnalyzeForEach(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        if (context.Operation is not IForEachLoopOperation { LoopControlVariable: IVariableDeclaratorOperation { Symbol: { } local } })
        {
            return;
        }
        if (local.RefKind != RefKind.None || !IsBufferStruct(local.Type, interfaces))
        {
            return;
        }
        var location = local.Locations.IsDefaultOrEmpty ? context.Operation.Syntax.GetLocation() : local.Locations[0];
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, local.Type.Name, "copied by foreach (iterate by ref, or index the collection)"));
    }

    static void AnalyzePattern(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var declared = context.Operation switch
        {
            IDeclarationPatternOperation pattern => pattern.DeclaredSymbol as ILocalSymbol,
            IRecursivePatternOperation pattern => pattern.DeclaredSymbol as ILocalSymbol,
            _ => null,
        };
        if (declared is null || !IsBufferStruct(declared.Type, interfaces))
        {
            return;
        }
        var location = declared.Locations.IsDefaultOrEmpty ? context.Operation.Syntax.GetLocation() : declared.Locations[0];
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, declared.Type.Name, "copied by a pattern match"));
    }

    static void AnalyzeWith(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var with = (IWithOperation)context.Operation;
        if (with.Operand.Type is { } type && IsBufferStruct(type, interfaces))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, with.Syntax.GetLocation(), type.Name, "copied by a with-expression"));
        }
    }

    // even a FRESH buffer is flagged here: the tuple itself is not a buffer type, so
    // every subsequent tuple copy would duplicate the buffer invisibly to all rules
    static void AnalyzeTuple(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var tuple = (ITupleOperation)context.Operation;
        foreach (var element in tuple.Elements)
        {
            if (Unwrap(element).Type is { } type && IsBufferStruct(type, interfaces))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, element.Syntax.GetLocation(), type.Name, "captured in a tuple (tuples copy freely and hide the buffer)"));
            }
        }
    }

    static void AnalyzeCollectionElements(OperationAnalysisContext context, IEnumerable<IOperation> elements, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        foreach (var element in elements)
        {
            var unwrapped = Unwrap(element);
            if (unwrapped.Type is { } type && IsBufferStruct(type, interfaces) && !IsFreshValue(unwrapped))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, element.Syntax.GetLocation(), type.Name, "copied into a collection"));
            }
        }
    }

    // `using (a)` copies the resource into a hidden slot and disposes THAT; the
    // declaration form (`using var a = ...;`) is handled by the assignment rule
    static void AnalyzeUsing(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var usingOperation = (IUsingOperation)context.Operation;
        if (usingOperation.Resources is IVariableDeclarationGroupOperation)
        {
            return;
        }
        var resource = Unwrap(usingOperation.Resources);
        if (resource.Type is { } type && IsBufferStruct(type, interfaces) && !IsFreshValue(resource))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, resource.Syntax.GetLocation(), type.Name, "copied by a using statement (declare the resource inside the using)"));
        }
    }

    // a buffer reached through a READONLY reference (readonly field, `ref readonly`
    // local/return, an `in` path through a containing struct, `this` inside a readonly
    // member) cannot be mutated in place: the compiler runs every non-readonly member on
    // a hidden defensive copy, so the write index advances on a temporary and the real
    // buffer never moves. The `in`-parameter form is already reported at its declaration.
    static void AnalyzeReadOnlyReceiver(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> interfaces)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.IsStatic ||
            invocation.TargetMethod.IsReadOnly ||
            invocation.Instance is not { Type: { } receiverType } receiver ||
            !IsBufferStruct(receiverType, interfaces) ||
            receiver is IParameterReferenceOperation ||
            !IsReadOnlyReference(receiver, context.ContainingSymbol))
        {
            return;
        }
        context.ReportDiagnostic(Diagnostic.Create(Rule, receiver.Syntax.GetLocation(), receiverType.Name, $"mutated through a readonly reference (the non-readonly member '{invocation.TargetMethod.Name}' runs on a hidden defensive copy; reach the buffer through a ref or a mutable field)"));
    }

    static bool IsReadOnlyReference(IOperation reference, ISymbol containingSymbol)
    {
        switch (reference)
        {
            case IFieldReferenceOperation field:
                if (field.Field.IsReadOnly)
                {
                    return true;
                }
                // a mutable field of a struct is still readonly when the struct itself is
                // reached through a readonly reference (h.b with `in Holder h`)
                return field.Field.ContainingType.IsValueType && field.Instance is { } instance && IsReadOnlyReference(instance, containingSymbol);
            case ILocalReferenceOperation local:
                return local.Local.RefKind == RefKind.RefReadOnly;
            case IParameterReferenceOperation parameter:
                return parameter.Parameter.RefKind is RefKind.In or RefKind.RefReadOnlyParameter;
            case IInvocationOperation call:
                return call.TargetMethod.RefKind == RefKind.RefReadOnly;
            case IPropertyReferenceOperation property:
                return property.Property.RefKind == RefKind.RefReadOnly;
            case IInstanceReferenceOperation:
                // `this` inside a readonly member (or any member of a readonly struct)
                return containingSymbol is IMethodSymbol { IsReadOnly: true } || containingSymbol.ContainingType is { IsReadOnly: true };
            default:
                return false;
        }
    }

    static IOperation Unwrap(IOperation value)
    {
        while (value is IConversionOperation conversion)
        {
            value = conversion.Operand;
        }
        return value;
    }

    // fresh values transfer ownership (move, not copy): construction, default, and
    // by-value method returns; everything read from an existing storage location is a copy.
    // A ref-returning method hands out existing storage, so receiving its result by value
    // dereferences and copies, same as reading a field.
    static bool IsFreshValue(IOperation value)
    {
        return Unwrap(value) switch
        {
            IObjectCreationOperation or IDefaultValueOperation or ILiteralOperation => true,
            IInvocationOperation invocation => invocation.TargetMethod.RefKind == RefKind.None,
            IConditionalOperation { WhenFalse: { } whenFalse } conditional => IsFreshValue(conditional.WhenTrue) && IsFreshValue(whenFalse),
            ISwitchExpressionOperation switchExpression => switchExpression.Arms.All(static arm => IsFreshValue(arm.Value)),
            _ => false,
        };
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

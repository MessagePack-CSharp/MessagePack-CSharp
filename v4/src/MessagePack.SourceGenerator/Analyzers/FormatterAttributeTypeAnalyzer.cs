using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// [MessagePackFormatter]'s Type argument only fails at runtime when it is not a usable factory or formatter,
/// and the generator's own MsgPack012/MsgPack013 cover just the contexts it consumes ([MessagePackObject] members and
/// type-level targets). MsgPack105 checks every application live: the type must be a concrete,
/// fully constructed MessagePackFormatterFactory, or a formatter passed as an unbound generic over the buffer pair,
/// and expose an accessible constructor matching the supplied argument count,
/// both attribute paths construct via new, so a private-ctor singleton is unusable here no matter how valid its shape
/// is. The MessagePackFormatterAttribute&lt;TFactory&gt; variant gets the factory check from its constraint;
/// this analyzer adds the abstract/unbound/constructor checks there.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FormatterAttributeTypeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MsgPack105";

    const string AttributeName = "MessagePack.MessagePackFormatterAttribute";
    const string FactoryBaseName = "MessagePack.MessagePackFormatterFactory";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "MessagePackFormatter attribute must name a factory or an unbound formatter",
        "'{0}' is not usable with [MessagePackFormatter]: {1}",
        "MessagePack.Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The attribute's type must be a concrete, fully constructed MessagePackFormatterFactory (arguments go to its constructor), or a formatter passed as an unbound generic over the buffer pair (typeof(MyFormatter<,>)), with an accessible constructor matching the supplied argument count (the attribute paths construct via new; a static Instance does not help). Anything else fails at resolve time with a runtime exception; this rule surfaces it at compile time.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static startContext =>
        {
            if (startContext.Compilation.GetTypeByMetadataName(AttributeName) is null)
            {
                return;
            }
            startContext.RegisterSymbolAction(
                AnalyzeSymbol,
                SymbolKind.NamedType, SymbolKind.Property, SymbolKind.Field);
        });
    }

    static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        CheckAttributes(context, context.Symbol.GetAttributes());
    }

    static void CheckAttributes(SymbolAnalysisContext context, ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            var isFormatterAttribute = false;
            for (var attributeType = attribute.AttributeClass; attributeType is not null; attributeType = attributeType.BaseType)
            {
                if (attributeType is { IsGenericType: false } && attributeType.ToDisplayString() == AttributeName)
                {
                    isFormatterAttribute = true;
                    break;
                }
            }
            if (!isFormatterAttribute)
            {
                continue;
            }

            // the generic variant carries the factory as the attribute's type argument;
            // the Type-based form as the first constructor argument. A derived attribute baking the type into a base()
            // call is invisible here, the runtime tiers still validate it.
            INamedTypeSymbol? target = null;
            var argumentCount = 0;
            if (attribute.AttributeClass is { IsGenericType: true, TypeArguments.Length: 1 } genericAttribute
                && genericAttribute.TypeArguments[0] is INamedTypeSymbol attributeTypeArgument)
            {
                target = attributeTypeArgument;
            }
            foreach (var argument in attribute.ConstructorArguments)
            {
                if (target is null && argument is { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol namedType })
                {
                    target = namedType;
                }
                else if (argument is { Kind: TypedConstantKind.Array, IsNull: false } argumentArray)
                {
                    argumentCount = argumentArray.Values.Length;
                }
            }
            if (target is null || target.TypeKind == TypeKind.Error)
            {
                // an error type (e.g. an open generic name in the attribute's type argument, CS0305)
                // already carries a compiler error; piling MsgPack105 on top is noise
                continue;
            }

            if (Validate(target, argumentCount, context.Compilation) is { } reason)
            {
                var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? context.Symbol.Locations.FirstOrDefault() ?? Location.None;
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, target.ToDisplayString(), reason));
            }
        }
    }

    static string? Validate(INamedTypeSymbol target, int argumentCount, Compilation compilation)
    {
        // the base class itself counts as "factory-shaped" so it gets the abstract message instead of the neither-form
        // one
        for (var baseType = target; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.ToDisplayString() == FactoryBaseName)
            {
                if (target.IsAbstract)
                {
                    return "the factory type is abstract";
                }
                if (target.IsUnboundGenericType)
                {
                    return "close every type argument of the factory inside the typeof";
                }
                // arity + accessibility only: private-ctor singletons and argument-count mismatches fail right here;
                // the exact type binding is the generator's MsgPack013 where it consumes the attribute,
                // and the runtime's elsewhere
                return CheckConstructor(target, argumentCount, compilation);
            }
        }

        var definition = target.OriginalDefinition;
        var implementsFormatter = false;
        foreach (var implemented in definition.AllInterfaces)
        {
            if (implemented.OriginalDefinition is { MetadataName: "IMessagePackFormatter`3" } original
                && original.ContainingNamespace.ToDisplayString() == "MessagePack")
            {
                implementsFormatter = true;
                break;
            }
        }
        if (!implementsFormatter)
        {
            return "it is neither a MessagePackFormatterFactory nor an IMessagePackFormatter implementation";
        }
        if (!target.IsUnboundGenericType || definition.TypeParameters.Length != 2)
        {
            return "pass a formatter as an unbound generic over the buffer pair (typeof(MyFormatter<,>)), or use a MessagePackFormatterFactory";
        }
        return CheckConstructor(definition, argumentCount, compilation);
    }

    static string? CheckConstructor(INamedTypeSymbol target, int argumentCount, Compilation compilation)
    {
        foreach (var constructor in target.InstanceConstructors)
        {
            // trailing optional parameters bind like in any C# call (the generator emits source)
            if (constructor.Parameters.Length < argumentCount
                || !(constructor.DeclaredAccessibility == Accessibility.Public
                    || (constructor.DeclaredAccessibility == Accessibility.Internal
                        && SymbolEqualityComparer.Default.Equals(constructor.ContainingAssembly, compilation.Assembly))))
            {
                continue;
            }
            var tailOptional = true;
            for (int i = argumentCount; i < constructor.Parameters.Length && tailOptional; i++)
            {
                tailOptional = constructor.Parameters[i].IsOptional;
            }
            if (tailOptional)
            {
                return null;
            }
        }
        return $"no accessible constructor takes the {argumentCount} supplied argument(s)";
    }
}

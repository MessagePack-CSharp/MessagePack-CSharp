// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using AnalyzerOptions = MessagePack.SourceGenerator.CodeAnalysis.AnalyzerOptions;

namespace MessagePack.SourceGenerator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MsgPack00xMessagePackAnalyzer : DiagnosticAnalyzer
{
    public const string UseMessagePackObjectAttributeId = "MsgPack003";
    public const string AttributeMessagePackObjectMembersId = "MsgPack004";
    public const string InvalidMessagePackObjectId = "MsgPack005";
    public const string MessagePackFormatterMustBeMessagePackFormatterId = "MsgPack006";
    public const string DeserializingConstructorId = "MsgPack007";
    public const string AOTLimitationsId = "MsgPack008";
    public const string CollidingFormattersId = "MsgPack009";
    public const string InaccessibleFormatterTypeId = "MsgPack010";
    public const string PartialTypeRequiredId = "MsgPack011";
    public const string InaccessibleDataTypeId = "MsgPack012";
    public const string InaccessibleFormatterInstanceId = "MsgPack013";

    internal const string Category = "Usage";

    public const string MessagePackObjectAttributeShortName = Constants.MessagePackObjectAttributeName;
    public const string KeyAttributeShortName = "KeyAttribute";
    public const string IgnoreShortName = "IgnoreMemberAttribute";
    public const string IgnoreDataMemberShortName = "IgnoreDataMemberAttribute";

    private const string InvalidMessagePackObjectTitle = "MessagePackObject validation";
    private const DiagnosticSeverity InvalidMessagePackObjectSeverity = DiagnosticSeverity.Error;

    public static readonly DiagnosticDescriptor TypeMustBeMessagePackObject = new DiagnosticDescriptor(
        id: UseMessagePackObjectAttributeId,
        title: "Use MessagePackObjectAttribute",
        category: Category,
        messageFormat: "Type must be marked with MessagePackObjectAttribute: {0}", // type.Name
        description: "Type must be marked with MessagePackObjectAttribute.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(UseMessagePackObjectAttributeId));

    public static readonly DiagnosticDescriptor MessageFormatterMustBeMessagePackFormatter = new DiagnosticDescriptor(
        id: MessagePackFormatterMustBeMessagePackFormatterId,
        title: "Must be IMessageFormatter",
        category: Category,
        messageFormat: "Type must be of IMessagePackFormatter: {0}", // type.Name
        description: "Type must be of IMessagePackFormatter.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(MessagePackFormatterMustBeMessagePackFormatterId));

    public static readonly DiagnosticDescriptor MemberNeedsKey = new DiagnosticDescriptor(
        id: AttributeMessagePackObjectMembersId,
        title: "Attribute properties and fields of MessagePack objects",
        category: Category,
        messageFormat: "Properties and fields of MessagePackObject-attributed types require either KeyAttribute or IgnoreMemberAttribute: {0}.{1}", // type.Name + "." + item.Name
        description: "Member must be marked with KeyAttribute or IgnoreMemberAttribute.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(AttributeMessagePackObjectMembersId));

    public static readonly DiagnosticDescriptor BaseTypeContainsUnattributedPublicMembers = new DiagnosticDescriptor(
        id: AttributeMessagePackObjectMembersId,
        title: "Attribute properties and fields of MessagePack objects",
        category: Category,
        messageFormat: "Properties and fields of base types of MessagePackObject-attributed types require either KeyAttribute or IgnoreMemberAttribute: {0}.{1}", // type.Name + "." + item.Name
        description: "Member must be marked with KeyAttribute or IgnoreMemberAttribute.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(AttributeMessagePackObjectMembersId));

    public static readonly DiagnosticDescriptor InvalidMessagePackObject = new DiagnosticDescriptor(
        id: InvalidMessagePackObjectId,
        title: InvalidMessagePackObjectTitle,
        category: Category,
        messageFormat: "Invalid MessagePackObject definition: {0}", // details
        description: "Invalid MessagePackObject definition.",
        defaultSeverity: InvalidMessagePackObjectSeverity,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMessagePackObjectId));

    public static readonly DiagnosticDescriptor BothStringAndIntKeyAreNull = new DiagnosticDescriptor(
        id: InvalidMessagePackObjectId,
        title: InvalidMessagePackObjectTitle,
        category: Category,
        messageFormat: "Both int and string keys are null: {0}.{1}", // type.Name + "." + item.Name
        description: "An int or string key must be supplied to the KeyAttribute.",
        defaultSeverity: InvalidMessagePackObjectSeverity,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMessagePackObjectId));

    public static readonly DiagnosticDescriptor DoNotMixStringAndIntKeys = new DiagnosticDescriptor(
        id: InvalidMessagePackObjectId,
        title: InvalidMessagePackObjectTitle,
        category: Category,
        messageFormat: "All KeyAttribute arguments must be of the same type (either string or int)",
        description: "Use string or int keys consistently.",
        defaultSeverity: InvalidMessagePackObjectSeverity,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMessagePackObjectId));

    public static readonly DiagnosticDescriptor KeysMustBeUnique = new DiagnosticDescriptor(
        id: InvalidMessagePackObjectId,
        title: InvalidMessagePackObjectTitle,
        category: Category,
        messageFormat: "All KeyAttribute arguments must be unique",
        description: "Each key must be unique.",
        defaultSeverity: InvalidMessagePackObjectSeverity,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMessagePackObjectId));

    public static readonly DiagnosticDescriptor UnionAttributeRequired = new DiagnosticDescriptor(
        id: InvalidMessagePackObjectId,
        title: InvalidMessagePackObjectTitle,
        category: Category,
        messageFormat: "This type must carry a UnionAttribute",
        description: "A UnionAttribute is required on interfaces and abstract base classes used as serialized types.",
        defaultSeverity: InvalidMessagePackObjectSeverity,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMessagePackObjectId));

    // This is important because [Key] on a private member still will not be serialized, which is very confusing until
    // one realizes the type is serializing in map mode.
    public static readonly DiagnosticDescriptor KeyAnnotatedMemberInMapMode = new DiagnosticDescriptor(
        id: InvalidMessagePackObjectId,
        title: InvalidMessagePackObjectTitle,
        category: Category,
        messageFormat: "Types in map mode should not annotate members with KeyAttribute",
        description: "When in map mode (by compilation setting or with [MessagePackObject(true)]), internal and public members are automatically included in serialization and should not be annotated with KeyAttribute.",
        defaultSeverity: InvalidMessagePackObjectSeverity,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMessagePackObjectId));

    public static readonly DiagnosticDescriptor NoDeserializingConstructor = new DiagnosticDescriptor(
        id: DeserializingConstructorId,
        title: "Deserializing constructors",
        category: Category,
        messageFormat: "Cannot find a public constructor",
        description: "A deserializable type must carry a public constructor.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(DeserializingConstructorId));

    public static readonly DiagnosticDescriptor DeserializingConstructorParameterTypeMismatch = new DiagnosticDescriptor(
        id: DeserializingConstructorId,
        title: "Deserializing constructors",
        category: Category,
        messageFormat: "Deserializing constructor parameter type mismatch",
        description: "Constructor parameter types must match the serializable members.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(DeserializingConstructorId));

    public static readonly DiagnosticDescriptor DeserializingConstructorParameterIndexMissing = new DiagnosticDescriptor(
        id: DeserializingConstructorId,
        title: "Deserializing constructors",
        category: Category,
        messageFormat: "Deserializing constructor parameter count mismatch",
        description: "Constructor parameter count must meet or exceed the number of serialized members or the highest key index.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(DeserializingConstructorId));

    public static readonly DiagnosticDescriptor DeserializingConstructorParameterNameMissing = new DiagnosticDescriptor(
        id: DeserializingConstructorId,
        title: "Deserializing constructors",
        category: Category,
        messageFormat: "Deserializing constructor parameter name mismatch",
        description: "Parameter names must match the serialized members' named keys.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(DeserializingConstructorId));

    public static readonly DiagnosticDescriptor DeserializingConstructorParameterNameDuplicate = new DiagnosticDescriptor(
        id: DeserializingConstructorId,
        title: "Deserializing constructors",
        category: Category,
        messageFormat: "Duplicate matched constructor parameter name",
        description: "Parameter names must match the serialized members' named keys.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(DeserializingConstructorId));

    public static readonly DiagnosticDescriptor AotUnionAttributeRequiresTypeArg = new DiagnosticDescriptor(
        id: AOTLimitationsId,
        title: "AOT limitations",
        category: Category,
        messageFormat: "The source generator only supports UnionAttribute with a Type argument",
        description: "Use a type argument with UnionAttribute.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(AOTLimitationsId));

    public static readonly DiagnosticDescriptor AotArrayRankTooHigh = new DiagnosticDescriptor(
        id: AOTLimitationsId,
        title: "AOT limitations",
        category: Category,
        messageFormat: "Array rank too high for built-in array formatters",
        description: "Avoid excessively high array ranks, or write a custom formatter.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(AOTLimitationsId));

    public static readonly DiagnosticDescriptor CollidingFormatters = new(
        id: CollidingFormattersId,
        title: "Colliding formatters",
        category: Category,
        messageFormat: "Multiple formatters for type {0} found",
        description: "Only one formatter per type is allowed.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(CollidingFormattersId));

    public static readonly DiagnosticDescriptor InaccessibleFormatterInstance = new(
        id: InaccessibleFormatterInstanceId,
        title: "Inaccessible formatter",
        category: Category,
        messageFormat: "Formatter should declare a default constructor with at least internal visibility or a public static readonly field named Instance that returns the singleton",
        description: "The auto-generated resolver cannot construct this formatter without a constructor. It will be omitted from the resolver.",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InaccessibleFormatterInstanceId));

    public static readonly DiagnosticDescriptor InaccessibleFormatterType = new(
        id: InaccessibleFormatterTypeId,
        title: "Inaccessible formatter",
        category: Category,
        messageFormat: "Formatter should be declared with at least internal visibility",
        description: "The auto-generated resolver cannot access this formatter. It will be omitted from the resolver.",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InaccessibleFormatterTypeId));

    public static readonly DiagnosticDescriptor PartialTypeRequired = new(
        id: PartialTypeRequiredId,
        title: "Partial type required",
        category: Category,
        messageFormat: "Types with private, serializable members must be declared as partial, including nesting types",
        description: "When a data type has serializable members that may only be accessible to the class itself (e.g. private or protected members), the type must be declared as partial to allow source generation of the formatter as a nested type. When a data type is itself a nested type, its declaring types must also be partial.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(PartialTypeRequiredId));

    public static readonly DiagnosticDescriptor InaccessibleDataType = new(
        id: InaccessibleDataTypeId,
        title: "Internally accessible data type required",
        category: Category,
        messageFormat: "This MessagePack formattable type must have at least internal visibility",
        description: "MessagePack serializable objects must be at least internally accessible so a source-generated formatter can access it.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InaccessibleDataTypeId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        TypeMustBeMessagePackObject,
        MessageFormatterMustBeMessagePackFormatter,
        MemberNeedsKey,
        BaseTypeContainsUnattributedPublicMembers,
        InvalidMessagePackObject,
        BothStringAndIntKeyAreNull,
        DoNotMixStringAndIntKeys,
        KeysMustBeUnique,
        UnionAttributeRequired,
        KeyAnnotatedMemberInMapMode,
        NoDeserializingConstructor,
        DeserializingConstructorParameterTypeMismatch,
        DeserializingConstructorParameterIndexMissing,
        DeserializingConstructorParameterNameMissing,
        DeserializingConstructorParameterNameDuplicate,
        AotUnionAttributeRequiresTypeArg,
        AotArrayRankTooHigh,
        CollidingFormatters,
        InaccessibleFormatterInstance,
        InaccessibleFormatterType,
        PartialTypeRequired,
        InaccessibleDataType);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            if (ReferenceSymbols.TryCreate(context.Compilation, out ReferenceSymbols? typeReferences))
            {
                // Search the compilation for implementations of IMessagePackFormatter<T>.
                ImmutableHashSet<FormatterDescriptor> formatterTypes = this.SearchForFormatters(context.Compilation.Assembly.GlobalNamespace).ToImmutableHashSet();

                AnalyzerOptions options = new AnalyzerOptions()
                    .WithFormatterTypes(ImmutableArray<FormattableType>.Empty, formatterTypes)
                    .WithAssemblyAttributes(context.Compilation.Assembly.GetAttributes(), context.CancellationToken);
                context.RegisterSymbolAction(context => this.AnalyzeSymbol(context, typeReferences, options), SymbolKind.NamedType);
            }
        });
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context, ReferenceSymbols typeReferences, AnalyzerOptions options)
    {
        INamedTypeSymbol declaredSymbol = (INamedTypeSymbol)context.Symbol;
        QualifiedNamedTypeName typeName = new(declaredSymbol);

        // If this is a formatter, confirm that it meets requirements.
        if (options.KnownFormattersByName.TryGetValue(typeName, out FormatterDescriptor? formatter))
        {
            // Look for colliding formatters (multiple formatters that want to format the same type).
            foreach (FormattableType formattableType in options.GetCollidingFormatterDataTypes(typeName))
            {
                context.ReportDiagnostic(Diagnostic.Create(CollidingFormatters, declaredSymbol.Locations[0], formattableType.Name.GetQualifiedName(Qualifiers.Namespace)));
            }

            if (!formatter.ExcludeFromSourceGeneratedResolver && formatter.InaccessibleDescriptor is { } inaccessible)
            {
                context.ReportDiagnostic(Diagnostic.Create(inaccessible, declaredSymbol.Locations[0]));
            }
        }

        switch (declaredSymbol.TypeKind)
        {
            case TypeKind.Interface when declaredSymbol.GetAttributes().Any(x2 => SymbolEqualityComparer.Default.Equals(x2.AttributeClass, typeReferences.UnionAttribute)):
            case TypeKind.Class or TypeKind.Struct when declaredSymbol.GetAttributes().Any(x2 => SymbolEqualityComparer.Default.Equals(x2.AttributeClass, typeReferences.MessagePackObjectAttribute)):
                TypeCollector.Collect(context.Compilation, options, typeReferences, context.ReportDiagnostic, declaredSymbol, context.CancellationToken);
                break;
        }
    }

    private IEnumerable<FormatterDescriptor> SearchForFormatters(INamespaceOrTypeSymbol container)
    {
        if (container is INamespaceSymbol ns)
        {
            foreach (INamespaceSymbol childNamespace in ns.GetNamespaceMembers())
            {
                foreach (FormatterDescriptor x in this.SearchForFormatters(childNamespace))
                {
                    yield return x;
                }
            }
        }

        foreach (INamedTypeSymbol type in container.GetTypeMembers())
        {
            if (FormatterDescriptor.TryCreate(type, out FormatterDescriptor? formatter))
            {
                yield return formatter;
            }

            foreach (FormatterDescriptor nested in this.SearchForFormatters(type))
            {
                yield return nested;
            }
        }
    }
}

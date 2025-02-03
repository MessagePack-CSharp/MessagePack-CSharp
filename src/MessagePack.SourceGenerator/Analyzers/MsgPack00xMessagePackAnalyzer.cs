// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    public const string NullableReferenceTypeFormatterId = "MsgPack014";
    public const string MessagePackObjectAllowPrivateId = "MsgPack015";
    public const string AOTDerivedKeyId = "MsgPack016";
    public const string AOTInitPropertyId = "MsgPack017";
    public const string CollidingMemberNamesInForceMapModeId = "MsgPack018";

    internal const string Category = "Usage";

    public const string MessagePackObjectAttributeShortName = Constants.MessagePackObjectAttributeName;
    public const string KeyAttributeShortName = "KeyAttribute";
    public const string IgnoreShortName = "IgnoreMemberAttribute";
    public const string DataMemberShortName = "DataMemberAttribute";
    public const string IgnoreDataMemberShortName = "IgnoreDataMemberAttribute";
    public const string AllowPrivatePropertyName = "AllowPrivate";

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
        description: "The deserializing constructor parameter count must meet or exceed the number of serialized members.",
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

    public static readonly DiagnosticDescriptor AOTDerivedKeyAttribute = new DiagnosticDescriptor(
        id: AOTDerivedKeyId,
        title: "KeyAttribute derivatives",
        category: Category,
        messageFormat: "KeyAttribute-derived attributes are not supported by AOT formatters",
        description: "Use [Key(x)] attributes directly, or switch off source generation for this type using [MessagePackObject(SuppressSourceGeneration = true)].",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(AOTDerivedKeyId));

    public static readonly DiagnosticDescriptor AOTInitProperty = new(
        id: AOTInitPropertyId,
        title: "Property with init accessor and initializer",
        category: Category,
        messageFormat: "The value of a property with an init accessor and an initializer will be reset to the default value for the type upon deserialization when no value for them is deserialized",
        description: "Due to a limitation in the C# language, properties with init accessor must be set by source generated deserializers unconditionally. When the data stream lacks a value for the property, it will be set with the default value for the property type, overriding a default that an initializer might set.",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(AOTInitPropertyId));

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

    public static readonly DiagnosticDescriptor NullableReferenceTypeFormatter = new(
        id: NullableReferenceTypeFormatterId,
        title: "Format nullable reference types",
        category: Category,
        messageFormat: "Implement IMessagePackFormatter<{0}?> (with nullable annotation)",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(NullableReferenceTypeFormatterId));

    public static readonly DiagnosticDescriptor MessagePackObjectAllowPrivateRequired = new(
        id: MessagePackObjectAllowPrivateId,
        title: "MessagePackObjectAttribute.AllowPrivate should be set",
        category: Category,
        messageFormat: "MessagePackObjectAttribute.AllowPrivate should be set to true because the type is not public and/or at least one of its non-public members are attributed for serialization",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(MessagePackObjectAllowPrivateId));

    public static readonly DiagnosticDescriptor CollidingMemberNamesInForceMapMode = new(
        id: CollidingMemberNamesInForceMapModeId,
        title: "Unique names required in force map mode",
        category: Category,
        messageFormat: "All serialized member names must be unique in force map mode, but this member redeclares a member with the same name as one found on a base type",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(CollidingMemberNamesInForceMapModeId));

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
        AOTDerivedKeyAttribute,
        AOTInitProperty,
        CollidingFormatters,
        InaccessibleFormatterInstance,
        InaccessibleFormatterType,
        PartialTypeRequired,
        InaccessibleDataType,
        NullableReferenceTypeFormatter,
        MessagePackObjectAllowPrivateRequired,
        CollidingMemberNamesInForceMapMode);

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
                context.RegisterSymbolStartAction(context => SymbolStartAction(context, typeReferences, options), SymbolKind.NamedType);
            }
        });
    }

    private static void SymbolStartAction(SymbolStartAnalysisContext context, ReferenceSymbols typeReferences, AnalyzerOptions options)
    {
        INamedTypeSymbol declaredSymbol = (INamedTypeSymbol)context.Symbol;
        QualifiedNamedTypeName typeName = new(declaredSymbol, ImmutableStack<GenericTypeParameterInfo>.Empty);

        // If this is a formatter, confirm that it meets requirements.
        if (options.KnownFormattersByName.TryGetValue(typeName, out FormatterDescriptor? formatter))
        {
            // Call out any formattable reference types that are not nullable.
            INamedTypeSymbol[] missing = declaredSymbol.Interfaces
                .Where(iface => iface.IsGenericType)
                .Where(iface => SymbolEqualityComparer.Default.Equals(iface.ConstructUnboundGenericType(), typeReferences.MessagePackFormatterOfT)
                             && iface.TypeArguments is [INamedTypeSymbol { IsReferenceType: true, NullableAnnotation: NullableAnnotation.NotAnnotated } a])
                .Select(iface => (INamedTypeSymbol)iface.TypeArguments[0])
                .ToArray();

            if (missing.Length > 0)
            {
                // Look for the base list so we can report the diagnostic(s) at the actual type argument.
                context.RegisterSyntaxNodeAction(
                    context =>
                    {
                        BaseListSyntax baseList = (BaseListSyntax)context.Node;

                        // Find the location of the actual generic type argument if we can.
                        foreach (BaseTypeSyntax baseTypeSyntax in baseList.Types)
                        {
                            if (baseTypeSyntax.Type is GenericNameSyntax { TypeArgumentList: { Arguments: [TypeSyntax typeArg] } })
                            {
                                ITypeSymbol? actual = context.SemanticModel.GetTypeInfo(typeArg, context.CancellationToken).Type;
                                if (missing.Any(m => SymbolEqualityComparer.Default.Equals(actual, m)))
                                {
                                    Location location = typeArg.GetLocation();
                                    context.ReportDiagnostic(Diagnostic.Create(NullableReferenceTypeFormatter, location, typeArg.ToString()));
                                    break;
                                }
                            }
                        }
                    },
                    SyntaxKind.BaseList);
            }
        }
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context, ReferenceSymbols typeReferences, AnalyzerOptions options)
    {
        INamedTypeSymbol declaredSymbol = (INamedTypeSymbol)context.Symbol;
        QualifiedNamedTypeName typeName = new(declaredSymbol, ImmutableStack<GenericTypeParameterInfo>.Empty);

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
            if (type.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.FilePath.Contains("MessagePack.SourceGenerator") is true)
            {
                // We do not want to find source generated formatters during analysis.
                continue;
            }

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

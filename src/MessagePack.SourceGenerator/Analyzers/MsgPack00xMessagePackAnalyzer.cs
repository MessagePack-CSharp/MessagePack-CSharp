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

    internal const string Category = "Usage";

    internal const string MessagePackObjectAttributeShortName = Constants.MessagePackObjectAttributeName;
    internal const string KeyAttributeShortName = "KeyAttribute";
    internal const string IgnoreShortName = "IgnoreMemberAttribute";
    internal const string IgnoreDataMemberShortName = "IgnoreDataMemberAttribute";

    internal static readonly DiagnosticDescriptor TypeMustBeMessagePackObject = new DiagnosticDescriptor(
        id: UseMessagePackObjectAttributeId,
        title: "Use MessagePackObjectAttribute",
        category: Category,
        messageFormat: "Type must be marked with MessagePackObjectAttribute: {0}", // type.Name
        description: "Type must be marked with MessagePackObjectAttribute.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(UseMessagePackObjectAttributeId));

    internal static readonly DiagnosticDescriptor MessageFormatterMustBeMessagePackFormatter = new DiagnosticDescriptor(
        id: MessagePackFormatterMustBeMessagePackFormatterId,
        title: "Must be IMessageFormatter",
        category: Category,
        messageFormat: "Type must be of IMessagePackFormatter: {0}", // type.Name
        description: "Type must be of IMessagePackFormatter.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(MessagePackFormatterMustBeMessagePackFormatterId));

    internal static readonly DiagnosticDescriptor PublicMemberNeedsKey = new DiagnosticDescriptor(
        id: AttributeMessagePackObjectMembersId,
        title: "Attribute public members of MessagePack objects",
        category: Category,
        messageFormat: "Public members of MessagePackObject-attributed types require either KeyAttribute or IgnoreMemberAttribute: {0}.{1}", // type.Name + "." + item.Name
        description: "Public member must be marked with KeyAttribute or IgnoreMemberAttribute.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(AttributeMessagePackObjectMembersId));

    internal static readonly DiagnosticDescriptor BaseTypeContainsUnattributedPublicMembers = new DiagnosticDescriptor(
        id: AttributeMessagePackObjectMembersId,
        title: "Attribute public members of MessagePack objects",
        category: Category,
        messageFormat: "Public members of base types of MessagePackObject-attributed types require either KeyAttribute or IgnoreMemberAttribute: {0}.{1}", // type.Name + "." + item.Name
        description: "Public member must be marked with KeyAttribute or IgnoreMemberAttribute.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(AttributeMessagePackObjectMembersId));

    internal static readonly DiagnosticDescriptor InvalidMessagePackObject = new DiagnosticDescriptor(
        id: InvalidMessagePackObjectId,
        title: "MessagePackObject validation",
        category: Category,
        messageFormat: "Invalid MessagePackObject definition: {0}", // details
        description: "Invalid MessagePackObject definition.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMessagePackObjectId));

    internal static readonly DiagnosticDescriptor BothStringAndIntKeyAreNull = new DiagnosticDescriptor(
        id: InvalidMessagePackObjectId,
        title: "Attribute public members of MessagePack objects",
        category: Category,
        messageFormat: "Both int and string keys are null: {0}.{1}", // type.Name + "." + item.Name
        description: "An int or string key must be supplied to the KeyAttribute.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMessagePackObjectId));

    internal static readonly DiagnosticDescriptor DoNotMixStringAndIntKeys = new DiagnosticDescriptor(
        id: InvalidMessagePackObjectId,
        title: "Attribute public members of MessagePack objects",
        category: Category,
        messageFormat: "All KeyAttribute arguments must be of the same type (either string or int)",
        description: "Use string or int keys consistently.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMessagePackObjectId));

    internal static readonly DiagnosticDescriptor KeysMustBeUnique = new DiagnosticDescriptor(
        id: InvalidMessagePackObjectId,
        title: "Attribute public members of MessagePack objects",
        category: Category,
        messageFormat: "All KeyAttribute arguments must be unique",
        description: "Each key must be unique.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMessagePackObjectId));

    internal static readonly DiagnosticDescriptor UnionAttributeRequired = new DiagnosticDescriptor(
        id: InvalidMessagePackObjectId,
        title: "Attribute public members of MessagePack objects",
        category: Category,
        messageFormat: "This type must carry a UnionAttribute",
        description: "A UnionAttribute is required on interfaces and abstract base classes used as serialized types.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMessagePackObjectId));

    internal static readonly DiagnosticDescriptor NoDeserializingConstructor = new DiagnosticDescriptor(
        id: DeserializingConstructorId,
        title: "Deserializing constructors",
        category: Category,
        messageFormat: "Cannot find a public constructor",
        description: "A deserializable type must carry a public constructor.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(DeserializingConstructorId));

    internal static readonly DiagnosticDescriptor DeserializingConstructorParameterTypeMismatch = new DiagnosticDescriptor(
        id: DeserializingConstructorId,
        title: "Deserializing constructors",
        category: Category,
        messageFormat: "Deserializing constructor parameter type mismatch",
        description: "Constructor parameter types must match the serializable members.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(DeserializingConstructorId));

    internal static readonly DiagnosticDescriptor DeserializingConstructorParameterIndexMissing = new DiagnosticDescriptor(
        id: DeserializingConstructorId,
        title: "Deserializing constructors",
        category: Category,
        messageFormat: "Deserializing constructor parameter count mismatch",
        description: "Constructor parameter count must meet or exceed the number of serialized members or the highest key index.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(DeserializingConstructorId));

    internal static readonly DiagnosticDescriptor DeserializingConstructorParameterNameMissing = new DiagnosticDescriptor(
        id: DeserializingConstructorId,
        title: "Deserializing constructors",
        category: Category,
        messageFormat: "Deserializing constructor parameter name mismatch",
        description: "Parameter names must match the serialized members' named keys.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(DeserializingConstructorId));

    internal static readonly DiagnosticDescriptor DeserializingConstructorParameterNameDuplicate = new DiagnosticDescriptor(
        id: DeserializingConstructorId,
        title: "Deserializing constructors",
        category: Category,
        messageFormat: "Duplicate matched constructor parameter name",
        description: "Parameter names must match the serialized members' named keys.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(DeserializingConstructorId));

    internal static readonly DiagnosticDescriptor AotUnionAttributeRequiresTypeArg = new DiagnosticDescriptor(
        id: AOTLimitationsId,
        title: "AOT limitations",
        category: Category,
        messageFormat: "The source generator only supports UnionAttribute with a Type argument",
        description: "Use a type argument with UnionAttribute.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(AOTLimitationsId));

    internal static readonly DiagnosticDescriptor AotArrayRankTooHigh = new DiagnosticDescriptor(
        id: AOTLimitationsId,
        title: "AOT limitations",
        category: Category,
        messageFormat: "Array rank too high for built-in array formatters",
        description: "Avoid excessively high array ranks, or write a custom formatter.",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: AnalyzerUtilities.GetHelpLink(AOTLimitationsId));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        TypeMustBeMessagePackObject,
        PublicMemberNeedsKey,
        InvalidMessagePackObject,
        MessageFormatterMustBeMessagePackFormatter);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            if (ReferenceSymbols.TryCreate(context.Compilation, out ReferenceSymbols? typeReferences))
            {
                // Search the compilation for implementations of IMessagePackFormatter<T>.
                ImmutableDictionary<string, ImmutableHashSet<string>> formatterTypes = this.SearchNamespaceForFormatters(context.Compilation.Assembly.GlobalNamespace);

                AnalyzerOptions options = new AnalyzerOptions()
                    .WithFormatterTypes(ImmutableArray<string>.Empty, formatterTypes)
                    .WithAssemblyAttributes(context.Compilation.Assembly.GetAttributes(), context.CancellationToken);
                context.RegisterSymbolAction(context => this.AnalyzeSymbol(context, typeReferences, options), SymbolKind.NamedType);
            }
        });
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context, ReferenceSymbols typeReferences, AnalyzerOptions options)
    {
        INamedTypeSymbol declaredSymbol = (INamedTypeSymbol)context.Symbol;
        switch (declaredSymbol.TypeKind)
        {
            case TypeKind.Interface when declaredSymbol.GetAttributes().Any(x2 => SymbolEqualityComparer.Default.Equals(x2.AttributeClass, typeReferences.UnionAttribute)):
            case TypeKind.Class or TypeKind.Struct when declaredSymbol.GetAttributes().Any(x2 => SymbolEqualityComparer.Default.Equals(x2.AttributeClass, typeReferences.MessagePackObjectAttribute)):
                TypeCollector.Collect(context.Compilation, options, typeReferences, context.ReportDiagnostic, declaredSymbol);
                break;
        }
    }

    private ImmutableDictionary<string, ImmutableHashSet<string>> SearchNamespaceForFormatters(INamespaceSymbol ns)
    {
        ImmutableDictionary<string, ImmutableHashSet<string>> result = ImmutableDictionary<string, ImmutableHashSet<string>>.Empty;

        foreach (INamespaceSymbol childNamespace in ns.GetNamespaceMembers())
        {
            result = result.AddRange(this.SearchNamespaceForFormatters(childNamespace));
        }

        foreach (INamedTypeSymbol type in ns.GetTypeMembers())
        {
            ImmutableHashSet<string> formatters = AnalyzerUtilities.SearchTypeForFormatterImplementations(type);
            if (!formatters.IsEmpty)
            {
                result = result.Add(type.GetCanonicalTypeFullName(), formatters);
            }
        }

        return result;
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public record ResolverRegisterInfo
{
    protected static readonly SymbolDisplayFormat ShortTypeNameFormat = new SymbolDisplayFormat(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

    protected static readonly SymbolDisplayFormat BinaryWriteFormat = new SymbolDisplayFormat(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

    /// <summary>
    /// Creates formatter and resolver info for a data type that should be serializable.
    /// </summary>
    /// <param name="dataType">The type to be serialized.</param>
    /// <param name="resolverOptions">The resolver options.</param>
    /// <param name="formatterLocation">Position the formatter under the data type when it must access private members.</param>
    /// <returns>The resolver and formatter information.</returns>
    public static ResolverRegisterInfo Create(ITypeSymbol dataType, ResolverOptions resolverOptions, FormatterPosition formatterLocation = FormatterPosition.UnderResolver)
    {
        QualifiedTypeName dataTypeName = new(dataType);

        return new ResolverRegisterInfo
        {
            FormatterLocation = formatterLocation,
            DataType = dataTypeName,
            Formatter = dataTypeName with
            {
                Namespace = null,
                NestingType = formatterLocation switch
                {
                    FormatterPosition.UnderResolver => GetNestingTypeForFormatterUnderResolver(dataTypeName, resolverOptions),
                    FormatterPosition.UnderDataType => dataTypeName,
                    _ => throw new NotImplementedException(),
                },
                Name = $"{dataTypeName.Name}Formatter",
                Kind = TypeKind.Class,
            },
        };
    }

    /// <summary>
    /// Gets the name of the type to be formatted.
    /// </summary>
    public required QualifiedTypeName DataType { get; init; }

    /// <summary>
    /// Gets the namespace of the formatter.
    /// </summary>
    public required QualifiedTypeName Formatter { get; init; }

    /// <summary>
    /// Gets the declaration parent of the formatter.
    /// </summary>
    /// <remarks>
    /// The formatter is always a nested class of something else (the resolver or the data type).
    /// If the formatter is nested under the resolver, the namespaces of the data type become nested types
    /// under which the formatter is declared.
    /// </remarks>
    public FormatterPosition FormatterLocation { get; init; }

    public virtual string GetFormatterNameForResolver(GenericParameterStyle style = GenericParameterStyle.Identifiers) => this.Formatter.GetQualifiedName(Qualifiers.GlobalNamespace, style);

    /// <summary>
    /// Gets a value indicating whether this data type is a generic type with unknown type arguments.
    /// </summary>
    public virtual bool IsUnboundGenericType => this.DataType.Arity > 0;

    public string FileNameHint => $"Formatters.{this.Formatter.GetQualifiedName(Qualifiers.Namespace, GenericParameterStyle.ArityOnly)}";

    private static QualifiedTypeName? GetNestingTypeForFormatterUnderResolver(QualifiedTypeName dataType, ResolverOptions resolverOptions)
    {
        // Each nesting type of the data type become a nested type for the formatter.
        if (dataType.NestingType is not null)
        {
            // The data type aready has a nesting type. Reusing that for the formatter, but recurse in
            // because ultimately we need to add the namespaces as nesting types, and then add the resolver
            // as the outermost type and finally the namespace the resolver appears in.
            return dataType.NestingType with
            {
                Namespace = null,
                NestingType = GetNestingTypeForFormatterUnderResolver(dataType.NestingType, resolverOptions),
                AccessModifier = Accessibility.Internal,
            };
        }

        QualifiedTypeName generatedResolverName = new(
            resolverOptions.Namespace,
            TypeKind.Class,
            resolverOptions.Name);

        // Each namespace of the data type also becomes a nesting type of the formatter.
        if (dataType.Namespace is not null)
        {
            string[] namespaces = dataType.Namespace.Split('.');
            QualifiedTypeName? partialClassAsNamespaceStep = generatedResolverName;
            for (int i = 0; i < namespaces.Length; i++)
            {
                partialClassAsNamespaceStep = new QualifiedTypeName(null, TypeKind.Class, namespaces[i])
                {
                    NestingType = partialClassAsNamespaceStep,
                    AccessModifier = Accessibility.Internal,
                };
            }

            return partialClassAsNamespaceStep;
        }

        return generatedResolverName;
    }

    public enum FormatterPosition
    {
        UnderResolver,
        UnderDataType,
    }
}

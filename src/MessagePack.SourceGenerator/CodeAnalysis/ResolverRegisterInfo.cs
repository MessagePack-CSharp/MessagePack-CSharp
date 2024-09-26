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
    public static ResolverRegisterInfo Create(INamedTypeSymbol dataType, ResolverOptions resolverOptions, FormatterPosition formatterLocation = FormatterPosition.UnderResolver)
    {
        QualifiedNamedTypeName dataTypeName = new(dataType);

        return new ResolverRegisterInfo
        {
            FormatterLocation = formatterLocation,
            DataType = dataTypeName,
            Formatter = new QualifiedNamedTypeName(TypeKind.Class)
            {
                Container = NestingTypeContainer.From(formatterLocation switch
                {
                    FormatterPosition.UnderResolver => GetNestingTypeForFormatterUnderResolver(dataTypeName, resolverOptions),
                    FormatterPosition.UnderDataType => dataTypeName,
                    _ => throw new NotImplementedException(),
                }),
                Name = $"{dataTypeName.Name}Formatter",
                Kind = TypeKind.Class,
                AccessModifier = dataTypeName.AccessModifier,
                TypeParameters = dataTypeName.TypeParameters,
                TypeArguments = dataTypeName.TypeArguments,
            },
        };
    }

    /// <summary>
    /// Creates formatter and resolver info for an array data type that should be serializable.
    /// </summary>
    /// <param name="dataType">The type to be serialized.</param>
    /// <param name="resolverOptions">The resolver options.</param>
    /// <param name="formatterLocation">Position the formatter under the data type when it must access private members.</param>
    /// <returns>The resolver and formatter information.</returns>
    public static ResolverRegisterInfo CreateArray(IArrayTypeSymbol dataType, ResolverOptions resolverOptions, FormatterPosition formatterLocation = FormatterPosition.UnderResolver)
    {
        QualifiedArrayTypeName dataTypeName = new(dataType);

        return new ResolverRegisterInfo
        {
            FormatterLocation = formatterLocation,
            DataType = dataTypeName,
            Formatter = new QualifiedNamedTypeName(TypeKind.Class)
            {
                Name = $"XXXFormatter",
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
    public required QualifiedNamedTypeName Formatter { get; init; }

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
    /// Gets the C# expression that will provide an instance of the formatter.
    /// </summary>
    public virtual string GetFormatterInstanceForResolver() => $"new {this.GetFormatterNameForResolver()}()";

    /// <summary>
    /// Gets a value indicating whether this data type is a generic type with unknown type arguments.
    /// </summary>
    public virtual bool IsUnboundGenericType => this.DataType is QualifiedNamedTypeName { Arity: > 0 };

    public string FileNameHint => $"Formatters.{this.Formatter.GetQualifiedName(Qualifiers.Namespace, GenericParameterStyle.ArityOnly)}";

    private static QualifiedNamedTypeName? GetNestingTypeForFormatterUnderResolver(QualifiedTypeName dataType, ResolverOptions resolverOptions)
    {
        // Each nesting type of the data type become a nested type for the formatter.
        if (dataType is QualifiedNamedTypeName { Container: NestingTypeContainer { NestingType: { } nestingType } })
        {
            // The data type aready has a nesting type. Reusing that for the formatter, but recurse in
            // because ultimately we need to add the namespaces as nesting types, and then add the resolver
            // as the outermost type and finally the namespace the resolver appears in.
            return nestingType with
            {
                Container = GetNestingTypeForFormatterUnderResolver(nestingType, resolverOptions) is { } n ? new NestingTypeContainer(n) : null,
                AccessModifier = Accessibility.Internal,
            };
        }

        // Each namespace of the data type also becomes a nesting type of the formatter.
        if (dataType is QualifiedNamedTypeName { Container: NamespaceTypeContainer { Namespace: string ns } })
        {
            string[] namespaces = ns.Split('.');
            QualifiedNamedTypeName? partialClassAsNamespaceStep = resolverOptions.Name;
            for (int i = 0; i < namespaces.Length; i++)
            {
                partialClassAsNamespaceStep = new(TypeKind.Class)
                {
                    Name = namespaces[i],
                    Container = new NestingTypeContainer(partialClassAsNamespaceStep),
                    AccessModifier = Accessibility.Internal,
                };
            }

            return partialClassAsNamespaceStep;
        }

        return resolverOptions.Name;
    }

    public override int GetHashCode() => this.DataType.GetHashCode();

    public enum FormatterPosition
    {
        UnderResolver,
        UnderDataType,
    }
}

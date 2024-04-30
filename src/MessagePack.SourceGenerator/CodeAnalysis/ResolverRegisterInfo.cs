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

    public static ResolverRegisterInfo Create(ITypeSymbol dataType)
    {
        QualifiedTypeName dataTypeName = new(dataType);
        return new ResolverRegisterInfo
        {
            DataType = dataTypeName,
            Formatter = dataTypeName with
            {
                NestingTypes = null,
                Name = $"{dataTypeName.GetQualifiedName(Qualifiers.ContainingTypes, GenericParameterStyle.None).Replace('.', '_')}Formatter",
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

    public virtual string GetFormatterNameForResolver(GenericParameterStyle style = GenericParameterStyle.Identifiers) => this.Formatter.GetQualifiedName(Qualifiers.Namespace, style);

    /// <summary>
    /// Gets a value indicating whether this data type is a generic type with unknown type arguments.
    /// </summary>
    public virtual bool IsUnboundGenericType => this.DataType.Arity > 0;

    public string FileNameHint => $"Formatters.{this.Formatter.GetQualifiedName(Qualifiers.Namespace, GenericParameterStyle.ArityOnly)}";
}

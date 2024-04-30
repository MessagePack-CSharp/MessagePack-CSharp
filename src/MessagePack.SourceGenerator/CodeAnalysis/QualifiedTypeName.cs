// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

/// <summary>
/// Describes a type by its name and qualifiers.
/// </summary>
public record QualifiedTypeName : IComparable<QualifiedTypeName>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualifiedTypeName"/> class.
    /// </summary>
    /// <param name="symbol">The symbol this instance will identify.</param>
    public QualifiedTypeName(ITypeSymbol symbol)
    {
        if (symbol is IArrayTypeSymbol arrayType)
        {
            this.ArrayRank = arrayType.Rank;
            this.Namespace = arrayType.ElementType.ContainingNamespace.GetFullNamespaceName();
            this.Name = arrayType.ElementType.Name;
            this.NestingTypes = GetNestingTypes(arrayType.ElementType);
        }
        else
        {
            this.Namespace = symbol.ContainingNamespace.GetFullNamespaceName();
            this.Name = symbol.Name;
            this.NestingTypes = GetNestingTypes(symbol);
        }

        this.TypeParameters = CodeAnalysisUtilities.GetTypeParameters(symbol);
    }

    /// <inheritdoc cref="QualifiedTypeName(string?, string?, string, ImmutableArray{string})"/>
    public QualifiedTypeName(string? @namespace, string name, ImmutableArray<string>? typeParameters = default)
        : this(@namespace, nestingTypes: null, name, typeParameters ?? ImmutableArray<string>.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QualifiedTypeName"/> class.
    /// </summary>
    /// <param name="namespace">The full namespace of the type. Must not be empty, but may be <see langword="null" />.</param>
    /// <param name="nestingTypes">All the nesting types that contain the target type.</param>
    /// <param name="name">The simple name of the type.</param>
    /// <param name="typeParameters">The generic type identifiers.</param>
    public QualifiedTypeName(string? @namespace, string? nestingTypes, string name, ImmutableArray<string> typeParameters)
    {
        if (@namespace == string.Empty)
        {
            throw new ArgumentException("May be null but not empty.", nameof(@namespace));
        }

        if (nestingTypes == string.Empty)
        {
            throw new ArgumentException("May be null but not empty.", nameof(nestingTypes));
        }

        this.Namespace = @namespace;
        this.NestingTypes = nestingTypes;
        this.Name = name;
        this.TypeParameters = typeParameters;
    }

    public string? Namespace { get; init; }

    public string? NestingTypes { get; init; }

    public string Name { get; init; }

    public ImmutableArray<string> TypeParameters { get; init; }

    /// <summary>
    /// Gets the number of generic type parameters that belong to the type.
    /// </summary>
    public int Arity => this.TypeParameters.Length;

    public int ArrayRank { get; init; }

    public string GetQualifiedName(Qualifiers qualifier = Qualifiers.GlobalNamespace, GenericParameterStyle genericStyle = GenericParameterStyle.Identifiers)
    {
        StringBuilder builder = new();
        if (qualifier is Qualifiers.GlobalNamespace)
        {
            builder.Append("global::");
        }

        if (this.Namespace is not null && qualifier >= Qualifiers.Namespace)
        {
            builder.Append(this.Namespace);
            builder.Append('.');
        }

        if (this.NestingTypes is not null && qualifier >= Qualifiers.ContainingTypes)
        {
            builder.Append(this.NestingTypes);
            builder.Append('.');
        }

        builder.Append(this.Name);

        builder.Append(this.GetTypeParameters(genericStyle));

        if (this.ArrayRank > 0)
        {
            builder.Append('[');
            builder.Append(',', this.ArrayRank - 1);
            builder.Append(']');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Builds a string that acts as a suffix for a C# type name to represent the generic type parameters.
    /// </summary>
    /// <param name="style">The generic type parameters style to generate.</param>
    /// <returns>A string to append to a generic type name, or an empty string if <see cref="Arity"/> is 0.</returns>
    public string GetTypeParameters(GenericParameterStyle style)
    {
        if (this.TypeParameters.IsEmpty || style == GenericParameterStyle.None)
        {
            return string.Empty;
        }

        return style switch
        {
            GenericParameterStyle.Identifiers => $"<{string.Join(", ", this.TypeParameters)}>",
            GenericParameterStyle.TypeDefinition => $"<{new string(',', this.Arity - 1)}>",
            GenericParameterStyle.ArityOnly => $"`{this.Arity}",
            _ => throw new NotImplementedException(),
        };
    }

    private static string? GetNestingTypes(ITypeSymbol symbol)
    {
        string? nestingTypes = null;
        INamedTypeSymbol? nestingType = symbol.ContainingType;
        while (nestingType is not null)
        {
            nestingTypes = nestingTypes is null ? nestingType.Name : $"{nestingType.Name}.{nestingTypes}";
            nestingType = nestingType.ContainingType;
        }

        return nestingTypes;
    }

    public int CompareTo(QualifiedTypeName other)
    {
        int result = StringComparer.Ordinal.Compare(this.Namespace, other.Namespace);
        if (result != 0)
        {
            return result;
        }

        result = StringComparer.Ordinal.Compare(this.Name, other.Name);
        if (result != 0)
        {
            return result;
        }

        result = StringComparer.Ordinal.Compare(this.NestingTypes, other.NestingTypes);
        if (result != 0)
        {
            return result;
        }

        return StringComparer.Ordinal.Compare(this.Name, other.Name);
    }

    public virtual bool Equals(QualifiedTypeName? other)
    {
        if (other is null)
        {
            return false;
        }

        return this.Namespace == other.Namespace &&
               this.NestingTypes == other.NestingTypes &&
               this.Name == other.Name &&
               this.TypeParameters.SequenceEqual(other.TypeParameters);
    }

    public override int GetHashCode() => this.Name.GetHashCode();
}

public enum GenericParameterStyle
{
    /// <summary>
    /// No generic type parameters will be included at all.
    /// </summary>
    None,

    /// <summary>
    /// The generic type definition suffix (lacking type identifiers, such as e.g. <c>&lt;,&gt;</c> for a 2-arity generic type).
    /// </summary>
    TypeDefinition,

    /// <summary>
    /// The most common suffix that includes type identifiers (e.g. <c>&lt;T1, T2&gt;</c>).
    /// </summary>
    Identifiers,

    /// <summary>
    /// A suffix that only indicates arity of the generic type. e.g. <c>`1</c> or <c>`2</c>.
    /// </summary>
    ArityOnly,
}

public enum Qualifiers
{
    /// <summary>
    /// No qualifiers. The type name will stand alone.
    /// </summary>
    None,

    /// <summary>
    /// Includes nesting types, if any.
    /// </summary>
    ContainingTypes,

    /// <summary>
    /// The type name will be qualified by the full namespace (without <c>global::</c>).
    /// </summary>
    Namespace,

    /// <summary>
    /// The type name will be qualified by the full namespace and leading <c>global::</c>.
    /// </summary>
    GlobalNamespace,
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.SourceGenerator.CodeAnalysis;

/// <summary>
/// Describes a type by its name and qualifiers.
/// </summary>
public record QualifiedTypeName : IComparable<QualifiedTypeName>
{
    private QualifiedTypeName? nestingType;
    private string? @namespace;

    /// <summary>
    /// Initializes a new instance of the <see cref="QualifiedTypeName"/> class.
    /// </summary>
    /// <param name="symbol">The symbol this instance will identify.</param>
    public QualifiedTypeName(ITypeSymbol symbol)
    {
        this.Kind = symbol.TypeKind;
        this.IsRecord = symbol.IsRecord;
        ITypeSymbol symbolToConsider = symbol;
        if (symbol is IArrayTypeSymbol arrayType)
        {
            this.ArrayRank = arrayType.Rank;
            symbolToConsider = arrayType.ElementType;
        }

        this.Name = symbolToConsider.Name;
        if (symbolToConsider.ContainingType is not null)
        {
            this.NestingType = new(symbolToConsider.ContainingType);
        }
        else
        {
            this.Namespace = symbolToConsider.ContainingNamespace.GetFullNamespaceName();
        }

        this.TypeParameters = CodeAnalysisUtilities.GetTypeParameters(symbolToConsider);
        this.TypeArguments = CodeAnalysisUtilities.GetTypeArguments(symbolToConsider);
    }

    /// <inheritdoc cref="QualifiedTypeName(string?, QualifiedTypeName?, TypeKind, string, ImmutableArray{string})"/>
    public QualifiedTypeName(string? @namespace, TypeKind kind, string name, ImmutableArray<string>? typeParameters = default)
        : this(@namespace, nestingType: null, kind, name, typeParameters ?? ImmutableArray<string>.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QualifiedTypeName"/> class.
    /// </summary>
    /// <param name="namespace">The full namespace of the type. Must not be empty, but may be <see langword="null" />.</param>
    /// <param name="nestingType">The type that contains this one, if any.</param>
    /// <param name="kind">The C# keyword that identifies the kind of the type (e.g. "class", "struct").</param>
    /// <param name="name">The simple name of the type.</param>
    /// <param name="typeParameters">The generic type identifiers.</param>
    public QualifiedTypeName(string? @namespace, QualifiedTypeName? nestingType, TypeKind kind, string name, ImmutableArray<string> typeParameters)
    {
        if (@namespace == string.Empty)
        {
            throw new ArgumentException("May be null but not empty.", nameof(@namespace));
        }

        if (@namespace is not null && nestingType is not null)
        {
            throw new ArgumentException("A type cannot have both a namespace and a containing type.", nameof(@namespace));
        }

        this.Namespace = @namespace;
        this.NestingType = nestingType;
        this.Kind = kind;
        this.Name = name;
        this.TypeParameters = typeParameters;
    }

    public string? Namespace
    {
        get => this.@namespace;
        init
        {
            if (value is not null && this.NestingType is not null)
            {
                throw new InvalidOperationException("Namespace cannot be specified while NestingType is not null.");
            }

            @namespace = value;
        }
    }

    public QualifiedTypeName? NestingType
    {
        get => this.nestingType;
        init
        {
            if (value is not null && this.Namespace is not null)
            {
                throw new InvalidOperationException("Nesting type cannot be specified while Namespace is not null.");
            }

            nestingType = value;
        }
    }

    /// <summary>
    /// Gets the kind of type.
    /// </summary>
    public TypeKind Kind { get; init; }

    /// <summary>
    /// Gets a value indicating whether the type is a record type.
    /// </summary>
    public bool IsRecord { get; init; }

    /// <summary>
    /// Gets the access modifier that should accompany any declaration of this type, if any.
    /// </summary>
    public Accessibility? AccessModifier { get; init; }

    /// <summary>
    /// Gets the simple name of the type (unqualified by namespace or containing types),
    /// and without any generic type parameters.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the generic type parameters that belong to the type (e.g. T, T2).
    /// </summary>
    public ImmutableArray<string> TypeParameters { get; init; }

    /// <summary>
    /// Gets the generic type arguments that may be present to close a generic type (e.g. <c>string</c>).
    /// </summary>
    public ImmutableArray<string> TypeArguments { get; init; }

    /// <summary>
    /// Gets the number of generic type parameters that belong to the type.
    /// </summary>
    public int Arity => this.TypeParameters.Length;

    public int ArrayRank { get; init; }

    public string GetQualifiedName(Qualifiers qualifier = Qualifiers.GlobalNamespace, GenericParameterStyle genericStyle = GenericParameterStyle.Identifiers)
    {
        StringBuilder builder = new();

        // No type should have both a namespace and a containing type.
        if (this.NestingType is not null && this.Namespace is not null)
        {
            throw new InvalidOperationException("No type has both a namespace and containing type.");
        }

        if (this.NestingType is not null)
        {
            if (qualifier >= Qualifiers.ContainingTypes)
            {
                builder.Append(this.NestingType.GetQualifiedName(qualifier, genericStyle));
                builder.Append('.');
            }
        }
        else
        {
            // Only add the global:: prefix if an alias prefix is not already present.
            if (qualifier is Qualifiers.GlobalNamespace && this.Namespace?.Contains("::") is not true)
            {
                builder.Append("global::");
            }

            if (this.Namespace is not null && qualifier >= Qualifiers.Namespace)
            {
                builder.Append(this.Namespace);
                builder.Append('.');
            }
        }

        builder.Append(this.Name);

        builder.Append(this.GetTypeParametersOrArgs(genericStyle));

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
    public string GetTypeParametersOrArgs(GenericParameterStyle style)
    {
        if (this.TypeParameters.IsEmpty || style == GenericParameterStyle.None)
        {
            return string.Empty;
        }

        return style switch
        {
            GenericParameterStyle.Identifiers => $"<{string.Join(", ", this.TypeParameters)}>",
            GenericParameterStyle.Arguments => $"<{string.Join(", ", this.TypeArguments)}>",
            GenericParameterStyle.TypeDefinition => $"<{new string(',', this.Arity - 1)}>",
            GenericParameterStyle.ArityOnly => $"`{this.Arity}",
            _ => throw new NotImplementedException(),
        };
    }

    public int CompareTo(QualifiedTypeName other) => Compare(this, other);

    public virtual bool Equals(QualifiedTypeName? other)
    {
        if (other is null)
        {
            return false;
        }

        return this.Namespace == other.Namespace &&
               this.NestingType == other.NestingType &&
               this.AccessModifier == other.AccessModifier &&
               this.Kind == other.Kind &&
               this.ArrayRank == other.ArrayRank &&
               this.Name == other.Name &&
               this.TypeParameters.SequenceEqual(other.TypeParameters);
    }

    public override int GetHashCode() => this.Name.GetHashCode();

    public override string ToString() => this.GetQualifiedName(Qualifiers.Namespace);

    private static int Compare(QualifiedTypeName? left, QualifiedTypeName? right)
    {
        if (left is null && right is null)
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        int result = StringComparer.Ordinal.Compare(left.Namespace, right.Namespace);
        if (result != 0)
        {
            return result;
        }

        result = StringComparer.Ordinal.Compare(left.Kind, right.Kind);
        if (result != 0)
        {
            return result;
        }

        result = left.ArrayRank.CompareTo(right.ArrayRank);
        if (result != 0)
        {
            return result;
        }

        result = StringComparer.Ordinal.Compare(left.Name, right.Name);
        if (result != 0)
        {
            return result;
        }

        result = Compare(left.NestingType, right.NestingType);
        if (result != 0)
        {
            return result;
        }

        result = StringComparer.Ordinal.Compare(left.Name, right.Name);
        if (result != 0)
        {
            return result;
        }

        result = left.TypeParameters.Length.CompareTo(right.TypeParameters.Length);
        if (result != 0)
        {
            return result;
        }

        for (int i = 0; i < left.TypeParameters.Length; i++)
        {
            result = StringComparer.Ordinal.Compare(left.TypeParameters[i], right.TypeParameters[i]);
            if (result != 0)
            {
                return result;
            }
        }

        return 0;
    }
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
    /// The original type arguments (e.g. <c>&lt;string, int&gt;</c>).
    /// </summary>
    Arguments,

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

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

/// <summary>
/// Describes a type (that is not an array, nor nested type) by its name and qualifiers.
/// </summary>
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
public record QualifiedNamedTypeName : QualifiedTypeName, IComparable<QualifiedNamedTypeName>
{
    private readonly TypeKind kind;
    private readonly bool isRecord;

    public QualifiedNamedTypeName(TypeKind kind, bool isRecord = false)
    {
        if (kind == TypeKind.Array)
        {
            throw new ArgumentException($"Create an {nameof(QualifiedArrayTypeName)} instead.", nameof(kind));
        }

        if (isRecord && kind is not TypeKind.Class or TypeKind.Struct)
        {
            throw new ArgumentException("Cannot be a record if kind is not a class or struct.", nameof(isRecord));
        }

        this.kind = kind;
        this.isRecord = isRecord;
    }

    [SetsRequiredMembers]
    public QualifiedNamedTypeName(INamedTypeSymbol symbol)
    {
        this.Name = symbol.Name;
        this.Kind = symbol.TypeKind;
        this.isRecord |= symbol.IsRecord;
        this.Container = symbol.ContainingType is { } nesting ? new NestingTypeContainer(new(nesting)) :
            symbol.ContainingNamespace?.GetFullNamespaceName() is string ns ? new NamespaceTypeContainer(ns) :
            null;
        this.TypeParameters = CodeAnalysisUtilities.GetTypeParameters(symbol);
        this.TypeArguments = CodeAnalysisUtilities.GetTypeArguments(symbol);
    }

    [SetsRequiredMembers]
    public QualifiedNamedTypeName(ITypeParameterSymbol symbol)
    {
        this.Name = symbol.Name;
        this.Kind = TypeKind.TypeParameter;
    }

    /// <summary>
    /// Gets the containing namespace or nesting type of this type.
    /// </summary>
    public TypeContainer? Container { get; init; }

    /// <summary>
    /// Gets the simple name of the type (unqualified by namespace or containing types),
    /// and without any generic type parameters.
    /// </summary>
    public required string Name { get; init; }

    public override TypeKind Kind
    {
        get => this.kind;

        init
        {
            if (kind == TypeKind.Array)
            {
                throw new ArgumentException($"Create an {nameof(QualifiedArrayTypeName)} instead.", nameof(kind));
            }

            this.kind = value;
        }
    }

    public override bool IsRecord => this.isRecord;

    /// <summary>
    /// Gets the access modifier that should accompany any declaration of this type, if any.
    /// </summary>
    public Accessibility? AccessModifier { get; init; }

    /// <summary>
    /// Gets the generic type parameters that belong to the type (e.g. T, T2).
    /// </summary>
    public ImmutableArray<string> TypeParameters { get; init; } = ImmutableArray<string>.Empty;

    /// <summary>
    /// Gets the generic type arguments that may be present to close a generic type (e.g. <c>string</c>).
    /// </summary>
    public ImmutableArray<string> TypeArguments { get; init; } = ImmutableArray<string>.Empty;

    /// <summary>
    /// Gets the number of generic type parameters that belong to the type.
    /// </summary>
    public int Arity => this.TypeParameters.Length;

    public override int GetHashCode() => this.Name.GetHashCode();

    public virtual bool Equals(QualifiedNamedTypeName? other)
    {
        if (other is not QualifiedNamedTypeName typedOther)
        {
            return false;
        }

        return base.Equals(other)
            && this.Container == typedOther.Container
            && this.Name == typedOther.Name
            && this.AccessModifier == typedOther.AccessModifier
            && this.TypeParameters.SequenceEqual(typedOther.TypeParameters);
    }

    public override string ToString() => this.GetQualifiedName(Qualifiers.Namespace, GenericParameterStyle.Arguments);

    public int CompareTo(QualifiedNamedTypeName other)
    {
        int result = Comparer<TypeContainer?>.Default.Compare(this.Container, other.Container);
        if (result != 0)
        {
            return result;
        }

        result = StringComparer.Ordinal.Compare(this.Kind, other.Kind);
        if (result != 0)
        {
            return result;
        }

        result = StringComparer.Ordinal.Compare(this.Name, other.Name);
        if (result != 0)
        {
            return result;
        }

        result = this.TypeParameters.Length.CompareTo(other.TypeParameters.Length);
        if (result != 0)
        {
            return result;
        }

        for (int i = 0; i < this.TypeParameters.Length; i++)
        {
            result = StringComparer.Ordinal.Compare(this.TypeParameters[i], other.TypeParameters[i]);
            if (result != 0)
            {
                return result;
            }
        }

        return 0;
    }

    public override string GetQualifiedName(Qualifiers qualifier = Qualifiers.GlobalNamespace, GenericParameterStyle genericStyle = GenericParameterStyle.Identifiers)
    {
        StringBuilder builder = new();

        switch (this.Container)
        {
            case NestingTypeContainer { NestingType: { } nesting }:
                if (qualifier >= Qualifiers.ContainingTypes)
                {
                    builder.Append(nesting.GetQualifiedName(qualifier, genericStyle));
                    builder.Append('.');
                }

                break;
            default:
                string? ns = (this.Container as NamespaceTypeContainer)?.Namespace;

                // Only add the global:: prefix if an alias prefix is not already present.
                if (qualifier is Qualifiers.GlobalNamespace && ns?.Contains("::") is not true)
                {
                    builder.Append("global::");
                }

                if (ns is not null && qualifier >= Qualifiers.Namespace)
                {
                    builder.Append(ns);
                    builder.Append('.');
                }

                break;
        }

        builder.Append(this.Name);
        builder.Append(this.GetTypeParametersOrArgs(genericStyle));

        return builder.ToString();
    }

    public override string GetTypeParametersOrArgs(GenericParameterStyle style)
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
}

public abstract record TypeContainer : IComparable<TypeContainer?>
{
    public abstract int CompareTo(TypeContainer? other);
}

public record NestingTypeContainer(QualifiedNamedTypeName NestingType) : TypeContainer
{
    public static NestingTypeContainer? From(QualifiedNamedTypeName? nesting) => nesting is null ? null : new(nesting);

    public override int CompareTo(TypeContainer? other)
    {
        return other is NestingTypeContainer nestingOther ? this.NestingType.CompareTo(nestingOther.NestingType) :
            other is NamespaceTypeContainer ? -1 : // nested types come before namespace types
            1; // nested types come after non-qualified types.
    }
}

public record NamespaceTypeContainer(string Namespace) : TypeContainer
{
    public static NamespaceTypeContainer? From(string? ns) => ns is null ? null : new(ns);

    public override int CompareTo(TypeContainer? other)
    {
        return other is NamespaceTypeContainer nsOther ? this.Namespace.CompareTo(nsOther.Namespace) :
            other is NestingTypeContainer ? 1 : // namespace types come after nested ones
            1; // namespace types comes after non-qualified types
    }
}

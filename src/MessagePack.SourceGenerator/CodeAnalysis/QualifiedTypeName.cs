// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.SourceGenerator.CodeAnalysis;

/// <summary>
/// Describes a type by its name and qualifiers.
/// </summary>
public abstract record QualifiedTypeName : IComparable<QualifiedTypeName>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualifiedTypeName"/> class.
    /// </summary>
    /// <param name="symbol">The symbol this instance will identify.</param>
    public static QualifiedTypeName Create(ITypeSymbol symbol)
    {
        return symbol switch
        {
            IArrayTypeSymbol arrayType => new QualifiedArrayTypeName(arrayType),
            INamedTypeSymbol namedType => new QualifiedNamedTypeName(namedType),
            ITypeParameterSymbol typeParam => new QualifiedNamedTypeName(typeParam),
            _ => throw new NotSupportedException(),
        };
    }

    /// <summary>
    /// Gets the kind of type.
    /// </summary>
    public abstract TypeKind Kind { get; init; }

    /// <summary>
    /// Gets a value indicating whether the type is a record type.
    /// </summary>
    public abstract bool IsRecord { get; }

    protected string DebuggerDisplay => this.GetQualifiedName(Qualifiers.Namespace, GenericParameterStyle.Arguments);

    public abstract string GetQualifiedName(Qualifiers qualifier = Qualifiers.GlobalNamespace, GenericParameterStyle genericStyle = GenericParameterStyle.Identifiers);

    /// <summary>
    /// Builds a string that acts as a suffix for a C# type name to represent the generic type parameters.
    /// </summary>
    /// <param name="style">The generic type parameters style to generate.</param>
    /// <returns>A string to append to a generic type name, or an empty string if arity is 0.</returns>
    public virtual string GetTypeParametersOrArgs(GenericParameterStyle style) => string.Empty;

    public int CompareTo(QualifiedTypeName other) => Compare(this, other);

    public override string ToString() => this.GetQualifiedName(Qualifiers.Namespace);

    protected int Compare(QualifiedTypeName? left, QualifiedTypeName? right)
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

        // Sort array types before named types.
        if (left is QualifiedArrayTypeName leftArray)
        {
            return right is QualifiedArrayTypeName rightArray ? leftArray.CompareTo(rightArray) : -1;
        }
        else if (left is QualifiedNamedTypeName leftNamed)
        {
            return right is QualifiedNamedTypeName rightNamed ? leftNamed.CompareTo(rightNamed) : 1;
        }
        else
        {
            throw new NotImplementedException();
        }
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

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

/// <summary>
/// Wraps a generic type parameter that is used as an (unresolved) generic type argument.
/// </summary>
/// <param name="Identifier">The identifier (e.g. <c>T</c>).</param>
internal record TypeParameter(string Identifier) : QualifiedTypeName, IComparable<TypeParameter>
{
    public TypeParameter(ITypeParameterSymbol symbol)
        : this(symbol.Name)
    {
    }

    public override TypeKind Kind
    {
        get => TypeKind.TypeParameter;
        init
        {
            if (value != TypeKind.TypeParameter)
            {
                throw new NotSupportedException();
            }
        }
    }

    public override bool IsRecord => false;

    public int CompareTo(TypeParameter other) => this.Identifier.CompareTo(other.Identifier);

    public override string GetQualifiedName(Qualifiers qualifier = Qualifiers.GlobalNamespace, GenericParameterStyle genericStyle = GenericParameterStyle.Identifiers, bool includeNullableAnnotation = false)
        => this.Identifier;
}

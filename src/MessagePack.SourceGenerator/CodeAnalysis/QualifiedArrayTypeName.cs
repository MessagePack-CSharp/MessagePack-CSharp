// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

/// <summary>
/// Describes an array type by its name and qualifiers.
/// </summary>
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
public record QualifiedArrayTypeName : QualifiedTypeName, IComparable<QualifiedArrayTypeName>
{
    private int arrayRank;

    public QualifiedArrayTypeName()
    {
    }

    [SetsRequiredMembers]
    public QualifiedArrayTypeName(IArrayTypeSymbol symbol)
    {
        this.ArrayRank = symbol.Rank;
        this.ElementType = Create(symbol.ElementType);
    }

    public required int ArrayRank
    {
        get => this.arrayRank;
        init
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            this.arrayRank = value;
        }
    }

    public required QualifiedTypeName ElementType { get; init; }

    public override TypeKind Kind
    {
        get => TypeKind.Array;

        init
        {
            if (value != TypeKind.Array)
            {
                throw new NotSupportedException();
            }
        }
    }

    public override bool IsRecord => false;

    public int CompareTo(QualifiedArrayTypeName other)
    {
        int compare = this.ArrayRank.CompareTo(other.ArrayRank);
        if (compare != 0)
        {
            return compare;
        }

        return this.ElementType.CompareTo(other.ElementType);
    }

    public override string ToString() => this.GetQualifiedName(Qualifiers.Namespace, GenericParameterStyle.Arguments);

    public override string GetQualifiedName(Qualifiers qualifier = Qualifiers.GlobalNamespace, GenericParameterStyle genericStyle = GenericParameterStyle.Identifiers)
    {
        StringBuilder builder = new();

        builder.Append(this.ElementType.GetQualifiedName(qualifier, genericStyle));

        Debug.Assert(this.ArrayRank > 0, "ElementType is not null === ArrayRank > 0");
        builder.Append('[');
        builder.Append(',', this.ArrayRank - 1);
        builder.Append(']');

        return builder.ToString();
    }
}

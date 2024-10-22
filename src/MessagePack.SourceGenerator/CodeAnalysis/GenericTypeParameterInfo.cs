// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public record GenericTypeParameterInfo(string Name) : IComparable<GenericTypeParameterInfo>
{
    private string? constraints;

    public GenericTypeParameterInfo(ITypeParameterSymbol typeParameter)
        : this(typeParameter.Name)
    {
        this.HasUnmanagedTypeConstraint = typeParameter.HasUnmanagedTypeConstraint;
        this.HasReferenceTypeConstraint = typeParameter.HasReferenceTypeConstraint;
        this.HasValueTypeConstraint = typeParameter.HasValueTypeConstraint;
        this.HasNotNullConstraint = typeParameter.HasNotNullConstraint;
        this.ConstraintTypes = typeParameter.ConstraintTypes.Select(t => QualifiedTypeName.Create(t)).ToImmutableArray();
        this.HasConstructorConstraint = typeParameter.HasConstructorConstraint;
        this.ReferenceTypeConstraintNullableAnnotation = typeParameter.ReferenceTypeConstraintNullableAnnotation;
    }

    public string Constraints => this.constraints ??= this.ConstructConstraintString();

    public bool HasConstraints => this.Constraints.Length > 0;

    public bool HasUnmanagedTypeConstraint { get; init; }

    public bool HasReferenceTypeConstraint { get; init; }

    public NullableAnnotation ReferenceTypeConstraintNullableAnnotation { get; init; }

    public bool HasValueTypeConstraint { get; init; }

    public bool HasNotNullConstraint { get; init; }

    public ImmutableArray<QualifiedTypeName> ConstraintTypes { get; init; }

    public bool HasConstructorConstraint { get; init; }

    public int CompareTo(GenericTypeParameterInfo other) => this.Name.CompareTo(other.Name);

    private string ConstructConstraintString()
    {
        StringBuilder builder = new();

        // `notnull`, `unmanaged`, `class`, `struct` constraint must come before any constraints.
        AddIf(this.HasNotNullConstraint, "notnull");
        AddIf(this.HasReferenceTypeConstraint, this.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
        AddIf(this.HasValueTypeConstraint, this.HasUnmanagedTypeConstraint ? "unmanaged" : "struct");

        // constraint types (IDisposable, IEnumerable ...)
        foreach (QualifiedNamedTypeName constraintType in this.ConstraintTypes)
        {
            AddIf(true, constraintType.GetQualifiedName(genericStyle: GenericParameterStyle.Arguments, includeNullableAnnotation: true));
        }

        // `new()` constraint must be last in constraints.
        AddIf(this.HasConstructorConstraint, "new()");

        void AddIf(bool condition, string constraint)
        {
            if (condition)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(constraint);
            }
        }

        return builder.ToString();
    }
}

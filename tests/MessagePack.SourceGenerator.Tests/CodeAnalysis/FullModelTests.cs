// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

public class FullModelTests
{
    [Fact]
    public void Equals_Null()
    {
        Assert.False(FullModel.Empty.Equals(null));
    }

    [Fact]
    public void Equals_ByValue()
    {
        EnumSerializationInfo enumInfo = new()
        {
            DataType = new QualifiedNamedTypeName(TypeKind.Enum)
            {
                Container = new NamespaceTypeContainer("My"),
                Name = "MyEnum",
            },
            UnderlyingTypeName = "System.Int32",
            Formatter = new QualifiedNamedTypeName(TypeKind.Class)
            {
                Container = new NamespaceTypeContainer("some"),
                Name = "some",
            },
        };

        // Construct a FullModel with a non-default value for each property.
        FullModel model1a = new(
            ImmutableSortedSet.Create<ObjectSerializationInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<EnumSerializationInfo>(ResolverRegisterInfoComparer.Default).Add(enumInfo),
            ImmutableSortedSet.Create<GenericSerializationInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<UnionSerializationInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<CustomFormatterRegisterInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<ResolverRegisterInfo>(ResolverRegisterInfoComparer.Default),
            new AnalyzerOptions());
        FullModel model1b = new(
            ImmutableSortedSet.Create<ObjectSerializationInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<EnumSerializationInfo>(ResolverRegisterInfoComparer.Default).Add(enumInfo),
            ImmutableSortedSet.Create<GenericSerializationInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<UnionSerializationInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<CustomFormatterRegisterInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<ResolverRegisterInfo>(ResolverRegisterInfoComparer.Default),
            new AnalyzerOptions());

        FullModel model2 = new(
            ImmutableSortedSet.Create<ObjectSerializationInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<EnumSerializationInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<GenericSerializationInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<UnionSerializationInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<CustomFormatterRegisterInfo>(ResolverRegisterInfoComparer.Default),
            ImmutableSortedSet.Create<ResolverRegisterInfo>(ResolverRegisterInfoComparer.Default),
            new AnalyzerOptions());

        Assert.Equal(model1b, model1a);
        Assert.NotEqual(model2, model1a);
        Assert.Equal(model1b.GetHashCode(), model1a.GetHashCode());
    }
}

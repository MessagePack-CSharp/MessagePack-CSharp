// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace MessagePack.Generator.CodeAnalysis;

public record FullModel(
    ImmutableSortedSet<ObjectSerializationInfo> ObjectInfos,
    ImmutableSortedSet<EnumSerializationInfo> EnumInfos,
    ImmutableSortedSet<GenericSerializationInfo> GenericInfos,
    ImmutableSortedSet<UnionSerializationInfo> UnionInfos,
    AnalyzerOptions Options)
{
    public bool IsEmpty => this.ObjectInfos.IsEmpty && this.EnumInfos.IsEmpty && this.GenericInfos.IsEmpty && this.UnionInfos.IsEmpty;

    /// <summary>
    /// Returns a new model that contains all the content of a collection of models.
    /// </summary>
    /// <param name="models">The models to be combined.</param>
    /// <returns>The new, combined model.</returns>
    /// <exception cref="NotSupportedException">Thrown if <see cref="Options"/> is not equal between any two models.</exception>
    public static FullModel Combine(ImmutableArray<FullModel> models)
    {
        if (models.Length == 0)
        {
            return new FullModel(
                ImmutableSortedSet.Create<ObjectSerializationInfo>(ResolverRegisterInfoComparer.Default),
                ImmutableSortedSet.Create<EnumSerializationInfo>(ResolverRegisterInfoComparer.Default),
                ImmutableSortedSet.Create<GenericSerializationInfo>(ResolverRegisterInfoComparer.Default),
                ImmutableSortedSet.Create<UnionSerializationInfo>(ResolverRegisterInfoComparer.Default),
                AnalyzerOptions.Default);
        }

        AnalyzerOptions options = models[0].Options;
        var objectInfos = ImmutableSortedSet.CreateBuilder<ObjectSerializationInfo>(ResolverRegisterInfoComparer.Default);
        var enumInfos = ImmutableSortedSet.CreateBuilder<EnumSerializationInfo>(ResolverRegisterInfoComparer.Default);
        var genericInfos = ImmutableSortedSet.CreateBuilder<GenericSerializationInfo>(ResolverRegisterInfoComparer.Default);
        var unionInfos = ImmutableSortedSet.CreateBuilder<UnionSerializationInfo>(ResolverRegisterInfoComparer.Default);

        foreach (FullModel model in models)
        {
            objectInfos.UnionWith(model.ObjectInfos);
            enumInfos.UnionWith(model.EnumInfos);
            genericInfos.UnionWith(model.GenericInfos);
            unionInfos.UnionWith(model.UnionInfos);

            if (options != model.Options)
            {
                throw new NotSupportedException("Options must be equal.");
            }
        }

        return new FullModel(
            objectInfos.ToImmutable(),
            enumInfos.ToImmutable(),
            genericInfos.ToImmutable(),
            unionInfos.ToImmutable(),
            options);
    }
}

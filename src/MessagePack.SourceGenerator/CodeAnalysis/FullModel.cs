﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public record FullModel(
    ImmutableSortedSet<ObjectSerializationInfo> ObjectInfos,
    ImmutableSortedSet<EnumSerializationInfo> EnumInfos,
    ImmutableSortedSet<GenericSerializationInfo> GenericInfos,
    ImmutableSortedSet<UnionSerializationInfo> UnionInfos,
    ImmutableSortedSet<CustomFormatterRegisterInfo> CustomFormatterInfos,
    ImmutableSortedSet<ResolverRegisterInfo> ArrayFormatterInfos,
    AnalyzerOptions Options)
{
    public static readonly FullModel Empty = new(
        ImmutableSortedSet.Create<ObjectSerializationInfo>(ResolverRegisterInfoComparer.Default),
        ImmutableSortedSet.Create<EnumSerializationInfo>(ResolverRegisterInfoComparer.Default),
        ImmutableSortedSet.Create<GenericSerializationInfo>(ResolverRegisterInfoComparer.Default),
        ImmutableSortedSet.Create<UnionSerializationInfo>(ResolverRegisterInfoComparer.Default),
        ImmutableSortedSet.Create<CustomFormatterRegisterInfo>(ResolverRegisterInfoComparer.Default),
        ImmutableSortedSet.Create<ResolverRegisterInfo>(ResolverRegisterInfoComparer.Default),
        new AnalyzerOptions());

    public bool IsEmpty => this.ObjectInfos.IsEmpty && this.EnumInfos.IsEmpty && this.GenericInfos.IsEmpty && this.UnionInfos.IsEmpty && this.CustomFormatterInfos.IsEmpty;

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
            return Empty;
        }

        AnalyzerOptions options = models[0].Options;
        var objectInfos = ImmutableSortedSet.CreateBuilder<ObjectSerializationInfo>(ResolverRegisterInfoComparer.Default);
        var enumInfos = ImmutableSortedSet.CreateBuilder<EnumSerializationInfo>(ResolverRegisterInfoComparer.Default);
        var genericInfos = ImmutableSortedSet.CreateBuilder<GenericSerializationInfo>(ResolverRegisterInfoComparer.Default);
        var unionInfos = ImmutableSortedSet.CreateBuilder<UnionSerializationInfo>(ResolverRegisterInfoComparer.Default);
        var customFormatterInfos = ImmutableSortedSet.CreateBuilder<CustomFormatterRegisterInfo>(ResolverRegisterInfoComparer.Default);
        var arrayFormatterInfos = ImmutableSortedSet.CreateBuilder<ResolverRegisterInfo>(ResolverRegisterInfoComparer.Default);

        foreach (FullModel model in models)
        {
            objectInfos.UnionWith(model.ObjectInfos);
            enumInfos.UnionWith(model.EnumInfos);
            genericInfos.UnionWith(model.GenericInfos);
            unionInfos.UnionWith(model.UnionInfos);
            customFormatterInfos.UnionWith(model.CustomFormatterInfos);
            arrayFormatterInfos.UnionWith(model.ArrayFormatterInfos);

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
            customFormatterInfos.ToImmutable(),
            arrayFormatterInfos.ToImmutable(),
            options);
    }

    public virtual bool Equals(FullModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (this.GetType() != other.GetType())
        {
            return false;
        }

        return ObjectInfos.SequenceEqual(other.ObjectInfos)
            && EnumInfos.SequenceEqual(other.EnumInfos)
            && GenericInfos.SequenceEqual(other.GenericInfos)
            && UnionInfos.SequenceEqual(other.UnionInfos)
            && CustomFormatterInfos.SequenceEqual(other.CustomFormatterInfos)
            && Options.Equals(other.Options);
    }

    public override int GetHashCode()
    {
        int hashCode = 0;

        hashCode = Hash(hashCode, this.ObjectInfos.Count);
        if (this.ObjectInfos.Count > 0)
        {
            hashCode = Hash(hashCode, this.ObjectInfos[0].GetHashCode());
        }

        hashCode = Hash(hashCode, this.EnumInfos.Count);
        if (this.EnumInfos.Count > 0)
        {
            hashCode = Hash(hashCode, this.EnumInfos[0].GetHashCode());
        }

        hashCode = Hash(hashCode, this.GenericInfos.Count);
        if (this.GenericInfos.Count > 0)
        {
            hashCode = Hash(hashCode, this.GenericInfos[0].GetHashCode());
        }

        hashCode = Hash(hashCode, this.UnionInfos.Count);
        if (this.UnionInfos.Count > 0)
        {
            hashCode = Hash(hashCode, this.UnionInfos[0].GetHashCode());
        }

        hashCode = Hash(hashCode, this.CustomFormatterInfos.Count);
        if (this.CustomFormatterInfos.Count > 0)
        {
            hashCode = Hash(hashCode, this.CustomFormatterInfos[0].GetHashCode());
        }

        hashCode = Hash(hashCode, this.Options.GetHashCode());

        return hashCode;

        static int Hash(int hashCode, int value)
        {
            return (hashCode * 31) + value;
        }
    }
}

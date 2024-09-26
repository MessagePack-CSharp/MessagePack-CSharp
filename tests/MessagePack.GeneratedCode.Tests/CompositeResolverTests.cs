// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using MessagePack;
using MessagePack.Formatters;
using Xunit;

public partial class CompositeResolverTests
{
    /// <summary>
    /// Verifies that formatters for types not defined in the same assemblies are <em>not</em> included in the default generated resolver.
    /// </summary>
    [Fact]
    public void ExternalTypeFormatterNotIncludedInDefaultSourceGeneratedResolver()
    {
        Assert.Null(GeneratedMessagePackResolver.Instance.GetFormatter<BinaryReader>());
    }

    [Fact]
    public void ExternalTypeFormatterNotIncludedInCompositeResolverByDefault()
    {
        Assert.Null(CompositeResolverWithoutLocalFormatters.Instance.GetFormatter<BinaryReader>());
    }

    [Fact]
    public void LocalDataTypeNotIncludedInCompositeResolverByDefault()
    {
        Assert.Null(CompositeResolverWithoutLocalFormatters.Instance.GetFormatter<MPO>());
    }

    [Fact]
    public void ExternalTypeFormatterIncludedInCompositeResolverWithOptIn()
    {
        Assert.NotNull(CompositeResolverWithLocalFormatters.Instance.GetFormatter<BinaryReader>());
    }

    [Fact]
    public void LocalDataTypeIncludedInCompositeResolverWithOptIn()
    {
        Assert.NotNull(CompositeResolverWithLocalFormatters.Instance.GetFormatter<MPO>());
    }

    [CompositeResolver(IncludeLocalFormatters = true)]
    internal partial class CompositeResolverWithLocalFormatters
    {
    }

    [CompositeResolver]
    internal partial class CompositeResolverWithoutLocalFormatters
    {
    }

    /// <summary>
    /// This formatter is for a ridiculous type so as to ensure that it would never have a formatter for it in other circumstances.
    /// </summary>
    internal class FormatterForExternalType : IMessagePackFormatter<BinaryReader>
    {
        public BinaryReader Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            throw new System.NotImplementedException();
        }

        public void Serialize(ref MessagePackWriter writer, BinaryReader value, MessagePackSerializerOptions options)
        {
            throw new System.NotImplementedException();
        }
    }

    [MessagePackObject]
    internal class MPO
    {
    }
}

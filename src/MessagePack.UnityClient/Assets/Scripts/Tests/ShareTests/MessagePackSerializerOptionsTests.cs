// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack;
using MessagePack.Resolvers;
using Xunit;

public class MessagePackSerializerOptionsTests
{
    private static readonly MessagePackSerializerOptions NonDefaultOptions = MessagePackSerializerOptions.Standard
        .WithCompression(MessagePackCompression.LZ4Block)
        .WithAllowAssemblyVersionMismatch(true)
        .WithOmitAssemblyVersion(true)
        .WithResolver(BuiltinResolver.Instance)
        .WithOldSpec(false);

    [Fact]
    public void AllowAssemblyVersionMismatch()
    {
        Assert.False(MessagePackSerializerOptions.Standard.AllowAssemblyVersionMismatch);
        Assert.True(MessagePackSerializerOptions.Standard.WithAllowAssemblyVersionMismatch(true).AllowAssemblyVersionMismatch);
    }

    [Fact]
    public void OmitAssemblyVersion()
    {
        Assert.False(MessagePackSerializerOptions.Standard.OmitAssemblyVersion);
        Assert.True(MessagePackSerializerOptions.Standard.WithOmitAssemblyVersion(true).OmitAssemblyVersion);
    }

    [Fact]
    public void Compression()
    {
        Assert.Equal(MessagePackCompression.None, MessagePackSerializerOptions.Standard.Compression);
        Assert.Equal(MessagePackCompression.LZ4Block, MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.LZ4Block).Compression);
    }

    [Fact]
    public void OldSpec()
    {
        Assert.Null(MessagePackSerializerOptions.Standard.OldSpec);
        Assert.True(MessagePackSerializerOptions.Standard.WithOldSpec(true).OldSpec.Value);
    }

    [Fact]
    public void Resolver()
    {
        Assert.Same((object)StandardResolver.Instance, (object)MessagePackSerializerOptions.Standard.Resolver);
        Assert.Same(BuiltinResolver.Instance, MessagePackSerializerOptions.Standard.WithResolver(BuiltinResolver.Instance).Resolver);
    }

    [Fact]
    public void WithOldSpec_PreservesOtherProperties()
    {
        var mutated = NonDefaultOptions.WithOldSpec(true);
        Assert.True(mutated.OldSpec.Value);
        Assert.Equal(NonDefaultOptions.Compression, mutated.Compression);
        Assert.Equal(NonDefaultOptions.AllowAssemblyVersionMismatch, mutated.AllowAssemblyVersionMismatch);
        Assert.Equal(NonDefaultOptions.OmitAssemblyVersion, mutated.OmitAssemblyVersion);
        Assert.Same((object)NonDefaultOptions.Resolver, (object)mutated.Resolver);
    }

    [Fact]
    public void WithLZ4Compression_PreservesOtherProperties()
    {
        var mutated = NonDefaultOptions.WithCompression(MessagePackCompression.None);
        Assert.Equal(MessagePackCompression.None, mutated.Compression);
        Assert.Equal(NonDefaultOptions.OldSpec, mutated.OldSpec);
        Assert.Equal(NonDefaultOptions.AllowAssemblyVersionMismatch, mutated.AllowAssemblyVersionMismatch);
        Assert.Equal(NonDefaultOptions.OmitAssemblyVersion, mutated.OmitAssemblyVersion);
        Assert.Same((object)NonDefaultOptions.Resolver, (object)mutated.Resolver);
    }

    [Fact]
    public void WithAllowAssemblyVersionMismatch_PreservesOtherProperties()
    {
        var mutated = NonDefaultOptions.WithAllowAssemblyVersionMismatch(false);
        Assert.False(mutated.AllowAssemblyVersionMismatch);
        Assert.Equal(NonDefaultOptions.Compression, mutated.Compression);
        Assert.Equal(NonDefaultOptions.OldSpec, mutated.OldSpec);
        Assert.Equal(NonDefaultOptions.OmitAssemblyVersion, mutated.OmitAssemblyVersion);
        Assert.Same((object)NonDefaultOptions.Resolver, (object)mutated.Resolver);
    }

    [Fact]
    public void WithOmitAssemblyVersion_PreservesOtherProperties()
    {
        var mutated = NonDefaultOptions.WithOmitAssemblyVersion(false);
        Assert.False(mutated.OmitAssemblyVersion);
        Assert.Equal(NonDefaultOptions.Compression, mutated.Compression);
        Assert.Equal(NonDefaultOptions.OldSpec, mutated.OldSpec);
        Assert.Equal(NonDefaultOptions.AllowAssemblyVersionMismatch, mutated.AllowAssemblyVersionMismatch);
        Assert.Same((object)NonDefaultOptions.Resolver, (object)mutated.Resolver);
    }

    [Fact]
    public void WithResolver_PreservesOtherProperties()
    {
        var mutated = NonDefaultOptions.WithResolver(ContractlessStandardResolver.Instance);
        Assert.Same((object)ContractlessStandardResolver.Instance, (object)mutated.Resolver);
        Assert.Equal(NonDefaultOptions.Compression, mutated.Compression);
        Assert.Equal(NonDefaultOptions.OldSpec, mutated.OldSpec);
        Assert.Equal(NonDefaultOptions.AllowAssemblyVersionMismatch, mutated.AllowAssemblyVersionMismatch);
        Assert.Equal(NonDefaultOptions.OmitAssemblyVersion, mutated.OmitAssemblyVersion);
    }
}

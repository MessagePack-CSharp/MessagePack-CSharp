// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using MessagePack;
using MessagePack.Resolvers;
using Xunit;

public class MessagePackSerializerOptionsTests
{
    private static readonly MessagePackSerializerOptions NonDefaultOptions = MessagePackSerializerOptions.Default
        .WithCompression(MessagePackCompression.Lz4Block)
        .WithAllowAssemblyVersionMismatch(true)
        .WithOmitAssemblyVersion(true)
        .WithResolver(BuiltinResolver.Instance)
        .WithOldSpec(false)
        .WithSecurity(MySecurityOptions.Instance)
        .WithSuggestedContiguousMemorySize(64 * 1024);

    [Fact]
    public void AllowAssemblyVersionMismatch()
    {
        Assert.False(MessagePackSerializerOptions.Default.AllowAssemblyVersionMismatch);
        Assert.True(MessagePackSerializerOptions.Default.WithAllowAssemblyVersionMismatch(true).AllowAssemblyVersionMismatch);
    }

    [Fact]
    public void OmitAssemblyVersion()
    {
        Assert.False(MessagePackSerializerOptions.Default.OmitAssemblyVersion);
        Assert.True(MessagePackSerializerOptions.Default.WithOmitAssemblyVersion(true).OmitAssemblyVersion);
    }

    [Fact]
    public void Compression()
    {
        Assert.Equal(MessagePackCompression.None, MessagePackSerializerOptions.Default.Compression);
        Assert.Equal(MessagePackCompression.Lz4Block, MessagePackSerializerOptions.Default.WithCompression(MessagePackCompression.Lz4Block).Compression);
    }

    [Fact]
    public void CompressionMinLength()
    {
        Assert.Equal(64, MessagePackSerializerOptions.Default.CompressionMinLength);
        Assert.Throws<ArgumentOutOfRangeException>(() => MessagePackSerializerOptions.Default.WithCompressionMinLength(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => MessagePackSerializerOptions.Default.WithCompressionMinLength(-1));
        MessagePackSerializerOptions options = MessagePackSerializerOptions.Default.WithCompressionMinLength(128);
        Assert.Equal(128, options.CompressionMinLength);
    }

    [Fact]
    public void SuggestedContiguousMemorySize()
    {
        Assert.Equal(1024 * 1024, MessagePackSerializerOptions.Default.SuggestedContiguousMemorySize);
        Assert.Throws<ArgumentOutOfRangeException>(() => MessagePackSerializerOptions.Default.WithSuggestedContiguousMemorySize(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => MessagePackSerializerOptions.Default.WithSuggestedContiguousMemorySize(4));
        MessagePackSerializerOptions options = MessagePackSerializerOptions.Default.WithSuggestedContiguousMemorySize(512);
        Assert.Equal(512, options.SuggestedContiguousMemorySize);
    }

    [Fact]
    public void OldSpec()
    {
        Assert.Null(MessagePackSerializerOptions.Default.OldSpec);
        Assert.True(MessagePackSerializerOptions.Default.WithOldSpec(true).OldSpec.Value);
    }

    [Fact]
    public void Resolver()
    {
        Assert.Same(StandardResolver.Instance, MessagePackSerializerOptions.Default.Resolver);
        Assert.Same(BuiltinResolver.Instance, MessagePackSerializerOptions.Default.WithResolver(BuiltinResolver.Instance).Resolver);
    }

    [Fact]
    public void Security()
    {
        Assert.Same(MessagePackSecurity.TrustedData, MessagePackSerializerOptions.Default.Security);
        Assert.Same(MessagePackSecurity.UntrustedData, MessagePackSerializerOptions.Default.WithSecurity(MessagePackSecurity.UntrustedData).Security);
    }

    [Fact]
    public void WithOldSpec_PreservesOtherProperties()
    {
        var mutated = NonDefaultOptions.WithOldSpec(true);
        Assert.True(mutated.OldSpec.Value);
        Assert.Equal(NonDefaultOptions.Compression, mutated.Compression);
        Assert.Equal(NonDefaultOptions.SuggestedContiguousMemorySize, mutated.SuggestedContiguousMemorySize);
        Assert.Equal(NonDefaultOptions.AllowAssemblyVersionMismatch, mutated.AllowAssemblyVersionMismatch);
        Assert.Equal(NonDefaultOptions.OmitAssemblyVersion, mutated.OmitAssemblyVersion);
        Assert.Same(NonDefaultOptions.Resolver, mutated.Resolver);
        Assert.Same(MySecurityOptions.Instance, mutated.Security);
    }

    [Fact]
    public void WithLZ4Compression_PreservesOtherProperties()
    {
        var mutated = NonDefaultOptions.WithCompression(MessagePackCompression.None);
        Assert.Equal(MessagePackCompression.None, mutated.Compression);
        Assert.Equal(NonDefaultOptions.OldSpec, mutated.OldSpec);
        Assert.Equal(NonDefaultOptions.AllowAssemblyVersionMismatch, mutated.AllowAssemblyVersionMismatch);
        Assert.Equal(NonDefaultOptions.OmitAssemblyVersion, mutated.OmitAssemblyVersion);
        Assert.Same(NonDefaultOptions.Resolver, mutated.Resolver);
        Assert.Same(MySecurityOptions.Instance, mutated.Security);
    }

    [Fact]
    public void WithSuggestedContiguousMemorySize_PreservesOtherProperties()
    {
        var mutated = NonDefaultOptions.WithSuggestedContiguousMemorySize(612);
        Assert.Equal(612, mutated.SuggestedContiguousMemorySize);
        Assert.Equal(NonDefaultOptions.Compression, mutated.Compression);
        Assert.Equal(NonDefaultOptions.OldSpec, mutated.OldSpec);
        Assert.Equal(NonDefaultOptions.AllowAssemblyVersionMismatch, mutated.AllowAssemblyVersionMismatch);
        Assert.Equal(NonDefaultOptions.OmitAssemblyVersion, mutated.OmitAssemblyVersion);
        Assert.Same(NonDefaultOptions.Resolver, mutated.Resolver);
        Assert.Same(MySecurityOptions.Instance, mutated.Security);
    }

    [Fact]
    public void WithAllowAssemblyVersionMismatch_PreservesOtherProperties()
    {
        var mutated = NonDefaultOptions.WithAllowAssemblyVersionMismatch(false);
        Assert.False(mutated.AllowAssemblyVersionMismatch);
        Assert.Equal(NonDefaultOptions.Compression, mutated.Compression);
        Assert.Equal(NonDefaultOptions.SuggestedContiguousMemorySize, mutated.SuggestedContiguousMemorySize);
        Assert.Equal(NonDefaultOptions.OldSpec, mutated.OldSpec);
        Assert.Equal(NonDefaultOptions.OmitAssemblyVersion, mutated.OmitAssemblyVersion);
        Assert.Same(NonDefaultOptions.Resolver, mutated.Resolver);
        Assert.Same(MySecurityOptions.Instance, mutated.Security);
    }

    [Fact]
    public void WithOmitAssemblyVersion_PreservesOtherProperties()
    {
        var mutated = NonDefaultOptions.WithOmitAssemblyVersion(false);
        Assert.False(mutated.OmitAssemblyVersion);
        Assert.Equal(NonDefaultOptions.Compression, mutated.Compression);
        Assert.Equal(NonDefaultOptions.SuggestedContiguousMemorySize, mutated.SuggestedContiguousMemorySize);
        Assert.Equal(NonDefaultOptions.OldSpec, mutated.OldSpec);
        Assert.Equal(NonDefaultOptions.AllowAssemblyVersionMismatch, mutated.AllowAssemblyVersionMismatch);
        Assert.Same(NonDefaultOptions.Resolver, mutated.Resolver);
        Assert.Same(MySecurityOptions.Instance, mutated.Security);
    }

    [Fact]
    public void WithResolver_PreservesOtherProperties()
    {
        var mutated = NonDefaultOptions.WithResolver(ContractlessStandardResolver.Instance);
        Assert.Same(ContractlessStandardResolver.Instance, mutated.Resolver);
        Assert.Equal(NonDefaultOptions.Compression, mutated.Compression);
        Assert.Equal(NonDefaultOptions.SuggestedContiguousMemorySize, mutated.SuggestedContiguousMemorySize);
        Assert.Equal(NonDefaultOptions.OldSpec, mutated.OldSpec);
        Assert.Equal(NonDefaultOptions.AllowAssemblyVersionMismatch, mutated.AllowAssemblyVersionMismatch);
        Assert.Equal(NonDefaultOptions.OmitAssemblyVersion, mutated.OmitAssemblyVersion);
        Assert.Same(MySecurityOptions.Instance, mutated.Security);
    }

    [Fact]
    public void WithSecurity_PreservesOtherProperties()
    {
        var mutated = NonDefaultOptions.WithSecurity(MessagePackSecurity.TrustedData);
        Assert.Same(MessagePackSecurity.TrustedData, mutated.Security);
        Assert.Same(NonDefaultOptions.Resolver, mutated.Resolver);
        Assert.Equal(NonDefaultOptions.Compression, mutated.Compression);
        Assert.Equal(NonDefaultOptions.SuggestedContiguousMemorySize, mutated.SuggestedContiguousMemorySize);
        Assert.Equal(NonDefaultOptions.OldSpec, mutated.OldSpec);
        Assert.Equal(NonDefaultOptions.AllowAssemblyVersionMismatch, mutated.AllowAssemblyVersionMismatch);
        Assert.Equal(NonDefaultOptions.OmitAssemblyVersion, mutated.OmitAssemblyVersion);
    }

    private class MySecurityOptions : MessagePackSecurity
    {
        internal static readonly MySecurityOptions Instance = new MySecurityOptions();

        private MySecurityOptions()
            : base(TrustedData)
        {
        }
    }
}

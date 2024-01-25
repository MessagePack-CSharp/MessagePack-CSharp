// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tests;

public class GeneratedCompositeResolverTests
{
    [Fact]
    public void CanFindFormatterFromVariousResolvers()
    {
        Assert.NotNull(MyGeneratedCompositeResolver.Instance.GetFormatter<Guid>());
        Assert.NotNull(MyGeneratedCompositeResolver.Instance.GetFormatter<decimal>());
        Assert.Null(MyGeneratedCompositeResolver.Instance.GetFormatter<string>());
    }
}

namespace Tests
{
    [CompositeResolver(typeof(NativeGuidResolver), typeof(NativeDecimalResolver))]
    internal partial class MyGeneratedCompositeResolver
    {
    }
}

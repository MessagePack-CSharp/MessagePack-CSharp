// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CSharpSourceGeneratorVerifier<MessagePack.SourceGenerator.CompositeResolverGenerator>;

public class CompositeResolverGeneratorTests
{
    private readonly ITestOutputHelper logger;

    public CompositeResolverGeneratorTests(ITestOutputHelper logger)
    {
        this.logger = logger;
    }

    [Fact]
    public async Task CompositeResolver_MixedResolverTypes()
    {
        string testSource = """
            using System;
            using MessagePack;
            using MessagePack.Formatters;
            using MessagePack.Resolvers;

            [CompositeResolver(typeof(NativeGuidResolver), typeof(ResolverWithCtor))]
            partial class MyResolver { }

            class Test {
                void Foo() {
                    MyResolver.Instance.GetFormatter<Guid>();
                }
            }

            class ResolverWithCtor : IFormatterResolver {
                public IMessagePackFormatter<T> GetFormatter<T>() => null;
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(this.logger, testSource);
    }
}

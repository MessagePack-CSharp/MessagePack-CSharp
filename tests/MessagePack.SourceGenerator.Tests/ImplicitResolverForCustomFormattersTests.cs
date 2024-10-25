// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.SourceGenerator.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = CSharpSourceGeneratorVerifier<MessagePack.SourceGenerator.MessagePackGenerator>;

public class ImplicitResolverForCustomFormattersTests
{
    private readonly ITestOutputHelper logger;

    public ImplicitResolverForCustomFormattersTests(ITestOutputHelper logger)
    {
        this.logger = logger;
    }

    [Fact]
    public async Task OpenGenericFormatter()
    {
        string testSource = """
            using System;
            using MessagePack;
            using MessagePack.Formatters;
            using MessagePack.Resolvers;

            public struct DynamicArgumentTuple<T1, T2>
            {
                public T1 Item1 { get; set; }
                public T2 Item2 { get; set; }
            }

            internal class DynamicArgumentTupleFormatter<T1, T2> : IMessagePackFormatter<DynamicArgumentTuple<T1, T2>>
            {
                public void Serialize(ref MessagePackWriter writer, DynamicArgumentTuple<T1, T2> value, MessagePackSerializerOptions options)
                {
                    writer.WriteArrayHeader(2);
                    options.Resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, options);
                    options.Resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, options);
                }

                public DynamicArgumentTuple<T1, T2> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
                {
                    if (reader.TryReadArrayHeader(out var count) && count == 2)
                    {
                        T1 item1 = MessagePackSerializer.Deserialize<T1>(ref reader, options);
                        T2 item2 = MessagePackSerializer.Deserialize<T2>(ref reader, options);
                        return new DynamicArgumentTuple<T1, T2> { Item1 = item1, Item2 = item2 };
                    }

                    throw new MessagePackSerializationException("Invalid array length");
                }
            }
            """;
        await VerifyCS.Test.RunDefaultAsync(this.logger, testSource);
    }

    [Fact]
    public async Task MultipleFunctionalCustomFormattersForType()
    {
        string testSource = """
            using System;
            using MessagePack;
            using MessagePack.Formatters;
            using MessagePack.Resolvers;

            internal class {|MsgPack009:AFormatter1|} : IMessagePackFormatter<A>
            {
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) => throw new NotImplementedException();
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => throw new NotImplementedException();
            }

            internal class {|MsgPack009:AFormatter2|} : IMessagePackFormatter<A>
            {
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) => throw new NotImplementedException();
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => throw new NotImplementedException();
            }

            class A { }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.logger, testSource);
    }

    [Fact]
    public async Task MultipleFunctionalCustomFormattersForType_OnlyOneNonExcluded()
    {
        string testSource = """
            using System;
            using MessagePack;
            using MessagePack.Formatters;
            using MessagePack.Resolvers;

            [ExcludeFormatterFromSourceGeneratedResolver]
            internal class AFormatter1 : IMessagePackFormatter<A>
            {
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) => throw new NotImplementedException();
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => throw new NotImplementedException();
            }

            internal class AFormatter2 : IMessagePackFormatter<A>
            {
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) => throw new NotImplementedException();
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => throw new NotImplementedException();
            }

            class A { }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.logger, testSource);
    }

    [Fact]
    public async Task MultipleFunctionalCustomFormattersForType_MultiFormatter()
    {
        string testSource = """
            using System;
            using MessagePack;
            using MessagePack.Formatters;
            using MessagePack.Resolvers;

            internal class {|MsgPack009:AFormatter|} : IMessagePackFormatter<A>
            {
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) => throw new NotImplementedException();
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => throw new NotImplementedException();
            }

            internal class {|MsgPack009:MultiFormatter|} : IMessagePackFormatter<A>, IMessagePackFormatter<B>
            {
                void IMessagePackFormatter<A>.Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) => throw new NotImplementedException();
                A IMessagePackFormatter<A>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => throw new NotImplementedException();
                void IMessagePackFormatter<B>.Serialize(ref MessagePackWriter writer, B value, MessagePackSerializerOptions options) => throw new NotImplementedException();
                B IMessagePackFormatter<B>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => throw new NotImplementedException();
            }

            class A { }
            class B { }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.logger, testSource);
    }

    [Fact]
    public async Task CustomFormatterWithoutDefaultConstructor()
    {
        string testSource = """
            using System;
            using MessagePack;
            using MessagePack.Formatters;
            using MessagePack.Resolvers;

            internal class {|#0:AFormatter|} : IMessagePackFormatter<A>
            {
                public AFormatter(int value) { } // non-default constructor causes problem
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) => throw new NotImplementedException();
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => throw new NotImplementedException();
            }

            class A { }
            """;

        await new VerifyCS.Test()
        {
            TestState =
            {
                Sources = { testSource },
            },
            ExpectedDiagnostics =
            {
                new DiagnosticResult(MsgPack00xMessagePackAnalyzer.InaccessibleFormatterInstance).WithLocation(0),
            },
        }.RunDefaultAsync(this.logger);
    }

    [Fact]
    public async Task CustomFormatterWithoutDefaultConstructor_Excluded()
    {
        string testSource = """
            using System;
            using MessagePack;
            using MessagePack.Formatters;
            using MessagePack.Resolvers;

            [ExcludeFormatterFromSourceGeneratedResolver]
            internal class AFormatter : IMessagePackFormatter<A>
            {
                public AFormatter(int value) { } // non-default constructor causes problem
                public void Serialize(ref MessagePackWriter writer, A value, MessagePackSerializerOptions options) => throw new NotImplementedException();
                public A Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => throw new NotImplementedException();
            }

            class A { }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.logger, testSource);
    }
}

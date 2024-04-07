// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

            internal class {|MsgPack009:IntFormatter1|} : IMessagePackFormatter<int>
            {
                public void Serialize(ref MessagePackWriter writer, int value, MessagePackSerializerOptions options) => writer.Write(value);
                public int Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => reader.ReadInt32();
            }

            internal class {|MsgPack009:IntFormatter2|} : IMessagePackFormatter<int>
            {
                public void Serialize(ref MessagePackWriter writer, int value, MessagePackSerializerOptions options) => writer.Write(value);
                public int Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => reader.ReadInt32();
            }
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

            internal class {|MsgPack009:IntFormatter|} : IMessagePackFormatter<int>
            {
                public void Serialize(ref MessagePackWriter writer, int value, MessagePackSerializerOptions options) => writer.Write(value);
                public int Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => reader.ReadInt32();
            }

            internal class {|MsgPack009:MultiFormatter|} : IMessagePackFormatter<int>, IMessagePackFormatter<bool>
            {
                void IMessagePackFormatter<int>.Serialize(ref MessagePackWriter writer, int value, MessagePackSerializerOptions options) => writer.Write(value);
                int IMessagePackFormatter<int>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => reader.ReadInt32();
                void IMessagePackFormatter<bool>.Serialize(ref MessagePackWriter writer, bool value, MessagePackSerializerOptions options) => writer.Write(value);
                bool IMessagePackFormatter<bool>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => reader.ReadBoolean();
            }
            """;

        await VerifyCS.Test.RunDefaultAsync(this.logger, testSource);
    }
}

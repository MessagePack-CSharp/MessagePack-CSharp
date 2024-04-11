// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;

public class ExecutionTests
{
    private static readonly MessagePackSerializerOptions SerializerOptions = MessagePackSerializerOptions.Standard;

    private readonly ITestOutputHelper logger;

    public ExecutionTests(ITestOutputHelper logger)
    {
        this.logger = logger;
    }

    [Fact]
    public void ClassWithEnumProperty()
    {
        this.AssertRoundtrip(new MyMessagePackObject { EnumValue = MyEnum.B });
    }

    [Fact]
    public void ClassWithPropertiesWithGetterAndSetter()
    {
        this.AssertRoundtrip(new HasPropertiesWithGetterAndSetter { A = 1, B = 4 });
    }

    [Fact]
    public void ClassWithPropertiesWithGetterAndCtor()
    {
        this.AssertRoundtrip(new HasPropertiesWithGetterAndCtor(1, "four"));
    }

    [Fact]
    public void ClassWithUnionProperty()
    {
        this.AssertRoundtrip(new UnionContainer { Value = null });
        this.AssertRoundtrip(new UnionContainer { Value = new Derived1() });
        this.AssertRoundtrip(new UnionContainer { Value = new Derived2() });
    }

    [Fact]
    public void ClassWithPropertyWithTypeWithCustomFormatter()
    {
        this.AssertRoundtrip(new HasPropertyWithTypeWithCustomFormatter { CustomValue = new() { Value = 3 } });
    }

    [Fact]
    public void ClassWithPropertyWithCustomFormatterAttribute()
    {
        this.AssertRoundtrip(new HasPropertyWithCustomFormatterAttribute { CustomValue = new() { Value = 3 } });
    }

    [Fact]
    public void GeneratedResolverPicksUpCustomResolversAutomatically()
    {
        this.AssertRoundtrip(new MyCustomType());
        this.AssertRoundtrip(new MyCustomType2());
    }

    private T AssertRoundtrip<T>(T value)
    {
        byte[] serialized = MessagePackSerializer.Serialize(value, SerializerOptions);
        this.logger.WriteLine(MessagePackSerializer.ConvertToJson(serialized, SerializerOptions));
        T after = MessagePackSerializer.Deserialize<T>(serialized, SerializerOptions);
        Assert.Equal(value, after);
        return after;
    }

    internal record MyCustomType
    {
    }

    internal record MyCustomType2
    {
    }

    /// <remarks>
    /// This formatter is intentionally NOT included in any resolver.
    /// The point is to test whether the <see cref="GeneratedMessagePackResolver"/> generated resolver will automatically pick up on this formatter.
    /// </remarks>
    internal class MyCustomTypeFormatter : IMessagePackFormatter<MyCustomType?>, IMessagePackFormatter<MyCustomType2?>
    {
        MyCustomType? IMessagePackFormatter<MyCustomType?>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            reader.Skip();
            return new MyCustomType();
        }

        void IMessagePackFormatter<MyCustomType?>.Serialize(ref MessagePackWriter writer, MyCustomType? value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(0);
            }
        }

        MyCustomType2? IMessagePackFormatter<MyCustomType2?>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            reader.Skip();
            return new MyCustomType2();
        }

        void IMessagePackFormatter<MyCustomType2?>.Serialize(ref MessagePackWriter writer, MyCustomType2? value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(0);
            }
        }
    }
}

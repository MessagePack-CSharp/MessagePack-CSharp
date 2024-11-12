// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;

public class ExecutionTests(ITestOutputHelper logger)
{
    private static readonly MessagePackSerializerOptions SerializerOptions = MessagePackSerializerOptions.Standard;

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
        this.AssertRoundtrip(new HasPropertiesWithGetterAndCtor(1, "four") { C = 3 });
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

    [Fact]
    public void ClassWithInitProperty()
    {
        this.AssertRoundtrip(new HasInitProperty { A = 1, B = 4, C = 5 });
    }

    [Fact]
    public void ClassWithRequiredMembers()
    {
        this.AssertRoundtrip(new HasRequiredMembers { A = 1, B = 4, C = 5, D = 6 });
    }

    [Fact]
    public void NewPropertyInDerivedType_KeepsValueIndependent()
    {
        NewPropertyInDerivedType.Derived expected = new()
        {
            Prop = "DerivedProp",
            Field = "DerivedField",
            TwoFaced = "DerivedTwo",
        };
        NewPropertyInDerivedType.Base expectedBase = expected;
        expectedBase.Prop = "BaseProp";
        expectedBase.Field = "BaseField";
        expectedBase.TwoFaced = "BaseTwo";

        this.AssertRoundtrip(expected);
    }

    [Fact]
    public void DeserializingConstructorStartsWithIdx1()
    {
        this.AssertRoundtrip(new DeserializingConstructorStartsWithIdx1("foo"));
    }

#if !FORCE_MAP_MODE // forced map mode simply doesn't support private fields at all as it only notices internal and public members.
    [Fact]
    public void PrivateFieldIsSerialized()
    {
        this.AssertRoundtrip(new HasPrivateSerializedMembers { ValueAccessor = 3 });
    }
#endif

    private T AssertRoundtrip<T>(T value)
    {
        var after = Roundtrip(value);
        Assert.Equal(value, after);
        return after;
    }

    private T Roundtrip<T>(T value, MessagePackSerializerOptions? options = null)
    {
        byte[] serialized = MessagePackSerializer.Serialize(value, options ?? SerializerOptions);
        logger.WriteLine(MessagePackSerializer.ConvertToJson(serialized, options ?? SerializerOptions));
        return MessagePackSerializer.Deserialize<T>(serialized, options ?? SerializerOptions);
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

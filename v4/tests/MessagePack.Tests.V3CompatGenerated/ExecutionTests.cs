// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;

public class ExecutionTests(ITestOutputHelper logger)
{
    private static readonly MessagePackSerializerOptions SerializerOptions = MessagePackSerializerOptions.Default;

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

    // V3COMPAT-EDIT: v4 intentional divergence (MessagePackAttributes.cs deviation #2): the
    // generator never auto-collects hand-written formatter classes into a resolver; coverage
    // comes from registration or [MessagePackFormatter]. The formatter below is kept so the
    // build still proves a standalone dual-interface formatter class compiles cleanly.
    [Fact(Skip = "v4 intentional: the source generator does not auto-collect standalone formatter classes")]
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
        logger.WriteLine(MessagePackSerializer.ConvertToJson(serialized)); // V3COMPAT-EDIT: v4's ConvertToJson is a resolver-independent diagnostic view and takes no options
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
    /// The point is to test whether the generated registrations will automatically pick up on this formatter.
    /// V3COMPAT-EDIT: rewritten to the v4 buffer-generic shape; the dual-interface
    /// pickup scenario is unchanged.
    /// </remarks>
    internal class MyCustomTypeFormatter<TWriteBuffer, TReadBuffer> :
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, MyCustomType?>,
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, MyCustomType2?>
        where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer, allows ref struct
        where TReadBuffer : struct, SerializerFoundation.IReadBuffer, allows ref struct
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        void IMessagePackFormatter<TWriteBuffer, TReadBuffer, MyCustomType?>.Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref MyCustomType? value)
        {
            if (buffer.TryReadNil())
            {
                value = null;
                return;
            }

            buffer.Skip();
            value = new MyCustomType();
        }

        void IMessagePackFormatter<TWriteBuffer, TReadBuffer, MyCustomType?>.Serialize(ref TWriteBuffer buffer, ref SerializeState state, MyCustomType? value)
        {
            if (value is null)
            {
                buffer.WriteNil();
            }
            else
            {
                buffer.WriteArrayHeader(0);
            }
        }

        void IMessagePackFormatter<TWriteBuffer, TReadBuffer, MyCustomType2?>.Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref MyCustomType2? value)
        {
            if (buffer.TryReadNil())
            {
                value = null;
                return;
            }

            buffer.Skip();
            value = new MyCustomType2();
        }

        void IMessagePackFormatter<TWriteBuffer, TReadBuffer, MyCustomType2?>.Serialize(ref TWriteBuffer buffer, ref SerializeState state, MyCustomType2? value)
        {
            if (value is null)
            {
                buffer.WriteNil();
            }
            else
            {
                buffer.WriteArrayHeader(0);
            }
        }
    }
}

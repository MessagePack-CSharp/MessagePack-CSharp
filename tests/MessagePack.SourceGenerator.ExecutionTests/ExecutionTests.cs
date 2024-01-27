// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    private T AssertRoundtrip<T>(T value)
    {
        byte[] serialized = MessagePackSerializer.Serialize(value, SerializerOptions);
        this.logger.WriteLine(MessagePackSerializer.ConvertToJson(serialized, SerializerOptions));
        T after = MessagePackSerializer.Deserialize<T>(serialized, SerializerOptions);
        Assert.Equal(value, after);
        return after;
    }
}

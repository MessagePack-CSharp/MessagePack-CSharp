// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class ExecutionTests
{
    private static readonly MessagePackSerializerOptions SerializerOptions = MessagePackSerializerOptions.Standard
        .WithResolver(GeneratedResolver.Instance);

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
        this.AssertRoundtrip(new HasPropertiesWithGetterAndCtor(1, 4));
    }

    private T AssertRoundtrip<T>(T value)
    {
        byte[] serialized = MessagePackSerializer.Serialize(value, SerializerOptions);
        this.logger.WriteLine(MessagePackSerializer.ConvertToJson(serialized, SerializerOptions));
        T after = MessagePackSerializer.Deserialize<T>(serialized, SerializerOptions);
        Assert.Equal(value, after);
        return after;
    }

    [MessagePackObject]
    public record MyMessagePackObject
    {
        [Key(0)]
        public MyEnum EnumValue { get; set; }
    }

    [MessagePackObject(false)]
    public record HasPropertiesWithGetterAndSetter
    {
        [Key(0)]
        public int A { get; set; }

        [Key(1)]
        public int B { get; set; }
    }

    [MessagePackObject(false)]
    public record HasPropertiesWithGetterAndCtor
    {
        [Key(0)]
        public int A { get; }

        [Key(1)]
        public int B { get; }

        public HasPropertiesWithGetterAndCtor(int a, int b)
        {
            A = a;
            B = b;
        }
    }

    public enum MyEnum
    {
        A,
        B,
        C,
    }
}

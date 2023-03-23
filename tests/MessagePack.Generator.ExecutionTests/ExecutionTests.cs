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

    public enum MyEnum
    {
        A,
        B,
        C,
    }
}

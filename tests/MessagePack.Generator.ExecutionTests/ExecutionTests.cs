// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class ExecutionTests
{
    private static readonly MessagePackSerializerOptions SerializerOptions = MessagePackSerializerOptions.Standard
        .WithResolver(GeneratedResolver.Instance);

    [Fact]
    public void ClassWithEnumProperty()
    {
        MyMessagePackObject before = new() { EnumValue = MyEnum.B };
        byte[] serialized = MessagePackSerializer.Serialize(before, SerializerOptions);
        MyMessagePackObject after = MessagePackSerializer.Deserialize<MyMessagePackObject>(serialized, SerializerOptions);
        Assert.Equal(before, after);
    }

    [MessagePackObject]
    public class MyMessagePackObject
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

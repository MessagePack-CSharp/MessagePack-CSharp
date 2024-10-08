// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class MapModeExecutionTests(ITestOutputHelper logger)
{
    private static readonly MessagePackSerializerOptions SerializerOptions = MessagePackSerializerOptions.Standard;

    [Fact]
    public void TestMapModeAgainstVariousVisibility()
    {
        ClassWithUseMapAndMembersOfVariousVisibility original = new()
        {
            PublicProperty = 1,
            InternalProperty = 2,
            PublicField = 4,
            InternalField = 5,
        };
        original.PrivateFieldAccessor() = 3;
        original.SetPrivateProperty(6);

        ClassWithUseMapAndMembersOfVariousVisibility deserialized = Roundtrip(original, MessagePackSerializerOptions.Standard);
        Assert.Equal(original.PublicProperty, deserialized.PublicProperty);
        Assert.Equal(original.PublicField, deserialized.PublicField);
        Assert.Equal(0, deserialized.InternalProperty);
        Assert.Equal(0, deserialized.InternalField);
        Assert.Equal(0, deserialized.GetPrivateProperty());
        Assert.Equal(0, deserialized.PrivateFieldAccessor());
    }

    [Fact]
    public void TestMapModeAgainstVariousVisibility_AllowPrivateAttribute()
    {
        ClassWithUseMapAndMembersOfVariousVisibilityAllowPrivate original = new()
        {
            PublicProperty = 1,
            InternalProperty = 2,
            PublicField = 4,
            InternalField = 5,
        };
        original.PrivateFieldAccessor() = 3;
        original.SetPrivateProperty(6);

        AssertRoundtrip(original);
    }

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
}

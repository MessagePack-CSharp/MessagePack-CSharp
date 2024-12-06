// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class StringKeyOverrideTests(ITestOutputHelper logger)
{
    [Fact]
    public void KeyOnSettableProperty()
    {
        SettableProperty original = new() { TheMessage = "hi" };
        var deserialized = AssertSerializedName(original);
        Assert.Equal(original.TheMessage, deserialized.TheMessage);
    }

    [Fact]
    public void KeyOnReadOnlyProperty()
    {
        ReadOnlyProperty original = new("hi");
        var deserialized = AssertSerializedName(original);
        Assert.Equal(original.TheMessage, deserialized.TheMessage);
    }

    [Fact]
    public void KeyOnReadOnlyField()
    {
        ReadOnlyField original = new("hi");
        var deserialized = AssertSerializedName(original);
        Assert.Equal(original.TheMessage, deserialized.TheMessage);
    }

    [Fact]
    public void KeyOnSettableField()
    {
        SettableField original = new() { TheMessage = "hi" };
        var deserialized = AssertSerializedName(original);
        Assert.Equal(original.TheMessage, deserialized.TheMessage);
    }

    private T AssertSerializedName<T>(T value)
    {
        byte[] msgpack = MessagePackSerializer.Serialize(value, MessagePackSerializerOptions.Standard);

        logger.WriteLine(MessagePackSerializer.ConvertToJson(msgpack));

        MessagePackReader reader = new(msgpack);
        Assert.Equal(1, reader.ReadMapHeader());
        Assert.Equal("message", reader.ReadString());
        return MessagePackSerializer.Deserialize<T>(msgpack, MessagePackSerializerOptions.Standard);
    }

    [MessagePackObject]
    public struct ReadOnlyProperty
    {
        [Key("message")]
        public string TheMessage { get; }

        [SerializationConstructor]
        public ReadOnlyProperty(string theMessage) => this.TheMessage = theMessage;
    }

    [MessagePackObject]
    public struct SettableProperty
    {
        [Key("message")]
        public string TheMessage { get; set; }
    }

    [MessagePackObject]
    public struct ReadOnlyField
    {
        [Key("message")]
        public readonly string TheMessage;

        [SerializationConstructor]
        public ReadOnlyField(string theMessage) => this.TheMessage = theMessage;
    }

    [MessagePackObject]
    public struct SettableField
    {
        [Key("message")]
        public string TheMessage;
    }
}

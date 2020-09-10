// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY_2018_3_OR_NEWER

using System;
using System.Runtime.Serialization;
using MessagePack;
using MessagePack.Resolvers;
using Xunit;
using Xunit.Abstractions;

public class MessagePackSerializerTypelessTests
{
    private readonly ITestOutputHelper logger;

    public MessagePackSerializerTypelessTests(ITestOutputHelper logger)
    {
        this.logger = logger;
    }

    [Fact]
    public void SerializationOfString()
    {
        byte[] msgpack = MessagePackSerializer.Typeless.Serialize("hi");
        this.logger.WriteLine(MessagePackSerializer.ConvertToJson(msgpack));
        Assert.Equal("hi", MessagePackSerializer.Typeless.Deserialize(msgpack));
    }

    [Fact]
    public void SerializationOfSystemType()
    {
        Type type = typeof(string);
        byte[] msgpack = MessagePackSerializer.Typeless.Serialize(type);
        this.logger.WriteLine(MessagePackSerializer.ConvertToJson(msgpack));
        Assert.Equal(type, MessagePackSerializer.Typeless.Deserialize(msgpack));
    }

    [Fact]
    public void SerializationOfDisallowedType()
    {
        var myOptions = new MyTypelessOptions();
        byte[] msgpack = MessagePackSerializer.Typeless.Serialize(new MyObject { SomeValue = 5 }, myOptions);
        this.logger.WriteLine(MessagePackSerializer.ConvertToJson(msgpack, myOptions));
        var ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Typeless.Deserialize(msgpack, myOptions));
        Assert.IsType<TypeAccessException>(ex.InnerException);
    }

    [Fact(Skip = "Known bug https://github.com/neuecc/MessagePack-CSharp/issues/651")]
    public void DecimalShouldBeDeserializedAsDecimal()
    {
        var boxedValue = new TypelessBox { Value = 2049905m };
        var roundTripValue = MessagePackSerializer.Deserialize<TypelessBox>(MessagePackSerializer.Serialize(boxedValue));
        Assert.Equal(boxedValue.Value, roundTripValue.Value);
        Assert.IsType(boxedValue.Value.GetType(), roundTripValue.Value);
    }

    [Fact(Skip = "Known bug https://github.com/neuecc/MessagePack-CSharp/issues/651")]
    public void GuidShouldBeDeserializedAsGuid()
    {
        var boxedValue = new TypelessBox { Value = Guid.NewGuid() };
        var roundTripValue = MessagePackSerializer.Deserialize<TypelessBox>(MessagePackSerializer.Serialize(boxedValue));
        Assert.Equal(boxedValue.Value, roundTripValue.Value);
        Assert.IsType(boxedValue.Value.GetType(), roundTripValue.Value);
    }

    [Fact]
    public void OmitAssemblyVersion()
    {
        string json = MessagePackSerializer.ConvertToJson(MessagePackSerializer.Typeless.Serialize(new MyObject { SomeValue = 5 }));
        this.logger.WriteLine(json);
        Assert.Contains(ThisAssembly.AssemblyVersion, json);
        json = MessagePackSerializer.ConvertToJson(MessagePackSerializer.Typeless.Serialize(new MyObject { SomeValue = 5 }, MessagePackSerializer.Typeless.DefaultOptions.WithOmitAssemblyVersion(true)));
        this.logger.WriteLine(json);
        Assert.DoesNotContain(ThisAssembly.AssemblyVersion, json);
    }

    [Theory]
    [InlineData((sbyte)1)]
    [InlineData((byte)1)]
    [InlineData((short)1)]
    [InlineData((ushort)1)]
    [InlineData((int)1)]
    [InlineData((uint)1)]
    [InlineData((long)1)]
    [InlineData((ulong)1)]
    public void PrimitiveIntTypePreservation(object boxedValue)
    {
        object roundTripValue = MessagePackSerializer.Typeless.Deserialize(MessagePackSerializer.Typeless.Serialize(boxedValue));
        Assert.Equal(boxedValue, roundTripValue);
        Assert.IsType(boxedValue.GetType(), roundTripValue);
    }

    public class MyObject
    {
        public object SomeValue { get; set; }
    }

    [DataContract]
    public class TypelessBox
    {
        [DataMember]
        public object Value { get; set; }
    }

    private class MyTypelessOptions : MessagePackSerializerOptions
    {
        internal MyTypelessOptions()
            : base(TypelessContractlessStandardResolver.Options)
        {
        }

        internal MyTypelessOptions(MyTypelessOptions copyFrom)
            : base(copyFrom)
        {
        }

        public override void ThrowIfDeserializingTypeIsDisallowed(Type type)
        {
            if (type == typeof(MyObject))
            {
                throw new TypeAccessException();
            }
        }

        protected override MessagePackSerializerOptions Clone() => new MyTypelessOptions(this);
    }
}

#endif

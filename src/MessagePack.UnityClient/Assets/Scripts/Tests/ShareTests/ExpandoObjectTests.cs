﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY_2018_3_OR_NEWER

using System.Dynamic;
using System.Runtime.Serialization;
using MessagePack.Resolvers;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Tests
{
    public class ExpandoObjectTests
    {
        private readonly ITestOutputHelper logger;

#if UNITY_2018_3_OR_NEWER

        public ExpandoObjectTests()
        {
            this.logger = new NullTestOutputHelper();
        }

#endif

        public ExpandoObjectTests(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

        [Fact]
        public void ExpandoObject_Roundtrip()
        {
            var options = MessagePackSerializerOptions.Standard;

            dynamic expando = new ExpandoObject();
            expando.Name = "George";
            expando.Age = 18;

            byte[] bin = MessagePackSerializer.Serialize(expando, options);
            this.logger.WriteLine(MessagePackSerializer.ConvertToJson(bin));

            dynamic expando2 = MessagePackSerializer.Deserialize<ExpandoObject>(bin, options);
            Assert.Equal(expando.Name, expando2.Name);
            Assert.Equal(expando.Age, expando2.Age);
        }

        [Fact]
        public void ExpandoObject_DeepGraphContainsAnonymousType()
        {
            dynamic expando = new ExpandoObject();
            expando.Name = "George";
            expando.Age = 18;
            expando.Other = new { OtherProperty = "foo" };

            byte[] bin = MessagePackSerializer.Serialize(expando, MessagePackSerializerOptions.Standard);
            this.logger.WriteLine(MessagePackSerializer.ConvertToJson(bin));

            dynamic expando2 = MessagePackSerializer.Deserialize<ExpandoObject>(bin, ExpandoObjectResolver.Options);
            Assert.Equal(expando.Name, expando2.Name);
            Assert.Equal(expando.Age, expando2.Age);
            Assert.NotNull(expando2.Other);
            Assert.Equal(expando.Other.OtherProperty, expando2.Other.OtherProperty);
        }

        [Fact]
        public void ExpandoObject_DeepGraphContainsCustomTypes()
        {
            var options = MessagePackSerializerOptions.Standard;
            var f = options.Resolver.GetFormatter<string>();

            dynamic expando = new ExpandoObject();
            expando.Name = "George";
            expando.Age = 18;
            expando.Other = new CustomObject { OtherProperty = "foo" };

            byte[] bin = MessagePackSerializer.Serialize(expando, MessagePackSerializerOptions.Standard);
            this.logger.WriteLine(MessagePackSerializer.ConvertToJson(bin));

            dynamic expando2 = MessagePackSerializer.Deserialize<ExpandoObject>(bin, ExpandoObjectResolver.Options);
            Assert.Equal(expando.Name, expando2.Name);
            Assert.Equal(expando.Age, expando2.Age);
            Assert.NotNull(expando2.Other);
            Assert.Equal(expando.Other.OtherProperty, expando2.Other.OtherProperty);
        }

#if !UNITY_2018_3_OR_NEWER

        [Fact]
        public void ExpandoObject_DeepGraphContainsCustomTypes_TypeAnnotated()
        {
            var options = MessagePackSerializerOptions.Standard.WithResolver(TypelessObjectResolver.Instance);

            dynamic expando = new ExpandoObject();
            expando.Name = "George";
            expando.Age = 18;
            expando.Other = new CustomObject { OtherProperty = "foo" };

            byte[] bin = MessagePackSerializer.Serialize(expando, options);
            this.logger.WriteLine(MessagePackSerializer.ConvertToJson(bin));

            dynamic expando2 = MessagePackSerializer.Deserialize<ExpandoObject>(bin, options);
            Assert.Equal(expando.Name, expando2.Name);
            Assert.Equal(expando.Age, expando2.Age);
            Assert.IsType<CustomObject>(expando2.Other);
            Assert.Equal(expando.Other.OtherProperty, expando2.Other.OtherProperty);
        }

#endif

        [DataContract]
        public class CustomObject
        {
            [DataMember]
            public string OtherProperty { get; set; }
        }
    }
}

#endif

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
    public class DynamicObjectResolverDuplicateKeyTests : TestBase
    {
        [Fact]
        public void TestPlainType()
        {
            AssertDetectsDuplicateKey<Request_IntKey>();
            AssertDetectsDuplicateKey<Request_StringKey>();
            AssertDetectsDuplicateKey<Request_IntKey_DataContract>();
            AssertDetectsDuplicateKey<Request_StringKey_DataContract>();
        }

        [Fact]
        public void TestTypeWithInterface()
        {
            // Implicit interface implementation property methods have IsVirtual == True && IsFinal == True
            AssertDetectsDuplicateKey<RequestWithInterface_IntKey>();
            AssertDetectsDuplicateKey<RequestWithInterface_StringKey>();
            AssertDetectsDuplicateKey<RequestWithInterface_IntKey_DataContract>();
            AssertDetectsDuplicateKey<RequestWithInterface_StringKey_DataContract>();
        }

        [Fact]
        public void TestTypeWithVirtualProps()
        {
            AssertDetectsDuplicateKey<RequestWithVirtualProps_IntKey>();
            AssertDetectsDuplicateKey<RequestWithVirtualProps_StringKey>();
            AssertDetectsDuplicateKey<RequestWithVirtualProps_IntKey_DataContract>();
            AssertDetectsDuplicateKey<RequestWithVirtualProps_StringKey_DataContract>();
        }

        private static void AssertDetectsDuplicateKey<T>()
        {
            var resolver = StandardResolver.Instance;
            var ex = Assert.Throws<TypeInitializationException>(resolver.GetFormatterWithVerify<T>);
            Assert.Contains("duplicated", ex.InnerException.Message);
        }

        [MessagePackObject]
        public class Request_IntKey
        {
            [Key(1)]
            public string Id { get; set; }

            [Key(1)]
            public string ClientIp { get; set; }
        }

        [MessagePackObject]
        public class Request_StringKey
        {
            [Key("1")]
            public string Id { get; set; }

            [Key("1")]
            public string ClientIp { get; set; }
        }

        [DataContract]
        public class Request_IntKey_DataContract
        {
            [DataMember(Order = 1)]
            public string Id { get; set; }

            [DataMember(Order = 1)]
            public string ClientIp { get; set; }
        }

        [DataContract]
        public class Request_StringKey_DataContract
        {
            [DataMember(Name = "1")]
            public string Id { get; set; }

            [DataMember(Name = "1")]
            public string ClientIp { get; set; }
        }

        public interface IRequest
        {
            public string Id { get; set; }

            public string ClientIp { get; set; }
        }

        [MessagePackObject]
        public class RequestWithInterface_IntKey : IRequest
        {
            [Key(1)]
            public string Id { get; set; }

            [Key(1)]
            public string ClientIp { get; set; }
        }

        [MessagePackObject]
        public class RequestWithInterface_StringKey : IRequest
        {
            [Key("1")]
            public string Id { get; set; }

            [Key("1")]
            public string ClientIp { get; set; }
        }

        [DataContract]
        public class RequestWithInterface_IntKey_DataContract : IRequest
        {
            [DataMember(Order = 1)]
            public string Id { get; set; }

            [DataMember(Order = 1)]
            public string ClientIp { get; set; }
        }

        [DataContract]
        public class RequestWithInterface_StringKey_DataContract : IRequest
        {
            [DataMember(Name = "1")]
            public string Id { get; set; }

            [DataMember(Name = "1")]
            public string ClientIp { get; set; }
        }

        [MessagePackObject]
        public class RequestWithVirtualProps_IntKey
        {
            [Key(1)]
            public virtual string Id { get; set; }

            [Key(1)]
            public virtual string ClientIp { get; set; }
        }

        [MessagePackObject]
        public class RequestWithVirtualProps_StringKey
        {
            [Key("1")]
            public virtual string Id { get; set; }

            [Key("1")]
            public virtual string ClientIp { get; set; }
        }

        [DataContract]
        public class RequestWithVirtualProps_IntKey_DataContract
        {
            [DataMember(Order = 1)]
            public virtual string Id { get; set; }

            [DataMember(Order = 1)]
            public virtual string ClientIp { get; set; }
        }

        [DataContract]
        public class RequestWithVirtualProps_StringKey_DataContract
        {
            [DataMember(Name = "1")]
            public virtual string Id { get; set; }

            [DataMember(Name = "1")]
            public virtual string ClientIp { get; set; }
        }
    }
}

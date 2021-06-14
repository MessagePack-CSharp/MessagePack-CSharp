// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !ENABLE_IL2CPP

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Resolvers;
using MsgPack.Serialization;
using Xunit;

#pragma warning disable SA1300 // Element should begin with uppercase letter

namespace MessagePack.Tests
{
    public class AllowPrivateTest
    {
        [MessagePackObject]
        public class HasPrivate
        {
            [Key(0)]
            private int privateKey;

            [Key(1)]
            public int PublicKey { get; set; }

            [Key(2)]
            private string privateKeyS { get; set; }

            [Key(3)]
            public string PublicKeyS { get; set; }

            public void SetPrivate(int p1, string p2)
            {
                this.privateKey = p1;
                this.privateKeyS = p2;
            }

            public int GetPrivateInt()
            {
                return this.privateKey;
            }

            public string GetPrivateStr()
            {
                return this.privateKeyS;
            }
        }

        [MessagePackObject]
        public struct HasPrivateStruct
        {
            [Key(0)]
            private int privateKey;

            [Key(1)]
            public int PublicKey { get; set; }

            [Key(2)]
            private string privateKeyS { get; set; }

            [Key(3)]
            public string PublicKeyS { get; set; }

            public void SetPrivate(int p1, string p2)
            {
                this.privateKey = p1;
                this.privateKeyS = p2;
            }

            public int GetPrivateInt()
            {
                return this.privateKey;
            }

            public string GetPrivateStr()
            {
                return this.privateKeyS;
            }
        }

        [MessagePackObject(true)]
        public class HasPrivateStringKey
        {
            private int privateKey;

            public int PublicKey { get; set; }

            private string privateKeyS { get; set; }

            public string PublicKeyS { get; set; }

            public void SetPrivate(int p1, string p2)
            {
                this.privateKey = p1;
                this.privateKeyS = p2;
            }

            public int GetPrivateInt()
            {
                return this.privateKey;
            }

            public string GetPrivateStr()
            {
                return this.privateKeyS;
            }
        }

        public class HasPrivateContractless
        {
            private int privateKey;

            public int PublicKey { get; set; }

            private string privateKeyS { get; set; }

            public string PublicKeyS { get; set; }

            public void SetPrivate(int p1, string p2)
            {
                this.privateKey = p1;
                this.privateKeyS = p2;
            }

            public int GetPrivateInt()
            {
                return this.privateKey;
            }

            public string GetPrivateStr()
            {
                return this.privateKeyS;
            }
        }

        [MessagePackObject]
        public struct EmptyConstructorStruct
        {
            [Key(0)]
            public int X;
        }

        internal enum InternalEnum
        {
            One,
            Two,
        }

        [MessagePackObject]
        internal class InternalClass
        {
            [Key(0)]
            internal InternalEnum EnumProperty { get; set; }
        }

        [MessagePackObject]
        public class PrivateReadonlyField
        {
            public static PrivateReadonlyField WithNullValue { get; } = new PrivateReadonlyField();

            [Key(0)]
            private readonly string field;

            [SerializationConstructor]
            public PrivateReadonlyField(string field)
            {
                this.field = field ?? "not null";
            }

            private PrivateReadonlyField()
            {
            }

            [IgnoreMember]
            public string Field => field;
        }

#if !ENABLE_IL2CPP

        [MessagePackObject]
        public class ImmutablePrivateClass
        {
            [Key(0)]
            private int x;

            [Key(1)]
            private int y;

            [SerializationConstructor]
            private ImmutablePrivateClass(int x, int y)
            {
                this.x = x;
                this.y = y;
                this.CreatedUsingPrivateCtor = true;
            }

            public ImmutablePrivateClass(int x, int y, bool dummy)
            {
                this.x = x;
                this.y = y;
                this.CreatedUsingPrivateCtor = false;
            }

            [IgnoreMember]
            public int X => this.x;

            [IgnoreMember]
            public int Y => this.y;

            [IgnoreMember]
            public bool CreatedUsingPrivateCtor { get; }
        }

        [MessagePackObject]
        public class CompletelyPrivateConstructor
        {
            [Key(0)]
            private int x;

            [Key(1)]
            private int y;

            private CompletelyPrivateConstructor(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public static CompletelyPrivateConstructor Create(int x, int y)
            {
                return new CompletelyPrivateConstructor(x, y);
            }

            [IgnoreMember]
            public int X => this.x;

            [IgnoreMember]
            public int Y => this.y;
        }

#endif

        [Fact]
        public void AllowPrivate()
        {
            {
                var p = new HasPrivate { PublicKey = 100, PublicKeyS = "foo" };
                p.SetPrivate(99, "bar");

                var bin = MessagePackSerializer.Serialize(p, StandardResolverAllowPrivate.Options);
                var json = MessagePackSerializer.ConvertToJson(bin);

                json.Is("[99,100,\"bar\",\"foo\"]");

                HasPrivate r2 = MessagePackSerializer.Deserialize<HasPrivate>(bin, StandardResolverAllowPrivate.Options);
                r2.PublicKey.Is(100);
                r2.PublicKeyS.Is("foo");
                r2.GetPrivateInt().Is(99);
                r2.GetPrivateStr().Is("bar");
            }

            {
                var p = new HasPrivateStruct { PublicKey = 100, PublicKeyS = "foo" };
                p.SetPrivate(99, "bar");

                var bin = MessagePackSerializer.Serialize(p, StandardResolverAllowPrivate.Options);
                var json = MessagePackSerializer.ConvertToJson(bin);

                json.Is("[99,100,\"bar\",\"foo\"]");

                HasPrivate r2 = MessagePackSerializer.Deserialize<HasPrivate>(bin, StandardResolverAllowPrivate.Options);
                r2.PublicKey.Is(100);
                r2.PublicKeyS.Is("foo");
                r2.GetPrivateInt().Is(99);
                r2.GetPrivateStr().Is("bar");
            }

            {
                var p = new HasPrivateStringKey { PublicKey = 100, PublicKeyS = "foo" };
                p.SetPrivate(99, "bar");

                var bin = MessagePackSerializer.Serialize(p, StandardResolverAllowPrivate.Options);
                var json = MessagePackSerializer.ConvertToJson(bin);

                json.Is("{\"PublicKey\":100,\"privateKeyS\":\"bar\",\"PublicKeyS\":\"foo\",\"privateKey\":99}");

                HasPrivateStringKey r2 = MessagePackSerializer.Deserialize<HasPrivateStringKey>(bin, StandardResolverAllowPrivate.Options);
                r2.PublicKey.Is(100);
                r2.PublicKeyS.Is("foo");
                r2.GetPrivateInt().Is(99);
                r2.GetPrivateStr().Is("bar");
            }

            {
                var p = new HasPrivateContractless { PublicKey = 100, PublicKeyS = "foo" };
                p.SetPrivate(99, "bar");

                var bin = MessagePackSerializer.Serialize(p, ContractlessStandardResolverAllowPrivate.Options);
                var json = MessagePackSerializer.ConvertToJson(bin);

                json.Is("{\"PublicKey\":100,\"privateKeyS\":\"bar\",\"PublicKeyS\":\"foo\",\"privateKey\":99}");

                HasPrivateContractless r2 = MessagePackSerializer.Deserialize<HasPrivateContractless>(bin, ContractlessStandardResolverAllowPrivate.Options);
                r2.PublicKey.Is(100);
                r2.PublicKeyS.Is("foo");
                r2.GetPrivateInt().Is(99);
                r2.GetPrivateStr().Is("bar");
            }
        }

        [Fact]
        public void Empty()
        {
            var x = MessagePackSerializer.Serialize(new EmptyConstructorStruct { X = 99 }, StandardResolverAllowPrivate.Options);
            MessagePackSerializer.Deserialize<EmptyConstructorStruct>(x, StandardResolverAllowPrivate.Options).X.Is(99);
        }

        [Fact]
        public void InternalClassWithInternalEnum()
        {
            InternalClass expected = new InternalClass
            {
                EnumProperty = InternalEnum.Two,
            };

            byte[] bytes = MessagePackSerializer.Serialize(expected, StandardResolverAllowPrivate.Options);
            InternalClass actual = MessagePackSerializer.Deserialize<InternalClass>(bytes, StandardResolverAllowPrivate.Options);
            Assert.Equal(expected.EnumProperty, actual.EnumProperty);
        }

        [Fact]
        public void PrivateReadonlyFieldSetInConstructor()
        {
            PrivateReadonlyField initial = PrivateReadonlyField.WithNullValue;
            var bin = MessagePackSerializer.Serialize(initial, StandardResolverAllowPrivate.Options);
            var deserialized = MessagePackSerializer.Deserialize<PrivateReadonlyField>(bin, StandardResolverAllowPrivate.Options);
            Assert.Equal("not null", deserialized.Field);
        }

#if !ENABLE_IL2CPP

        [Fact]
        public void PrivateConstructor()
        {
            var p1 = new ImmutablePrivateClass(10, 20, dummy: false);
            var bin = MessagePackSerializer.Serialize(p1, StandardResolverAllowPrivate.Options);
            var p2 = MessagePackSerializer.Deserialize<ImmutablePrivateClass>(bin, StandardResolverAllowPrivate.Options);

            Assert.Equal(p1.X, p2.X);
            Assert.Equal(p1.Y, p2.Y);
            Assert.False(p1.CreatedUsingPrivateCtor);
            Assert.True(p2.CreatedUsingPrivateCtor);
        }

        [Fact]
        public void PrivateConstructor2()
        {
            var p1 = CompletelyPrivateConstructor.Create(10, 20);
            var bin = MessagePackSerializer.Serialize(p1, StandardResolverAllowPrivate.Options);
            var p2 = MessagePackSerializer.Deserialize<CompletelyPrivateConstructor>(bin, StandardResolverAllowPrivate.Options);

            Assert.Equal(p1.X, p2.X);
            Assert.Equal(p1.Y, p2.Y);
        }
#endif
    }
}

#endif

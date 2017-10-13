using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

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
            string privateKeyS { get; set; }

            [Key(3)]
            public string PublicKeyS { get; set; }

            public void SetPrivate(int p1, string p2)
            {
                privateKey = p1;
                privateKeyS = p2;
            }

            public int GetPrivateInt()
            {
                return privateKey;
            }

            public string GetPrivateStr()
            {
                return privateKeyS;
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
            string privateKeyS { get; set; }

            [Key(3)]
            public string PublicKeyS { get; set; }

            public void SetPrivate(int p1, string p2)
            {
                privateKey = p1;
                privateKeyS = p2;
            }

            public int GetPrivateInt()
            {
                return privateKey;
            }

            public string GetPrivateStr()
            {
                return privateKeyS;
            }
        }

        [MessagePackObject(true)]
        public class HasPrivateStringKey
        {
            private int privateKey;
            public int PublicKey { get; set; }
            string privateKeyS { get; set; }
            public string PublicKeyS { get; set; }

            public void SetPrivate(int p1, string p2)
            {
                privateKey = p1;
                privateKeyS = p2;
            }

            public int GetPrivateInt()
            {
                return privateKey;
            }

            public string GetPrivateStr()
            {
                return privateKeyS;
            }
        }


        public class HasPrivateContractless
        {
            private int privateKey;

            public int PublicKey { get; set; }


            string privateKeyS { get; set; }

            public string PublicKeyS { get; set; }

            public void SetPrivate(int p1, string p2)
            {
                privateKey = p1;
                privateKeyS = p2;
            }

            public int GetPrivateInt()
            {
                return privateKey;
            }

            public string GetPrivateStr()
            {
                return privateKeyS;
            }
        }

        [MessagePackObject]
        public struct EmptyConstructorStruct
        {
            [Key(0)]
            public int X;
        }

        [Fact]
        public void AllowPrivate()
        {
            {
                var p = new HasPrivate { PublicKey = 100, PublicKeyS = "foo" };
                p.SetPrivate(99, "bar");

                var bin = MessagePackSerializer.Serialize(p, MessagePack.Resolvers.StandardResolverAllowPrivate.Instance);
                var json = MessagePackSerializer.ToJson(bin);

                json.Is("[99,100,\"bar\",\"foo\"]");

                var r2 = MessagePackSerializer.Deserialize<HasPrivate>(bin, MessagePack.Resolvers.StandardResolverAllowPrivate.Instance);
                r2.PublicKey.Is(100);
                r2.PublicKeyS.Is("foo");
                r2.GetPrivateInt().Is(99);
                r2.GetPrivateStr().Is("bar");
            }
            {
                var p = new HasPrivateStruct { PublicKey = 100, PublicKeyS = "foo" };
                p.SetPrivate(99, "bar");

                var bin = MessagePackSerializer.Serialize(p, MessagePack.Resolvers.StandardResolverAllowPrivate.Instance);
                var json = MessagePackSerializer.ToJson(bin);

                json.Is("[99,100,\"bar\",\"foo\"]");

                var r2 = MessagePackSerializer.Deserialize<HasPrivate>(bin, MessagePack.Resolvers.StandardResolverAllowPrivate.Instance);
                r2.PublicKey.Is(100);
                r2.PublicKeyS.Is("foo");
                r2.GetPrivateInt().Is(99);
                r2.GetPrivateStr().Is("bar");
            }
            {
                var p = new HasPrivateStringKey { PublicKey = 100, PublicKeyS = "foo" };
                p.SetPrivate(99, "bar");

                var bin = MessagePackSerializer.Serialize(p, MessagePack.Resolvers.StandardResolverAllowPrivate.Instance);
                var json = MessagePackSerializer.ToJson(bin);

                json.Is("{\"PublicKey\":100,\"privateKeyS\":\"bar\",\"PublicKeyS\":\"foo\",\"privateKey\":99}");

                var r2 = MessagePackSerializer.Deserialize<HasPrivateStringKey>(bin, MessagePack.Resolvers.StandardResolverAllowPrivate.Instance);
                r2.PublicKey.Is(100);
                r2.PublicKeyS.Is("foo");
                r2.GetPrivateInt().Is(99);
                r2.GetPrivateStr().Is("bar");
            }
            {
                var p = new HasPrivateContractless { PublicKey = 100, PublicKeyS = "foo" };
                p.SetPrivate(99, "bar");

                var bin = MessagePackSerializer.Serialize(p, MessagePack.Resolvers.ContractlessStandardResolverAllowPrivate.Instance);
                var json = MessagePackSerializer.ToJson(bin);

                json.Is("{\"PublicKey\":100,\"privateKeyS\":\"bar\",\"PublicKeyS\":\"foo\",\"privateKey\":99}");

                var r2 = MessagePackSerializer.Deserialize<HasPrivateContractless>(bin, MessagePack.Resolvers.ContractlessStandardResolverAllowPrivate.Instance);
                r2.PublicKey.Is(100);
                r2.PublicKeyS.Is("foo");
                r2.GetPrivateInt().Is(99);
                r2.GetPrivateStr().Is("bar");
            }
        }

        [Fact]
        public void Empty()
        {
            var x = MessagePackSerializer.Serialize(new EmptyConstructorStruct { X = 99 }, StandardResolverAllowPrivate.Instance);
            MessagePackSerializer.Deserialize<EmptyConstructorStruct>(x, StandardResolverAllowPrivate.Instance).X.Is(99);
        }
    }
}

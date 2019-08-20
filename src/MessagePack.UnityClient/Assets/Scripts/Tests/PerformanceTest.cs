using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using MsgPack.Serialization;
using NUnit.Framework;
using Sandbox.Shared;
using Sandbox.Shared.GeneratedSerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

namespace Sandbox.Shared
{
    [Serializable]
    [DataContract]
    [MessagePackObject]
    public class Person : IEquatable<Person>
    {
        [Key(0)]
        [DataMember(Order = 0)]
        public int Age { get; set; }
        [Key(1)]
        [DataMember(Order = 1)]
        public string FirstName { get; set; }
        [Key(2)]
        [DataMember(Order = 2)]
        public string LastName { get; set; }
        [Key(3)]
        [DataMember(Order = 3)]
        public Sex Sex { get; set; }

        public bool Equals(Person other)
        {
            return Age == other.Age && FirstName == other.FirstName && LastName == other.LastName && Sex == other.Sex;
        }
    }

    public enum Sex : sbyte
    {
        Unknown, Male, Female,
    }
}

namespace MessagePack.UnityClient.Tests
{
    // for JsonUtility:)
    [Serializable]
    public class PersonLike
    {
        public int Age;
        public string FirstName;
        public string LastName;
        public Sex2 Sex;
    }

    [Serializable]
    public class PersonLikeVector
    {
        public PersonLike[] List;
    }

    [Serializable]
    public class Vector3ArrayWrapper
    {
        public Vector3[] List;
    }


    public enum Sex2 : int
    {
        Unknown = 0,
        Male = 1,
        Female = 2
    }

    public class MsgPackUnsafeDefaultResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance;

        public static readonly MessagePackSerializerOptions Options;

        public static readonly MessagePackSerializerOptions LZ4Options;

        static MsgPackUnsafeDefaultResolver()
        {
            Instance = new MsgPackUnsafeDefaultResolver();
            Options = MessagePackSerializer.DefaultOptions.WithResolver(Instance);
            LZ4Options = Options.WithLZ4Compression();
        }

        MsgPackUnsafeDefaultResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
#if ENABLE_UNSAFE_MSGPACK
                formatter = Unity.Extension.UnityBlitResolver.Instance.GetFormatter<T>();
#endif
                if (formatter == null)
                {
                    formatter = MessagePackSerializer.DefaultOptions.Resolver.GetFormatter<T>();
                }
            }
        }
    }

    public class TempVector3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<Vector3>
    {

        public void Serialize(ref MessagePackWriter writer, Vector3 value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public Vector3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var length = reader.ReadArrayHeader();

            var __MyProperty1__ = default(float);
            var __MyProperty2__ = default(float);
            var __MyProperty3__ = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __MyProperty1__ = reader.ReadSingle();
                        break;
                    case 1:
                        __MyProperty2__ = reader.ReadSingle();
                        break;
                    case 2:
                        __MyProperty3__ = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            return new Vector3(__MyProperty1__, __MyProperty2__, __MyProperty3__);
        }
    }

    public class PerformanceTest
    {
        const int Iteration = 500; // 500 iteration.

        Person p;
        Person[] l;
        PersonLike p2;
        PersonLikeVector l2;

        Vector3 v3;
        Vector3[] v3Array;
        Vector3ArrayWrapper v3Wrapper;

        byte[] msgPackCSharpFormatterSingleBytes;
        byte[] msgPackCSharpArrayBytes;
        byte[] msgPackCSharpLZ4FormatterSingleBytes;
        byte[] msgPackCSharpLZ4ArrayBytes;
        byte[] msgpackSingleBytes;
        byte[] msgpackArrayBytes;
        byte[] jsonSingleBytes;
        byte[] jsonArrayBytes;

        byte[] msgPackCSharpv3Bytes;
        byte[] msgPackCSharpv3ArrayBytes;
        byte[] msgpackv3Bytes;
        byte[] msgpackv3ArrayBytes;
        byte[] jsonv3Bytes;
        byte[] jsonv3ArrayBytes;

        SerializationContext msgPackContext;

        // Test Initialize:)
        public PerformanceTest()
        {
            // MsgPack Prepare
            MsgPack.Serialization.MessagePackSerializer.PrepareType<Sex>();
            this.msgPackContext = new MsgPack.Serialization.SerializationContext();
            this.msgPackContext.ResolveSerializer += SerializationContext_ResolveSerializer;

            this.p = new Person
            {
                Age = 99999,
                FirstName = "Windows",
                LastName = "Server",
                Sex = Sex.Male,
            };
            this.p2 = new PersonLike
            {

                Age = 99999,
                FirstName = "Windows",
                LastName = "Server",
                Sex = Sex2.Male
            };

            this.l = Enumerable.Range(1000, 1000).Select(x => new Person { Age = x, FirstName = "Windows", LastName = "Server", Sex = Sex.Female }).ToArray();
            this.l2 = new PersonLikeVector { List = Enumerable.Range(1000, 1000).Select(x => new PersonLike { Age = x, FirstName = "Windows", LastName = "Server", Sex = Sex2.Female }).ToArray() };

            msgPackCSharpFormatterSingleBytes = MessagePackSerializer.Serialize(p, MsgPackUnsafeDefaultResolver.Options);
            msgPackCSharpArrayBytes = MessagePackSerializer.Serialize(l, MsgPackUnsafeDefaultResolver.Options);

            msgPackCSharpLZ4FormatterSingleBytes = MessagePackSerializer.Serialize(p, MsgPackUnsafeDefaultResolver.LZ4Options);
            msgPackCSharpLZ4ArrayBytes = MessagePackSerializer.Serialize(l, MsgPackUnsafeDefaultResolver.LZ4Options);

            var serializer1 = this.msgPackContext.GetSerializer<Person>();
            msgpackSingleBytes = serializer1.PackSingleObject(p);
            var serializer2 = this.msgPackContext.GetSerializer<IList<Person>>();
            msgpackArrayBytes = serializer2.PackSingleObject(l);

            jsonSingleBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(p2));
            jsonArrayBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(l2));

            // vector

            MsgPack.Serialization.MessagePackSerializer.PrepareType<Vector3>();
            MsgPack.Serialization.MessagePackSerializer.PrepareType<Vector3[]>();

            v3 = new Vector3 { x = 12345.12345f, y = 3994.35226f, z = 325125.52426f };
            v3Array = Enumerable.Range(1, 100).Select(_ => new Vector3 { x = 12345.12345f, y = 3994.35226f, z = 325125.52426f }).ToArray();
            v3Wrapper = new Vector3ArrayWrapper { List = v3Array };

            msgPackCSharpv3Bytes = MessagePackSerializer.Serialize(v3, MsgPackUnsafeDefaultResolver.Options);
            msgPackCSharpv3ArrayBytes = MessagePackSerializer.Serialize(v3Array, MsgPackUnsafeDefaultResolver.Options);
            var serializer3 = this.msgPackContext.GetSerializer<Vector3>();
            msgpackv3Bytes = serializer3.PackSingleObject(v3);
            var serializer4 = this.msgPackContext.GetSerializer<Vector3[]>();
            msgpackv3ArrayBytes = serializer4.PackSingleObject(v3Array);

            jsonv3Bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(v3));
            jsonv3ArrayBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(v3Wrapper));
        }

        private void SerializationContext_ResolveSerializer(object sender, ResolveSerializerEventArgs e)
        {
            if (e.TargetType == typeof(Person)) { e.SetSerializer(new PersonSerializer(e.Context)); return; }
            if (e.TargetType == typeof(Sex)) { e.SetSerializer(new SexSerializer(e.Context)); return; }
        }

        [Test]
        public void MessagePackCSharpSerializeSingle()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Serialize(p, MsgPackUnsafeDefaultResolver.Options);
            }
        }

        [Test]
        public void MessagePackCSharpSerializeArray()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Serialize(l, MsgPackUnsafeDefaultResolver.Options);
            }
        }

        [Test]
        public void MessagePackCSharpDeserializeSingle()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Deserialize<Person>(msgPackCSharpFormatterSingleBytes, MsgPackUnsafeDefaultResolver.Options);
            }
        }

        [Test]
        public void MessagePackCSharpDeserializeArray()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Deserialize<Person[]>(msgPackCSharpArrayBytes, MsgPackUnsafeDefaultResolver.Options);
            }
        }



        [Test]
        public void LZ4MessagePackCSharpSerializeSingle()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Serialize(p, MsgPackUnsafeDefaultResolver.LZ4Options);
            }
        }

        [Test]
        public void LZ4MessagePackCSharpSerializeArray()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Serialize(l, MsgPackUnsafeDefaultResolver.LZ4Options);
            }
        }

        [Test]
        public void LZ4MessagePackCSharpDeserializeSingle()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Deserialize<Person>(msgPackCSharpLZ4FormatterSingleBytes, MsgPackUnsafeDefaultResolver.LZ4Options);
            }
        }

        [Test]
        public void LZ4MessagePackCSharpDeserializeArray()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Deserialize<Person[]>(msgPackCSharpLZ4ArrayBytes, MsgPackUnsafeDefaultResolver.LZ4Options);
            }
        }

        [Test]
        public void MsgPackSerializeSingle()
        {
            var serializer = this.msgPackContext.GetSerializer<Person>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.PackSingleObject(p);
            }
        }

        [Test]
        public void MsgPackSerializeArray()
        {
            var serializer = this.msgPackContext.GetSerializer<Person[]>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.PackSingleObject(l);
            }
        }

        [Test]
        public void MsgPackDeserializeSingle()
        {
            var serializer = this.msgPackContext.GetSerializer<Person>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.UnpackSingleObject(msgpackSingleBytes);
            }
        }

        [Test]
        public void MsgPackDeserializeArray()
        {
            var serializer = this.msgPackContext.GetSerializer<Person[]>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.UnpackSingleObject(msgpackArrayBytes);
            }
        }

        [Test]
        public void JsonUtilitySerializeSingle()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = JsonUtility.ToJson(p2);
                Encoding.UTF8.GetBytes(str); // testing with binary...
            }
        }

        [Test]
        public void JsonUtilitySerializeArray()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = JsonUtility.ToJson(l2);
                Encoding.UTF8.GetBytes(str); // testing with binary...
            }
        }

        [Test]
        public void JsonUtilityDeserializeSingle()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = Encoding.UTF8.GetString(jsonSingleBytes);
                JsonUtility.FromJson<PersonLike>(str);
            }
        }

        [Test]
        public void JsonUtilityDeserializeArray()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = Encoding.UTF8.GetString(jsonArrayBytes);
                JsonUtility.FromJson<PersonLikeVector>(str);
            }
        }


        // more...

        [Test]
        public void MessagePackCSharpSerializeVector3()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Serialize(v3, MsgPackUnsafeDefaultResolver.Options);
            }
        }

        [Test]
        public void MessagePackCSharpSerializeVector3Array()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Serialize(v3Array, MsgPackUnsafeDefaultResolver.Options);
            }
        }

        [Test]
        public void MessagePackCSharpDeserializeVector3()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Deserialize<Vector3>(msgPackCSharpv3Bytes, MsgPackUnsafeDefaultResolver.Options);
            }
        }

        [Test]
        public void MessagePackCSharpDeserializeVector3Array()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePackSerializer.Deserialize<Vector3[]>(msgPackCSharpv3ArrayBytes, MsgPackUnsafeDefaultResolver.Options);
            }
        }

        [Test]
        public void MsgPackSerializeVector3()
        {
            var serializer = this.msgPackContext.GetSerializer<Vector3>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.PackSingleObject(v3);
            }
        }

        [Test]
        public void MsgPackSerializeVector3Array()
        {
            var serializer = this.msgPackContext.GetSerializer<Vector3[]>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.PackSingleObject(v3Array);
            }
        }

        [Test]
        public void MsgPackDeserializeVector3()
        {
            var serializer = this.msgPackContext.GetSerializer<Vector3>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.UnpackSingleObject(msgpackv3Bytes);
            }
        }

        [Test]
        public void MsgPackDeserializeVector3Array()
        {
            var serializer = this.msgPackContext.GetSerializer<Vector3[]>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.UnpackSingleObject(msgpackv3ArrayBytes);
            }
        }

        [Test]
        public void JsonUtilitySerializeVector3()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = JsonUtility.ToJson(v3);
                Encoding.UTF8.GetBytes(str); // testing with binary...
            }
        }

        [Test]
        public void JsonUtilitySerializeVector3Array()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = JsonUtility.ToJson(v3Wrapper);
                var bs = Encoding.UTF8.GetBytes(str); // testing with binary...
            }
        }

        [Test]
        public void JsonUtilityDeserializeVector3()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = Encoding.UTF8.GetString(jsonv3Bytes);
                JsonUtility.FromJson<Vector3>(str);
            }
        }

        [Test]
        public void JsonUtilityDeserializeVector3Array()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = Encoding.UTF8.GetString(jsonv3ArrayBytes);
                JsonUtility.FromJson<Vector3[]>(str);
            }
        }
    }
}

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using MsgPack.Serialization;
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
        public static IFormatterResolver Instance = new MsgPackUnsafeDefaultResolver();

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
                if (typeof(T) == typeof(Vector3))
                {
                    formatter = (IMessagePackFormatter<T>)(object)new TempVector3Formatter();
                    return;
                }

                formatter = Unity.Extension.UnityBlitResolver.Instance.GetFormatter<T>();
                if (formatter == null)
                {
                    formatter = StandardResolver.Instance.GetFormatter<T>();
                }
            }
        }
    }

    public class TempVector3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<Vector3>
    {

        public int Serialize(ref byte[] bytes, int offset, Vector3 value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);
            offset += global::MessagePack.MessagePackBinary.WriteSingle(ref bytes, offset, value.x);
            offset += global::MessagePack.MessagePackBinary.WriteSingle(ref bytes, offset, value.y);
            offset += global::MessagePack.MessagePackBinary.WriteSingle(ref bytes, offset, value.z);

            return offset - startOffset;
        }

        public Vector3 Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            var startOffset = offset;
            var length = global::MessagePack.MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
            offset += readSize;

            var __MyProperty1__ = default(float);
            var __MyProperty2__ = default(float);
            var __MyProperty3__ = default(float);

            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        __MyProperty1__ = global::MessagePack.MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 1:
                        __MyProperty2__ = global::MessagePack.MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    case 2:
                        __MyProperty3__ = global::MessagePack.MessagePackBinary.ReadSingle(bytes, offset, out readSize);
                        break;
                    default:
                        readSize = global::MessagePack.MessagePackBinary.ReadNext(bytes, offset);
                        break;
                }
                offset += readSize;
            }

            readSize = offset - startOffset;

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
        public void _Init()
        {
            // MessagePack Prepare
            MessagePackSerializer.SetDefaultResolver(MsgPackUnsafeDefaultResolver.Instance);

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

            msgPackCSharpFormatterSingleBytes = MessagePack.MessagePackSerializer.Serialize(p);
            msgPackCSharpArrayBytes = MessagePack.MessagePackSerializer.Serialize(l);
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

            msgPackCSharpv3Bytes = MessagePack.MessagePackSerializer.Serialize(v3);
            msgPackCSharpv3ArrayBytes = MessagePack.MessagePackSerializer.Serialize(v3Array);
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

        public void MessagePackCSharpSerializeSingle()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePack.MessagePackSerializer.Serialize(p);
            }
        }

        public void MessagePackCSharpSerializeArray()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePack.MessagePackSerializer.Serialize(l);
            }
        }

        public void MessagePackCSharpDeserializeSingle()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePack.MessagePackSerializer.Deserialize<Person>(msgPackCSharpFormatterSingleBytes);
            }
        }

        public void MessagePackCSharpDeserializeArray()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePack.MessagePackSerializer.Deserialize<Person[]>(msgPackCSharpArrayBytes);
            }
        }

        public void MsgPackSerializeSingle()
        {
            var serializer = this.msgPackContext.GetSerializer<Person>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.PackSingleObject(p);
            }
        }

        public void MsgPackSerializeArray()
        {
            var serializer = this.msgPackContext.GetSerializer<Person[]>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.PackSingleObject(l);
            }
        }

        public void MsgPackDeserializeSingle()
        {
            var serializer = this.msgPackContext.GetSerializer<Person>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.UnpackSingleObject(msgpackSingleBytes);
            }
        }

        public void MsgPackDeserializeArray()
        {
            var serializer = this.msgPackContext.GetSerializer<Person[]>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.UnpackSingleObject(msgpackArrayBytes);
            }
        }

        public void JsonUtilitySerializeSingle()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = JsonUtility.ToJson(p2);
                Encoding.UTF8.GetBytes(str); // testing with binary...
            }
        }

        public void JsonUtilitySerializeArray()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = JsonUtility.ToJson(l2);
                Encoding.UTF8.GetBytes(str); // testing with binary...
            }
        }

        public void JsonUtilityDeserializeSingle()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = Encoding.UTF8.GetString(jsonSingleBytes);
                JsonUtility.FromJson<PersonLike>(str);
            }
        }

        public void JsonUtilityDeserializeArray()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = Encoding.UTF8.GetString(jsonArrayBytes);
                JsonUtility.FromJson<PersonLikeVector>(str);
            }
        }


        // more...

        public void MessagePackCSharpSerializeVector3()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePack.MessagePackSerializer.Serialize(v3);
            }
        }

        public void MessagePackCSharpSerializeVector3Array()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePack.MessagePackSerializer.Serialize(v3Array);
            }
        }

        public void MessagePackCSharpDeserializeVector3()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePack.MessagePackSerializer.Deserialize<Vector3>(msgPackCSharpv3Bytes);
            }
        }

        public void MessagePackCSharpDeserializeVector3Array()
        {
            for (int i = 0; i < Iteration; i++)
            {
                MessagePack.MessagePackSerializer.Deserialize<Vector3[]>(msgPackCSharpv3ArrayBytes);
            }
        }

        public void MsgPackSerializeVector3()
        {
            var serializer = this.msgPackContext.GetSerializer<Vector3>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.PackSingleObject(v3);
            }
        }

        public void MsgPackSerializeVector3Array()
        {
            var serializer = this.msgPackContext.GetSerializer<Vector3[]>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.PackSingleObject(v3Array);
            }
        }

        public void MsgPackDeserializeVector3()
        {
            var serializer = this.msgPackContext.GetSerializer<Vector3>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.UnpackSingleObject(msgpackv3Bytes);
            }
        }

        public void MsgPackDeserializeVector3Array()
        {
            var serializer = this.msgPackContext.GetSerializer<Vector3[]>();
            for (int i = 0; i < Iteration; i++)
            {
                serializer.UnpackSingleObject(msgpackv3ArrayBytes);
            }
        }

        public void JsonUtilitySerializeVector3()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = JsonUtility.ToJson(v3);
                Encoding.UTF8.GetBytes(str); // testing with binary...
            }
        }

        public void JsonUtilitySerializeVector3Array()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = JsonUtility.ToJson(v3Wrapper);
                var bs = Encoding.UTF8.GetBytes(str); // testing with binary...
            }
        }

        public void JsonUtilityDeserializeVector3()
        {
            for (int i = 0; i < Iteration; i++)
            {
                var str = Encoding.UTF8.GetString(jsonv3Bytes);
                JsonUtility.FromJson<Vector3>(str);
            }
        }

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
using MessagePack.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{

    public class SpecifiedFormatterResolverTest
    {
        [MessagePackFormatter(typeof(NoObjectFormatter))]
        class CustomClassObject
        {
            int X;

            public CustomClassObject(int x)
            {
                this.X = x;
            }

            public int GetX()
            {
                return X;
            }

            class NoObjectFormatter : IMessagePackFormatter<CustomClassObject>
            {
                public CustomClassObject Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
                {
                    var r = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                    return new CustomClassObject(r);
                }

                public int Serialize(ref byte[] bytes, int offset, CustomClassObject value, IFormatterResolver formatterResolver)
                {
                    return MessagePackBinary.WriteInt32(ref bytes, offset, value.X);
                }
            }
        }

        [MessagePackFormatter(typeof(CustomStructObjectFormatter))]
        struct CustomStructObject
        {
            int X;

            public CustomStructObject(int x)
            {
                this.X = x;
            }

            public int GetX()
            {
                return X;
            }

            class CustomStructObjectFormatter : IMessagePackFormatter<CustomStructObject>
            {
                public CustomStructObject Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
                {
                    var r = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                    return new CustomStructObject(r);
                }

                public int Serialize(ref byte[] bytes, int offset, CustomStructObject value, IFormatterResolver formatterResolver)
                {
                    return MessagePackBinary.WriteInt32(ref bytes, offset, value.X);
                }
            }
        }

        [MessagePackFormatter(typeof(CustomEnumObjectFormatter))]
        enum CustomyEnumObject
        {
            A = 0, B = 1, C = 2
        }

        class CustomEnumObjectFormatter : IMessagePackFormatter<CustomyEnumObject>
        {
            public CustomyEnumObject Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
            {
                var r = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                if (r == 0)
                {
                    return CustomyEnumObject.A;
                }
                else if (r == 2)
                {
                    return CustomyEnumObject.C;
                }
                return CustomyEnumObject.B;
            }

            public int Serialize(ref byte[] bytes, int offset, CustomyEnumObject value, IFormatterResolver formatterResolver)
            {
                return MessagePackBinary.WriteInt32(ref bytes, offset, (int)value);
            }
        }

        [MessagePackFormatter(typeof(CustomInterfaceObjectFormatter))]
        interface ICustomInterfaceObject
        {
            int A { get; }
        }

        class CustomInterfaceObjectFormatter : IMessagePackFormatter<ICustomInterfaceObject>
        {
            public ICustomInterfaceObject Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
            {
                var r = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                return new InheritDefault(r);
            }

            public int Serialize(ref byte[] bytes, int offset, ICustomInterfaceObject value, IFormatterResolver formatterResolver)
            {
                return MessagePackBinary.WriteInt32(ref bytes, offset, value.A);
            }
        }

        class InheritDefault : ICustomInterfaceObject
        {
            public int A { get; }

            public InheritDefault(int a)
            {
                this.A = a;
            }
        }

        class HogeMoge : ICustomInterfaceObject
        {
            public int A { get; set; }
        }

        T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, MessagePack.Resolvers.StandardResolver.Instance), MessagePack.Resolvers.StandardResolver.Instance);
        }

        [Fact]
        public void CustomFormatters()
        {
            Convert(new CustomClassObject(999)).GetX().Is(999);
            Convert(new CustomStructObject(1234)).GetX().Is(1234);
            Convert(CustomyEnumObject.C).Is(CustomyEnumObject.C);
            Convert((CustomyEnumObject)(1234)).Is(CustomyEnumObject.B);
            Convert<ICustomInterfaceObject>(new HogeMoge { A = 999 }).A.Is(999);
        }
    }
}

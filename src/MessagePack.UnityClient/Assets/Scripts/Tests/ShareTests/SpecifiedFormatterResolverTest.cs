// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Formatters;
using Xunit;

namespace MessagePack.Tests
{
    public class SpecifiedFormatterResolverTest
    {
        [MessagePackFormatter(typeof(NoObjectFormatter))]
        private class CustomClassObject
        {
            private int x;

            public CustomClassObject(int x)
            {
                this.x = x;
            }

            public int GetX()
            {
                return this.x;
            }

            private class NoObjectFormatter : IMessagePackFormatter<CustomClassObject>
            {
                public CustomClassObject Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
                {
                    var r = reader.ReadInt32();
                    return new CustomClassObject(r);
                }

                public void Serialize(ref MessagePackWriter writer, CustomClassObject value, MessagePackSerializerOptions options)
                {
                    writer.Write(value.x);
                }
            }
        }

        [MessagePackFormatter(typeof(CustomStructObjectFormatter))]
        private struct CustomStructObject
        {
            private int x;

            public CustomStructObject(int x)
            {
                this.x = x;
            }

            public int GetX()
            {
                return this.x;
            }

            private class CustomStructObjectFormatter : IMessagePackFormatter<CustomStructObject>
            {
                public CustomStructObject Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
                {
                    var r = reader.ReadInt32();
                    return new CustomStructObject(r);
                }

                public void Serialize(ref MessagePackWriter writer, CustomStructObject value, MessagePackSerializerOptions options)
                {
                    writer.Write(value.x);
                }
            }
        }

        [MessagePackFormatter(typeof(CustomEnumObjectFormatter))]
        private enum CustomyEnumObject
        {
            A = 0,
            B = 1,
            C = 2,
        }

        private class CustomEnumObjectFormatter : IMessagePackFormatter<CustomyEnumObject>
        {
            public CustomyEnumObject Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                var r = reader.ReadInt32();
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

            public void Serialize(ref MessagePackWriter writer, CustomyEnumObject value, MessagePackSerializerOptions options)
            {
                writer.Write((int)value);
            }
        }

        [MessagePackFormatter(typeof(CustomInterfaceObjectFormatter))]
        private interface ICustomInterfaceObject
        {
            int A { get; }
        }

        private class CustomInterfaceObjectFormatter : IMessagePackFormatter<ICustomInterfaceObject>
        {
            public ICustomInterfaceObject Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                var r = reader.ReadInt32();
                return new InheritDefault(r);
            }

            public void Serialize(ref MessagePackWriter writer, ICustomInterfaceObject value, MessagePackSerializerOptions options)
            {
                writer.Write(value.A);
            }
        }

        private class InheritDefault : ICustomInterfaceObject
        {
            public int A { get; }

            public InheritDefault(int a)
            {
                this.A = a;
            }
        }

        private class HogeMoge : ICustomInterfaceObject
        {
            public int A { get; set; }
        }

        [MessagePackFormatter(typeof(NoObjectFormatter), CustomyEnumObject.C)]
        private class CustomClassObjectWithArgument
        {
            private int x;

            public CustomClassObjectWithArgument(int x)
            {
                this.x = x;
            }

            public int GetX()
            {
                return this.x;
            }

            private class NoObjectFormatter : IMessagePackFormatter<CustomClassObjectWithArgument>
            {
                private CustomyEnumObject x;

                public NoObjectFormatter(CustomyEnumObject x)
                {
                    this.x = x;
                }

                public CustomClassObjectWithArgument Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
                {
                    var r = reader.ReadInt32();
                    return new CustomClassObjectWithArgument(r);
                }

                public void Serialize(ref MessagePackWriter writer, CustomClassObjectWithArgument value, MessagePackSerializerOptions options)
                {
                    writer.Write(value.x * (int)this.x);
                }
            }
        }

        private T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }

        [Fact]
        public void CustomFormatters()
        {
            this.Convert(new CustomClassObject(999)).GetX().Is(999);
            this.Convert(new CustomStructObject(1234)).GetX().Is(1234);
            this.Convert(CustomyEnumObject.C).Is(CustomyEnumObject.C);
            this.Convert((CustomyEnumObject)1234).Is(CustomyEnumObject.B);
            this.Convert<ICustomInterfaceObject>(new HogeMoge { A = 999 }).A.Is(999);
        }

        [Fact]
        public void WithArg()
        {
            this.Convert(new CustomClassObjectWithArgument(999)).GetX().Is(999 * 2);
        }
    }
}

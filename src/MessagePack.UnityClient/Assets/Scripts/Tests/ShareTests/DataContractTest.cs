// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
#if !ENABLE_IL2CPP

    public class DataContractTest
    {
        [DataContract]
        public class MyClass
        {
            [DataMember(Order = 0)]
            public int MyProperty1 { get; set; }

            [DataMember(Order = 1)]
            public string MyProperty2;
        }

        [DataContract]
        public class MyClass1
        {
            [DataMember(Name = "mp1")]
            public int MyProperty1 { get; set; }

            [DataMember(Name = "mp2")]
            public string MyProperty2;
        }

        [DataContract]
        public class MyClass2
        {
            [DataMember]
            public int MyProperty1 { get; set; }

            [DataMember]
            public string MyProperty2;
        }

        [DataContract]
        public class ClassWithPublicMembersWithoutAttributes
        {
            [DataMember]
            public int AttributedProperty { get; set; }

            public int UnattributedProperty { get; set; }

            [IgnoreDataMember]
            public int IgnoredProperty { get; set; }

            [DataMember]
            public int AttributedField;

            public int UnattributedField;

            [IgnoreDataMember]
            public int IgnoredField;
        }

        [Fact]
        public void SerializeOrder()
        {
            var mc = new MyClass { MyProperty1 = 100, MyProperty2 = "foobar" };

            var bin = MessagePackSerializer.Serialize(mc);
            MyClass mc2 = MessagePackSerializer.Deserialize<MyClass>(bin);

            mc.MyProperty1.Is(mc2.MyProperty1);
            mc.MyProperty2.Is(mc2.MyProperty2);

            MessagePackSerializer.ConvertToJson(bin).Is(@"[100,""foobar""]");
        }

        [Fact]
        public void SerializeName()
        {
            var mc = new MyClass1 { MyProperty1 = 100, MyProperty2 = "foobar" };

            var bin = MessagePackSerializer.Serialize(mc);

            MessagePackSerializer.ConvertToJson(bin).Is(@"{""mp1"":100,""mp2"":""foobar""}");

            MyClass1 mc2 = MessagePackSerializer.Deserialize<MyClass1>(bin);

            mc.MyProperty1.Is(mc2.MyProperty1);
            mc.MyProperty2.Is(mc2.MyProperty2);
        }

        [Fact]
        public void Serialize()
        {
            var mc = new MyClass2 { MyProperty1 = 100, MyProperty2 = "foobar" };

            var bin = MessagePackSerializer.Serialize(mc);
            MyClass2 mc2 = MessagePackSerializer.Deserialize<MyClass2>(bin);

            mc.MyProperty1.Is(mc2.MyProperty1);
            mc.MyProperty2.Is(mc2.MyProperty2);

            MessagePackSerializer.ConvertToJson(bin).Is(@"{""MyProperty1"":100,""MyProperty2"":""foobar""}");
        }

        [Fact]
        public void Serialize_WithVariousAttributes()
        {
            var mc = new ClassWithPublicMembersWithoutAttributes
            {
                AttributedProperty = 1,
                UnattributedProperty = 2,
                IgnoredProperty = 3,
                AttributedField = 4,
                UnattributedField = 5,
                IgnoredField = 6,
            };

            var bin = MessagePackSerializer.Serialize(mc);
            ClassWithPublicMembersWithoutAttributes mc2 = MessagePackSerializer.Deserialize<ClassWithPublicMembersWithoutAttributes>(bin);

            mc2.AttributedProperty.Is(mc.AttributedProperty);
            mc2.AttributedField.Is(mc.AttributedField);

            mc2.UnattributedProperty.Is(0);
            mc2.IgnoredProperty.Is(0);
            mc2.UnattributedField.Is(0);
            mc2.IgnoredField.Is(0);

            MessagePackSerializer.ConvertToJson(bin).Is(@"{""AttributedProperty"":1,""AttributedField"":4}");
        }

        [DataContract]
        public class Master : IEquatable<Master>
        {
            [DataMember]
            public int A { get; set; }

            [DataMember]
            internal Detail InternalComplexProperty { get; set; }

            [DataMember]
            internal Detail InternalComplexField;

            public bool Equals(Master other)
            {
                return other != null
                    && this.A == other.A
                    && EqualityComparer<Detail>.Default.Equals(this.InternalComplexProperty, other.InternalComplexProperty)
                    && EqualityComparer<Detail>.Default.Equals(this.InternalComplexField, other.InternalComplexField);
            }
        }

        public class Detail : IEquatable<Detail>
        {
            public int B1 { get; set; }

            internal int B2 { get; set; }

            public bool Equals(Detail other) => other != null && this.B1 == other.B1 && this.B2 == other.B2;
        }

        [Fact]
        public void DataContractSerializerCompatibility()
        {
            var master = new Master
            {
                A = 1,
                InternalComplexProperty = new Detail
                {
                    B1 = 2,
                    B2 = 3,
                },
                InternalComplexField = new Detail
                {
                    B1 = 4,
                    B2 = 5,
                },
            };

            var dcsValue = DataContractSerializerRoundTrip(master);

            var option = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(
                DynamicObjectResolverAllowPrivate.Instance,
                ContractlessStandardResolver.Instance));
            var mpValue = MessagePackSerializer.Deserialize<Master>(MessagePackSerializer.Serialize(master, option), option);

            Assert.Equal(dcsValue, mpValue);
        }

        private static T DataContractSerializerRoundTrip<T>(T value)
        {
            var ms = new MemoryStream();
            var dcs = new DataContractSerializer(typeof(T));
            dcs.WriteObject(ms, value);
            ms.Position = 0;
            return (T)dcs.ReadObject(ms);
        }
    }

#endif
}

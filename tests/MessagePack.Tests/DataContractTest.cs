using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
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
            var mc2 = MessagePackSerializer.Deserialize<MyClass>(bin);

            mc.MyProperty1.Is(mc2.MyProperty1);
            mc.MyProperty2.Is(mc2.MyProperty2);

            MessagePackSerializer.ToJson(bin).Is(@"[100,""foobar""]");
        }

        [Fact]
        public void SerializeName()
        {
            var mc = new MyClass1 { MyProperty1 = 100, MyProperty2 = "foobar" };

            var bin = MessagePackSerializer.Serialize(mc);

            MessagePackSerializer.ToJson(bin).Is(@"{""mp1"":100,""mp2"":""foobar""}");

            var mc2 = MessagePackSerializer.Deserialize<MyClass1>(bin);

            mc.MyProperty1.Is(mc2.MyProperty1);
            mc.MyProperty2.Is(mc2.MyProperty2);
        }

        [Fact]
        public void Serialize()
        {
            var mc = new MyClass2 { MyProperty1 = 100, MyProperty2 = "foobar" };

            var bin = MessagePackSerializer.Serialize(mc);
            var mc2 = MessagePackSerializer.Deserialize<MyClass2>(bin);

            mc.MyProperty1.Is(mc2.MyProperty1);
            mc.MyProperty2.Is(mc2.MyProperty2);

            MessagePackSerializer.ToJson(bin).Is(@"{""MyProperty1"":100,""MyProperty2"":""foobar""}");
        }

        [Fact]
        public void Serialize_WithVariousAttributes()
        {
            var mc = new ClassWithPublicMembersWithoutAttributes {
                AttributedProperty = 1,
                UnattributedProperty = 2,
                IgnoredProperty = 3,
                AttributedField = 4,
                UnattributedField = 5,
                IgnoredField = 6,
            };

            var bin = MessagePackSerializer.Serialize(mc);
            var mc2 = MessagePackSerializer.Deserialize<ClassWithPublicMembersWithoutAttributes>(bin);

            mc2.AttributedProperty.Is(mc.AttributedProperty);
            mc2.AttributedField.Is(mc.AttributedField);

            mc2.UnattributedProperty.Is(0);
            mc2.IgnoredProperty.Is(0);
            mc2.UnattributedField.Is(0);
            mc2.IgnoredField.Is(0);

            MessagePackSerializer.ToJson(bin).Is(@"{""AttributedProperty"":1,""AttributedField"":4}");
        }
    }
}

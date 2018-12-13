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
        private MessagePackSerializer serializer = new MessagePackSerializer();

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

        [Fact]
        public void SerializeOrder()
        {
            var mc = new MyClass { MyProperty1 = 100, MyProperty2 = "foobar" };

            var bin = serializer.Serialize(mc);
            var mc2 = serializer.Deserialize<MyClass>(bin);

            mc.MyProperty1.Is(mc2.MyProperty1);
            mc.MyProperty2.Is(mc2.MyProperty2);

            serializer.ToJson(bin).Is(@"[100,""foobar""]");
        }

        [Fact]
        public void SerializeName()
        {
            var mc = new MyClass1 { MyProperty1 = 100, MyProperty2 = "foobar" };

            var bin = serializer.Serialize(mc);

            serializer.ToJson(bin).Is(@"{""mp1"":100,""mp2"":""foobar""}");

            var mc2 = serializer.Deserialize<MyClass1>(bin);

            mc.MyProperty1.Is(mc2.MyProperty1);
            mc.MyProperty2.Is(mc2.MyProperty2);
        }

        [Fact]
        public void Serialize()
        {
            var mc = new MyClass2 { MyProperty1 = 100, MyProperty2 = "foobar" };

            var bin = serializer.Serialize(mc);
            var mc2 = serializer.Deserialize<MyClass2>(bin);

            mc.MyProperty1.Is(mc2.MyProperty1);
            mc.MyProperty2.Is(mc2.MyProperty2);

            serializer.ToJson(bin).Is(@"{""MyProperty1"":100,""MyProperty2"":""foobar""}");
        }
    }
}

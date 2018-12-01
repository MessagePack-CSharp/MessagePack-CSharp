using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;
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
        public class PublicPropWithoutAttrModel
        {
            public string PublicProp { get; set; }
            public string PublicFld;
            [DataMember]
            internal string PrivateProp { get; set; }
            [DataMember]
            internal string PrivateFld;
            [DataMember]
            public SubModel Sub { get; set; }
            [DataMember]
            public string AttrPublicProp { get; set; }
        }

        public class SubModel
        {
            public string SubPublicProp { get; set; }
            internal string SubPrivateProp { get; set; }
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
        public void SimulateDataContractSerializer()
        {
            var model = new PublicPropWithoutAttrModel
            {
                PublicProp = "a",
                PublicFld = "b",
                PrivateProp = "i",
                PrivateFld = "j",
                Sub = new SubModel { SubPublicProp = "sub1", SubPrivateProp = "sub2" },
                AttrPublicProp = "zzz"
            };

            //compare DataContractSerializerResolver clone
            var resolver = DataContractSerializerCompositeResolver.Instance;
            var packBin = MessagePackSerializer.Serialize(model, resolver);
            var packModel = MessagePackSerializer.Deserialize<PublicPropWithoutAttrModel>(packBin, resolver);
            packModel.PublicProp.Is(default(string));
            packModel.PublicFld.Is(default(string));
            packModel.PrivateProp.Is(model.PrivateProp);
            packModel.PrivateFld.Is(model.PrivateFld);
            packModel.AttrPublicProp.Is(model.AttrPublicProp);
            packModel.Sub.IsNotNull();
            packModel.Sub.SubPublicProp.Is(model.Sub.SubPublicProp);
            packModel.Sub.SubPrivateProp.Is(default(string));

            MessagePackSerializer.ToJson(packBin).Is(@"{""PrivateProp"":""i"",""Sub"":{""SubPublicProp"":""sub1""},""AttrPublicProp"":""zzz"",""PrivateFld"":""j""}");

            //compare MessagePackSerializer and DataContractSerializer
            var dataContractSerializer = new DataContractSerializer(typeof(PublicPropWithoutAttrModel));
            var dataStream = new MemoryStream();
            dataContractSerializer.WriteObject(dataStream, model);
            dataStream.Position = 0;
            var dataModel = (PublicPropWithoutAttrModel)dataContractSerializer.ReadObject(dataStream);
            dataStream.Dispose();
            dataModel.PublicProp.Is(packModel.PublicProp);
            dataModel.PublicFld.Is(packModel.PublicFld);
            dataModel.PrivateProp.Is(packModel.PrivateProp);
            dataModel.PrivateFld.Is(packModel.PrivateFld);
            dataModel.AttrPublicProp.Is(packModel.AttrPublicProp);
            dataModel.Sub.IsNotNull();
            dataModel.Sub.SubPublicProp.Is(packModel.Sub.SubPublicProp);
            dataModel.Sub.SubPrivateProp.Is(packModel.Sub.SubPrivateProp);
        }

        internal sealed class DataContractSerializerCompositeResolver : IFormatterResolver
        {
            public static IFormatterResolver Instance = new DataContractSerializerCompositeResolver();
            static readonly IFormatterResolver[] resolvers = new[]
            {
                DataContractSerializerResolver.Instance,
                ContractlessStandardResolver.Instance
            };

            DataContractSerializerCompositeResolver() { }

            public IMessagePackFormatter<T> GetFormatter<T>() => FormatterCache<T>.formatter;

            static class FormatterCache<T>
            {
                public static readonly IMessagePackFormatter<T> formatter = resolvers.Select(n => n.GetFormatter<T>()).FirstOrDefault(f => f != null);
            }
        }

    }
}

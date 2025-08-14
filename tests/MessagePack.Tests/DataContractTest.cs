// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !(MESSAGEPACK_FORCE_AOT || ENABLE_IL2CPP)
#define DYNAMIC_GENERATION
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Resolvers;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Tests
{
#if DYNAMIC_GENERATION
    public class DataContractTest
    {
        private readonly ITestOutputHelper logger;

        public DataContractTest(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

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

        [DataContract]
        public class ClassWithPrivateReadonlyDictionary
        {
            [DataMember(Name = "Key", Order = 0, EmitDefaultValue = false)]
            private readonly Guid? key;

            [DataMember(Name = "Body", Order = 1, EmitDefaultValue = false)]
            private readonly Dictionary<string, object> body = new Dictionary<string, object>();

            public ClassWithPrivateReadonlyDictionary(Guid? key)
            {
                this.key = key;
            }

            internal Dictionary<string, object> GetBody() => this.body;
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
        public void Serialize_WithNonSerializedAttribute()
        {
            var mc = new ClassWithOldSchoolNonSerializedAttribute
            {
                PublicField = 1,
                IgnoredPublicField = 2,
            };

            var options = ContractlessStandardResolverAllowPrivate.Options;
            var bin = MessagePackSerializer.Serialize(mc, options);
            var mc2 = MessagePackSerializer.Deserialize<ClassWithOldSchoolNonSerializedAttribute>(bin, options);

            mc2.PublicField.Is(1);
            mc2.IgnoredPublicField.Is(0);

            MessagePackSerializer.ConvertToJson(bin).Is(@"{""PublicField"":1}");
        }

        public class ClassWithOldSchoolNonSerializedAttribute
        {
            public int PublicField;

            [NonSerialized]
            public int IgnoredPublicField;
        }

#if !UNITY_2018_3_OR_NEWER

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

#endif

        private static T DataContractSerializerRoundTrip<T>(T value)
        {
            var ms = new MemoryStream();
            var dcs = new DataContractSerializer(typeof(T));
            dcs.WriteObject(ms, value);
            ms.Position = 0;
            return (T)dcs.ReadObject(ms);
        }

        [Fact]
        public void DeserializeTypeWithPrivateReadonlyDictionary()
        {
            var before = new ClassWithPrivateReadonlyDictionary(Guid.NewGuid());
            before.GetBody()["name"] = "my name";
            byte[] bytes = MessagePackSerializer.Serialize(before, StandardResolverAllowPrivate.Options);
            string json = MessagePackSerializer.ConvertToJson(bytes); // just for check that json has _body' values.
            this.logger.WriteLine(json);

            var after = MessagePackSerializer.Deserialize<ClassWithPrivateReadonlyDictionary>(bytes, StandardResolverAllowPrivate.Options);
            Assert.Equal("my name", after.GetBody()["name"]);
        }

        [Fact]
        public void DeserializeTypeWithPrivateReadonlyDictionary_DCS()
        {
            var before = new ClassWithPrivateReadonlyDictionary(Guid.NewGuid());
            before.GetBody()["name"] = "my name";
            DataContractSerializer dcs = new DataContractSerializer(typeof(ClassWithPrivateReadonlyDictionary));
            var ms = new MemoryStream();
            dcs.WriteObject(ms, before);
            ms.Position = 0;
            var after = (ClassWithPrivateReadonlyDictionary)dcs.ReadObject(ms);
            Assert.Equal("my name", after.GetBody()["name"]);
        }

        #region DataContractBehavior

        // This is a test for the behavior of DataContract serialization with mixing
        // attributes [DataMember] and [DataMember(int Order)]
        // and also setting Order to the same value for multiple properties.
        // wich usually is valid and commonly used

        [DataContract]
        public class MixAttrDataContractBase
        {
            [DataMember(Order = 5)]
            public string ID { get; set; }

            [DataMember]
            public int ZZNumber2 { get; set; }

            [DataMember]
            public int ANumber { get; set; }

            [DataMember(Order = 5)]
            public int HNumber { get; set; }

            [DataMember(Order = 0)]
            public DateTime ZDate { get; set; }
        }

        [DataContract]
        public class MixAttrDataContractUser : MixAttrDataContractBase
        {
            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public int Age { get; set; }

            public static MixAttrDataContractUser GetModelWithTestData()
            {
                return new MixAttrDataContractUser()
                {
                    ID = "12345",
                    Age = 30,
                    Name = "Test User",
                    ZDate = DateTime.Now.ToUniversalTime(),
                    ANumber = 1000,
                    HNumber = 50,
                    ZZNumber2 = 99,
                };
            }
        }

        [Fact]
        public void SerializeDeserialize_DataContractWithMixedKeys()
        {
            var model = MixAttrDataContractUser.GetModelWithTestData();

            DynamicObjectResolver.Instance.BuildFormatterHelperHook -= DynamicObjectResolver_BuildFormatterHelperHook;
            DynamicObjectResolver.Instance.BuildFormatterHelperHook += DynamicObjectResolver_BuildFormatterHelperHook;

            var opts = MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(
                DynamicObjectResolver.Instance,
                StandardResolver.Instance));

            var serialized = MessagePackSerializer.Serialize(model, opts);
            var modelRoundtrip = MessagePackSerializer.Deserialize<MixAttrDataContractUser>(serialized, opts);

            Assert.Equivalent(model, modelRoundtrip);
        }

        private void DynamicObjectResolver_BuildFormatterHelperHook(object sender, DynamicObjectResolver.BuildFormatterHelperHookEventArgs e)
        {
            if (!e.DynamicObjectTypeBuilderConfiguration.ForceStringKey
                && e.Ti.GetCustomAttribute<DataContractAttribute>() is not null
                && e.Ti.GetCustomAttribute<MessagePackObjectAttribute>() is null)
            {
                e.DynamicObjectTypeBuilderConfiguration.ForceStringKey = true;
            }
        }

        [Fact]
        public void ShowcaseDataContractSerializerMixedAttributes()
        {
            var model = MixAttrDataContractUser.GetModelWithTestData();

            MixAttrDataContractUser modelRoundtrip;
            string serializedSE = null;

            var ser = new DataContractSerializer(typeof(MixAttrDataContractUser));

            using (var tw = new StringWriter())
            using (var xw = new System.Xml.XmlTextWriter(tw))
            {
                ser.WriteObject(xw, model);
                serializedSE = tw.ToString();
            }

            using (TextReader tr = new StringReader(serializedSE))
            using (var xmlreader = System.Xml.XmlReader.Create(tr))
            {
                modelRoundtrip = ser.ReadObject(xmlreader) as MixAttrDataContractUser;
            }

            Assert.Equivalent(model, modelRoundtrip);
        }

        #endregion
    }

#endif
}

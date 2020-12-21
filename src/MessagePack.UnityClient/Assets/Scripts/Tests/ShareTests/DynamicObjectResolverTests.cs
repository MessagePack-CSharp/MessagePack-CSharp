// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !ENABLE_IL2CPP

using System;
using System.Runtime.Serialization;
using MessagePack.Resolvers;
using Nerdbank.Streams;
using Xunit;
using Xunit.Abstractions;

namespace MessagePack.Tests
{
    public class DynamicObjectResolverTests
    {
        private readonly ITestOutputHelper logger;

#if UNITY_2018_3_OR_NEWER

        public DynamicObjectResolverTests()
        {
            this.logger = new NullTestOutputHelper();
        }

#endif

        public DynamicObjectResolverTests(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

        [Fact]
        public void SerializeTypeWithReadOnlyField()
        {
            Assert3MemberClassSerializedContent(MessagePackSerializer.Serialize(new TestMessageWithReadOnlyField()));
        }

        [Fact]
        public void SerializeTypeWithReadOnlyProperty()
        {
            Assert3MemberClassSerializedContent(MessagePackSerializer.Serialize(new TestMessageWithReadOnlyProperty()));
        }

        [Fact]
        public void SerializeTypeWithReadWriteFields()
        {
            Assert3MemberClassSerializedContent(MessagePackSerializer.Serialize(new TestMessageWithReadWriteFields()));
        }

        [Fact]
        public void PrivateMembersInBaseClass_StandardResolverAllowPrivate()
        {
            var options = StandardResolverAllowPrivate.Options;
            PrivateMembersInBaseClass_Helper(options);
        }

        [Fact]
        public void DeserializerSetsMissingPropertiesToDefaultValue()
        {
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.WriteMapHeader(1);
            writer.Write(nameof(TwoProperties.Prop1));
            writer.Write("Set");
            writer.Flush();

            var instance = MessagePackSerializer.Deserialize<TwoProperties>(seq);
            Assert.Equal("Set", instance.Prop1);
            Assert.Null(instance.Prop2);
        }

        [Fact]
        public void DeserializerSetsMissingPropertiesToDefaultValue_OrdinalKey()
        {
            var seq = new Sequence<byte>();
            var writer = new MessagePackWriter(seq);
            writer.WriteArrayHeader(1);
            writer.Write("Set");
            writer.Flush();

            var instance = MessagePackSerializer.Deserialize<TwoPropertiesOrdinalKey>(seq);
            Assert.Equal("Set", instance.Prop1);
            Assert.Null(instance.Prop2);
        }

        [Fact]
        public void CtorParameterAndPropertySetterExists()
        {
            var m1 = new ClassWithPropertySetterAndDummyCtor(999) { MyProperty = 100 };
            byte[] bin = MessagePackSerializer.Serialize(m1);
            var m2 = MessagePackSerializer.Deserialize<ClassWithPropertySetterAndDummyCtor>(bin);

            // In this version of the deserializer, we expect the property setter to be invoked
            // and reaffirm the value already passed to the constructor.
            Assert.Equal(m1.MyProperty, m2.MyProperty);
        }

        /// <summary>
        /// Verifies that virtual and overridden properties do not cause the dynamic resolver to malfunction.
        /// </summary>
        [Fact]
        public void VirtualOverriddenProperties()
        {
            var obj = new DerivedClassThatOverridesProperty { VirtualProperty = 100 };
            byte[] bin = MessagePackSerializer.Serialize(obj);
            var obj2 = MessagePackSerializer.Deserialize<DerivedClassThatOverridesProperty>(bin);
            Assert.Equal(obj.VirtualProperty, obj2.VirtualProperty);
        }

        [Fact]
        public void VirtualOverriddenProperties_DataMemberOnBase()
        {
            var obj = new DerivedClassThatOverridesPropertyDataMemberOnVirtualOnly { VirtualProperty = 100 };
            byte[] bin = MessagePackSerializer.Serialize(obj);
            var obj2 = MessagePackSerializer.Deserialize<DerivedClassThatOverridesPropertyDataMemberOnVirtualOnly>(bin);
            Assert.Equal(obj.VirtualProperty, obj2.VirtualProperty);
        }

        [Fact]
        public void VirtualOverriddenProperties_DataMemberOnOverride()
        {
            var obj = new DerivedClassThatOverridesPropertyDataMemberOnOverrideOnly { VirtualProperty = 100 };
            byte[] bin = MessagePackSerializer.Serialize(obj);
            var obj2 = MessagePackSerializer.Deserialize<DerivedClassThatOverridesPropertyDataMemberOnOverrideOnly>(bin);
            Assert.Equal(obj.VirtualProperty, obj2.VirtualProperty);
        }

        /// <summary>
        /// Verifies that virtual and overridden properties do not cause the dynamic resolver to malfunction.
        /// </summary>
        [Fact]
        public void VirtualOverriddenProperties_OrdinalKey()
        {
            var obj = new DerivedClassThatOverridesPropertyOrdinalKey { VirtualProperty = 100 };
            byte[] bin = MessagePackSerializer.Serialize(obj);
            var obj2 = MessagePackSerializer.Deserialize<DerivedClassThatOverridesPropertyOrdinalKey>(bin);
            Assert.Equal(obj.VirtualProperty, obj2.VirtualProperty);
        }

        [Fact]
        public void VirtualOverriddenProperties_DataMemberOnBase_OrdinalKey()
        {
            var obj = new DerivedClassThatOverridesPropertyDataMemberOnVirtualOnlyOrdinalKey { VirtualProperty = 100 };
            byte[] bin = MessagePackSerializer.Serialize(obj);
            var obj2 = MessagePackSerializer.Deserialize<DerivedClassThatOverridesPropertyDataMemberOnVirtualOnlyOrdinalKey>(bin);
            Assert.Equal(obj.VirtualProperty, obj2.VirtualProperty);
        }

        [Fact]
        public void VirtualOverriddenProperties_DataMemberOnOverride_OrdinalKey()
        {
            var obj = new DerivedClassThatOverridesPropertyDataMemberOnOverrideOnlyOrdinalKey { VirtualProperty = 100 };
            byte[] bin = MessagePackSerializer.Serialize(obj);
            var obj2 = MessagePackSerializer.Deserialize<DerivedClassThatOverridesPropertyDataMemberOnOverrideOnlyOrdinalKey>(bin);
            Assert.Equal(obj.VirtualProperty, obj2.VirtualProperty);
        }

        private static void Assert3MemberClassSerializedContent(ReadOnlyMemory<byte> msgpack)
        {
            var reader = new MessagePackReader(msgpack);
            Assert.Equal(3, reader.ReadArrayHeader());
            Assert.Equal(1, reader.ReadInt32());
            Assert.Equal(2, reader.ReadInt32());
            Assert.Equal(3, reader.ReadInt32());
        }

        private void PrivateMembersInBaseClass_Helper(MessagePackSerializerOptions options)
        {
            var obj = new DerivedClass { Name = "name", BaseClassFieldAccessor = 15, BaseClassProperty = 5 };
            var bin = MessagePackSerializer.Serialize(obj, options);
            this.logger.WriteLine(MessagePackSerializer.ConvertToJson(bin));
            var obj2 = MessagePackSerializer.Deserialize<DerivedClass>(bin, options);
            Assert.Equal(obj.BaseClassFieldAccessor, obj2.BaseClassFieldAccessor);
            Assert.Equal(obj.BaseClassProperty, obj2.BaseClassProperty);
            Assert.Equal(obj.Name, obj2.Name);
        }

        [DataContract]
        public class BaseClassWithVirtualProperty
        {
            [DataMember]
            public virtual int VirtualProperty { get; set; }
        }

        [DataContract]
        public class DerivedClassThatOverridesProperty : BaseClassWithVirtualProperty
        {
            [DataMember]
            public override int VirtualProperty
            {
                get => base.VirtualProperty;
                set => base.VirtualProperty = value;
            }
        }

        [DataContract]
        public class BaseClassWithVirtualPropertyDataMemberOnOverrideOnly
        {
            public virtual int VirtualProperty { get; set; }
        }

        [DataContract]
        public class DerivedClassThatOverridesPropertyDataMemberOnOverrideOnly : BaseClassWithVirtualPropertyDataMemberOnOverrideOnly
        {
            [DataMember]
            public override int VirtualProperty
            {
                get => base.VirtualProperty;
                set => base.VirtualProperty = value;
            }
        }

        [DataContract]
        public class BaseClassWithVirtualPropertyDataMemberOnVirtualOnly
        {
            [DataMember]
            public virtual int VirtualProperty { get; set; }
        }

        [DataContract]
        public class DerivedClassThatOverridesPropertyDataMemberOnVirtualOnly : BaseClassWithVirtualPropertyDataMemberOnVirtualOnly
        {
            public override int VirtualProperty
            {
                get => base.VirtualProperty;
                set => base.VirtualProperty = value;
            }
        }

        [DataContract]
        public class BaseClassWithVirtualPropertyOrdinalKey
        {
            [DataMember(Order = 0)]
            public virtual int VirtualProperty { get; set; }
        }

        [DataContract]
        public class DerivedClassThatOverridesPropertyOrdinalKey : BaseClassWithVirtualPropertyOrdinalKey
        {
            [DataMember(Order = 0)]
            public override int VirtualProperty
            {
                get => base.VirtualProperty;
                set => base.VirtualProperty = value;
            }
        }

        [DataContract]
        public class BaseClassWithVirtualPropertyDataMemberOnOverrideOnlyOrdinalKey
        {
            public virtual int VirtualProperty { get; set; }
        }

        [DataContract]
        public class DerivedClassThatOverridesPropertyDataMemberOnOverrideOnlyOrdinalKey : BaseClassWithVirtualPropertyDataMemberOnOverrideOnlyOrdinalKey
        {
            [DataMember(Order = 0)]
            public override int VirtualProperty
            {
                get => base.VirtualProperty;
                set => base.VirtualProperty = value;
            }
        }

        [DataContract]
        public class BaseClassWithVirtualPropertyDataMemberOnVirtualOnlyOrdinalKey
        {
            [DataMember(Order = 0)]
            public virtual int VirtualProperty { get; set; }
        }

        [DataContract]
        public class DerivedClassThatOverridesPropertyDataMemberOnVirtualOnlyOrdinalKey : BaseClassWithVirtualPropertyDataMemberOnVirtualOnlyOrdinalKey
        {
            public override int VirtualProperty
            {
                get => base.VirtualProperty;
                set => base.VirtualProperty = value;
            }
        }

        [DataContract]
        public class TwoProperties
        {
            [DataMember]
            public string Prop1 { get; set; } = "Uninitialized";

            [DataMember]
            public string Prop2 { get; set; } = "Uninitialized";
        }

        [DataContract]
        public class TwoPropertiesOrdinalKey
        {
            [DataMember(Order = 0)]
            public string Prop1 { get; set; } = "Uninitialized";

            [DataMember(Order = 1)]
            public string Prop2 { get; set; } = "Uninitialized";
        }

        [MessagePackObject]
        public class TestMessageWithReadOnlyField
        {
            [Key(0)]
            public int Field1 = 1;

            [Key(1)]
            public readonly int Field2 = 2;

            [Key(2)]
            public int Field3 = 3;
        }

        [MessagePackObject]
        public class TestMessageWithReadWriteFields
        {
            [Key(0)]
            public int Property1 = 1;

            [Key(1)]
            public int Property2 = 2;

            [Key(2)]
            public int Property3 = 3;
        }

        [MessagePackObject]
        public class TestMessageWithReadOnlyProperty
        {
            [Key(0)]
            public int Property1 { get; set; } = 1;

            [Key(1)]
            public int Property2 => 2;

            [Key(2)]
            public int Property3 { get; set; } = 3;
        }

        [DataContract]
        public class BaseClass
        {
            [DataMember]
            private int baseClassField;

            public int BaseClassFieldAccessor
            {
                get => this.baseClassField;
                set => this.baseClassField = value;
            }

            [DataMember]
#pragma warning disable SA1300 // Element should begin with upper-case letter
            private int baseClassProperty { get; set; }
#pragma warning restore SA1300 // Element should begin with upper-case letter

            public int BaseClassProperty
            {
                get => this.baseClassProperty;
                set => this.baseClassProperty = value;
            }
        }

        [DataContract]
        public class DerivedClass : BaseClass
        {
            [DataMember]
            public string Name { get; set; }
        }

        [MessagePackObject(true)]
        public class ClassWithPropertySetterAndDummyCtor
        {
            public int MyProperty { get; set; }

            public ClassWithPropertySetterAndDummyCtor(int myProperty)
            {
                // This constructor intentionally left blank.
            }
        }
    }
}

#endif

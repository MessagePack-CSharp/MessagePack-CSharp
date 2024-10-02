// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using ComplexdUnion;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using SharedData;
using Xunit;

#pragma warning disable SA1302 // Interface names should begin with I
#pragma warning disable SA1403 // File may only contain a single namespace

namespace MessagePack.Tests
{
    public class AssemblyLoadContextTests : IDisposable
    {
        private readonly AssemblyLoadContext loadContext;
        private readonly Stream sharedAssemblyStream;

        public AssemblyLoadContextTests()
        {
            this.loadContext = new AssemblyLoadContext("TestContext", isCollectible: true);
            this.sharedAssemblyStream = this.GetSharedDataAssemblyStream();
        }

        public void Dispose()
        {
            this.loadContext.Unload();
            this.sharedAssemblyStream.Dispose();
        }

        [Fact]
        public void DynamicUnionResolverWorksAcrossAssemblyLoadContexts()
        {
            RootUnionType unionTypeInMainLoadContext = new SubUnionType1();
            var options = this.CreateSerializerOptions();

            var buffer1 = MessagePackSerializer.Serialize(unionTypeInMainLoadContext, options: options);
            var o1 = MessagePackSerializer.Deserialize<RootUnionType>(buffer1, options: options);

            Assert.True(o1 is SubUnionType1);

            var assembly = this.loadContext.LoadFromStream(sharedAssemblyStream);
            object unionTypeInOtherContext = assembly.CreateInstance(typeof(SubUnionType1).FullName);
            Type rootUnionType = assembly.GetType(typeof(RootUnionType).FullName);

            var buffer2 = MessagePackSerializer.Serialize(rootUnionType, unionTypeInOtherContext, options: options);
            var o2 = MessagePackSerializer.Deserialize(rootUnionType, buffer2, options: options);

            Assert.True(o2.GetType().IsAssignableTo(rootUnionType));
        }

        [Fact]
        public void DynamicEnumResolverWorksAcrossAssemblyLoadContexts()
        {
            ByteEnum e1 = ByteEnum.A;
            var options = this.CreateSerializerOptions();

            var b1 = MessagePackSerializer.Serialize(e1, options: options);
            var o1 = MessagePackSerializer.Deserialize<ByteEnum>(b1, options: options);

            Assert.Equal(typeof(ByteEnum), o1.GetType());

            var assembly = this.loadContext.LoadFromStream(sharedAssemblyStream);
            Type enumType = assembly.GetType(typeof(ByteEnum).FullName);
            object e2 = Enum.GetValues(enumType).GetValue(1);

            var b2 = MessagePackSerializer.Serialize(enumType, e2, options: options);
            var o2 = MessagePackSerializer.Deserialize(enumType, b2, options: options);

            Assert.Equal(o2.GetType(), e2.GetType());
        }

        [Fact]
        public void DynamicObjectResolverWorksAcrossAssemblyLoadContexts()
        {
            FirstSimpleData e1 = new FirstSimpleData();
            var options = this.CreateSerializerOptions();

            var b1 = MessagePackSerializer.Serialize(e1, options: options);
            var o1 = MessagePackSerializer.Deserialize<FirstSimpleData>(b1, options: options);

            Assert.Equal(typeof(FirstSimpleData), o1.GetType());

            var assembly = this.loadContext.LoadFromStream(sharedAssemblyStream);
            Type objectType = assembly.GetType(typeof(FirstSimpleData).FullName);
            object e2 = assembly.CreateInstance(typeof(FirstSimpleData).FullName);

            var b2 = MessagePackSerializer.Serialize(objectType, e2, options: options);
            var o2 = MessagePackSerializer.Deserialize(objectType, b2, options: options);

            Assert.Equal(o2.GetType(), e2.GetType());
        }

        [Fact]
        public void DynamicContractlessObjectResolverWorksAcrossAssemblyLoadContexts()
        {
            FirstSimpleData e1 = new FirstSimpleData();
            var options = new MessagePackSerializerOptions(
                CompositeResolver.Create(
                    BuiltinResolver.Instance,
                    PrimitiveObjectResolver.Instance,
                    DynamicContractlessObjectResolver.Instance));

            var b1 = MessagePackSerializer.Serialize(e1, options: options);
            var o1 = MessagePackSerializer.Deserialize<FirstSimpleData>(b1, options: options);

            Assert.Equal(typeof(FirstSimpleData), o1.GetType());

            var assembly = this.loadContext.LoadFromStream(sharedAssemblyStream);
            Type objectType = assembly.GetType(typeof(FirstSimpleData).FullName);
            object e2 = assembly.CreateInstance(typeof(FirstSimpleData).FullName);

            var b2 = MessagePackSerializer.Serialize(objectType, e2, options: options);
            var o2 = MessagePackSerializer.Deserialize(objectType, b2, options: options);

            Assert.Equal(o2.GetType(), e2.GetType());
        }

        private Stream GetSharedDataAssemblyStream()
        {
            string assemblyPath = typeof(RootUnionType).Assembly.Location;
            MemoryStream stream = new MemoryStream();
            using (var sourceStream = File.OpenRead(assemblyPath))
            {
                sourceStream.CopyTo(stream);
            }

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private MessagePackSerializerOptions CreateSerializerOptions()
        {
            // Avoid default options as it will use source generated formatter which works in this scenario.
            return new MessagePackSerializerOptions(
                CompositeResolver.Create(
                    BuiltinResolver.Instance,
                    AttributeFormatterResolver.Instance,
                    DynamicEnumResolver.Instance,
                    DynamicGenericResolver.Instance,
                    DynamicUnionResolver.Instance,
                    DynamicObjectResolver.Instance,
                    PrimitiveObjectResolver.Instance));
        }
    }
}

#endif

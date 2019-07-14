// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
    public class PrimitiveObjectFormatterTests
    {
        [Theory]
        [InlineData((sbyte)5)]
        [InlineData((byte)5)]
        [InlineData((short)5)]
        [InlineData((ushort)5)]
        [InlineData(5)]
        [InlineData(5U)]
        [InlineData(5L)]
        [InlineData(5UL)]
        public void CompressibleIntegersRetainTypeInfo<T>(T value)
        {
            var bin = MessagePackSerializer.Serialize<object>(value, PrimitiveObjectResolver.Options);
            T result = Assert.IsType<T>(MessagePackSerializer.Deserialize<object>(bin, PrimitiveObjectResolver.Options));
            Assert.Equal(value, result);
        }

        [Fact]
        public void EnumRetainsUnderlyingType()
        {
            var bin = MessagePackSerializer.Serialize<object>(SomeEnum.SomeValue, PrimitiveObjectResolver.Options);
            var result = (SomeEnum)MessagePackSerializer.Deserialize<object>(bin, PrimitiveObjectResolver.Options);
            Assert.Equal(SomeEnum.SomeValue, result);
        }

        public enum SomeEnum : ushort
        {
            None = 0,
            SomeValue = 1,
        }
    }
}

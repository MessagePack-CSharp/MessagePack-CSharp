// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class FloatConversionTest
    {
        [Theory]
        [InlineData(-10)]
        [InlineData(-120)]
        [InlineData(10)]
        [InlineData(0.000006f)]
        [InlineData(byte.MaxValue)]
        [InlineData(sbyte.MaxValue)]
        [InlineData(short.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData(ushort.MaxValue)]
        [InlineData(uint.MaxValue)]
        [InlineData(ulong.MaxValue)]
        public void FloatTest<T>(T value)
        {
            var bin = MessagePackSerializer.Serialize(value);
            MessagePackSerializer.Deserialize<float>(bin).Is(Convert.ToSingle(value));
        }

        [Theory]
        [InlineData(-10)]
        [InlineData(-120)]
        [InlineData(10)]
        [InlineData(0.000006)]
        [InlineData(byte.MaxValue)]
        [InlineData(sbyte.MaxValue)]
        [InlineData(short.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData(ushort.MaxValue)]
        [InlineData(uint.MaxValue)]
        [InlineData(ulong.MaxValue)]
        public void DoubleTest<T>(T value)
        {
            var bin = MessagePackSerializer.Serialize(value);
            MessagePackSerializer.Deserialize<double>(bin).Is(Convert.ToDouble(value));
        }

        [Fact]
        public void IL2CPPTypeHint()
        {
            FloatTest<float>(default);
            FloatTest<int>(default);
            FloatTest<byte>(default);
            FloatTest<sbyte>(default);
            FloatTest<short>(default);
            FloatTest<int>(default);
            FloatTest<long>(default);
            FloatTest<ushort>(default);
            FloatTest<uint>(default);
            FloatTest<ulong>(default);

            DoubleTest<int>(default);
            DoubleTest<byte>(default);
            DoubleTest<sbyte>(default);
            DoubleTest<short>(default);
            DoubleTest<int>(default);
            DoubleTest<long>(default);
            DoubleTest<ushort>(default);
            DoubleTest<uint>(default);
            DoubleTest<ulong>(default);
            DoubleTest<double>(default);
        }
    }
}

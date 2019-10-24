// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
    public class ContractlessStandardResolverConstructorTests
    {
        public class TestConstructor1
        {
            public int X { get; }

            public int Y { get; }

            public int Z { get; }

            public int CalledConstructorParameterCount { get; }

            public TestConstructor1()
            {
                this.CalledConstructorParameterCount = 0;
            }

            public TestConstructor1(int x)
            {
                this.X = x;
                this.CalledConstructorParameterCount = 1;
            }

            public TestConstructor1(int x, int y)
            {
                this.X = x;
                this.Y = y;
                this.CalledConstructorParameterCount = 2;
            }

            public TestConstructor1(int x, int y, int z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
                this.CalledConstructorParameterCount = 3;
            }
        }

        public class TestConstructor2
        {
            public int X { get; }

            public int Y { get; }

            public int Z { get; }

            public int CalledConstructorParameterCount { get; }

            public int Bad => throw new InvalidOperationException();

            public TestConstructor2()
            {
                this.CalledConstructorParameterCount = 0;
            }

            public TestConstructor2(int x)
            {
                this.X = x;
                this.CalledConstructorParameterCount = 1;
            }

            public TestConstructor2(int x, int y)
            {
                this.X = x;
                this.Y = y;
                this.CalledConstructorParameterCount = 2;
            }

            public TestConstructor2(int x, int y, int z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
                this.CalledConstructorParameterCount = 3;
            }
        }

        [Fact]
        public void UseConstructor()
        {
            var ctor = new TestConstructor1(10, 20, 30);
            var bin = MessagePackSerializer.Serialize(ctor, ContractlessStandardResolver.Options);
            var r = MessagePackSerializer.Deserialize<TestConstructor1>(bin, ContractlessStandardResolver.Options);

            r.CalledConstructorParameterCount.Is(3);
        }

        [Fact]
        public void IgnorePropertiesWithoutConstructorArgument()
        {
            var ctor = new TestConstructor2(10, 20, 30);
            var bin = MessagePackSerializer.Serialize(ctor, ContractlessStandardResolver.Options);
            var r = MessagePackSerializer.Deserialize<TestConstructor2>(bin, ContractlessStandardResolver.Options);

            r.CalledConstructorParameterCount.Is(3);
        }
    }
}

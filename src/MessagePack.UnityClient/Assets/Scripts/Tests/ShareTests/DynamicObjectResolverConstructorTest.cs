// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace MessagePack.Tests
{
    public class DynamicObjectResolverConstructorTest
    {
        [MessagePackObject(true)]
        public class TestConstructor1
        {
            public int X { get; }

            public int Y { get; }

            public int Z { get; }

            [IgnoreMember]
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

        [MessagePackObject]
        public class TestConstructor2
        {
            [Key(0)]
            public int X { get; }

            [Key(1)]
            public int Y { get; }

            [Key(2)]
            public int Z { get; }

            [IgnoreMember]
            public int CalledConstructorParameterCount { get; }

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

        [MessagePackObject]
        public class TestConstructor3
        {
            [Key(0)]
            public int X { get; }

            [Key(1)]
            public int Y { get; }

            [Key(2)]
            public int Z { get; }

            [IgnoreMember]
            public int CalledConstructorParameterCount { get; }

            public TestConstructor3()
            {
                this.CalledConstructorParameterCount = 0;
            }

            public TestConstructor3(int x)
            {
                this.X = x;
                this.CalledConstructorParameterCount = 1;
            }

            [SerializationConstructor]
            public TestConstructor3(int x, int y)
            {
                this.X = x;
                this.Y = y;
                this.CalledConstructorParameterCount = 2;
            }

            public TestConstructor3(int x, int y, int z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
                this.CalledConstructorParameterCount = 3;
            }
        }

        /// <summary>
        /// This is to test the fix for issue #987 where the constructor argument can be satisfied by the
        /// property value as typeof(object) IsAssignableFrom typeof(int) but the value has to be boxed.
        /// </summary>
        [MessagePackObject]
        public class TestConstructor4
        {
            [Key(0)]
            public int X { get; }

            public TestConstructor4(object x)
            {
                this.X = (int)Convert.ChangeType(x, typeof(int));
            }
        }

        /// <summary>
        /// This variation on TestConstructor4 ensures that the int value is not boxed when use to set
        /// the property value after the constructor has been called (which would result in the X value
        /// being set incorrectly).
        /// </summary>
        [MessagePackObject]
        public class TestConstructor5
        {
            [Key(0)]
            public int X { get; set; }

            public TestConstructor5(object x)
            {
                this.X = (int)Convert.ChangeType(x, typeof(int));
            }
        }

        /// <summary>
        /// This variation on TestConstructor4 exists because different code branches are followed when
        /// generated code to instantiate a class as opposed to a value type.
        /// </summary>
        [MessagePackObject]
        public struct TestConstructor6
        {
            [Key(0)]
            public int X { get; }

            public TestConstructor6(object x)
            {
                this.X = (int)Convert.ChangeType(x, typeof(int));
            }
        }

        [Fact]
        public void StringKey()
        {
            var ctor = new TestConstructor1(10, 20, 30);
            var bin = MessagePackSerializer.Serialize(ctor);
            var r = MessagePackSerializer.Deserialize<TestConstructor1>(bin);

            r.CalledConstructorParameterCount.Is(3);
        }

        [Fact]
        public void IntKey()
        {
            var ctor = new TestConstructor2(10, 20, 30);
            var bin = MessagePackSerializer.Serialize(ctor);
            var r = MessagePackSerializer.Deserialize<TestConstructor2>(bin);

            r.CalledConstructorParameterCount.Is(3);
        }

        [Fact]
        public void SerializationCtor()
        {
            var ctor = new TestConstructor3(10, 20, 30);
            var bin = MessagePackSerializer.Serialize(ctor);
            var r = MessagePackSerializer.Deserialize<TestConstructor3>(bin);

            r.CalledConstructorParameterCount.Is(2);
        }

        [Fact]
        public void MatchedClassCtorHasObjectArgProvidedByReadonlyValueTypeProperty()
        {
            var ctor = new TestConstructor4(10);
            var bin = MessagePackSerializer.Serialize(ctor);
            var r = MessagePackSerializer.Deserialize<TestConstructor4>(bin);

            r.X.Is(10);
        }

        [Fact]
        public void MatchedClassCtorHasObjectArgProvidedBySettableValueTypeProperty()
        {
            var ctor = new TestConstructor5(10);
            var bin = MessagePackSerializer.Serialize(ctor);
            var r = MessagePackSerializer.Deserialize<TestConstructor5>(bin);

            r.X.Is(10);
        }

        [Fact]
        public void MatchedStructCtorHasObjectArgProvidedByReadonlyValueTypeProperty()
        {
            var ctor = new TestConstructor6(10);
            var bin = MessagePackSerializer.Serialize(ctor);
            var r = MessagePackSerializer.Deserialize<TestConstructor6>(bin);

            r.X.Is(10);
        }
    }
}

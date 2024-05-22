// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

        /// <summary>
        /// This constructor tests the case where key names differ from property
        /// names, to ensure that the correct ctor is still found using the member name.
        /// (See issue #1016).
        /// </summary>
        [MessagePackObject]
        public struct TestConstructor7
        {
            [Key("x_val")]
            public int X { get; set; }

            [Key("y_val")]
            public int Y { get; set; }

            [Key("z_val")]
            public int Z { get; set; }

            public TestConstructor7(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        /// <summary>
        /// This variation on 7 will check that the original behavior still
        /// works (i.e. when users were forced to ensure that string key names
        /// matched the constructor parameter names).
        /// </summary>
        [MessagePackObject]
        public struct TestConstructor8
        {
            [Key("x_val")]
            public int X { get; set; }

            [Key("y_val")]
            public int Y { get; set; }

            [Key("z_val")]
            public int Z { get; set; }

            public TestConstructor8(int x_val, int y_val, int z_val)
            {
                X = x_val;
                Y = y_val;
                Z = z_val;
            }
        }

        /// <summary>
        /// Check the behavior that happens with a mix of ctor params matching
        /// string keys and member names.
        /// </summary>
        [MessagePackObject]
        public struct TestConstructor9
        {
            [Key("x_val")]
            public int X { get; set; }

            [Key("y")]
            public int Y { get; set; }

            [Key("Z")] // Capital Z
            public int Z { get; set; }

            public TestConstructor9(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        /// <summary>
        /// This constructor tests the case where there are missing int keys, to ensure that
        /// the correct ctor is still found using index numbers of the keys, not their values.
        /// </summary>
        [MessagePackObject]
        public class TestConstructor10
        {
            [Key(0)]
            public int X { get; }

            [Key(2)]
            public int Y { get; }

            [Key(5)]
            public int Z { get; }

            public TestConstructor10(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        [MessagePackObject]
        public class DeviceCommands : IDictionary<int, string>
        {
            [IgnoreMember]
            public static readonly DeviceCommands Empty = new(new Dictionary<int, string>());

            [Key(0)]
            public IDictionary<int, string> Commands { get; }

            [IgnoreMember]
            public ICollection<int> Keys => throw new NotImplementedException();

            [IgnoreMember]
            public ICollection<string> Values => throw new NotImplementedException();

            [IgnoreMember]
            public int Count => 0;

            [IgnoreMember]
            public bool IsReadOnly => throw new NotImplementedException();

            public string this[int key]
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }

            [SerializationConstructor]
            public DeviceCommands(IDictionary<int, string> commands)
            {
                Commands = commands;
            }

            public void Add(int key, string value)
            {
                throw new NotImplementedException();
            }

            public bool ContainsKey(int key)
            {
                throw new NotImplementedException();
            }

            public bool Remove(int key)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(int key, [MaybeNullWhen(false)] out string value)
            {
                throw new NotImplementedException();
            }

            public void Add(KeyValuePair<int, string> item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<int, string> item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(KeyValuePair<int, string>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public bool Remove(KeyValuePair<int, string> item)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<int, string>> GetEnumerator()
            {
                return Enumerable.Empty<KeyValuePair<int, string>>().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void DictionaryWithStaticCtor()
        {
            MessagePackSerializer.Serialize(new DeviceCommands(new Dictionary<int, string>()), MessagePackSerializerOptions.Standard);
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

        [Fact]
        public void MatchedStructCtorWorksEvenWhenKeyNameDifferentThanMember()
        {
            var ctor = new TestConstructor7(1, 2, 3);
            var bin = MessagePackSerializer.Serialize(ctor);
            var r = MessagePackSerializer.Deserialize<TestConstructor7>(bin);

            r.X.Is(1);
            r.Y.Is(2);
            r.Z.Is(3);
        }

        [Fact]
        public void MatchedStructCtorWorksWhenKeyNameSameAsCtorParameter()
        {
            var ctor = new TestConstructor8(4, 5, 6);
            var bin = MessagePackSerializer.Serialize(ctor);
            var r = MessagePackSerializer.Deserialize<TestConstructor8>(bin);

            r.X.Is(4);
            r.Y.Is(5);
            r.Z.Is(6);
        }

        [Fact]
        public void MatchedStructCtorFoundWithMixOfMemberNamesAndStringKeys()
        {
            var ctor = new TestConstructor9(7, 8, 9);
            var bin = MessagePackSerializer.Serialize(ctor);
            var r = MessagePackSerializer.Deserialize<TestConstructor9>(bin);

            r.X.Is(7);
            r.Y.Is(8);
            r.Z.Is(9);
        }

        [Fact]
        public void MatchedClassCtorFoundWithMissingIntKeys()
        {
            var ctor = new TestConstructor10(10, 11, 12);
            var bin = MessagePackSerializer.Serialize(ctor);
            var r = MessagePackSerializer.Deserialize<TestConstructor10>(bin);

            r.X.Is(10);
            r.Y.Is(11);
            r.Z.Is(12);
        }
    }
}

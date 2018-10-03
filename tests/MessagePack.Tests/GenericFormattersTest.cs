﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class GenericFormattersTest
    {
        T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }

        public static object[] tupleTestData = new object[]
        {
            new object[] { Tuple.Create(1) },
            new object[] { Tuple.Create(1, 2) },
            new object[] { Tuple.Create(1, 2, 3) },
            new object[] { Tuple.Create(1, 2, 3, 4) },
            new object[] { Tuple.Create(1, 2, 3, 4, 5) },
            new object[] { Tuple.Create(1, 2, 3, 4, 5, 6) },
            new object[] { Tuple.Create(1, 2, 3, 4, 5, 6, 7) },
            new object[] { Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8) },

            new object[] { Tuple.Create(1, 2, 3, 4, 5, 6, 7,
                            Tuple.Create(8, 9, 10, 11, 12, 13, 14, 15)) },

            new object[] { Tuple.Create(1, 2, 3, 4, 5, 6, 7,
                            Tuple.Create(8, 9, 10, 11, 12, 13, 14,
                              Tuple.Create(15, 16, 17, 18, 19, 20, 21, 22))) },
        };

        [Theory]
        [MemberData(nameof(tupleTestData))]
        public void TupleTest<T>(T data)
            where T : class
        {
            Convert(data).IsStructuralEqual(data);

            // Tuple<...> are nullable value types
            Convert<T>(null).IsNull();
        }

        public static object[] valueTupleTestData = new object[]
        {
            new object[] { ValueTuple.Create(1) },
            new object[] { ValueTuple.Create(1, 2) },
            new object[] { ValueTuple.Create(1, 2, 3) },
            new object[] { ValueTuple.Create(1, 2, 3, 4) },
            new object[] { ValueTuple.Create(1, 2, 3, 4, 5) },
            new object[] { ValueTuple.Create(1, 2, 3, 4, 5, 6) },
            new object[] { ValueTuple.Create(1, 2, 3, 4, 5, 6, 7) },
            new object[] { ValueTuple.Create(1, 2, 3, 4, 5, 6, 7, 8) },

            new object[] { ValueTuple.Create(1, 2, 3, 4, 5, 6, 7,
                            ValueTuple.Create(8, 9, 10, 11, 12, 13, 14, 15)) },

            new object[] { ValueTuple.Create(1, 2, 3, 4, 5, 6, 7,
                            ValueTuple.Create(8, 9, 10, 11, 12, 13, 14,
                              ValueTuple.Create(15, 16, 17, 18, 19, 20, 21, 22))) },
        };

        [Theory]
        [MemberData(nameof(valueTupleTestData))]
        public void ValueTupleTest<T>(T data)
            where T : struct
        {
            Convert(data).IsStructuralEqual(data);

            // ValueTuple<...> are non-nullable value types, we test Nullable<ValueTuple<...>> instead
            var nullable = new T?(data);

            Convert(nullable).IsStructuralEqual(nullable);
            Convert<T?>(null).IsNull();
        }

        public static object[] keyValuePairData = new object[]
        {
            new object[] { new KeyValuePair<int, int>(1,2), null },
            new object[] { new KeyValuePair<int, int>(3,4), new KeyValuePair<int, int>(5,6) },
        };

        [Theory]
        [MemberData(nameof(keyValuePairData))]
        public void KeyValuePairTest<T>(T t, T? t2)
            where T : struct
        {
            Convert(t).IsStructuralEqual(t);
            Convert(t2).IsStructuralEqual(t2);
        }

        public static object[] byteArraySegementData = new object[]
        {
            new object[] { new ArraySegment<byte>(new byte[] { 0, 0, 1, 2, 3 }, 2, 3), null, new byte[] { 1, 2, 3 }  },
            new object[] { new ArraySegment<byte>(new byte[0], 0, 0), null, new byte[0] },
        };

        [Theory]
        [MemberData(nameof(byteArraySegementData))]
        public void ByteArraySegmentTest(ArraySegment<byte> t, ArraySegment<byte>? t2, byte[] reference)
        {
            MessagePackSerializer.Serialize(t).Is(MessagePackSerializer.Serialize(reference));
            Convert(t).Array.Is(reference);
            MessagePackBinary.IsNil(MessagePackSerializer.Serialize(t2), 0).IsTrue();
        }


    }
}

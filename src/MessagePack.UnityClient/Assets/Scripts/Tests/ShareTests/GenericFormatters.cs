// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
#if !(MESSAGEPACK_FORCE_AOT || ENABLE_IL2CPP)

    public class GenericFormatters
    {
        private T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }

        public static object[][] TupleTestData = new object[][]
        {
            new object[] { Tuple.Create(1) },
            new object[] { Tuple.Create(1, 2) },
            new object[] { Tuple.Create(1, 2, 3) },
            new object[] { Tuple.Create(1, 2, 3, 4) },
            new object[] { Tuple.Create(1, 2, 3, 4, 5) },
            new object[] { Tuple.Create(1, 2, 3, 4, 5, 6) },
            new object[] { Tuple.Create(1, 2, 3, 4, 5, 6, 7) },
            new object[] { Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8) },
        };

        [Theory]
        [MemberData(nameof(TupleTestData))]
        public void TupleTest<T>(T data)
        {
            this.Convert(data).IsStructuralEqual(data);
        }

        public static object[][] ValueTupleTestData = new object[][]
        {
            new object[] { ValueTuple.Create(1), null },
            new object[] { ValueTuple.Create(1, 2), null },
            new object[] { ValueTuple.Create(1, 2, 3), null },
            new object[] { ValueTuple.Create(1, 2, 3, 4), null },
            new object[] { ValueTuple.Create(1, 2, 3, 4, 5), null },
            new object[] { ValueTuple.Create(1, 2, 3, 4, 5, 6), null },
            new object[] { ValueTuple.Create(1, 2, 3, 4, 5, 6, 7), null },
            new object[] { ValueTuple.Create(1, 2, 3, 4, 5, 6, 7, 8), null },
        };

        [Theory]
        [MemberData(nameof(ValueTupleTestData))]
        public void TupleTest2<T>(T data, T? @null)
            where T : struct
        {
            this.Convert(data).IsStructuralEqual(data);
            this.Convert(@null).IsNull();
        }

        public static object[][] KeyValuePairData = new object[][]
        {
            new object[] { new KeyValuePair<int, int>(1, 2), null },
            new object[] { new KeyValuePair<int, int>(3, 4), new KeyValuePair<int, int>(5, 6) },
        };

        [Theory]
        [MemberData(nameof(KeyValuePairData))]
        public void KeyValuePairTest<T>(T t, T? t2)
            where T : struct
        {
            this.Convert(t).IsStructuralEqual(t);
            this.Convert(t2).IsStructuralEqual(t2);
        }

        public static object[][] ByteArraySegementData = new object[][]
        {
            new object[] { new ArraySegment<byte>(new byte[] { 0, 0, 1, 2, 3 }, 2, 3), null, new byte[] { 1, 2, 3 } },
            new object[] { new ArraySegment<byte>(Array.Empty<byte>(), 0, 0), null, Array.Empty<byte>() },
        };

        [Theory]
        [MemberData(nameof(ByteArraySegementData))]
        public void ByteArraySegmentTest(ArraySegment<byte> t, ArraySegment<byte>? t2, byte[] reference)
        {
            MessagePackSerializer.Serialize(t).Is(MessagePackSerializer.Serialize(reference));
            new MessagePackReader(MessagePackSerializer.Serialize(t2)).IsNil.IsTrue();
        }
    }

#endif
}

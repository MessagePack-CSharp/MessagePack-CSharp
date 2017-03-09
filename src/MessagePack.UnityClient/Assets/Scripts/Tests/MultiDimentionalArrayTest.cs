using MessagePack.Formatters;
using MessagePack.Resolvers;
using RuntimeUnitTestToolkit;
using SharedData;
using System;

namespace MessagePack.UnityClient.Tests
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }

    public struct ValueTuple<T1, T2, T3>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;

        public ValueTuple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }
    }

    public struct ValueTuple<T1, T2, T3, T4>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
        }
    }

    public class ValueTupleFormatter<T1, T2> : IMessagePackFormatter<ValueTuple<T1, T2>>
    {
        public int Serialize(ref byte[] bytes, int offset, ValueTuple<T1, T2> value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 2);

            offset += formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref bytes, offset, value.Item1, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref bytes, offset, value.Item2, formatterResolver);

            return offset - startOffset;
        }

        public ValueTuple<T1, T2> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var startOffset = offset;
                var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                if (count != 2) throw new InvalidOperationException("Invalid ValueTuple count");
                offset += readSize;

                var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;
                var item2 = formatterResolver.GetFormatterWithVerify<T2>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;

                readSize = offset - startOffset;
                return new ValueTuple<T1, T2>(item1, item2);
            }
        }
    }


    public class ValueTupleFormatter<T1, T2, T3> : IMessagePackFormatter<ValueTuple<T1, T2, T3>>
    {
        public int Serialize(ref byte[] bytes, int offset, ValueTuple<T1, T2, T3> value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 3);

            offset += formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref bytes, offset, value.Item1, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref bytes, offset, value.Item2, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<T3>().Serialize(ref bytes, offset, value.Item3, formatterResolver);

            return offset - startOffset;
        }

        public ValueTuple<T1, T2, T3> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var startOffset = offset;
                var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                if (count != 3) throw new InvalidOperationException("Invalid ValueTuple count");
                offset += readSize;

                var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;
                var item2 = formatterResolver.GetFormatterWithVerify<T2>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;
                var item3 = formatterResolver.GetFormatterWithVerify<T3>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;

                readSize = offset - startOffset;
                return new ValueTuple<T1, T2, T3>(item1, item2, item3);
            }
        }
    }


    public class ValueTupleFormatter<T1, T2, T3, T4> : IMessagePackFormatter<ValueTuple<T1, T2, T3, T4>>
    {
        public int Serialize(ref byte[] bytes, int offset, ValueTuple<T1, T2, T3, T4> value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 4);

            offset += formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref bytes, offset, value.Item1, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref bytes, offset, value.Item2, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<T3>().Serialize(ref bytes, offset, value.Item3, formatterResolver);
            offset += formatterResolver.GetFormatterWithVerify<T4>().Serialize(ref bytes, offset, value.Item4, formatterResolver);

            return offset - startOffset;
        }

        public ValueTuple<T1, T2, T3, T4> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                throw new InvalidOperationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var startOffset = offset;
                var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                if (count != 4) throw new InvalidOperationException("Invalid ValueTuple count");
                offset += readSize;

                var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;
                var item2 = formatterResolver.GetFormatterWithVerify<T2>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;
                var item3 = formatterResolver.GetFormatterWithVerify<T3>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;
                var item4 = formatterResolver.GetFormatterWithVerify<T4>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;

                readSize = offset - startOffset;
                return new ValueTuple<T1, T2, T3, T4>(item1, item2, item3, item4);
            }
        }
    }

    public class IntTupleRegistered : IFormatterResolver
    {
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            if (typeof(T) == typeof(ValueTuple<int, int>))
            {
                return (IMessagePackFormatter<T>)(object)new ValueTupleFormatter<int, int>();
            }
            if (typeof(T) == typeof(ValueTuple<int, int, int>))
            {
                return (IMessagePackFormatter<T>)(object)new ValueTupleFormatter<int, int, int>();
            }
            if (typeof(T) == typeof(ValueTuple<int, int, int, int>))
            {
                return (IMessagePackFormatter<T>)(object)new ValueTupleFormatter<int, int, int, int>();
            }
            return StandardResolver.Instance.GetFormatter<T>();
        }
    }

    public class MultiDimentionalArrayTest
    {
        T Convert<T>(T value)
        {
            var resolver = new IntTupleRegistered();
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, resolver), resolver);
        }

        public void MDArrayTest()
        {
            var dataI = 100;
            var dataJ = 100;
            var dataK = 10;
            var dataL = 5;

            var two = new ValueTuple<int, int>[dataI, dataJ];
            var three = new ValueTuple<int, int, int>[dataI, dataJ, dataK];
            var four = new ValueTuple<int, int, int, int>[dataI, dataJ, dataK, dataL];

            for (int i = 0; i < dataI; i++)
            {
                for (int j = 0; j < dataJ; j++)
                {
                    two[i, j] = new ValueTuple<int, int>(i, j);
                    for (int k = 0; k < dataK; k++)
                    {
                        three[i, j, k] = new ValueTuple<int, int, int>(i, j, k);
                        for (int l = 0; l < dataL; l++)
                        {
                            four[i, j, k, l] = new ValueTuple<int, int, int, int>(i, j, k, l);
                        }
                    }
                }
            }

            var cTwo = Convert(two);
            var cThree = Convert(three);
            var cFour = Convert(four);

            cTwo.Length.Is(two.Length);
            cThree.Length.Is(three.Length);
            cFour.Length.Is(four.Length);

            for (int i = 0; i < dataI; i++)
            {
                for (int j = 0; j < dataJ; j++)
                {
                    cTwo[i, j].Is(two[i, j]);
                    for (int k = 0; k < dataK; k++)
                    {
                        cThree[i, j, k].Is(three[i, j, k]);
                        for (int l = 0; l < dataL; l++)
                        {
                            cFour[i, j, k, l].Is(four[i, j, k, l]);
                        }
                    }
                }
            }
        }
    }
}
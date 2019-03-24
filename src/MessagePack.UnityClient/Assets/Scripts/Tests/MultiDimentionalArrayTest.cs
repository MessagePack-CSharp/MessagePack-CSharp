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
        public void Serialize(ref MessagePackWriter writer, ValueTuple<T1, T2> value, IFormatterResolver formatterResolver)
        {
            writer.WriteArrayHeader(2);

            formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, formatterResolver);
            formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, formatterResolver);

            
        }

        public ValueTuple<T1, T2> Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 2) throw new InvalidOperationException("Invalid ValueTuple count");

                var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, formatterResolver);
                var item2 = formatterResolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, formatterResolver);

                return new ValueTuple<T1, T2>(item1, item2);
            }
        }
    }


    public class ValueTupleFormatter<T1, T2, T3> : IMessagePackFormatter<ValueTuple<T1, T2, T3>>
    {
        public void Serialize(ref MessagePackWriter writer, ValueTuple<T1, T2, T3> value, IFormatterResolver formatterResolver)
        {
            writer.WriteArrayHeader(3);

            formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, formatterResolver);
            formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, formatterResolver);
            formatterResolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, formatterResolver);
        }

        public ValueTuple<T1, T2, T3> Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 3) throw new InvalidOperationException("Invalid ValueTuple count");

                var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, formatterResolver);
                var item2 = formatterResolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, formatterResolver);
                var item3 = formatterResolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, formatterResolver);

                return new ValueTuple<T1, T2, T3>(item1, item2, item3);
            }
        }
    }


    public class ValueTupleFormatter<T1, T2, T3, T4> : IMessagePackFormatter<ValueTuple<T1, T2, T3, T4>>
    {
        public void Serialize(ref MessagePackWriter writer, ValueTuple<T1, T2, T3, T4> value, IFormatterResolver formatterResolver)
        {
            writer.WriteArrayHeader(4);

            formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, formatterResolver);
            formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, formatterResolver);
            formatterResolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, formatterResolver);
            formatterResolver.GetFormatterWithVerify<T4>().Serialize(ref writer, value.Item4, formatterResolver);

            
        }

        public ValueTuple<T1, T2, T3, T4> Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 4) throw new InvalidOperationException("Invalid ValueTuple count");

                var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, formatterResolver);
                var item2 = formatterResolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, formatterResolver);
                var item3 = formatterResolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, formatterResolver);
                var item4 = formatterResolver.GetFormatterWithVerify<T4>().Deserialize(ref reader, formatterResolver);

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
        MessagePackSerializer serializer = new MessagePackSerializer();

        T Convert<T>(T value)
        {
            var resolver = new IntTupleRegistered();
            return serializer.Deserialize<T>(serializer.Serialize(value, resolver), resolver);
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
#if !UNITY

using System;
using System.Buffers;

namespace MessagePack.Formatters
{

    public sealed class TupleFormatter<T1> : IMessagePackFormatter<Tuple<T1>>
    {
        public void Serialize(ref MessagePackWriter writer, Tuple<T1> value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(1);

                resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, resolver);
            }
        }

        public Tuple<T1> Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 1) throw new InvalidOperationException("Invalid Tuple count");

                var item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, resolver);

                return new Tuple<T1>(item1);
            }
        }
    }


    public sealed class TupleFormatter<T1, T2> : IMessagePackFormatter<Tuple<T1, T2>>
    {
        public void Serialize(ref MessagePackWriter writer, Tuple<T1, T2> value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(2);

                resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, resolver);
                resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, resolver);
            }
        }

        public Tuple<T1, T2> Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 2) throw new InvalidOperationException("Invalid Tuple count");

                var item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, resolver);
                var item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, resolver);

                return new Tuple<T1, T2>(item1, item2);
            }
        }
    }


    public sealed class TupleFormatter<T1, T2, T3> : IMessagePackFormatter<Tuple<T1, T2, T3>>
    {
        public void Serialize(ref MessagePackWriter writer, Tuple<T1, T2, T3> value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(3);

                resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, resolver);
                resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, resolver);
                resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, resolver);
            }
        }

        public Tuple<T1, T2, T3> Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 3) throw new InvalidOperationException("Invalid Tuple count");

                var item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, resolver);
                var item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, resolver);
                var item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, resolver);

                return new Tuple<T1, T2, T3>(item1, item2, item3);
            }
        }
    }


    public sealed class TupleFormatter<T1, T2, T3, T4> : IMessagePackFormatter<Tuple<T1, T2, T3, T4>>
    {
        public void Serialize(ref MessagePackWriter writer, Tuple<T1, T2, T3, T4> value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(4);

                resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, resolver);
                resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, resolver);
                resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, resolver);
                resolver.GetFormatterWithVerify<T4>().Serialize(ref writer, value.Item4, resolver);
            }
        }

        public Tuple<T1, T2, T3, T4> Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 4) throw new InvalidOperationException("Invalid Tuple count");

                var item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, resolver);
                var item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, resolver);
                var item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, resolver);
                var item4 = resolver.GetFormatterWithVerify<T4>().Deserialize(ref reader, resolver);

                return new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4);
            }
        }
    }


    public sealed class TupleFormatter<T1, T2, T3, T4, T5> : IMessagePackFormatter<Tuple<T1, T2, T3, T4, T5>>
    {
        public void Serialize(ref MessagePackWriter writer, Tuple<T1, T2, T3, T4, T5> value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(5);

                resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, resolver);
                resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, resolver);
                resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, resolver);
                resolver.GetFormatterWithVerify<T4>().Serialize(ref writer, value.Item4, resolver);
                resolver.GetFormatterWithVerify<T5>().Serialize(ref writer, value.Item5, resolver);
            }
        }

        public Tuple<T1, T2, T3, T4, T5> Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 5) throw new InvalidOperationException("Invalid Tuple count");

                var item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, resolver);
                var item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, resolver);
                var item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, resolver);
                var item4 = resolver.GetFormatterWithVerify<T4>().Deserialize(ref reader, resolver);
                var item5 = resolver.GetFormatterWithVerify<T5>().Deserialize(ref reader, resolver);

                return new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
            }
        }
    }


    public sealed class TupleFormatter<T1, T2, T3, T4, T5, T6> : IMessagePackFormatter<Tuple<T1, T2, T3, T4, T5, T6>>
    {
        public void Serialize(ref MessagePackWriter writer, Tuple<T1, T2, T3, T4, T5, T6> value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(6);

                resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, resolver);
                resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, resolver);
                resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, resolver);
                resolver.GetFormatterWithVerify<T4>().Serialize(ref writer, value.Item4, resolver);
                resolver.GetFormatterWithVerify<T5>().Serialize(ref writer, value.Item5, resolver);
                resolver.GetFormatterWithVerify<T6>().Serialize(ref writer, value.Item6, resolver);
            }
        }

        public Tuple<T1, T2, T3, T4, T5, T6> Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 6) throw new InvalidOperationException("Invalid Tuple count");

                var item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, resolver);
                var item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, resolver);
                var item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, resolver);
                var item4 = resolver.GetFormatterWithVerify<T4>().Deserialize(ref reader, resolver);
                var item5 = resolver.GetFormatterWithVerify<T5>().Deserialize(ref reader, resolver);
                var item6 = resolver.GetFormatterWithVerify<T6>().Deserialize(ref reader, resolver);

                return new Tuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
            }
        }
    }


    public sealed class TupleFormatter<T1, T2, T3, T4, T5, T6, T7> : IMessagePackFormatter<Tuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        public void Serialize(ref MessagePackWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7> value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(7);

                resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, resolver);
                resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, resolver);
                resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, resolver);
                resolver.GetFormatterWithVerify<T4>().Serialize(ref writer, value.Item4, resolver);
                resolver.GetFormatterWithVerify<T5>().Serialize(ref writer, value.Item5, resolver);
                resolver.GetFormatterWithVerify<T6>().Serialize(ref writer, value.Item6, resolver);
                resolver.GetFormatterWithVerify<T7>().Serialize(ref writer, value.Item7, resolver);
            }
        }

        public Tuple<T1, T2, T3, T4, T5, T6, T7> Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 7) throw new InvalidOperationException("Invalid Tuple count");

                var item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, resolver);
                var item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, resolver);
                var item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, resolver);
                var item4 = resolver.GetFormatterWithVerify<T4>().Deserialize(ref reader, resolver);
                var item5 = resolver.GetFormatterWithVerify<T5>().Deserialize(ref reader, resolver);
                var item6 = resolver.GetFormatterWithVerify<T6>().Deserialize(ref reader, resolver);
                var item7 = resolver.GetFormatterWithVerify<T7>().Deserialize(ref reader, resolver);

                return new Tuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
            }
        }
    }


    public sealed class TupleFormatter<T1, T2, T3, T4, T5, T6, T7, TRest> : IMessagePackFormatter<Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
    {
        public void Serialize(ref MessagePackWriter writer, Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(8);

                resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, resolver);
                resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, resolver);
                resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, resolver);
                resolver.GetFormatterWithVerify<T4>().Serialize(ref writer, value.Item4, resolver);
                resolver.GetFormatterWithVerify<T5>().Serialize(ref writer, value.Item5, resolver);
                resolver.GetFormatterWithVerify<T6>().Serialize(ref writer, value.Item6, resolver);
                resolver.GetFormatterWithVerify<T7>().Serialize(ref writer, value.Item7, resolver);
                resolver.GetFormatterWithVerify<TRest>().Serialize(ref writer, value.Rest, resolver);
            }
        }

        public Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 8) throw new InvalidOperationException("Invalid Tuple count");

                var item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, resolver);
                var item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, resolver);
                var item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, resolver);
                var item4 = resolver.GetFormatterWithVerify<T4>().Deserialize(ref reader, resolver);
                var item5 = resolver.GetFormatterWithVerify<T5>().Deserialize(ref reader, resolver);
                var item6 = resolver.GetFormatterWithVerify<T6>().Deserialize(ref reader, resolver);
                var item7 = resolver.GetFormatterWithVerify<T7>().Deserialize(ref reader, resolver);
                var item8 = resolver.GetFormatterWithVerify<TRest>().Deserialize(ref reader, resolver);

                return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, item8);
            }
        }
    }

}

#endif

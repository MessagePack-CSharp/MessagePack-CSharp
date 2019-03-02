#if !UNITY

using System;
using System.Buffers;

namespace MessagePack.Formatters
{

    public sealed class TupleFormatter<T1> : IMessagePackFormatter<Tuple<T1>>
    {
        public int Serialize(ref byte[] bytes, int offset, Tuple<T1> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 1);

                offset += formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref bytes, offset, value.Item1, formatterResolver);

                return offset - startOffset;
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
        public int Serialize(ref byte[] bytes, int offset, Tuple<T1, T2> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 2);

                offset += formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref bytes, offset, value.Item1, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref bytes, offset, value.Item2, formatterResolver);

                return offset - startOffset;
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
        public int Serialize(ref byte[] bytes, int offset, Tuple<T1, T2, T3> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 3);

                offset += formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref bytes, offset, value.Item1, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref bytes, offset, value.Item2, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T3>().Serialize(ref bytes, offset, value.Item3, formatterResolver);

                return offset - startOffset;
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
        public int Serialize(ref byte[] bytes, int offset, Tuple<T1, T2, T3, T4> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 4);

                offset += formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref bytes, offset, value.Item1, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref bytes, offset, value.Item2, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T3>().Serialize(ref bytes, offset, value.Item3, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T4>().Serialize(ref bytes, offset, value.Item4, formatterResolver);

                return offset - startOffset;
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
        public int Serialize(ref byte[] bytes, int offset, Tuple<T1, T2, T3, T4, T5> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 5);

                offset += formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref bytes, offset, value.Item1, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref bytes, offset, value.Item2, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T3>().Serialize(ref bytes, offset, value.Item3, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T4>().Serialize(ref bytes, offset, value.Item4, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T5>().Serialize(ref bytes, offset, value.Item5, formatterResolver);

                return offset - startOffset;
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
        public int Serialize(ref byte[] bytes, int offset, Tuple<T1, T2, T3, T4, T5, T6> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 6);

                offset += formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref bytes, offset, value.Item1, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref bytes, offset, value.Item2, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T3>().Serialize(ref bytes, offset, value.Item3, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T4>().Serialize(ref bytes, offset, value.Item4, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T5>().Serialize(ref bytes, offset, value.Item5, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T6>().Serialize(ref bytes, offset, value.Item6, formatterResolver);

                return offset - startOffset;
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
        public int Serialize(ref byte[] bytes, int offset, Tuple<T1, T2, T3, T4, T5, T6, T7> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 7);

                offset += formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref bytes, offset, value.Item1, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref bytes, offset, value.Item2, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T3>().Serialize(ref bytes, offset, value.Item3, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T4>().Serialize(ref bytes, offset, value.Item4, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T5>().Serialize(ref bytes, offset, value.Item5, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T6>().Serialize(ref bytes, offset, value.Item6, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T7>().Serialize(ref bytes, offset, value.Item7, formatterResolver);

                return offset - startOffset;
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
        public int Serialize(ref byte[] bytes, int offset, Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 8);

                offset += formatterResolver.GetFormatterWithVerify<T1>().Serialize(ref bytes, offset, value.Item1, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T2>().Serialize(ref bytes, offset, value.Item2, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T3>().Serialize(ref bytes, offset, value.Item3, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T4>().Serialize(ref bytes, offset, value.Item4, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T5>().Serialize(ref bytes, offset, value.Item5, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T6>().Serialize(ref bytes, offset, value.Item6, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<T7>().Serialize(ref bytes, offset, value.Item7, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<TRest>().Serialize(ref bytes, offset, value.Rest, formatterResolver);

                return offset - startOffset;
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

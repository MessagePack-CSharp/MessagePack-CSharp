#if NETSTANDARD1_4

using System;

namespace MessagePack.Formatters
{

	public class TupleFormatter<T1> : IMessagePackFormatter<Tuple<T1>>
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

        public Tuple<T1> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
				var startOffset = offset;
				var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
				if (count != 1) throw new InvalidOperationException("Invalid Tuple count");
				offset += readSize;

				var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
			
				readSize = offset - startOffset;
				return new Tuple<T1>(item1);
			}
        }
    }


	public class TupleFormatter<T1, T2> : IMessagePackFormatter<Tuple<T1, T2>>
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

        public Tuple<T1, T2> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
				var startOffset = offset;
				var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
				if (count != 2) throw new InvalidOperationException("Invalid Tuple count");
				offset += readSize;

				var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item2 = formatterResolver.GetFormatterWithVerify<T2>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
			
				readSize = offset - startOffset;
				return new Tuple<T1, T2>(item1, item2);
			}
        }
    }


	public class TupleFormatter<T1, T2, T3> : IMessagePackFormatter<Tuple<T1, T2, T3>>
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

        public Tuple<T1, T2, T3> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
				var startOffset = offset;
				var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
				if (count != 3) throw new InvalidOperationException("Invalid Tuple count");
				offset += readSize;

				var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item2 = formatterResolver.GetFormatterWithVerify<T2>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item3 = formatterResolver.GetFormatterWithVerify<T3>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
			
				readSize = offset - startOffset;
				return new Tuple<T1, T2, T3>(item1, item2, item3);
			}
        }
    }


	public class TupleFormatter<T1, T2, T3, T4> : IMessagePackFormatter<Tuple<T1, T2, T3, T4>>
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

        public Tuple<T1, T2, T3, T4> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
				var startOffset = offset;
				var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
				if (count != 4) throw new InvalidOperationException("Invalid Tuple count");
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
				return new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4);
			}
        }
    }


	public class TupleFormatter<T1, T2, T3, T4, T5> : IMessagePackFormatter<Tuple<T1, T2, T3, T4, T5>>
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

        public Tuple<T1, T2, T3, T4, T5> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
				var startOffset = offset;
				var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
				if (count != 5) throw new InvalidOperationException("Invalid Tuple count");
				offset += readSize;

				var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item2 = formatterResolver.GetFormatterWithVerify<T2>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item3 = formatterResolver.GetFormatterWithVerify<T3>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item4 = formatterResolver.GetFormatterWithVerify<T4>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item5 = formatterResolver.GetFormatterWithVerify<T5>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
			
				readSize = offset - startOffset;
				return new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
			}
        }
    }


	public class TupleFormatter<T1, T2, T3, T4, T5, T6> : IMessagePackFormatter<Tuple<T1, T2, T3, T4, T5, T6>>
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

        public Tuple<T1, T2, T3, T4, T5, T6> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
				var startOffset = offset;
				var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
				if (count != 6) throw new InvalidOperationException("Invalid Tuple count");
				offset += readSize;

				var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item2 = formatterResolver.GetFormatterWithVerify<T2>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item3 = formatterResolver.GetFormatterWithVerify<T3>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item4 = formatterResolver.GetFormatterWithVerify<T4>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item5 = formatterResolver.GetFormatterWithVerify<T5>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item6 = formatterResolver.GetFormatterWithVerify<T6>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
			
				readSize = offset - startOffset;
				return new Tuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
			}
        }
    }


	public class TupleFormatter<T1, T2, T3, T4, T5, T6, T7> : IMessagePackFormatter<Tuple<T1, T2, T3, T4, T5, T6, T7>>
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

        public Tuple<T1, T2, T3, T4, T5, T6, T7> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
				var startOffset = offset;
				var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
				if (count != 7) throw new InvalidOperationException("Invalid Tuple count");
				offset += readSize;

				var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item2 = formatterResolver.GetFormatterWithVerify<T2>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item3 = formatterResolver.GetFormatterWithVerify<T3>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item4 = formatterResolver.GetFormatterWithVerify<T4>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item5 = formatterResolver.GetFormatterWithVerify<T5>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item6 = formatterResolver.GetFormatterWithVerify<T6>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item7 = formatterResolver.GetFormatterWithVerify<T7>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
			
				readSize = offset - startOffset;
				return new Tuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
			}
        }
    }


	public class TupleFormatter<T1, T2, T3, T4, T5, T6, T7, TRest> : IMessagePackFormatter<Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
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

        public Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
				var startOffset = offset;
				var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
				if (count != 8) throw new InvalidOperationException("Invalid Tuple count");
				offset += readSize;

				var item1 = formatterResolver.GetFormatterWithVerify<T1>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item2 = formatterResolver.GetFormatterWithVerify<T2>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item3 = formatterResolver.GetFormatterWithVerify<T3>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item4 = formatterResolver.GetFormatterWithVerify<T4>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item5 = formatterResolver.GetFormatterWithVerify<T5>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item6 = formatterResolver.GetFormatterWithVerify<T6>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item7 = formatterResolver.GetFormatterWithVerify<T7>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
				var item8 = formatterResolver.GetFormatterWithVerify<TRest>().Deserialize(bytes, offset, formatterResolver, out readSize);
				offset += readSize;
			
				readSize = offset - startOffset;
				return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, item8);
			}
        }
    }

}

#endif
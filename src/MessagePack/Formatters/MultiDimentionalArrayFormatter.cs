using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Formatters
{
    // multi dimentional array serialize to [i, j, [seq]]

    public sealed class TwoDimentionalArrayFormatter<T> : IMessagePackFormatter<T[,]>
    {
        const int ArrayLength = 3;

        public int Serialize(ref byte[] bytes, int offset, T[,] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var i = value.GetLength(0);
                var j = value.GetLength(1);

                var startOffset = offset;
                var formatter = formatterResolver.GetFormatterWithVerify<T>();

                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, ArrayLength);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, i);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, j);

                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                foreach (var item in value)
                {
                    offset += formatter.Serialize(ref bytes, offset, item, formatterResolver);
                }

                return offset - startOffset;
            }
        }

        public T[,] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var formatter = resolver.GetFormatterWithVerify<T>();

                var len = reader.ReadArrayHeader();
                if (len != ArrayLength) throw new InvalidOperationException("Invalid T[,] format");

                var iLength = reader.ReadInt32();
                var jLength = reader.ReadInt32();
                var maxLen = reader.ReadArrayHeader();

                var array = new T[iLength, jLength];

                var i = 0;
                var j = -1;
                for (int loop = 0; loop < maxLen; loop++)
                {
                    if (j < jLength - 1)
                    {
                        j++;
                    }
                    else
                    {
                        j = 0;
                        i++;
                    }

                    array[i, j] = formatter.Deserialize(ref reader, resolver);
                }

                return array;
            }
        }
    }

    public sealed class ThreeDimentionalArrayFormatter<T> : IMessagePackFormatter<T[,,]>
    {
        const int ArrayLength = 4;

        public int Serialize(ref byte[] bytes, int offset, T[,,] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var i = value.GetLength(0);
                var j = value.GetLength(1);
                var k = value.GetLength(2);

                var startOffset = offset;
                var formatter = formatterResolver.GetFormatterWithVerify<T>();

                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, ArrayLength);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, i);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, j);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, k);

                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                foreach (var item in value)
                {
                    offset += formatter.Serialize(ref bytes, offset, item, formatterResolver);
                }

                return offset - startOffset;
            }
        }

        public T[,,] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var formatter = resolver.GetFormatterWithVerify<T>();

                var len = reader.ReadArrayHeader();
                if (len != ArrayLength) throw new InvalidOperationException("Invalid T[,,] format");

                var iLength = reader.ReadInt32();
                var jLength = reader.ReadInt32();
                var kLength = reader.ReadInt32();
                var maxLen = reader.ReadArrayHeader();

                var array = new T[iLength, jLength, kLength];

                var i = 0;
                var j = 0;
                var k = -1;
                for (int loop = 0; loop < maxLen; loop++)
                {
                    if (k < kLength - 1)
                    {
                        k++;
                    }
                    else if (j < jLength - 1)
                    {
                        k = 0;
                        j++;
                    }
                    else
                    {
                        k = 0;
                        j = 0;
                        i++;
                    }

                    array[i, j, k] = formatter.Deserialize(ref reader, resolver);
                }

                return array;
            }
        }
    }

    public sealed class FourDimentionalArrayFormatter<T> : IMessagePackFormatter<T[,,,]>
    {
        const int ArrayLength = 5;

        public int Serialize(ref byte[] bytes, int offset, T[,,,] value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var i = value.GetLength(0);
                var j = value.GetLength(1);
                var k = value.GetLength(2);
                var l = value.GetLength(3);

                var startOffset = offset;
                var formatter = formatterResolver.GetFormatterWithVerify<T>();

                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, ArrayLength);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, i);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, j);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, k);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, l);

                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);
                foreach (var item in value)
                {
                    offset += formatter.Serialize(ref bytes, offset, item, formatterResolver);
                }

                return offset - startOffset;
            }
        }

        public T[,,,] Deserialize(ref MessagePackReader reader, IFormatterResolver resolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var formatter = resolver.GetFormatterWithVerify<T>();

                var len = reader.ReadArrayHeader();
                if (len != ArrayLength) throw new InvalidOperationException("Invalid T[,,,] format");
                var iLength = reader.ReadInt32();
                var jLength = reader.ReadInt32();
                var kLength = reader.ReadInt32();
                var lLength = reader.ReadInt32();
                var maxLen = reader.ReadArrayHeader();
                var array = new T[iLength, jLength, kLength, lLength];

                var i = 0;
                var j = 0;
                var k = 0;
                var l = -1;
                for (int loop = 0; loop < maxLen; loop++)
                {
                    if (l < lLength - 1)
                    {
                        l++;
                    }
                    else if (k < kLength - 1)
                    {
                        l = 0;
                        k++;
                    }
                    else if (j < jLength - 1)
                    {
                        l = 0;
                        k = 0;
                        j++;
                    }
                    else
                    {
                        l = 0;
                        k = 0;
                        j = 0;
                        i++;
                    }

                    array[i, j, k, l] = formatter.Deserialize(ref reader, resolver);
                }

                return array;
            }
        }
    }
}

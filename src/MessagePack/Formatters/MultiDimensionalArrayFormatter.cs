using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Formatters
{
    // multi dimensional array serialize to [i, j, [seq]]

    public sealed class TwoDimensionalArrayFormatter<T> : IMessagePackFormatter<T[,]>
    {
        const int ArrayLength = 3;

        public void Serialize(ref MessagePackWriter writer, T[,] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                var i = value.GetLength(0);
                var j = value.GetLength(1);

                var formatter = resolver.GetFormatterWithVerify<T>();

                writer.WriteArrayHeader(ArrayLength);
                writer.Write(i);
                writer.Write(j);

                writer.WriteArrayHeader(value.Length);
                foreach (var item in value)
                {
                    formatter.Serialize(ref writer, item, resolver);
                }
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

    public sealed class ThreeDimensionalArrayFormatter<T> : IMessagePackFormatter<T[,,]>
    {
        const int ArrayLength = 4;

        public void Serialize(ref MessagePackWriter writer, T[,,] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                var i = value.GetLength(0);
                var j = value.GetLength(1);
                var k = value.GetLength(2);

                var formatter = resolver.GetFormatterWithVerify<T>();

                writer.WriteArrayHeader(ArrayLength);
                writer.Write(i);
                writer.Write(j);
                writer.Write(k);

                writer.WriteArrayHeader(value.Length);
                foreach (var item in value)
                {
                    formatter.Serialize(ref writer, item, resolver);
                }
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

    public sealed class FourDimensionalArrayFormatter<T> : IMessagePackFormatter<T[,,,]>
    {
        const int ArrayLength = 5;

        public void Serialize(ref MessagePackWriter writer, T[,,,] value, IFormatterResolver resolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                var i = value.GetLength(0);
                var j = value.GetLength(1);
                var k = value.GetLength(2);
                var l = value.GetLength(3);

                var formatter = resolver.GetFormatterWithVerify<T>();

                writer.WriteArrayHeader(ArrayLength);
                writer.Write(i);
                writer.Write(j);
                writer.Write(k);
                writer.Write(l);

                writer.WriteArrayHeader(value.Length);
                foreach (var item in value)
                {
                    formatter.Serialize(ref writer, item, resolver);
                }
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

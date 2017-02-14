using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Formatters
{
    public class ArrayFormatter<T> : IMessagePackFormatter<T[]>
    {
        public int Serialize(ref byte[] bytes, int offset, T[] value, IFormatterResolver formatterResolver)
        {
            var startOffset = offset;
            var formatter = formatterResolver.GetFormatterWithVerify<T>();

            // TODO:
            // MessagePackBinary.WriteArrayHeader(value.Length);
            foreach (var item in value)
            {
                offset += formatter.Serialize(ref bytes, offset, item, formatterResolver);
            }

            return offset - startOffset;
        }

        public T[] Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var formatter = formatterResolver.GetFormatterWithVerify<T>();

            // TODO:
            // var len = MessagePackBinary.ReadArrayHeader();
            var length = 5;
            var array = new T[length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = formatter.Deserialize(bytes, offset, formatterResolver, out readSize);
            }
            readSize = 0;
            return array;
        }
    }
}

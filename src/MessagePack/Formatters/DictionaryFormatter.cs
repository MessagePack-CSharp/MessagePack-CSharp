using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Formatters
{
    public abstract class DictionaryFormatterBase<TKey, TValue, TIntermediate, TDictionary> : IMessagePackFormatter<TDictionary>
        where TDictionary : IDictionary<TKey, TValue>
    {
        public int Serialize(ref byte[] bytes, int offset, TDictionary value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                var keyFormatter = formatterResolver.GetFormatterWithVerify<TKey>();
                var valueFormatter = formatterResolver.GetFormatterWithVerify<TValue>();

                offset += MessagePackBinary.WriteMapHeader(ref bytes, offset, value.Count);

                var dict = value as Dictionary<TKey, TValue>;
                if (dict != null)
                {
                    foreach (var item in dict) // use Dictionary.Enumerator
                    {
                        offset += keyFormatter.Serialize(ref bytes, offset, item.Key, formatterResolver);
                        offset += valueFormatter.Serialize(ref bytes, offset, item.Value, formatterResolver);
                    }
                }
                else
                {
                    foreach (var item in value)
                    {
                        offset += keyFormatter.Serialize(ref bytes, offset, item.Key, formatterResolver);
                        offset += valueFormatter.Serialize(ref bytes, offset, item.Value, formatterResolver);
                    }
                }

                return offset - startOffset;
            }
        }

        public TDictionary Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return default(TDictionary);
            }
            else
            {
                var startOffset = offset;
                var keyFormatter = formatterResolver.GetFormatterWithVerify<TKey>();
                var valueFormatter = formatterResolver.GetFormatterWithVerify<TValue>();

                var len = MessagePackBinary.ReadMapHeader(bytes, offset, out readSize);
                offset += readSize;

                var dict = Constructor(len);
                for (int i = 0; i < len; i++)
                {
                    var key = keyFormatter.Deserialize(bytes, offset, formatterResolver, out readSize);
                    offset += readSize;

                    var value = valueFormatter.Deserialize(bytes, offset, formatterResolver, out readSize);
                    offset += readSize;

                    Add(dict, i, key, value);
                }
                readSize = offset - startOffset;

                return Finalize(dict);
            }
        }

        // abstraction for deserialize
        protected abstract TIntermediate Constructor(int count);
        protected abstract void Add(TIntermediate collection, int index, TKey key, TValue value);
        protected abstract TDictionary Finalize(TIntermediate intermediateCollection);
    }

    public abstract class DictionaryFormatterBase<TKey, TValue, TDictionary> : DictionaryFormatterBase<TKey, TValue, TDictionary, TDictionary>
        where TDictionary : IDictionary<TKey, TValue>
    {
        protected override TDictionary Finalize(TDictionary intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public class DictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, Dictionary<TKey, TValue>>
    {
        protected override void Add(Dictionary<TKey, TValue> collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override Dictionary<TKey, TValue> Constructor(int count)
        {
            return new Dictionary<TKey, TValue>(count);
        }
    }
}
using MessagePack.Formatters;
using System.Collections.Immutable;
using System;

namespace MessagePack.ImmutableCollections
{
    // Immutablearray<T>.Enumerator is 'not' IEnumerator<T>, can't use abstraction layer.
    public class ImmutableArrayFormatter<T> : IMessagePackFormatter<ImmutableArray<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, ImmutableArray<T> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                var formatter = formatterResolver.GetFormatterWithVerify<T>();

                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Length);

                foreach (var item in value)
                {
                    offset += formatter.Serialize(ref bytes, offset, item, formatterResolver);
                }

                return offset - startOffset;
            }
        }

        public ImmutableArray<T> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return ImmutableArray<T>.Empty;
            }
            else
            {
                var startOffset = offset;
                var formatter = formatterResolver.GetFormatterWithVerify<T>();

                var len = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                offset += readSize;

                var builder = ImmutableArray.CreateBuilder<T>(len);
                for (int i = 0; i < len; i++)
                {
                    builder.Add(formatter.Deserialize(bytes, offset, formatterResolver, out readSize));
                    offset += readSize;
                }
                readSize = offset - startOffset;

                return builder.ToImmutable();
            }
        }
    }

    public class ImmutableListFormatter<T> : SequneceFormatterBase<T, ImmutableList<T>.Builder, ImmutableList<T>.Enumerator, ImmutableList<T>>
    {
        protected override void Add(ImmutableList<T>.Builder collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ImmutableList<T> Complete(ImmutableList<T>.Builder intermediateCollection)
        {
            return intermediateCollection.ToImmutable();
        }

        protected override ImmutableList<T>.Builder Create(int count)
        {
            return ImmutableList.CreateBuilder<T>();
        }

        protected override ImmutableList<T>.Enumerator GetSourceEnumerator(ImmutableList<T> source)
        {
            return source.GetEnumerator();
        }
    }

    // TODO:Impl notes

    // ImmutableDictionary
    // HashSet
    // SortedDictionary
    // SortedSet
    // SortedDictionary
    // Queue
    // Stack

    // interfaces
    // IDict
    // IList
    // ISet
    // IQueue
    // IStack

}
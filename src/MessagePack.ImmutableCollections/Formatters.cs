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

    public class ImmutableDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, ImmutableDictionary<TKey, TValue>.Builder, ImmutableDictionary<TKey, TValue>.Enumerator, ImmutableDictionary<TKey, TValue>>
    {
        protected override void Add(ImmutableDictionary<TKey, TValue>.Builder collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override ImmutableDictionary<TKey, TValue> Complete(ImmutableDictionary<TKey, TValue>.Builder intermediateCollection)
        {
            return intermediateCollection.ToImmutable();
        }

        protected override ImmutableDictionary<TKey, TValue>.Builder Create(int count)
        {
            return ImmutableDictionary.CreateBuilder<TKey, TValue>();
        }

        protected override ImmutableDictionary<TKey, TValue>.Enumerator GetSourceEnumerator(ImmutableDictionary<TKey, TValue> source)
        {
            return source.GetEnumerator();
        }
    }

    public class ImmutableHashSetFormatter<T> : SequneceFormatterBase<T, ImmutableHashSet<T>.Builder, ImmutableHashSet<T>.Enumerator, ImmutableHashSet<T>>
    {
        protected override void Add(ImmutableHashSet<T>.Builder collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ImmutableHashSet<T> Complete(ImmutableHashSet<T>.Builder intermediateCollection)
        {
            return intermediateCollection.ToImmutable();
        }

        protected override ImmutableHashSet<T>.Builder Create(int count)
        {
            return ImmutableHashSet.CreateBuilder<T>();
        }

        protected override ImmutableHashSet<T>.Enumerator GetSourceEnumerator(ImmutableHashSet<T> source)
        {
            return source.GetEnumerator();
        }
    }

    public class ImmutableSortedDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, ImmutableSortedDictionary<TKey, TValue>.Builder, ImmutableSortedDictionary<TKey, TValue>.Enumerator, ImmutableSortedDictionary<TKey, TValue>>
    {
        protected override void Add(ImmutableSortedDictionary<TKey, TValue>.Builder collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override ImmutableSortedDictionary<TKey, TValue> Complete(ImmutableSortedDictionary<TKey, TValue>.Builder intermediateCollection)
        {
            return intermediateCollection.ToImmutable();
        }

        protected override ImmutableSortedDictionary<TKey, TValue>.Builder Create(int count)
        {
            return ImmutableSortedDictionary.CreateBuilder<TKey, TValue>();
        }

        protected override ImmutableSortedDictionary<TKey, TValue>.Enumerator GetSourceEnumerator(ImmutableSortedDictionary<TKey, TValue> source)
        {
            return source.GetEnumerator();
        }
    }

    public class ImmutableSortedSetFormatter<T> : SequneceFormatterBase<T, ImmutableSortedSet<T>.Builder, ImmutableSortedSet<T>.Enumerator, ImmutableSortedSet<T>>
    {
        protected override void Add(ImmutableSortedSet<T>.Builder collection, int index, T value)
        {
            throw new NotImplementedException();
        }

        protected override ImmutableSortedSet<T> Complete(ImmutableSortedSet<T>.Builder intermediateCollection)
        {
            throw new NotImplementedException();
        }

        protected override ImmutableSortedSet<T>.Builder Create(int count)
        {
            throw new NotImplementedException();
        }

        protected override ImmutableSortedSet<T>.Enumerator GetSourceEnumerator(ImmutableSortedSet<T> source)
        {
            throw new NotImplementedException();
        }
    }

    // not best for performance(does not use ImmutableQueue<T>.Enumerator)
    public class ImmutableQueueFormatter<T> : SequneceFormatterBase<T, ImmutableQueueBuilder<T>, ImmutableQueue<T>>
    {
        protected override void Add(ImmutableQueueBuilder<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ImmutableQueue<T> Complete(ImmutableQueueBuilder<T> intermediateCollection)
        {
            return intermediateCollection.q;
        }

        protected override ImmutableQueueBuilder<T> Create(int count)
        {
            return new ImmutableQueueBuilder<T>();
        }
    }

    // not best for performance(does not use ImmutableQueue<T>.Enumerator)
    public class ImmutableStackFormatter<T> : SequneceFormatterBase<T, ImmutableStackBuilder<T>, ImmutableStack<T>>
    {
        protected override void Add(ImmutableStackBuilder<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ImmutableStack<T> Complete(ImmutableStackBuilder<T> intermediateCollection)
        {
            return intermediateCollection.stack;
        }

        protected override ImmutableStackBuilder<T> Create(int count)
        {
            return new ImmutableStackBuilder<T>();
        }
    }

    //public class IImmutableDictionaryFormatter<TKey
    //{

    //}

    // interfaces


    // IDict
    // IList
    // ISet
    // IQueue
    // IStack


    // pseudo builders

    public class ImmutableQueueBuilder<T>
    {
        public ImmutableQueue<T> q = ImmutableQueue<T>.Empty;

        public void Add(T value)
        {
            q = q.Enqueue(value);
        }
    }

    public class ImmutableStackBuilder<T>
    {
        public ImmutableStack<T> stack = ImmutableStack<T>.Empty;

        public void Add(T value)
        {
            stack = stack.Push(value);
        }
    }
}
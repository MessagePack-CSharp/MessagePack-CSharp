using MessagePack.Formatters;
using System.Collections.Immutable;
using System;

namespace MessagePack.ImmutableCollection
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

    public class ImmutableListFormatter<T> : CollectionFormatterBase<T, ImmutableList<T>.Builder, ImmutableList<T>.Enumerator, ImmutableList<T>>
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
            return ImmutableDictionary.CreateBuilder<TKey, TValue>(MessagePackSecurity.Active.GetEqualityComparer<TKey>());
        }

        protected override ImmutableDictionary<TKey, TValue>.Enumerator GetSourceEnumerator(ImmutableDictionary<TKey, TValue> source)
        {
            return source.GetEnumerator();
        }
    }

    public class ImmutableHashSetFormatter<T> : CollectionFormatterBase<T, ImmutableHashSet<T>.Builder, ImmutableHashSet<T>.Enumerator, ImmutableHashSet<T>>
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
            return ImmutableHashSet.CreateBuilder<T>(MessagePackSecurity.Active.GetEqualityComparer<T>());
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

    public class ImmutableSortedSetFormatter<T> : CollectionFormatterBase<T, ImmutableSortedSet<T>.Builder, ImmutableSortedSet<T>.Enumerator, ImmutableSortedSet<T>>
    {
        protected override void Add(ImmutableSortedSet<T>.Builder collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ImmutableSortedSet<T> Complete(ImmutableSortedSet<T>.Builder intermediateCollection)
        {
            return intermediateCollection.ToImmutable();
        }

        protected override ImmutableSortedSet<T>.Builder Create(int count)
        {
            return ImmutableSortedSet.CreateBuilder<T>();
        }

        protected override ImmutableSortedSet<T>.Enumerator GetSourceEnumerator(ImmutableSortedSet<T> source)
        {
            return source.GetEnumerator();
        }
    }

    // not best for performance(does not use ImmutableQueue<T>.Enumerator)
    public class ImmutableQueueFormatter<T> : CollectionFormatterBase<T, ImmutableQueueBuilder<T>, ImmutableQueue<T>>
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
    public class ImmutableStackFormatter<T> : CollectionFormatterBase<T, T[], ImmutableStack<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[collection.Length - 1 - index] = value;
        }

        protected override ImmutableStack<T> Complete(T[] intermediateCollection)
        {
            return ImmutableStack.CreateRange(intermediateCollection);
        }

        protected override T[] Create(int count)
        {
            return new T[count];
        }
    }

    public class InterfaceImmutableListFormatter<T> : CollectionFormatterBase<T, ImmutableList<T>.Builder, IImmutableList<T>>
    {
        protected override void Add(ImmutableList<T>.Builder collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override IImmutableList<T> Complete(ImmutableList<T>.Builder intermediateCollection)
        {
            return intermediateCollection.ToImmutable();
        }

        protected override ImmutableList<T>.Builder Create(int count)
        {
            return ImmutableList.CreateBuilder<T>();
        }
    }

    public class InterfaceImmutableDictionaryFormatter<TKey, TValue> : DictionaryFormatterBase<TKey, TValue, ImmutableDictionary<TKey, TValue>.Builder, IImmutableDictionary<TKey, TValue>>
    {
        protected override void Add(ImmutableDictionary<TKey, TValue>.Builder collection, int index, TKey key, TValue value)
        {
            collection.Add(key, value);
        }

        protected override IImmutableDictionary<TKey, TValue> Complete(ImmutableDictionary<TKey, TValue>.Builder intermediateCollection)
        {
            return intermediateCollection.ToImmutable();
        }

        protected override ImmutableDictionary<TKey, TValue>.Builder Create(int count)
        {
            return ImmutableDictionary.CreateBuilder<TKey, TValue>(MessagePackSecurity.Active.GetEqualityComparer<TKey>());
        }
    }

    public class InterfaceImmutableSetFormatter<T> : CollectionFormatterBase<T, ImmutableHashSet<T>.Builder, IImmutableSet<T>>
    {
        protected override void Add(ImmutableHashSet<T>.Builder collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override IImmutableSet<T> Complete(ImmutableHashSet<T>.Builder intermediateCollection)
        {
            return intermediateCollection.ToImmutable();
        }

        protected override ImmutableHashSet<T>.Builder Create(int count)
        {
            return ImmutableHashSet.CreateBuilder<T>(MessagePackSecurity.Active.GetEqualityComparer<T>());
        }
    }

    public class InterfaceImmutableQueueFormatter<T> : CollectionFormatterBase<T, ImmutableQueueBuilder<T>, IImmutableQueue<T>>
    {
        protected override void Add(ImmutableQueueBuilder<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override IImmutableQueue<T> Complete(ImmutableQueueBuilder<T> intermediateCollection)
        {
            return intermediateCollection.q;
        }

        protected override ImmutableQueueBuilder<T> Create(int count)
        {
            return new ImmutableQueueBuilder<T>();
        }
    }

    public class InterfaceImmutableStackFormatter<T> : CollectionFormatterBase<T, T[], IImmutableStack<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[collection.Length - 1 - index] = value;
        }

        protected override IImmutableStack<T> Complete(T[] intermediateCollection)
        {
            return ImmutableStack.CreateRange(intermediateCollection);
        }

        protected override T[] Create(int count)
        {
            return new T[count];
        }
    }


    // pseudo builders

    public class ImmutableQueueBuilder<T>
    {
        public ImmutableQueue<T> q = ImmutableQueue<T>.Empty;

        public void Add(T value)
        {
            q = q.Enqueue(value);
        }
    }
}
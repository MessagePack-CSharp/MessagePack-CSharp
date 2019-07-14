﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

#if !UNITY
using System.Collections.Concurrent;
#endif

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    public sealed class ArrayFormatter<T> : IMessagePackFormatter<T[]>
    {
        public void Serialize(in MessagePackWriter writer, T[] value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();

                writer.WriteArrayHeader(value.Length);

                for (int i = 0; i < value.Length; i++)
                {
                    formatter.Serialize(writer, value[i], options);
                }
            }
        }

        public T[] Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();

                var len = reader.ReadArrayHeader();
                var array = new T[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = formatter.Deserialize(ref reader, options);
                }

                return array;
            }
        }
    }

    public sealed class ByteArraySegmentFormatter : IMessagePackFormatter<ArraySegment<byte>>
    {
        public static readonly ByteArraySegmentFormatter Instance = new ByteArraySegmentFormatter();

        private ByteArraySegmentFormatter()
        {
        }

        public void Serialize(in MessagePackWriter writer, ArraySegment<byte> value, MessagePackSerializerOptions options)
        {
            if (value.Array == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value);
            }
        }

        public ArraySegment<byte> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return new ArraySegment<byte>(reader.ReadBytes().ToArray());
            }
        }
    }

    public sealed class ArraySegmentFormatter<T> : IMessagePackFormatter<ArraySegment<T>>
    {
        public void Serialize(in MessagePackWriter writer, ArraySegment<T> value, MessagePackSerializerOptions options)
        {
            if (value.Array == null)
            {
                writer.WriteNil();
            }
            else
            {
                IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();

                writer.WriteArrayHeader(value.Count);

                T[] array = value.Array;
                for (int i = 0; i < value.Count; i++)
                {
                    T item = array[value.Offset + i];
                    formatter.Serialize(writer, item, options);
                }
            }
        }

        public ArraySegment<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                T[] array = options.Resolver.GetFormatterWithVerify<T[]>().Deserialize(ref reader, options);
                return new ArraySegment<T>(array);
            }
        }
    }

    // List<T> is popular format, should avoid abstraction.
    public sealed class ListFormatter<T> : IMessagePackFormatter<List<T>>
    {
        public void Serialize(in MessagePackWriter writer, List<T> value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();

                var c = value.Count;
                writer.WriteArrayHeader(c);

                for (int i = 0; i < c; i++)
                {
                    formatter.Serialize(writer, value[i], options);
                }
            }
        }

        public List<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();

                var len = reader.ReadArrayHeader();
                var list = new List<T>((int)len);
                for (int i = 0; i < len; i++)
                {
                    list.Add(formatter.Deserialize(ref reader, options));
                }

                return list;
            }
        }
    }

    public abstract class CollectionFormatterBase<TElement, TIntermediate, TEnumerator, TCollection> : IMessagePackFormatter<TCollection>
        where TCollection : IEnumerable<TElement>
        where TEnumerator : IEnumerator<TElement>
    {
        public void Serialize(in MessagePackWriter writer, TCollection value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                // Optimize iteration(array is fastest)
                var array = value as TElement[];
                if (array != null)
                {
                    IMessagePackFormatter<TElement> formatter = options.Resolver.GetFormatterWithVerify<TElement>();

                    writer.WriteArrayHeader(array.Length);

                    foreach (TElement item in array)
                    {
                        formatter.Serialize(writer, item, options);
                    }

                    return;
                }
                else
                {
                    IMessagePackFormatter<TElement> formatter = options.Resolver.GetFormatterWithVerify<TElement>();

                    // knows count or not.
                    var seqCount = this.GetCount(value);
                    if (seqCount != null)
                    {
                        writer.WriteArrayHeader(seqCount.Value);

                        // Unity's foreach struct enumerator causes boxing so iterate manually.
                        TEnumerator e = this.GetSourceEnumerator(value);
                        try
                        {
                            while (e.MoveNext())
                            {
#if !UNITY
                                formatter.Serialize(writer, e.Current, options);
#else
                                formatter.Serialize(writer, (TElement)e.Current, (IFormatterResolver)resolver);
#endif
                            }
                        }
                        finally
                        {
                            e.Dispose();
                        }

                        return;
                    }
                    else
                    {
                        using (var scratch = new Nerdbank.Streams.Sequence<byte>())
                        {
                            MessagePackWriter scratchWriter = writer.Clone(scratch);
                            var count = 0;
                            TEnumerator e = this.GetSourceEnumerator(value);
                            try
                            {
                                while (e.MoveNext())
                                {
                                    count++;
#if !UNITY
                                    formatter.Serialize(scratchWriter, e.Current, options);
#else
                                    formatter.Serialize(scratchWriter, (TElement)e.Current, (IFormatterResolver)resolver);
#endif
                                }
                            }
                            finally
                            {
                                e.Dispose();
                            }

                            scratchWriter.Flush();
                            writer.WriteArrayHeader(count);
                            writer.WriteRaw(scratch.AsReadOnlySequence);
                        }
                    }
                }
            }
        }

        public TCollection Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default(TCollection);
            }
            else
            {
                IMessagePackFormatter<TElement> formatter = options.Resolver.GetFormatterWithVerify<TElement>();

                var len = reader.ReadArrayHeader();

                TIntermediate list = this.Create(len);
                for (int i = 0; i < len; i++)
                {
                    this.Add(list, i, formatter.Deserialize(ref reader, options));
                }

                return this.Complete(list);
            }
        }

        // abstraction for serialize
        protected virtual int? GetCount(TCollection sequence)
        {
            var collection = sequence as ICollection<TElement>;
            if (collection != null)
            {
                return collection.Count;
            }
#if !UNITY
            else
            {
                var c2 = sequence as IReadOnlyCollection<TElement>;
                if (c2 != null)
                {
                    return c2.Count;
                }
            }
#endif

            return null;
        }

        // Some collections can use struct iterator, this is optimization path
        protected abstract TEnumerator GetSourceEnumerator(TCollection source);

        // abstraction for deserialize
        protected abstract TIntermediate Create(int count);

        protected abstract void Add(TIntermediate collection, int index, TElement value);

        protected abstract TCollection Complete(TIntermediate intermediateCollection);
    }

    public abstract class CollectionFormatterBase<TElement, TIntermediate, TCollection> : CollectionFormatterBase<TElement, TIntermediate, IEnumerator<TElement>, TCollection>
        where TCollection : IEnumerable<TElement>
    {
        protected override IEnumerator<TElement> GetSourceEnumerator(TCollection source)
        {
            return source.GetEnumerator();
        }
    }

    public abstract class CollectionFormatterBase<TElement, TCollection> : CollectionFormatterBase<TElement, TCollection, TCollection>
        where TCollection : IEnumerable<TElement>
    {
        protected sealed override TCollection Complete(TCollection intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public sealed class GenericCollectionFormatter<TElement, TCollection> : CollectionFormatterBase<TElement, TCollection>
         where TCollection : ICollection<TElement>, new()
    {
        protected override TCollection Create(int count)
        {
            return new TCollection();
        }

        protected override void Add(TCollection collection, int index, TElement value)
        {
            collection.Add(value);
        }
    }

    public sealed class LinkedListFormatter<T> : CollectionFormatterBase<T, LinkedList<T>, LinkedList<T>.Enumerator, LinkedList<T>>
    {
        protected override void Add(LinkedList<T> collection, int index, T value)
        {
            collection.AddLast(value);
        }

        protected override LinkedList<T> Complete(LinkedList<T> intermediateCollection)
        {
            return intermediateCollection;
        }

        protected override LinkedList<T> Create(int count)
        {
            return new LinkedList<T>();
        }

        protected override LinkedList<T>.Enumerator GetSourceEnumerator(LinkedList<T> source)
        {
            return source.GetEnumerator();
        }
    }

    public sealed class QeueueFormatter<T> : CollectionFormatterBase<T, Queue<T>, Queue<T>.Enumerator, Queue<T>>
    {
        protected override int? GetCount(Queue<T> sequence)
        {
            return sequence.Count;
        }

        protected override void Add(Queue<T> collection, int index, T value)
        {
            collection.Enqueue(value);
        }

        protected override Queue<T> Create(int count)
        {
            return new Queue<T>(count);
        }

        protected override Queue<T>.Enumerator GetSourceEnumerator(Queue<T> source)
        {
            return source.GetEnumerator();
        }

        protected override Queue<T> Complete(Queue<T> intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    // should deserialize reverse order.
    public sealed class StackFormatter<T> : CollectionFormatterBase<T, T[], Stack<T>.Enumerator, Stack<T>>
    {
        protected override int? GetCount(Stack<T> sequence)
        {
            return sequence.Count;
        }

        protected override void Add(T[] collection, int index, T value)
        {
            // add reverse
            collection[collection.Length - 1 - index] = value;
        }

        protected override T[] Create(int count)
        {
            return new T[count];
        }

        protected override Stack<T>.Enumerator GetSourceEnumerator(Stack<T> source)
        {
            return source.GetEnumerator();
        }

        protected override Stack<T> Complete(T[] intermediateCollection)
        {
            return new Stack<T>(intermediateCollection);
        }
    }

    public sealed class HashSetFormatter<T> : CollectionFormatterBase<T, HashSet<T>, HashSet<T>.Enumerator, HashSet<T>>
    {
        protected override int? GetCount(HashSet<T> sequence)
        {
            return sequence.Count;
        }

        protected override void Add(HashSet<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override HashSet<T> Complete(HashSet<T> intermediateCollection)
        {
            return intermediateCollection;
        }

        protected override HashSet<T> Create(int count)
        {
            return new HashSet<T>();
        }

        protected override HashSet<T>.Enumerator GetSourceEnumerator(HashSet<T> source)
        {
            return source.GetEnumerator();
        }
    }

    public sealed class ReadOnlyCollectionFormatter<T> : CollectionFormatterBase<T, T[], ReadOnlyCollection<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[index] = value;
        }

        protected override ReadOnlyCollection<T> Complete(T[] intermediateCollection)
        {
            return new ReadOnlyCollection<T>(intermediateCollection);
        }

        protected override T[] Create(int count)
        {
            return new T[count];
        }
    }

    public sealed class InterfaceListFormatter<T> : CollectionFormatterBase<T, T[], IList<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[index] = value;
        }

        protected override T[] Create(int count)
        {
            return new T[count];
        }

        protected override IList<T> Complete(T[] intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public sealed class InterfaceCollectionFormatter<T> : CollectionFormatterBase<T, T[], ICollection<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[index] = value;
        }

        protected override T[] Create(int count)
        {
            return new T[count];
        }

        protected override ICollection<T> Complete(T[] intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public sealed class InterfaceEnumerableFormatter<T> : CollectionFormatterBase<T, T[], IEnumerable<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[index] = value;
        }

        protected override T[] Create(int count)
        {
            return new T[count];
        }

        protected override IEnumerable<T> Complete(T[] intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    // [Key, [Array]]
    public sealed class InterfaceGroupingFormatter<TKey, TElement> : IMessagePackFormatter<IGrouping<TKey, TElement>>
    {
        public void Serialize(in MessagePackWriter writer, IGrouping<TKey, TElement> value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(2);
                options.Resolver.GetFormatterWithVerify<TKey>().Serialize(writer, value.Key, options);
                options.Resolver.GetFormatterWithVerify<IEnumerable<TElement>>().Serialize(writer, value, options);
            }
        }

        public IGrouping<TKey, TElement> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var count = reader.ReadArrayHeader();

                if (count != 2)
                {
                    throw new InvalidOperationException("Invalid Grouping format.");
                }

                TKey key = options.Resolver.GetFormatterWithVerify<TKey>().Deserialize(ref reader, options);
                IEnumerable<TElement> value = options.Resolver.GetFormatterWithVerify<IEnumerable<TElement>>().Deserialize(ref reader, options);
                return new Grouping<TKey, TElement>(key, value);
            }
        }
    }

    public sealed class InterfaceLookupFormatter<TKey, TElement> : CollectionFormatterBase<IGrouping<TKey, TElement>, Dictionary<TKey, IGrouping<TKey, TElement>>, ILookup<TKey, TElement>>
    {
        protected override void Add(Dictionary<TKey, IGrouping<TKey, TElement>> collection, int index, IGrouping<TKey, TElement> value)
        {
            collection.Add(value.Key, value);
        }

        protected override ILookup<TKey, TElement> Complete(Dictionary<TKey, IGrouping<TKey, TElement>> intermediateCollection)
        {
            return new Lookup<TKey, TElement>(intermediateCollection);
        }

        protected override Dictionary<TKey, IGrouping<TKey, TElement>> Create(int count)
        {
            return new Dictionary<TKey, IGrouping<TKey, TElement>>(count);
        }
    }

    internal class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        private readonly TKey key;
        private readonly IEnumerable<TElement> elements;

        public Grouping(TKey key, IEnumerable<TElement> elements)
        {
            this.key = key;
            this.elements = elements;
        }

        public TKey Key
        {
            get
            {
                return this.key;
            }
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return this.elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.elements.GetEnumerator();
        }
    }

    internal class Lookup<TKey, TElement> : ILookup<TKey, TElement>
    {
        private readonly Dictionary<TKey, IGrouping<TKey, TElement>> groupings;

        public Lookup(Dictionary<TKey, IGrouping<TKey, TElement>> groupings)
        {
            this.groupings = groupings;
        }

        public IEnumerable<TElement> this[TKey key]
        {
            get
            {
                return this.groupings[key];
            }
        }

        public int Count
        {
            get
            {
                return this.groupings.Count;
            }
        }

        public bool Contains(TKey key)
        {
            return this.groupings.ContainsKey(key);
        }

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            return this.groupings.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.groupings.Values.GetEnumerator();
        }
    }

    /* NonGenerics */

    public sealed class NonGenericListFormatter<T> : IMessagePackFormatter<T>
        where T : class, IList, new()
    {
        public void Serialize(in MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IMessagePackFormatter<object> formatter = options.Resolver.GetFormatterWithVerify<object>();

            writer.WriteArrayHeader(value.Count);
            foreach (var item in value)
            {
                formatter.Serialize(writer, item, options);
            }
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default(T);
            }

            IMessagePackFormatter<object> formatter = options.Resolver.GetFormatterWithVerify<object>();

            var count = reader.ReadArrayHeader();

            var list = new T();
            for (int i = 0; i < count; i++)
            {
                list.Add(formatter.Deserialize(ref reader, options));
            }

            return list;
        }
    }

    public sealed class NonGenericInterfaceListFormatter : IMessagePackFormatter<IList>
    {
        public static readonly IMessagePackFormatter<IList> Instance = new NonGenericInterfaceListFormatter();

        private NonGenericInterfaceListFormatter()
        {
        }

        public void Serialize(in MessagePackWriter writer, IList value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IMessagePackFormatter<object> formatter = options.Resolver.GetFormatterWithVerify<object>();

            writer.WriteArrayHeader(value.Count);
            foreach (var item in value)
            {
                formatter.Serialize(writer, item, options);
            }
        }

        public IList Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default(IList);
            }

            IMessagePackFormatter<object> formatter = options.Resolver.GetFormatterWithVerify<object>();

            var count = reader.ReadArrayHeader();

            var list = new object[count];
            for (int i = 0; i < count; i++)
            {
                list[i] = formatter.Deserialize(ref reader, options);
            }

            return list;
        }
    }

    public sealed class NonGenericDictionaryFormatter<T> : IMessagePackFormatter<T>
        where T : class, IDictionary, new()
    {
        public void Serialize(in MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IMessagePackFormatter<object> formatter = options.Resolver.GetFormatterWithVerify<object>();

            writer.WriteMapHeader(value.Count);
            foreach (DictionaryEntry item in value)
            {
                formatter.Serialize(writer, item.Key, options);
                formatter.Serialize(writer, item.Value, options);
            }
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IMessagePackFormatter<object> formatter = options.Resolver.GetFormatterWithVerify<object>();

            var count = reader.ReadMapHeader();

            var dict = new T();
            for (int i = 0; i < count; i++)
            {
                var key = formatter.Deserialize(ref reader, options);
                var value = formatter.Deserialize(ref reader, options);
                dict.Add(key, value);
            }

            return dict;
        }
    }

    public sealed class NonGenericInterfaceDictionaryFormatter : IMessagePackFormatter<IDictionary>
    {
        public static readonly IMessagePackFormatter<IDictionary> Instance = new NonGenericInterfaceDictionaryFormatter();

        private NonGenericInterfaceDictionaryFormatter()
        {
        }

        public void Serialize(in MessagePackWriter writer, IDictionary value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            IMessagePackFormatter<object> formatter = options.Resolver.GetFormatterWithVerify<object>();

            writer.WriteMapHeader(value.Count);
            foreach (DictionaryEntry item in value)
            {
                formatter.Serialize(writer, item.Key, options);
                formatter.Serialize(writer, item.Value, options);
            }
        }

        public IDictionary Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            IMessagePackFormatter<object> formatter = options.Resolver.GetFormatterWithVerify<object>();

            var count = reader.ReadMapHeader();

            var dict = new Dictionary<object, object>(count);
            for (int i = 0; i < count; i++)
            {
                var key = formatter.Deserialize(ref reader, options);
                var value = formatter.Deserialize(ref reader, options);
                dict.Add(key, value);
            }

            return dict;
        }
    }

#if !UNITY

    public sealed class ObservableCollectionFormatter<T> : CollectionFormatterBase<T, ObservableCollection<T>>
    {
        protected override void Add(ObservableCollection<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ObservableCollection<T> Create(int count)
        {
            return new ObservableCollection<T>();
        }
    }

    public sealed class ReadOnlyObservableCollectionFormatter<T> : CollectionFormatterBase<T, ObservableCollection<T>, ReadOnlyObservableCollection<T>>
    {
        protected override void Add(ObservableCollection<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ObservableCollection<T> Create(int count)
        {
            return new ObservableCollection<T>();
        }

        protected override ReadOnlyObservableCollection<T> Complete(ObservableCollection<T> intermediateCollection)
        {
            return new ReadOnlyObservableCollection<T>(intermediateCollection);
        }
    }

    public sealed class InterfaceReadOnlyListFormatter<T> : CollectionFormatterBase<T, T[], IReadOnlyList<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[index] = value;
        }

        protected override T[] Create(int count)
        {
            return new T[count];
        }

        protected override IReadOnlyList<T> Complete(T[] intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public sealed class InterfaceReadOnlyCollectionFormatter<T> : CollectionFormatterBase<T, T[], IReadOnlyCollection<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[index] = value;
        }

        protected override T[] Create(int count)
        {
            return new T[count];
        }

        protected override IReadOnlyCollection<T> Complete(T[] intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public sealed class InterfaceSetFormatter<T> : CollectionFormatterBase<T, HashSet<T>, ISet<T>>
    {
        protected override void Add(HashSet<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ISet<T> Complete(HashSet<T> intermediateCollection)
        {
            return intermediateCollection;
        }

        protected override HashSet<T> Create(int count)
        {
            return new HashSet<T>();
        }
    }

    public sealed class ConcurrentBagFormatter<T> : CollectionFormatterBase<T, System.Collections.Concurrent.ConcurrentBag<T>>
    {
        protected override int? GetCount(ConcurrentBag<T> sequence)
        {
            return sequence.Count;
        }

        protected override void Add(ConcurrentBag<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ConcurrentBag<T> Create(int count)
        {
            return new ConcurrentBag<T>();
        }
    }

    public sealed class ConcurrentQueueFormatter<T> : CollectionFormatterBase<T, System.Collections.Concurrent.ConcurrentQueue<T>>
    {
        protected override int? GetCount(ConcurrentQueue<T> sequence)
        {
            return sequence.Count;
        }

        protected override void Add(ConcurrentQueue<T> collection, int index, T value)
        {
            collection.Enqueue(value);
        }

        protected override ConcurrentQueue<T> Create(int count)
        {
            return new ConcurrentQueue<T>();
        }
    }

    public sealed class ConcurrentStackFormatter<T> : CollectionFormatterBase<T, T[], ConcurrentStack<T>>
    {
        protected override int? GetCount(ConcurrentStack<T> sequence)
        {
            return sequence.Count;
        }

        protected override void Add(T[] collection, int index, T value)
        {
            // add reverse
            collection[collection.Length - 1 - index] = value;
        }

        protected override T[] Create(int count)
        {
            return new T[count];
        }

        protected override ConcurrentStack<T> Complete(T[] intermediateCollection)
        {
            return new ConcurrentStack<T>(intermediateCollection);
        }
    }

#endif
}

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MessagePack.Formatters
{
    public class ArrayFormatter<T> : IMessagePackFormatter<T[]>
    {
        public int Serialize(ref byte[] bytes, int offset, T[] value, IFormatterResolver formatterResolver)
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

        public T[] Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                var startOffset = offset;
                var formatter = formatterResolver.GetFormatterWithVerify<T>();

                var len = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                offset += readSize;
                var array = new T[len];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = formatter.Deserialize(bytes, offset, formatterResolver, out readSize);
                    offset += readSize;
                }
                readSize = offset - startOffset;
                return array;
            }
        }
    }

    public class ByteArraySegmentFormatter : IMessagePackFormatter<ArraySegment<byte>>
    {
        public int Serialize(ref byte[] bytes, int offset, ArraySegment<byte> value, IFormatterResolver formatterResolver)
        {
            if (value.Array == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                return MessagePackBinary.WriteBytes(ref bytes, offset, value.Array, value.Offset, value.Count);
            }
        }

        public ArraySegment<byte> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return default(ArraySegment<byte>);
            }
            else
            {
                var binary = MessagePackBinary.ReadBytes(bytes, offset, out readSize);
                return new ArraySegment<byte>(binary, 0, binary.Length);
            }
        }
    }

    public class ArraySegmentFormatter<T> : IMessagePackFormatter<ArraySegment<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, ArraySegment<T> value, IFormatterResolver formatterResolver)
        {
            if (value.Array == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                var formatter = formatterResolver.GetFormatterWithVerify<T>();

                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, value.Count);

                var array = value.Array;
                for (int i = 0; i < value.Count; i++)
                {
                    var item = array[value.Offset + i];
                    offset += formatter.Serialize(ref bytes, offset, item, formatterResolver);
                }

                return offset - startOffset;
            }
        }

        public ArraySegment<T> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return default(ArraySegment<T>);
            }
            else
            {
                var array = formatterResolver.GetFormatterWithVerify<T[]>().Deserialize(bytes, offset, formatterResolver, out readSize);
                return new ArraySegment<T>(array, 0, array.Length);
            }
        }
    }

    public abstract class SequneceFormatterBase<TElement, TIntermediate, TSequence> : IMessagePackFormatter<TSequence>
        where TSequence : IEnumerable<TElement>
    {
        public int Serialize(ref byte[] bytes, int offset, TSequence value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                // Optimize iteration(array is fastest)
                var array = value as TElement[];
                if (array != null && typeof(TElement) != typeof(byte)) // ByteArrayFormatter is special, should not use
                {
                    return formatterResolver.GetFormatterWithVerify<TElement[]>().Serialize(ref bytes, offset, array, formatterResolver);
                }
                else
                {
                    var startOffset = offset;
                    var formatter = formatterResolver.GetFormatterWithVerify<TElement>();

                    // knows count or not.
                    var seqCount = GetCount(value);
                    if (seqCount != null)
                    {
                        offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, seqCount.Value);

                        // Unity's foreach List<T>.Enumerator is slow, avoid use iterator.
                        var list = value as List<TElement>;
                        if (list != null)
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                offset += formatter.Serialize(ref bytes, offset, list[i], formatterResolver);
                            }
                        }
                        else
                        {
                            // Some collections can use struct iterator, more chance for optimization?
                            foreach (var item in value)
                            {
                                offset += formatter.Serialize(ref bytes, offset, item, formatterResolver);
                            }
                        }

                        return offset - startOffset;
                    }
                    else
                    {
                        // write message first -> open header space -> write header
                        var writeStarOffset = offset;

                        var count = 0;
                        var moveCount = 0;

                        // count = 16 <= 65535, header len is "3" so choose default space.
                        offset += 3;

                        foreach (var item in value)
                        {
                            count++;
                            var writeSize = formatter.Serialize(ref bytes, offset, item, formatterResolver);
                            moveCount += writeSize;
                            offset += writeSize;
                        }

                        var headerLength = MessagePackBinary.GetArrayHeaderLength(count);
                        if (headerLength != 3)
                        {
                            if (headerLength == 1) offset -= 2; // 1
                            else offset += 2; // 5

                            MessagePackBinary.EnsureCapacity(ref bytes, offset, headerLength);
                            Buffer.BlockCopy(bytes, writeStarOffset + 3, bytes, writeStarOffset + headerLength, moveCount);
                        }
                        MessagePackBinary.WriteArrayHeader(ref bytes, writeStarOffset, count);

                        return offset - startOffset;
                    }
                }
            }
        }

        public TSequence Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return default(TSequence);
            }
            else
            {
                var startOffset = offset;
                var formatter = formatterResolver.GetFormatterWithVerify<TElement>();

                var len = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                offset += readSize;

                var list = Constructor(len);
                for (int i = 0; i < len; i++)
                {
                    Add(list, i, formatter.Deserialize(bytes, offset, formatterResolver, out readSize));
                    offset += readSize;
                }
                readSize = offset - startOffset;

                return Complete(list);
            }
        }

        // abstraction for serialize
        protected virtual int? GetCount(TSequence sequence)
        {
            var collection = sequence as ICollection<TElement>;
            if (collection != null)
            {
                return collection.Count;
            }
            else
            {
                var c2 = sequence as IReadOnlyCollection<TElement>;
                if (c2 != null)
                {
                    return c2.Count;
                }
                return null;
            }
        }

        // abstraction for deserialize
        protected abstract TIntermediate Constructor(int count);
        protected abstract void Add(TIntermediate collection, int index, TElement value);
        protected abstract TSequence Complete(TIntermediate intermediateCollection);
    }

    public abstract class SequneceFormatterBase<TElement, TSequence> : SequneceFormatterBase<TElement, TSequence, TSequence>
        where TSequence : IEnumerable<TElement>
    {
        protected sealed override TSequence Complete(TSequence intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public class GenericCollectionFormatter<TElement, TCollection> : SequneceFormatterBase<TElement, TCollection>
         where TCollection : ICollection<TElement>, new()
    {
        protected override TCollection Constructor(int count)
        {
            return new TCollection();
        }

        protected override void Add(TCollection collection, int index, TElement value)
        {
            collection.Add(value);
        }
    }

    public class ListFormatter<T> : SequneceFormatterBase<T, List<T>>
    {
        protected override List<T> Constructor(int count)
        {
            return new List<T>(count);
        }

        protected override void Add(List<T> collection, int index, T value)
        {
            collection.Add(value);
        }
    }

    public class LinkedListFormatter<T> : SequneceFormatterBase<T, LinkedList<T>>
    {
        protected override LinkedList<T> Constructor(int count)
        {
            return new LinkedList<T>();
        }

        protected override void Add(LinkedList<T> collection, int index, T value)
        {
            collection.AddLast(value);
        }
    }

    public class QeueueFormatter<T> : SequneceFormatterBase<T, Queue<T>>
    {
        protected override int? GetCount(Queue<T> sequence)
        {
            return sequence.Count;
        }

        protected override void Add(Queue<T> collection, int index, T value)
        {
            collection.Enqueue(value);
        }

        protected override Queue<T> Constructor(int count)
        {
            return new Queue<T>(count);
        }
    }

    public class StackFormatter<T> : SequneceFormatterBase<T, Stack<T>>
    {
        protected override int? GetCount(Stack<T> sequence)
        {
            return sequence.Count;
        }

        protected override void Add(Stack<T> collection, int index, T value)
        {
            collection.Push(value);
        }

        protected override Stack<T> Constructor(int count)
        {
            return new Stack<T>(count);
        }
    }

    public class HashSetFormatter<T> : SequneceFormatterBase<T, HashSet<T>>
    {
        protected override void Add(HashSet<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override HashSet<T> Constructor(int count)
        {
            return new HashSet<T>();
        }
    }

    public class ReadOnlyCollectionFormatter<T> : SequneceFormatterBase<T, T[], ReadOnlyCollection<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[index] = value;
        }

        protected override ReadOnlyCollection<T> Complete(T[] intermediateCollection)
        {
            return new ReadOnlyCollection<T>(intermediateCollection);
        }

        protected override T[] Constructor(int count)
        {
            return new T[count];
        }
    }

    public class ObservableCollectionFormatter<T> : SequneceFormatterBase<T, ObservableCollection<T>>
    {
        protected override void Add(ObservableCollection<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ObservableCollection<T> Constructor(int count)
        {
            return new ObservableCollection<T>();
        }
    }

    public class ReadOnlyObservableCollectionFormatter<T> : SequneceFormatterBase<T, ObservableCollection<T>, ReadOnlyObservableCollection<T>>
    {
        protected override void Add(ObservableCollection<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ObservableCollection<T> Constructor(int count)
        {
            return new ObservableCollection<T>();
        }

        protected override ReadOnlyObservableCollection<T> Complete(ObservableCollection<T> intermediateCollection)
        {
            return new ReadOnlyObservableCollection<T>(intermediateCollection);
        }
    }

    public class InterfaceListFormatter<T> : SequneceFormatterBase<T, T[], IList<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[index] = value;
        }

        protected override T[] Constructor(int count)
        {
            return new T[count];
        }

        protected override IList<T> Complete(T[] intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public class InterfaceCollectionFormatter<T> : SequneceFormatterBase<T, T[], ICollection<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[index] = value;
        }

        protected override T[] Constructor(int count)
        {
            return new T[count];
        }

        protected override ICollection<T> Complete(T[] intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public class InterfaceEnumerableFormatter<T> : SequneceFormatterBase<T, T[], IEnumerable<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[index] = value;
        }

        protected override T[] Constructor(int count)
        {
            return new T[count];
        }

        protected override IEnumerable<T> Complete(T[] intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public class InterfaceReadOnlyListFormatter<T> : SequneceFormatterBase<T, T[], IReadOnlyList<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[index] = value;
        }

        protected override T[] Constructor(int count)
        {
            return new T[count];
        }

        protected override IReadOnlyList<T> Complete(T[] intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public class InterfaceReadOnlyCollectionFormatter<T> : SequneceFormatterBase<T, T[], IReadOnlyCollection<T>>
    {
        protected override void Add(T[] collection, int index, T value)
        {
            collection[index] = value;
        }

        protected override T[] Constructor(int count)
        {
            return new T[count];
        }

        protected override IReadOnlyCollection<T> Complete(T[] intermediateCollection)
        {
            return intermediateCollection;
        }
    }

    public class InterfaceSetFormatter<T> : SequneceFormatterBase<T, HashSet<T>, ISet<T>>
    {
        protected override void Add(HashSet<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ISet<T> Complete(HashSet<T> intermediateCollection)
        {
            return intermediateCollection;
        }

        protected override HashSet<T> Constructor(int count)
        {
            return new HashSet<T>();
        }
    }

    public class ConcurrentBagFormatter<T> : SequneceFormatterBase<T, System.Collections.Concurrent.ConcurrentBag<T>>
    {
        protected override void Add(ConcurrentBag<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ConcurrentBag<T> Constructor(int count)
        {
            return new ConcurrentBag<T>();
        }
    }

    public class ConcurrentQueueFormatter<T> : SequneceFormatterBase<T, System.Collections.Concurrent.ConcurrentQueue<T>>
    {
        protected override void Add(ConcurrentQueue<T> collection, int index, T value)
        {
            collection.Enqueue(value);
        }

        protected override ConcurrentQueue<T> Constructor(int count)
        {
            return new ConcurrentQueue<T>();
        }
    }

    public class ConcurrentStackFormatter<T> : SequneceFormatterBase<T, System.Collections.Concurrent.ConcurrentStack<T>>
    {
        protected override void Add(ConcurrentStack<T> collection, int index, T value)
        {
            collection.Push(value);
        }

        protected override ConcurrentStack<T> Constructor(int count)
        {
            return new ConcurrentStack<T>();
        }
    }

    // [Key, [Array]]
    public class InterfaceGroupingFormatter<TKey, TElement> : IMessagePackFormatter<IGrouping<TKey, TElement>>
    {
        public int Serialize(ref byte[] bytes, int offset, IGrouping<TKey, TElement> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, 2);
                offset += formatterResolver.GetFormatterWithVerify<TKey>().Serialize(ref bytes, offset, value.Key, formatterResolver);
                offset += formatterResolver.GetFormatterWithVerify<IEnumerable<TElement>>().Serialize(ref bytes, offset, value, formatterResolver);
                return offset - startOffset;
            }
        }

        public IGrouping<TKey, TElement> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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
                offset += readSize;

                if (count != 2) throw new InvalidOperationException("Invalid Grouping format.");

                var key = formatterResolver.GetFormatterWithVerify<TKey>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;

                var value = formatterResolver.GetFormatterWithVerify<IEnumerable<TElement>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;

                readSize = offset - startOffset;
                return new Grouping<TKey, TElement>(key, value);
            }
        }
    }

    public class InterfaceLookupFormatter<TKey, TElement> : SequneceFormatterBase<IGrouping<TKey, TElement>, Dictionary<TKey, IGrouping<TKey, TElement>>, ILookup<TKey, TElement>>
    {
        protected override void Add(Dictionary<TKey, IGrouping<TKey, TElement>> collection, int index, IGrouping<TKey, TElement> value)
        {
            collection.Add(value.Key, value);
        }

        protected override ILookup<TKey, TElement> Complete(Dictionary<TKey, IGrouping<TKey, TElement>> intermediateCollection)
        {
            return new Lookup<TKey, TElement>(intermediateCollection);
        }

        protected override Dictionary<TKey, IGrouping<TKey, TElement>> Constructor(int count)
        {
            return new Dictionary<TKey, IGrouping<TKey, TElement>>(count);
        }
    }

    class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        readonly TKey key;
        readonly IEnumerable<TElement> elements;

        public Grouping(TKey key, IEnumerable<TElement> elements)
        {
            this.key = key;
            this.elements = elements;
        }

        public TKey Key
        {
            get
            {
                return key;
            }
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return elements.GetEnumerator();
        }
    }

    class Lookup<TKey, TElement> : ILookup<TKey, TElement>
    {
        readonly Dictionary<TKey, IGrouping<TKey, TElement>> groupings;

        public Lookup(Dictionary<TKey, IGrouping<TKey, TElement>> groupings)
        {
            this.groupings = groupings;
        }

        public IEnumerable<TElement> this[TKey key]
        {
            get
            {
                return groupings[key];
            }
        }

        public int Count
        {
            get
            {
                return groupings.Count;
            }
        }

        public bool Contains(TKey key)
        {
            return groupings.ContainsKey(key);
        }

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            return groupings.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return groupings.Values.GetEnumerator();
        }
    }
}

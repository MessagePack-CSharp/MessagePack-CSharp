using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections;

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
                // Optimize iteration(array is fastest, Unity's forach List<T>.Enumerator is slow)
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
                            moveCount += formatter.Serialize(ref bytes, offset, item, formatterResolver);
                            offset += moveCount;
                        }

                        var headerLength = MessagePackBinary.GetArrayHeaderLength(count);
                        if (headerLength != 3)
                        {
                            MessagePackBinary.EnsureCapacity(ref bytes, offset, headerLength);
                            Buffer.BlockCopy(bytes, writeStarOffset + 3, bytes, writeStarOffset + (headerLength - 1), moveCount);
                        }
                        MessagePackBinary.WriteArrayHeader(ref bytes, writeStarOffset, headerLength);

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

    // TODO:more interface collections....
}

#if NETSTANDARD1_4

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Internal
{
    // Key = long, Value = int
    internal unsafe class AutomataNode : IComparable<AutomataNode>
    {
        public readonly long Key;
        public int Value; // can only set from private

        AutomataNode[] nexts; // immutable array

        public AutomataNode(long key)
        {
            this.Key = key;
            this.Value = -1;
            this.nexts = new AutomataNode[0];
        }

        public AutomataNode Add(long key)
        {
            var index = Array.BinarySearch(nexts, new AutomataNode(key)); // TODO:dummy should be removed
            if (index < 0)
            {
                Array.Resize<AutomataNode>(ref nexts, nexts.Length + 1);
                var nextNode = new AutomataNode(key);
                nexts[nexts.Length - 1] = nextNode;
                Array.Sort(nexts);
                return nextNode;
            }
            else
            {
                return nexts[index];
            }
        }

        public AutomataNode Add(long key, int value)
        {
            var v = Add(key);
            v.Value = value;
            return v;
        }

        static long Fetch64(byte* p)
        {
            return *(long*)p;
        }

        static int Fetch32(byte* p)
        {
            return *(int*)p;
        }

        static short Fetch16(byte* p)
        {
            return *(short*)p;
        }

        public AutomataNode SearchNext(ref byte* p, ref int rest)
        {
            long v;
            if (rest >= 8)
            {
                v = Fetch64(p);
                p += 8;
                rest -= 8;
            }
            else if (rest >= 4)
            {
                v = Fetch32(p);
                p += 4;
                rest -= 4;
            }
            else if (rest >= 2)
            {
                v = Fetch16(p);
                p += 2;
                rest -= 2;
            }
            else
            {
                v = *(byte*)p;
                p += 1;
                rest -= 1;
            }

            //if (nexts.Length < 7)
            //{
            // linear search
            for (int i = 0; i < nexts.Length; i++)
            {
                if (nexts[i].Key == v)
                {
                    return nexts[i];
                }
            }
            //}
            //else
            //{
            //    // binary search
            //    var index = Array.BinarySearch(nexts, v); // TODO:can not do binary search
            //    if (index < 0)
            //    {
            //        return nexts[index];
            //    }
            //}

            return null;
        }

        public int GetMaxDepth()
        {
            return GetDepth(nexts, 0);
        }

        int GetDepth(AutomataNode[] nodes, int currentDepth)
        {
            var maxDepth = currentDepth;
            for (int i = 0; i < nodes.Length; i++)
            {
                var depth = GetDepth(nodes[i].nexts, currentDepth + 1);
                maxDepth = Math.Max(maxDepth, depth);
            }

            return maxDepth;
        }

        public int CompareTo(AutomataNode other)
        {
            return this.Key.CompareTo(other.Key);
        }
    }

    public class ByteArrayAutomataDictionary : IEnumerable<int>
    {
        readonly AutomataNode root;

        public ByteArrayAutomataDictionary()
        {
            root = new AutomataNode(-1);
        }

        public void Add(string str, int value)
        {
            Add(Encoding.UTF8.GetBytes(str), value);
        }

        public void Add(byte[] bytes, int value)
        {
            var node = root;

            var i = 0;
            while (i != bytes.Length)
            {
                var rest = bytes.Length - i;
                if (rest >= 8)
                {
                    var l = BitConverter.ToInt64(bytes, i);
                    i += 8;
                    if (i == bytes.Length)
                    {
                        node = node.Add(l, value);
                    }
                    else
                    {
                        node = node.Add(l);
                    }
                    continue;
                }
                else if (rest >= 4)
                {
                    var l = (long)BitConverter.ToInt32(bytes, i);
                    i += 4;
                    if (i == bytes.Length)
                    {
                        node = node.Add(l, value);
                    }
                    else
                    {
                        node = node.Add(l);
                    }
                    continue;
                }
                else if (rest >= 2)
                {
                    var l = (long)BitConverter.ToInt16(bytes, i);
                    i += 2;
                    if (i == bytes.Length)
                    {
                        node = node.Add(l, value);
                    }
                    else
                    {
                        node = node.Add(l);
                    }
                    continue;
                }
                else
                {
                    var l = (long)bytes[i];
                    i += 1;
                    if (i == bytes.Length)
                    {
                        node = node.Add(l, value);
                    }
                    else
                    {
                        node = node.Add(l);
                    }
                    continue;
                }
            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public unsafe bool TryGetValue(byte[] bytes, int offset, int count, out int value)
        {
            fixed (byte* p = bytes)
            {
                var p1 = p;
                var node = root;
                var rest = count;

                while (rest != 0 && node != null)
                {
                    node = node.SearchNext(ref p1, ref rest);
                }

                if (node == null)
                {
                    value = -1;
                    return false;
                }
                else
                {
                    value = node.Value;
                    return true;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

}

#endif
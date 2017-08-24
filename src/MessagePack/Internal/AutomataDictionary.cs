#if NETSTANDARD1_4

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace MessagePack.Internal
{
    // Key = long, Value = int
    internal unsafe class AutomataNode : IComparable<AutomataNode>
    {
        public readonly long Key;
        public int Value; // can only set from private
        internal string originalKey; // for debugging

        internal AutomataNode[] nexts; // immutable array
        internal long[] nextKeys;      // for index search

        public AutomataNode(long key)
        {
            this.Key = key;
            this.Value = -1;
            this.nexts = new AutomataNode[0];
            this.nextKeys = new long[0];
            this.originalKey = null;
        }

        public AutomataNode Add(long key)
        {
            var index = Array.BinarySearch(nextKeys, key);
            if (index < 0)
            {
                Array.Resize<AutomataNode>(ref nexts, nexts.Length + 1);
                Array.Resize<long>(ref nextKeys, nextKeys.Length + 1);
                var nextNode = new AutomataNode(key);
                nexts[nexts.Length - 1] = nextNode;
                nextKeys[nextKeys.Length - 1] = key;
                Array.Sort(nexts);
                Array.Sort(nextKeys);
                return nextNode;
            }
            else
            {
                return nexts[index];
            }
        }

        public AutomataNode Add(long key, int value, string originalKey)
        {
            var v = Add(key);
            v.Value = value;
            v.originalKey = originalKey;
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
            long key;
            if (rest >= 8)
            {
                key = Fetch64(p);
                p += 8;
                rest -= 8;
            }
            else if (rest >= 4)
            {
                key = Fetch32(p);
                p += 4;
                rest -= 4;
            }
            else if (rest >= 2)
            {
                key = Fetch16(p);
                p += 2;
                rest -= 2;
            }
            else
            {
                key = *(byte*)p;
                p += 1;
                rest -= 1;
            }

            if (nextKeys.Length < 7)
            {
                // linear search
                for (int i = 0; i < nextKeys.Length; i++)
                {
                    if (nextKeys[i] == key)
                    {
                        return nexts[i];
                    }
                }
            }
            else
            {
                // binary search
                var index = Array.BinarySearch(nextKeys, key);
                if (index < 0)
                {
                    return nexts[index];
                }
            }

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

        // SearchNext(ref byte* p, ref int rest, ref long key)
        public void EmitSearchNext(ILGenerator il, LocalBuilder p, LocalBuilder rest, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action notFound)
        {
            il.EmitLdloca(p);
            il.EmitLdloca(rest);
            il.EmitCall(AutomataEmitHelper.GetKeyMethod);
            il.EmitStloc(key);

            // TODO:Emit Binary Search
            var loopEnd = il.DefineLabel();
            var nextIf = Enumerable.Range(1, Math.Max(nexts.Length - 2, 0)).Select(_ => il.DefineLabel()).ToArray();
            for (int i = 0; i < nexts.Length; i++)
            {
                if (i != 0)
                {
                    il.MarkLabel(nextIf[i]);
                }

                il.EmitLdloc(key);
                il.Emit(OpCodes.Ldc_I8, nexts[i].Key);
                if (i != nexts.Length - 1)
                {
                    il.Emit(OpCodes.Bne_Un, nextIf[i + 1]);
                }
                else
                {
                    il.Emit(OpCodes.Bne_Un, loopEnd);
                }

                if (nexts[i].Value != -1)
                {
                    onFound(new KeyValuePair<string, int>(nexts[i].originalKey, nexts[i].Value));
                }
                il.Emit(OpCodes.Br, loopEnd);
            }
            // TODO:Emit NotFound

            il.MarkLabel(loopEnd);
        }
    }

    public static class AutomataEmitHelper
    {
        public static readonly MethodInfo GetKeyMethod = typeof(AutomataEmitHelper).GetRuntimeMethod("GetKey", new[] { typeof(byte*).MakeByRefType(), typeof(int).MakeByRefType() });

        public static unsafe long GetKey(ref byte* p, ref int rest)
        {
            long key;
            if (rest >= 8)
            {
                key = *(long*)p;
                p += 8;
                rest -= 8;
            }
            else if (rest >= 4)
            {
                key = *(int*)p;
                p += 4;
                rest -= 4;
            }
            else if (rest >= 2)
            {
                key = *(short*)p;
                p += 2;
                rest -= 2;
            }
            else
            {
                key = *(byte*)p;
                p += 1;
                rest -= 1;
            }
            return key;
        }
    }

    internal partial class AutomataDictionary : IEnumerable<KeyValuePair<string, int>>
    {
        readonly AutomataNode root;

        public AutomataDictionary()
        {
            root = new AutomataNode(-1);
        }

        static unsafe long Fetch64(byte[] xs, int offset)
        {
            fixed (byte* p = &xs[offset])
            {
                return *(long*)p;
            }
        }

        static unsafe int Fetch32(byte[] xs, int offset)
        {
            fixed (byte* p = &xs[offset])
            {
                return *(int*)p;
            }
        }

        static unsafe short Fetch16(byte[] xs, int offset)
        {
            fixed (byte* p = &xs[offset])
            {
                return *(short*)p;
            }
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
                    var l = Fetch64(bytes, i);
                    i += 8;
                    if (i == bytes.Length)
                    {
                        node = node.Add(l, value, Encoding.UTF8.GetString(bytes));
                    }
                    else
                    {
                        node = node.Add(l);
                    }
                    continue;
                }
                else if (rest >= 4)
                {
                    var l = (long)Fetch32(bytes, i);
                    i += 4;
                    if (i == bytes.Length)
                    {
                        node = node.Add(l, value, Encoding.UTF8.GetString(bytes));
                    }
                    else
                    {
                        node = node.Add(l);
                    }
                    continue;
                }
                else if (rest >= 2)
                {
                    var l = (long)Fetch16(bytes, i);
                    i += 2;
                    if (i == bytes.Length)
                    {
                        node = node.Add(l, value, Encoding.UTF8.GetString(bytes));
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
                        node = node.Add(l, value, Encoding.UTF8.GetString(bytes));
                    }
                    else
                    {
                        node = node.Add(l);
                    }
                    continue;
                }
            }
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

        // for debugging
        public override string ToString()
        {
            var sb = new StringBuilder();
            ToStringCore(root.nexts, sb, 0);
            return sb.ToString();
        }

        static void ToStringCore(AutomataNode[] nexts, StringBuilder sb, int depth)
        {
            foreach (var item in nexts)
            {
                if (depth != 0)
                {
                    sb.Append(' ', depth * 2);
                }
                sb.Append("[" + item.Key + "]");
                if (item.Value != -1)
                {
                    sb.Append("(" + item.originalKey + ")");
                    sb.Append(" = ");
                    sb.Append(item.Value);
                }
                sb.AppendLine();
                ToStringCore(item.nexts, sb, depth + 1);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<KeyValuePair<string, int>>).GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, int>> IEnumerable<KeyValuePair<string, int>>.GetEnumerator()
        {
            return YieldCore(this.root.nexts).GetEnumerator();
        }

        static IEnumerable<KeyValuePair<string, int>> YieldCore(AutomataNode[] nexts)
        {
            foreach (var item in nexts)
            {
                if (item.Value != -1) yield return new KeyValuePair<string, int>(item.originalKey, item.Value);
                YieldCore(item.nexts);
            }
        }

        public void EmitMatch(ILGenerator il, LocalBuilder arraySegment, Action<KeyValuePair<string, int>> onFound, Action notFound)
        {
            var fixedP = il.DeclareLocal(typeof(byte).MakeByRefType(), true);
            var p = il.DeclareLocal(typeof(byte*));
            //var rest = il.DeclareLocal(typeof(int));
            //var key = il.DeclareLocal(typeof(long));
            //il.EmitLdc_I4(0);
            //il.Emit(OpCodes.Conv_I8);
            //il.EmitStloc(key);

            // fixed (byte* p1 = &arraySegment.Array[arraySegment.Offset])
            il.EmitLdloca(arraySegment);
            il.EmitCall(typeof(ArraySegment<byte>).GetRuntimeProperty("Array").GetGetMethod());
            il.EmitLdloca(arraySegment);
            il.EmitCall(typeof(ArraySegment<byte>).GetRuntimeProperty("Offset").GetGetMethod());
            il.Emit(OpCodes.Ldelema, typeof(byte));
            il.EmitStloc(fixedP);

            // var p2 = p1;
            il.Emit(OpCodes.Ldloc, fixedP); // fixed byte&
            il.Emit(OpCodes.Conv_I);
            il.EmitStloc(p);


            // var rest = arraySegment.Count;
            //il.EmitLdloca(arraySegment);
            //il.EmitCall(typeof(ArraySegment<byte>).GetRuntimeProperty("Count").GetGetMethod());
            //il.EmitStloc(rest);

            //root.EmitSearchNext(il, p, rest, key, onFound, notFound);

            // end fixed
            //il.Emit(OpCodes.Ldc_I4_0);
            //il.Emit(OpCodes.Conv_U);
            //il.EmitStloc(fixedP);
        }
    }
}

#endif
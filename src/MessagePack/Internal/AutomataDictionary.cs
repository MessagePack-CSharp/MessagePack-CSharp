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

        public AutomataNode SearchNext(ref byte* p, ref int rest)
        {
            var key = AutomataEmitHelper.GetKey(ref p, ref rest);
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
                if (index >= 0)
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

            EmitSearchNextCore(il, p, rest, key, onFound, notFound, nexts);
        }

        static void EmitSearchNextCore(ILGenerator il, LocalBuilder p, LocalBuilder rest, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action notFound, AutomataNode[] nexts)
        {
            // TODO:Emit NotFound

            var loopEnd = il.DefineLabel();
            if (nexts.Length < 4)
            {
                // linear-search
                var nextIf = Enumerable.Range(0, Math.Max(nexts.Length - 1, 0)).Select(_ => il.DefineLabel()).ToArray();
                for (int i = 0; i < nexts.Length; i++)
                {
                    if (i != 0)
                    {
                        il.MarkLabel(nextIf[i - 1]);
                    }

                    il.EmitLdloc(key);
                    il.Emit(OpCodes.Ldc_I8, nexts[i].Key);
                    if (i != nexts.Length - 1)
                    {
                        il.Emit(OpCodes.Bne_Un, nextIf[i]);
                    }
                    else
                    {
                        // TODO:is notfound here?
                        // notFound();
                        il.Emit(OpCodes.Bne_Un, loopEnd);
                    }

                    if (nexts[i].Value != -1)
                    {
                        onFound(new KeyValuePair<string, int>(nexts[i].originalKey, nexts[i].Value));
                        il.Emit(OpCodes.Br, loopEnd);
                    }
                    else
                    {
                        nexts[i].EmitSearchNext(il, p, rest, key, onFound, notFound);
                    }
                }
            }
            else
            {
                // switch-case
                if (IsSequential(nexts))
                {
                    // TODO:require default
                    var switchLabels = nexts.Select(_ => il.DefineLabel()).ToArray();

                    il.EmitLdloc(key);
                    il.Emit(OpCodes.Switch, switchLabels);

                    for (int i = 0; i < nexts.Length; i++)
                    {
                        il.MarkLabel(switchLabels[i]);
                        if (nexts[i].Value != -1)
                        {
                            onFound(new KeyValuePair<string, int>(nexts[i].originalKey, nexts[i].Value));
                            il.Emit(OpCodes.Br, loopEnd);
                        }
                        else
                        {
                            nexts[i].EmitSearchNext(il, p, rest, key, onFound, notFound);
                        }
                    }
                }
                else
                {
                    // binary-search
                    var midline = nexts.Length / 2;
                    var mid = nexts[midline].Key;

                    var gotoRight = il.DefineLabel();

                    // if(key < mid)
                    il.EmitLdloc(key);
                    il.Emit(OpCodes.Ldc_I8, mid);
                    il.Emit(OpCodes.Bge, gotoRight);
                    EmitSearchNextCore(il, p, rest, key, onFound, notFound, nexts.Take(midline).ToArray());

                    // else
                    il.MarkLabel(gotoRight);
                    EmitSearchNextCore(il, p, rest, key, onFound, notFound, nexts.Skip(midline).ToArray());
                }
            }

            il.MarkLabel(loopEnd);
        }

        static bool IsSequential(AutomataNode[] array)
        {
            var v = array[0].Key;
            for (int i = 0; i < array.Length; i++)
            {
                if (v++ != array[i].Key) return false;
            }
            return true;
        }
    }

    public static class AutomataEmitHelper
    {
        public static readonly MethodInfo GetKeyMethod = typeof(AutomataEmitHelper).GetRuntimeMethod("GetKey", new[] { typeof(byte*).MakeByRefType(), typeof(int).MakeByRefType() });

        public static unsafe long GetKey(ref byte* p, ref int rest)
        {
            if (rest >= 8)
            {
                var key = *(long*)p;
                p += 8;
                rest -= 8;
                return key;
            }
            else if (rest >= 4)
            {
                var key = *(int*)p;
                p += 4;
                rest -= 4;
                return key;
            }
            else if (rest >= 2)
            {
                var key = *(short*)p;
                p += 2;
                rest -= 2;
                return key;
            }
            else
            {
                var key = *(byte*)p;
                p += 1;
                rest -= 1;
                return key;
            }
        }
    }

    public class AutomataDictionary : IEnumerable<KeyValuePair<string, int>>
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
            var rest = il.DeclareLocal(typeof(int));
            var key = il.DeclareLocal(typeof(long));
            il.EmitLdc_I4(0);
            il.Emit(OpCodes.Conv_I8);
            il.EmitStloc(key);

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
            il.EmitLdloca(arraySegment);
            il.EmitCall(typeof(ArraySegment<byte>).GetRuntimeProperty("Count").GetGetMethod());
            il.EmitStloc(rest);

            root.EmitSearchNext(il, p, rest, key, onFound, notFound);

            // end fixed
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_U);
            il.EmitStloc(fixedP);
        }
    }
}

#endif
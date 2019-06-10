using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace MessagePack.Internal
{
    // Key = long, Value = int for UTF8String Dictionary

    /// <remarks>
    /// This code is used by dynamically generated code as well as AOT generated code,
    /// and thus must be public for the "C# generated and compiled into saved assembly" scenario.
    /// </remarks>
    public class AutomataDictionary : IEnumerable<KeyValuePair<string, int>>
    {
        private readonly AutomataNode root;

        public AutomataDictionary()
        {
            root = new AutomataNode(0);
        }

#if !UNITY
        public void Add(string str, int value)
        {
            ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(str);
            var node = root;

            while (bytes.Length > 0)
            {
                var key = AutomataKeyGen.GetKey(ref bytes);

                if (bytes.Length == 0)
                {
                    node = node.Add(key, value, str);
                }
                else
                {
                    node = node.Add(key);
                }
            }
        }

        public bool TryGetValue(in ReadOnlySequence<byte> bytes, out int value) => TryGetValue(bytes.ToArray(), out value);

        public bool TryGetValue(ReadOnlySpan<byte> bytes, out int value)
        {
            var node = root;

            while (bytes.Length > 0 && node != null)
            {
                node = node.SearchNext(ref bytes);
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
#else
        // for Unity, use safe only.

        public void Add(string str, int value)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            var offset = 0;

            var node = root;

            var rest = bytes.Length;
            while (rest != 0)
            {
                var key = AutomataKeyGen.GetKeySafe(bytes, ref offset, ref rest);

                if (rest == 0)
                {
                    node = node.Add(key, value, str);
                }
                else
                {
                    node = node.Add(key);
                }
            }
        }

#endif

        // for debugging
        public override string ToString()
        {
            var sb = new StringBuilder();
            ToStringCore(root.YieldChildren(), sb, 0);
            return sb.ToString();
        }

        private static void ToStringCore(IEnumerable<AutomataNode> nexts, StringBuilder sb, int depth)
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
                ToStringCore(item.YieldChildren(), sb, depth + 1);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            return YieldCore(this.root.YieldChildren()).GetEnumerator();
        }

        private static IEnumerable<KeyValuePair<string, int>> YieldCore(IEnumerable<AutomataNode> nexts)
        {
            foreach (var item in nexts)
            {
                if (item.Value != -1)
                {
                    yield return new KeyValuePair<string, int>(item.originalKey, item.Value);
                }

                foreach (var x in YieldCore(item.YieldChildren()))
                {
                    yield return x;
                }
            }
        }

        // IL Emit

#if !NET_STANDARD_2_0

        public void EmitMatch(ILGenerator il, LocalBuilder bytesSpan, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action onNotFound)
        {
            root.EmitSearchNext(il, bytesSpan, key, onFound, onNotFound);
        }

#endif

        private class AutomataNode : IComparable<AutomataNode>
        {
            private static readonly AutomataNode[] emptyNodes = new AutomataNode[0];
            private static readonly ulong[] emptyKeys = new ulong[0];

            public ulong Key;
            public int Value;
            public string originalKey;
            private AutomataNode[] nexts;
            private ulong[] nextKeys;
            private int count;

            public bool HasChildren { get { return count != 0; } }

            public AutomataNode(ulong key)
            {
                this.Key = key;
                this.Value = -1;
                this.nexts = emptyNodes;
                this.nextKeys = emptyKeys;
                this.count = 0;
                this.originalKey = null;
            }

            public AutomataNode Add(ulong key)
            {
                var index = Array.BinarySearch(nextKeys, 0, count, key);
                if (index < 0)
                {
                    if (nexts.Length == count)
                    {
                        Array.Resize<AutomataNode>(ref nexts, (count == 0) ? 4 : (count * 2));
                        Array.Resize<ulong>(ref nextKeys, (count == 0) ? 4 : (count * 2));
                    }
                    count++;

                    var nextNode = new AutomataNode(key);
                    nexts[count - 1] = nextNode;
                    nextKeys[count - 1] = key;
                    Array.Sort(nexts, 0, count);
                    Array.Sort(nextKeys, 0, count);
                    return nextNode;
                }
                else
                {
                    return nexts[index];
                }
            }

            public AutomataNode Add(ulong key, int value, string originalKey)
            {
                var v = Add(key);
                v.Value = value;
                v.originalKey = originalKey;
                return v;
            }

#if !UNITY

            public AutomataNode SearchNext(ref ReadOnlySpan<byte> value)
            {
                var key = AutomataKeyGen.GetKey(ref value);
                if (count < 4)
                {
                    // linear search
                    for (int i = 0; i < count; i++)
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
                    var index = BinarySearch(nextKeys, 0, count, key);
                    if (index >= 0)
                    {
                        return nexts[index];
                    }
                }

                return null;
            }

#endif

            internal static int BinarySearch(ulong[] array, int index, int length, ulong value)
            {
                int lo = index;
                int hi = index + length - 1;
                while (lo <= hi)
                {
                    int i = lo + ((hi - lo) >> 1);

                    var arrayValue = array[i];
                    int order;
                    if (arrayValue < value)
                    {
                        order = -1;
                    }
                    else if (arrayValue > value)
                    {
                        order = 1;
                    }
                    else
                    {
                        order = 0;
                    }

                    if (order == 0)
                    {
                        return i;
                    }

                    if (order < 0)
                    {
                        lo = i + 1;
                    }
                    else
                    {
                        hi = i - 1;
                    }
                }

                return ~lo;
            }

            public int CompareTo(AutomataNode other)
            {
                return this.Key.CompareTo(other.Key);
            }

            public IEnumerable<AutomataNode> YieldChildren()
            {
                for (int i = 0; i < count; i++)
                {
                    yield return nexts[i];
                }
            }

#if !NET_STANDARD_2_0

            // SearchNext(ref ReadOnlySpan<byte> bytes)
            public void EmitSearchNext(ILGenerator il, LocalBuilder bytesSpan, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action onNotFound)
            {
                // key = AutomataKeyGen.GetKey(ref bytesSpan);
                il.EmitLdloca(bytesSpan);
                il.EmitCall(AutomataKeyGen.GetKeyMethod);
                il.EmitStloc(key);

                // match children.
                EmitSearchNextCore(il, bytesSpan, key, onFound, onNotFound, nexts, count);
            }

            private static void EmitSearchNextCore(ILGenerator il, LocalBuilder bytesSpan, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action onNotFound, AutomataNode[] nexts, int count)
            {
                if (count < 4)
                {
                    // linear-search
                    var valueExists = nexts.Take(count).Where(x => x.Value != -1).ToArray();
                    var childrenExists = nexts.Take(count).Where(x => x.HasChildren).ToArray();
                    var gotoSearchNext = il.DefineLabel();
                    var gotoNotFound = il.DefineLabel();

                    {
                        // bytesSpan.Length
                        il.EmitLdloca(bytesSpan);
                        il.EmitCall(typeof(ReadOnlySpan<byte>).GetRuntimeProperty(nameof(ReadOnlySpan<byte>.Length)).GetMethod);
                        if (childrenExists.Length != 0 && valueExists.Length == 0)
                        {

                            il.Emit(OpCodes.Brfalse, gotoNotFound); // if(bytesSpan.Length == 0)
                        }
                        else
                        {
                            il.Emit(OpCodes.Brtrue, gotoSearchNext); // if(bytesSpan.Length != 0)
                        }
                    }
                    {
                        var ifValueNexts = Enumerable.Range(0, Math.Max(valueExists.Length - 1, 0)).Select(_ => il.DefineLabel()).ToArray();
                        for (int i = 0; i < valueExists.Length; i++)
                        {
                            var notFoundLabel = il.DefineLabel();
                            if (i != 0)
                            {
                                il.MarkLabel(ifValueNexts[i - 1]);
                            }

                            il.EmitLdloc(key);
                            il.EmitULong(valueExists[i].Key);
                            il.Emit(OpCodes.Bne_Un, notFoundLabel);
                            // found
                            onFound(new KeyValuePair<string, int>(valueExists[i].originalKey, valueExists[i].Value));

                            // notfound
                            il.MarkLabel(notFoundLabel);
                            if (i != valueExists.Length - 1)
                            {
                                il.Emit(OpCodes.Br, ifValueNexts[i]);
                            }
                            else
                            {
                                onNotFound();
                            }
                        }
                    }

                    il.MarkLabel(gotoSearchNext);
                    var ifRecNext = Enumerable.Range(0, Math.Max(childrenExists.Length - 1, 0)).Select(_ => il.DefineLabel()).ToArray();
                    for (int i = 0; i < childrenExists.Length; i++)
                    {
                        var notFoundLabel = il.DefineLabel();
                        if (i != 0)
                        {
                            il.MarkLabel(ifRecNext[i - 1]);
                        }

                        il.EmitLdloc(key);
                        il.EmitULong(childrenExists[i].Key);
                        il.Emit(OpCodes.Bne_Un, notFoundLabel);
                        // found
                        childrenExists[i].EmitSearchNext(il, bytesSpan, key, onFound, onNotFound);
                        // notfound
                        il.MarkLabel(notFoundLabel);
                        if (i != childrenExists.Length - 1)
                        {
                            il.Emit(OpCodes.Br, ifRecNext[i]);
                        }
                        else
                        {
                            onNotFound();
                        }
                    }

                    il.MarkLabel(gotoNotFound);
                    onNotFound();
                }
                else
                {
                    // binary-search
                    var midline = count / 2;
                    var mid = nexts[midline].Key;
                    var l = nexts.Take(count).Take(midline).ToArray();
                    var r = nexts.Take(count).Skip(midline).ToArray();

                    var gotoRight = il.DefineLabel();

                    // if(key < mid)
                    il.EmitLdloc(key);
                    il.EmitULong(mid);
                    il.Emit(OpCodes.Bge, gotoRight);
                    EmitSearchNextCore(il, bytesSpan, key, onFound, onNotFound, l, l.Length);

                    // else
                    il.MarkLabel(gotoRight);
                    EmitSearchNextCore(il, bytesSpan, key, onFound, onNotFound, r, r.Length);
                }
            }

#endif
        }
    }

    /// <remarks>
    /// This is used by dynamically generated code. It can be made internal after we enable our dynamic assemblies to access internals.
    /// But that trick may require net46, so maybe we should leave this as public.
    /// </remarks>
    public static class AutomataKeyGen
    {
#if !UNITY
        public static readonly MethodInfo GetKeyMethod = typeof(AutomataKeyGen).GetRuntimeMethod(nameof(GetKey), new[] { typeof(ReadOnlySpan<byte>).MakeByRefType() });
#endif

        public static ulong GetKey(ref ReadOnlySpan<byte> span)
        {
            ulong key;

            unchecked
            {
                if (span.Length >= 8)
                {
                    key = MemoryMarshal.Cast<byte, ulong>(span)[0];
                    span = span.Slice(8);
                }
                else
                {
                    switch (span.Length)
                    {
                        case 1:
                            {
                                key = span[0];
                                span = span.Slice(1);
                                break;
                            }
                        case 2:
                            {
                                key = MemoryMarshal.Cast<byte, ushort>(span)[0];
                                span = span.Slice(2);
                                break;
                            }
                        case 3:
                            {
                                var a = span[0];
                                var b = MemoryMarshal.Cast<byte, ushort>(span.Slice(1))[0];
                                key = (a | (ulong)b << 8);
                                span = span.Slice(3);
                                break;
                            }
                        case 4:
                            {
                                key = MemoryMarshal.Cast<byte, uint>(span)[0];
                                span = span.Slice(4);
                                break;
                            }
                        case 5:
                            {
                                var a = span[0];
                                var b = MemoryMarshal.Cast<byte, uint>(span.Slice(1))[0];
                                key = (a | (ulong)b << 8);
                                span = span.Slice(5);
                                break;
                            }
                        case 6:
                            {
                                ulong a = MemoryMarshal.Cast<byte, ushort>(span)[0];
                                ulong b = MemoryMarshal.Cast<byte, uint>(span.Slice(2))[0];
                                key = (a | (b << 16));
                                span = span.Slice(6);
                                break;
                            }
                        case 7:
                            {
                                var a = span[0];
                                var b = MemoryMarshal.Cast<byte, ushort>(span.Slice(1))[0];
                                var c = MemoryMarshal.Cast<byte, uint>(span.Slice(3))[0];
                                key = (a | (ulong)b << 8 | (ulong)c << 24);
                                span = span.Slice(7);
                                break;
                            }
                        default:
                            throw new InvalidOperationException("Not Supported Length");
                    }
                }

                return key;
            }
        }
    }
}
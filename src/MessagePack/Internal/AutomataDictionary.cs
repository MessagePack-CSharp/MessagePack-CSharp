using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace MessagePack.Internal
{
    // Key = long, Value = int for UTF8String Dictionary

    public class AutomataDictionary : IEnumerable<KeyValuePair<string, int>>
    {
        readonly AutomataNode root;

        public AutomataDictionary()
        {
            root = new AutomataNode(0);
        }

#if NETSTANDARD
        public unsafe void Add(string str, int value)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            fixed (byte* buffer = &bytes[0])
            {
                var node = root;

                var p = buffer;
                var rest = bytes.Length;
                while (rest != 0)
                {
                    var key = AutomataKeyGen.GetKey(ref p, ref rest);

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
        }

        public unsafe bool TryGetValue(byte[] bytes, int offset, int count, out int value)
        {
            fixed (byte* p = &bytes[0])
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


        public bool TryGetValueSafe(ArraySegment<byte> key, out int value)
        {
            var node = root;
            var bytes = key.Array;
            var offset = key.Offset;
            var rest = key.Count;

            while (rest != 0 && node != null)
            {
                node = node.SearchNextSafe(bytes, ref offset, ref rest);
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

        // for debugging
        public override string ToString()
        {
            var sb = new StringBuilder();
            ToStringCore(root.YieldChildren(), sb, 0);
            return sb.ToString();
        }

        static void ToStringCore(IEnumerable<AutomataNode> nexts, StringBuilder sb, int depth)
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

        static IEnumerable<KeyValuePair<string, int>> YieldCore(IEnumerable<AutomataNode> nexts)
        {
            foreach (var item in nexts)
            {
                if (item.Value != -1) yield return new KeyValuePair<string, int>(item.originalKey, item.Value);
                foreach (var x in YieldCore(item.YieldChildren())) yield return x;
            }
        }

        // IL Emit

        public void EmitMatch(ILGenerator il, LocalBuilder p, LocalBuilder rest, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action onNotFound)
        {
            root.EmitSearchNext(il, p, rest, key, onFound, onNotFound);
        }

        class AutomataNode : IComparable<AutomataNode>
        {
            static readonly AutomataNode[] emptyNodes = new AutomataNode[0];
            static readonly ulong[] emptyKeys = new ulong[0];

            public ulong Key;
            public int Value;
            public string originalKey;

            AutomataNode[] nexts;
            ulong[] nextKeys;
            int count;

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

            public unsafe AutomataNode SearchNext(ref byte* p, ref int rest)
            {
                var key = AutomataKeyGen.GetKey(ref p, ref rest);
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

            public unsafe AutomataNode SearchNextSafe(byte[] p, ref int offset, ref int rest)
            {
                var key = AutomataKeyGen.GetKeySafe(p, ref offset, ref rest);
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

            internal static int BinarySearch(ulong[] array, int index, int length, ulong value)
            {
                int lo = index;
                int hi = index + length - 1;
                while (lo <= hi)
                {
                    int i = lo + ((hi - lo) >> 1);

                    var arrayValue = array[i];
                    int order;
                    if (arrayValue < value) order = -1;
                    else if (arrayValue > value) order = 1;
                    else order = 0;

                    if (order == 0) return i;
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

            // SearchNext(ref byte* p, ref int rest, ref ulong key)
            public void EmitSearchNext(ILGenerator il, LocalBuilder p, LocalBuilder rest, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action onNotFound)
            {
                // key = AutomataKeyGen.GetKey(ref p, ref rest);
                il.EmitLdloca(p);
                il.EmitLdloca(rest);
                il.EmitCall(AutomataKeyGen.GetKeyMethod);
                il.EmitStloc(key);

                // match children.
                EmitSearchNextCore(il, p, rest, key, onFound, onNotFound, nexts, count);
            }

            static void EmitSearchNextCore(ILGenerator il, LocalBuilder p, LocalBuilder rest, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action onNotFound, AutomataNode[] nexts, int count)
            {
                if (count < 4)
                {
                    // linear-search
                    var valueExists = nexts.Take(count).Where(x => x.Value != -1).ToArray();
                    var childrenExists = nexts.Take(count).Where(x => x.HasChildren).ToArray();
                    var gotoSearchNext = il.DefineLabel();
                    var gotoNotFound = il.DefineLabel();

                    {
                        il.EmitLdloc(rest);
                        if (childrenExists.Length != 0 && valueExists.Length == 0)
                        {

                            il.Emit(OpCodes.Brfalse, gotoNotFound); // if(rest == 0)
                        }
                        else
                        {
                            il.Emit(OpCodes.Brtrue, gotoSearchNext); // if(rest != 0)
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
                        childrenExists[i].EmitSearchNext(il, p, rest, key, onFound, onNotFound);
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
                    EmitSearchNextCore(il, p, rest, key, onFound, onNotFound, l, l.Length);

                    // else
                    il.MarkLabel(gotoRight);
                    EmitSearchNextCore(il, p, rest, key, onFound, onNotFound, r, r.Length);
                }
            }
        }
    }

    public static class AutomataKeyGen
    {
        public static readonly MethodInfo GetKeyMethod = typeof(AutomataKeyGen).GetRuntimeMethod("GetKey", new[] { typeof(byte*).MakeByRefType(), typeof(int).MakeByRefType() });
        // public static readonly MethodInfo GetKeySafeMethod = typeof(AutomataKeyGen).GetRuntimeMethod("GetKeySafe", new[] { typeof(byte[]), typeof(int).MakeByRefType(), typeof(int).MakeByRefType() });

        public static unsafe ulong GetKey(ref byte* p, ref int rest)
        {
            int readSize;
            ulong key;

            unchecked
            {
                if (rest >= 8)
                {
                    key = *(ulong*)p;
                    readSize = 8;
                }
                else
                {
                    switch (rest)
                    {
                        case 1:
                            {
                                key = *(byte*)p;
                                readSize = 1;
                                break;
                            }
                        case 2:
                            {
                                key = *(ushort*)p;
                                readSize = 2;
                                break;
                            }
                        case 3:
                            {
                                var a = *p;
                                var b = *(ushort*)(p + 1);
                                key = ((ulong)a | (ulong)b << 8);
                                readSize = 3;
                                break;
                            }
                        case 4:
                            {
                                key = *(uint*)p;
                                readSize = 4;
                                break;
                            }
                        case 5:
                            {
                                var a = *p;
                                var b = *(uint*)(p + 1);
                                key = ((ulong)a | (ulong)b << 8);
                                readSize = 5;
                                break;
                            }
                        case 6:
                            {
                                ulong a = *(ushort*)p;
                                ulong b = *(uint*)(p + 2);
                                key = (a | (b << 16));
                                readSize = 6;
                                break;
                            }
                        case 7:
                            {
                                var a = *(byte*)p;
                                var b = *(ushort*)(p + 1);
                                var c = *(uint*)(p + 3);
                                key = ((ulong)a | (ulong)b << 8 | (ulong)c << 24);
                                readSize = 7;
                                break;
                            }
                        default:
                            throw new InvalidOperationException("Not Supported Length");
                    }
                }

                p += readSize;
                rest -= readSize;
                return key;
            }
        }

        public static ulong GetKeySafe(byte[] bytes, ref int offset, ref int rest)
        {
            int readSize;
            ulong key;

            if (BitConverter.IsLittleEndian)
            {
                unchecked
                {
                    if (rest >= 8)
                    {
                        key = (ulong)bytes[offset] << 0 | (ulong)bytes[offset + 1] << 8 | (ulong)bytes[offset + 2] << 16 | (ulong)bytes[offset + 3] << 24
                            | (ulong)bytes[offset + 4] << 32 | (ulong)bytes[offset + 5] << 40 | (ulong)bytes[offset + 6] << 48 | (ulong)bytes[offset + 7] << 56;
                        readSize = 8;
                    }
                    else
                    {
                        switch (rest)
                        {
                            case 1:
                                {
                                    key = bytes[offset];
                                    readSize = 1;
                                    break;
                                }
                            case 2:
                                {
                                    key = (ulong)bytes[offset] << 0 | (ulong)bytes[offset + 1] << 8;
                                    readSize = 2;
                                    break;
                                }
                            case 3:
                                {
                                    key = (ulong)bytes[offset] << 0 | (ulong)bytes[offset + 1] << 8 | (ulong)bytes[offset + 2] << 16;
                                    readSize = 3;
                                    break;
                                }
                            case 4:
                                {
                                    key = (ulong)bytes[offset] << 0 | (ulong)bytes[offset + 1] << 8 | (ulong)bytes[offset + 2] << 16 | (ulong)bytes[offset + 3] << 24;
                                    readSize = 4;
                                    break;
                                }
                            case 5:
                                {
                                    key = (ulong)bytes[offset] << 0 | (ulong)bytes[offset + 1] << 8 | (ulong)bytes[offset + 2] << 16 | (ulong)bytes[offset + 3] << 24
                                        | (ulong)bytes[offset + 4] << 32;
                                    readSize = 5;
                                    break;
                                }
                            case 6:
                                {
                                    key = (ulong)bytes[offset] << 0 | (ulong)bytes[offset + 1] << 8 | (ulong)bytes[offset + 2] << 16 | (ulong)bytes[offset + 3] << 24
                                        | (ulong)bytes[offset + 4] << 32 | (ulong)bytes[offset + 5] << 40;
                                    readSize = 6;
                                    break;
                                }
                            case 7:
                                {
                                    key = (ulong)bytes[offset] << 0 | (ulong)bytes[offset + 1] << 8 | (ulong)bytes[offset + 2] << 16 | (ulong)bytes[offset + 3] << 24
                                        | (ulong)bytes[offset + 4] << 32 | (ulong)bytes[offset + 5] << 40 | (ulong)bytes[offset + 6] << 48;
                                    readSize = 7;
                                    break;
                                }
                            default:
                                throw new InvalidOperationException("Not Supported Length");
                        }
                    }

                    offset += readSize;
                    rest -= readSize;
                    return key;
                }
            }
            else
            {
                unchecked
                {
                    if (rest >= 8)
                    {
                        key = (ulong)bytes[offset] << 56 | (ulong)bytes[offset + 1] << 48 | (ulong)bytes[offset + 2] << 40 | (ulong)bytes[offset + 3] << 32
                            | (ulong)bytes[offset + 4] << 24 | (ulong)bytes[offset + 5] << 16 | (ulong)bytes[offset + 6] << 8 | (ulong)bytes[offset + 7];
                        readSize = 8;
                    }
                    else
                    {
                        switch (rest)
                        {
                            case 1:
                                {
                                    key = bytes[offset];
                                    readSize = 1;
                                    break;
                                }
                            case 2:
                                {
                                    key = (ulong)bytes[offset] << 8 | (ulong)bytes[offset + 1] << 0;
                                    readSize = 2;
                                    break;
                                }
                            case 3:
                                {
                                    key = (ulong)bytes[offset] << 16 | (ulong)bytes[offset + 1] << 8 | (ulong)bytes[offset + 2] << 0;
                                    readSize = 3;
                                    break;
                                }
                            case 4:
                                {
                                    key = (ulong)bytes[offset] << 24 | (ulong)bytes[offset + 1] << 16 | (ulong)bytes[offset + 2] << 8 | (ulong)bytes[offset + 3] << 0;
                                    readSize = 4;
                                    break;
                                }
                            case 5:
                                {
                                    key = (ulong)bytes[offset] << 32 | (ulong)bytes[offset + 1] << 24 | (ulong)bytes[offset + 2] << 16 | (ulong)bytes[offset + 3] << 8
                                        | (ulong)bytes[offset + 4] << 0;
                                    readSize = 5;
                                    break;
                                }
                            case 6:
                                {
                                    key = (ulong)bytes[offset] << 40 | (ulong)bytes[offset + 1] << 32 | (ulong)bytes[offset + 2] << 24 | (ulong)bytes[offset + 3] << 16
                                        | (ulong)bytes[offset + 4] << 8 | (ulong)bytes[offset + 5] << 0;
                                    readSize = 6;
                                    break;
                                }
                            case 7:
                                {
                                    key = (ulong)bytes[offset] << 48 | (ulong)bytes[offset + 1] << 40 | (ulong)bytes[offset + 2] << 32 | (ulong)bytes[offset + 3] << 24
                                        | (ulong)bytes[offset + 4] << 16 | (ulong)bytes[offset + 5] << 8 | (ulong)bytes[offset + 6] << 0;
                                    readSize = 7;
                                    break;
                                }
                            default:
                                throw new InvalidOperationException("Not Supported Length");
                        }
                    }

                    offset += readSize;
                    rest -= readSize;
                    return key;
                }
            }
        }
    }
}
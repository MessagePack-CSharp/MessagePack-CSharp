// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack.Internal;
using MessagePackCompiler.CodeAnalysis;

namespace MessagePackCompiler.Generator
{
    internal static class StringKeyFormatterDeserializeHelper
    {
        public static string Classify(MemberSerializationInfo[] memberArray, string indent)
        {
            var buffer = new StringBuilder();
            foreach (var memberInfoTuples in memberArray.Select(member => new MemberInfoTuple(member)).GroupBy(member => member.Binary.Length))
            {
                var binaryLength = memberInfoTuples.Key;
                var keyLength = binaryLength >> 3;
                keyLength += keyLength << 3 == binaryLength ? 0 : 1;

                buffer.Append(indent).Append("case ").Append(binaryLength).Append(":\r\n");
                ClassifyRecursion(buffer, indent, 1, keyLength, memberInfoTuples);
            }

            return buffer.ToString();
        }

        private static void Assign(StringBuilder buffer, in MemberInfoTuple member)
        {
            buffer.Append("__").Append(member.Info.Name).Append("__ = ").Append(member.Info.GetDeserializeMethodString()).Append(";\r\n");
        }

        private static void ClassifyRecursion(StringBuilder buffer, string indent, int tabCount, int keyLength, IEnumerable<MemberInfoTuple> memberCollection)
        {
            const string Tab = "    ";
            buffer.Append(indent);
            for (var i = 0; i < tabCount; i++)
            {
                buffer.Append(Tab);
            }

            var memberArray = memberCollection.ToArray();
            if (memberArray.Length == 1)
            {
                var member = memberArray[0];
                EmbedOne(buffer, indent, tabCount, member);
                return;
            }

            buffer.Append("switch (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey))\r\n").Append(indent);
            for (var i = 0; i < tabCount; i++)
            {
                buffer.Append(Tab);
            }

            buffer.Append("{\r\n" + Tab).Append(indent);
            for (var i = 0; i < tabCount; i++)
            {
                buffer.Append(Tab);
            }

            buffer.Append("default: goto FAIL;");

            foreach (var grouping in memberArray.GroupBy(member => member.Key[tabCount - 1]))
            {
                buffer.Append("\r\n" + Tab).Append(indent);
                for (var i = 0; i < tabCount; i++)
                {
                    buffer.Append(Tab);
                }

                buffer.Append("case ").Append(grouping.Key).Append("UL:\r\n");

                if (tabCount == keyLength)
                {
                    buffer.Append(Tab + Tab).Append(indent);
                    for (var i = 0; i < tabCount; i++)
                    {
                        buffer.Append(Tab);
                    }

                    var member = grouping.Single();
                    Assign(buffer, member);
                    buffer.Append(Tab + Tab).Append(indent);
                    for (var i = 0; i < tabCount; i++)
                    {
                        buffer.Append(Tab);
                    }

                    buffer.Append("continue;");
                    continue;
                }

                ClassifyRecursion(buffer, indent + Tab, tabCount + 1, keyLength, grouping);
            }

            buffer.Append("\r\n").Append(indent);
            for (var i = 0; i < tabCount; i++)
            {
                buffer.Append(Tab);
            }

            buffer.Append("}\r\n");
        }

        private static void EmbedOne(StringBuilder buffer, string indent, int tabCount, in MemberInfoTuple member)
        {
            const string Tab = "    ";
            var binary = member.Binary.AsSpan((tabCount - 1) << 3);

            switch (binary.Length)
            {
                case 1:
                    buffer.Append("if (stringKey[0] != ").Append(binary[0]);
                    break;
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                    buffer.Append("if (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey) != ").Append(member.Key[tabCount - 1]).Append("UL");
                    break;
                default:
                    EmbedSequenceEqual(buffer, member, (tabCount << 3) - 8);
                    break;
            }

            buffer.Append(") { goto FAIL; }\r\n\r\n").Append(indent);
            for (var i = 0; i < tabCount; i++)
            {
                buffer.Append(Tab);
            }

            Assign(buffer, member);
            buffer.Append(indent);
            for (var i = 0; i < tabCount; i++)
            {
                buffer.Append(Tab);
            }

            buffer.Append("continue;\r\n");
        }

        private static void EmbedSequenceEqual(StringBuilder buffer, MemberInfoTuple member, int startPosition)
        {
            buffer
                .Append("if (!global::System.MemoryExtensions.SequenceEqual(stringKey, GetSpan_")
                .Append(member.Info.Name)
                .Append("().Slice(")
                .Append(EmbedStringHelper.GetHeaderLength(member.Binary.Length));

            if (startPosition != 0)
            {
                buffer.Append(" + ").Append(startPosition);
            }

            buffer.Append("))");
        }
    }

    internal readonly struct MemberInfoTuple : IComparable<MemberInfoTuple>
    {
        public readonly MemberSerializationInfo Info;
        public readonly byte[] Binary;
        public readonly ulong[] Key;

        public MemberInfoTuple(MemberSerializationInfo info)
        {
            Info = info;
            Binary = EmbedStringHelper.Utf8.GetBytes(info.StringKey);
            ReadOnlySpan<byte> span = Binary;
            var keyLength = Binary.Length >> 3;
            keyLength += keyLength << 3 == Binary.Length ? 0 : 1;
            Key = new ulong[keyLength];
            for (var i = 0; i < Key.Length; i++)
            {
                Key[i] = AutomataKeyGen.GetKey(ref span);
            }
        }

        public int CompareTo(MemberInfoTuple other)
        {
            if (Info == other.Info)
            {
                return 0;
            }

            var c = Binary.Length.CompareTo(other.Binary.Length);
            if (c != 0)
            {
                return c;
            }

            for (var i = 0; i < Key.Length; i++)
            {
                c = Key[i].CompareTo(other.Key[i]);
                if (c != 0)
                {
                    return c;
                }
            }

            return 0;
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using MessagePack.Internal;
using MessagePackCompiler.CodeAnalysis;
using StringLiteral;

namespace MessagePackCompiler.Generator;

internal static partial class StringKeyFormatterDeserializeHelper
{
    [Utf8("case ")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralCaseSpace();

    [Utf8("____result.")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralResult();

    [Utf8(" = ")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralSpaceAssignSpace();

    [Utf8("__")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralUnderscoreUnderscore();

    [Utf8("__ = ")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralUnderscoreUnderscoreSpaceAssignSpace();

    [Utf8("__IsInitialized = true;")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralIsInitialized();

    [Utf8("reader.Skip();")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralReaderSkip();

    [Utf8("switch (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey))")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralSwitch();

    [Utf8("default: goto FAIL;")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralDefault();

    [Utf8("UL")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralUL();

    [Utf8("UL:")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralULColon();

    [Utf8("continue;")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralContinue();

    [Utf8("if (stringKey[0] != ")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralIfStringKeyNotEqual();

    [Utf8("if (global::MessagePack.Internal.AutomataKeyGen.GetKey(ref stringKey) != ")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralGetKey();

    [Utf8(") { goto FAIL; }")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralGotoFail();

    [Utf8("if (!global::System.MemoryExtensions.SequenceEqual(stringKey, GetSpan_")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralSequenceEqual();

    [Utf8("().Slice(")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralSlice();

    [Utf8(" + ")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralSpacePlusSpace();

    [Utf8("))")]
    private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralParenParen();

    private static void Append(ref Utf8ValueStringBuilder buffer, ReadOnlySpan<byte> span)
    {
        var destination = buffer.GetSpan(span.Length);
        span.CopyTo(destination);
        buffer.Advance(span.Length);
    }

    private static void Append(ref Utf8ValueStringBuilder buffer, byte c)
    {
        var destination = buffer.GetSpan(1);
        destination[0] = c;
        buffer.Advance(1);
    }

    private static void AppendTab(ref Utf8ValueStringBuilder buffer, int count)
    {
        count <<= 2;
        var span = buffer.GetSpan(count);
        span.Fill((byte)' ');
        buffer.Advance(count);
    }

    private static bool IsConstructorParameter(ObjectSerializationInfo objectSerializationInfo, MemberSerializationInfo member)
    {
        foreach (var parameter in objectSerializationInfo.ConstructorParameters)
        {
            if (parameter.Equals(member))
            {
                return true;
            }
        }

        return false;
    }

    public static void Classify(ref Utf8ValueStringBuilder buffer, ObjectSerializationInfo objectSerializationInfo, int indentTabCount, bool canOverwrite)
    {
        var memberArray = objectSerializationInfo.Members;
        foreach (var memberInfoTuples in memberArray.Select(member => new MemberInfoTuple(member, IsConstructorParameter(objectSerializationInfo, member))).GroupBy(member => member.Binary.Length))
        {
            var binaryLength = memberInfoTuples.Key;
            var keyLength = binaryLength >> 3;
            keyLength += keyLength << 3 == binaryLength ? 0 : 1;

            AppendTab(ref buffer, indentTabCount);
            Append(ref buffer, GetUtf8ConstLiteralCaseSpace());
            buffer.Append(binaryLength);
            Append(ref buffer, (byte)':');
            buffer.AppendLine();
            ClassifyRecursion(ref buffer, indentTabCount, 1, keyLength, memberInfoTuples, canOverwrite);
        }
    }

    private static void Assign(ref Utf8ValueStringBuilder buffer, in MemberInfoTuple member, bool canOverwrite, int indentTabCount, int tabCount)
    {
        if (member.Info.IsWritable || member.IsConstructorParameter)
        {
            if (canOverwrite)
            {
                Append(ref buffer, GetUtf8ConstLiteralResult());
                buffer.Append(member.Info.Name);
                Append(ref buffer, GetUtf8ConstLiteralSpaceAssignSpace());
            }
            else
            {
                if (!member.IsConstructorParameter)
                {
                    Append(ref buffer, GetUtf8ConstLiteralUnderscoreUnderscore());
                    buffer.Append(member.Info.Name);
                    Append(ref buffer, GetUtf8ConstLiteralIsInitialized());
                    buffer.AppendLine();
                    AppendTab(ref buffer, indentTabCount + tabCount);
                }

                Append(ref buffer, GetUtf8ConstLiteralUnderscoreUnderscore());
                buffer.Append(member.Info.Name);
                Append(ref buffer, GetUtf8ConstLiteralUnderscoreUnderscoreSpaceAssignSpace());
            }

            member.Info.AppendDeserializeMethod(ref buffer);
            Append(ref buffer, (byte)';');
            buffer.AppendLine();
        }
        else
        {
            Append(ref buffer, GetUtf8ConstLiteralReaderSkip());
            buffer.AppendLine();
        }
    }

    private static void ClassifyRecursion(ref Utf8ValueStringBuilder buffer, int indentTabCount, int tabCount, int keyLength, IEnumerable<MemberInfoTuple> memberCollection, bool canOverwrite)
    {
        AppendTab(ref buffer, indentTabCount + tabCount);
        var memberArray = memberCollection.ToArray();
        if (memberArray.Length == 1)
        {
            var member = memberArray[0];
            EmbedOne(ref buffer, indentTabCount, tabCount, member, canOverwrite);
            return;
        }

        Append(ref buffer, GetUtf8ConstLiteralSwitch());
        buffer.AppendLine();
        AppendTab(ref buffer, indentTabCount + tabCount);
        Append(ref buffer, (byte)'{');
        buffer.AppendLine();
        AppendTab(ref buffer, indentTabCount + tabCount + 1);
        Append(ref buffer, GetUtf8ConstLiteralDefault());
        foreach (var grouping in memberArray.GroupBy(member => member.Key[tabCount - 1]))
        {
            buffer.AppendLine();
            AppendTab(ref buffer, indentTabCount + tabCount + 1);
            Append(ref buffer, GetUtf8ConstLiteralCaseSpace());
            buffer.Append(grouping.Key);
            Append(ref buffer, GetUtf8ConstLiteralULColon());
            buffer.AppendLine();
            if (tabCount == keyLength)
            {
                AppendTab(ref buffer, indentTabCount + tabCount + 2);
                var member = grouping.Single();
                Assign(ref buffer, member, canOverwrite, indentTabCount, tabCount + 2);
                AppendTab(ref buffer, indentTabCount + tabCount + 2);
                Append(ref buffer, GetUtf8ConstLiteralContinue());
                continue;
            }

            ClassifyRecursion(ref buffer, indentTabCount + 1, tabCount + 1, keyLength, grouping, canOverwrite);
        }

        buffer.AppendLine();
        AppendTab(ref buffer, indentTabCount + tabCount);
        Append(ref buffer, (byte)'}');
        if (tabCount == 1)
        {
            buffer.AppendLine();
        }
    }

    private static void EmbedOne(ref Utf8ValueStringBuilder buffer, int indentTabCount, int tabCount, in MemberInfoTuple member, bool canOverwrite)
    {
        var binary = member.Binary.AsSpan((tabCount - 1) << 3);
        switch (binary.Length)
        {
            case 1:
                Append(ref buffer, GetUtf8ConstLiteralIfStringKeyNotEqual());
                buffer.Append(binary[0]);
                break;
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
                Append(ref buffer, GetUtf8ConstLiteralGetKey());
                buffer.Append(member.Key[tabCount - 1]);
                Append(ref buffer, GetUtf8ConstLiteralUL());
                break;
            default:
                EmbedSequenceEqual(ref buffer, member, (tabCount << 3) - 8);
                break;
        }

        Append(ref buffer, GetUtf8ConstLiteralGotoFail());
        buffer.AppendLine();
        buffer.AppendLine();
        AppendTab(ref buffer, indentTabCount + tabCount);
        Assign(ref buffer, member, canOverwrite, indentTabCount, tabCount);
        AppendTab(ref buffer, indentTabCount + tabCount);
        Append(ref buffer, GetUtf8ConstLiteralContinue());
        buffer.AppendLine();
    }

    private static void EmbedSequenceEqual(ref Utf8ValueStringBuilder buffer, MemberInfoTuple member, int startPosition)
    {
        Append(ref buffer, GetUtf8ConstLiteralSequenceEqual());
        buffer.Append(member.Info.Name);
        Append(ref buffer, GetUtf8ConstLiteralSlice());
        buffer.Append(EmbedStringHelper.GetHeaderLength(member.Binary.Length));
        if (startPosition != 0)
        {
            Append(ref buffer, GetUtf8ConstLiteralSpacePlusSpace());
            buffer.Append(startPosition);
        }

        Append(ref buffer, GetUtf8ConstLiteralParenParen());
    }
}

internal readonly struct MemberInfoTuple : IComparable<MemberInfoTuple>
{
    public readonly MemberSerializationInfo Info;
    public readonly bool IsConstructorParameter;
    public readonly byte[] Binary;
    public readonly ulong[] Key;

    public MemberInfoTuple(MemberSerializationInfo info, bool isConstructorParameter)
    {
        Info = info;
        IsConstructorParameter = isConstructorParameter;
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

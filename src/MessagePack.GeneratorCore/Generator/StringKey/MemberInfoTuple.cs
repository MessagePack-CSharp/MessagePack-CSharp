// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack.Internal;
using MessagePackCompiler.CodeAnalysis;

namespace MessagePackCompiler.Generator;

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

internal static class MemberInfoTupleHelper
{
    public static IEnumerable<IGrouping<int, MemberInfoTuple>> SelectMany(this ObjectSerializationInfo info)
    {
        return info.Members.Select(member =>
        {
            var value = false;
            foreach (var parameter in info.ConstructorParameters)
            {
                if (parameter.Equals(member))
                {
                    value = true;
                    break;
                }
            }

            return new MemberInfoTuple(member, value);
        }).GroupBy(member => member.Binary.Length);
    }

    public static void Deconstruct(this IGrouping<int, MemberInfoTuple> group, out int binaryLength, out int keyLength)
    {
        binaryLength = group.Key;
        keyLength = binaryLength >> 3;
        keyLength += keyLength << 3 != binaryLength ? 1 : 0;
    }
}

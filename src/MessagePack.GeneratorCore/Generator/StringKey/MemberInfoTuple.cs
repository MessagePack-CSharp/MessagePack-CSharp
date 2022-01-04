// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

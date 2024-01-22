// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Threading;

namespace MessagePack.Formatters;

internal static partial class ReadOnlySpanSerializeHelper
{
    private static void BigEndianSerialize(ref MessagePackWriter writer, ref double input, int length, CancellationToken cancellationToken)
    {
        var i = 0;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref float input, int length, CancellationToken cancellationToken)
    {
        var i = 0;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref long input, int length, CancellationToken cancellationToken)
    {
        var i = 0;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref ulong input, int length, CancellationToken cancellationToken)
    {
        var i = 0;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref int input, int length, CancellationToken cancellationToken)
    {
        var i = 0;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref uint input, int length, CancellationToken cancellationToken)
    {
        var i = 0;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref short input, int length, CancellationToken cancellationToken)
    {
        var i = 0;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }

    private static void BigEndianSerialize(ref MessagePackWriter writer, ref ushort input, int length, CancellationToken cancellationToken)
    {
        var i = 0;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(Unsafe.Add(ref input, i));
        }
        while (++i < length);
    }
}

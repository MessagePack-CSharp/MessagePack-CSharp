// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace MessagePack.Tests;

public class PrimitiveCollectionTests
{
    private static T Convert<T>(T value)
    {
        var binary = MessagePackSerializer.Serialize(value);
        var newValue = MessagePackSerializer.Deserialize<T>(binary);
        return newValue;
    }

    public static object[][] CollectionTestData = [
        [Array.Empty<bool>()],
        [Array.Empty<sbyte>()],
        [Array.Empty<short>()],
        [Array.Empty<ushort>()],
        [Array.Empty<int>()],
        [Array.Empty<uint>()],
        [Array.Empty<long>()],
        [Array.Empty<ulong>()],

        [new bool[31] { true, true, false, false, true, false, false, false, false, true, true, true, true, true, true, false, true, true, false, false, true, false, false, false, false, true, true, true, true, true, true }],
        [new sbyte[15] { 2, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, }], // 1byte
        [new short[15] { 2, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, }], // 1byte
        [new int[15] { 2, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, }], // 1byte
        [new long[15] { 2, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, }], // 1byte
        [new sbyte[15] { -128, 127, -126, 126, -124, 125, -122, 124, -120, 123, -118, 122, -116, 121, -114, }], // 2byte, 1byte
        [new short[15] { -128, 127, -126, 126, -124, 125, -122, 124, -120, 123, -118, 122, -116, 121, -114, }], // 2byte, 1byte
        [new int[15] { -128, 127, -126, 126, -124, 125, -122, 124, -120, 123, -118, 122, -116, 121, -114, }], // 2byte, 1byte
        [new long[15] { -128, 127, -126, 126, -124, 125, -122, 124, -120, 123, -118, 122, -116, 121, -114, }], // 2byte, 1byte
        [new short[15] { short.MinValue, -128, 127, short.MaxValue, -124, 125, 129, 124, -120, 255, -118, 122, 256, -116, 121 }], // 3byte, 2byte, 1byte
        [new int[15] { short.MinValue, -128, 127, short.MaxValue, -124, 125, 129, 124, -120, 255, -118, 122, 256, -116, 121 }], // 3byte, 2byte, 1byte
        [new long[15] { short.MinValue, -128, 127, short.MaxValue, -124, 125, 129, 124, -120, 255, -118, 122, 256, -116, 121 }], // 3byte, 2byte, 1byte
        [new int[15] { int.MinValue, ushort.MaxValue, sbyte.MinValue, sbyte.MaxValue, int.MaxValue, short.MinValue, byte.MaxValue, 0, int.MaxValue / 2, ushort.MaxValue - 4, -100, -1, int.MinValue + 4, short.MaxValue - 4, 1 }], // 5byte, 3byte, 2byte, 1byte
        [new long[15] { int.MinValue, ushort.MaxValue, sbyte.MinValue, sbyte.MaxValue, int.MaxValue, short.MinValue, byte.MaxValue, 0, int.MaxValue / 2, ushort.MaxValue - 4, -100, -1, int.MinValue + 4, short.MaxValue - 4, 1 }], // 5byte, 3byte, 2byte, 1byte
        [new long[15] { long.MinValue, uint.MaxValue, short.MinValue, byte.MaxValue, sbyte.MaxValue, long.MaxValue, int.MinValue, short.MaxValue, sbyte.MinValue, -2, long.MaxValue - 1000L, uint.MaxValue - 1000L, short.MinValue + 1000L, sbyte.MaxValue + 10L, 0, }],
        [new ushort[15] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, (ushort)sbyte.MaxValue }], // 1byte
        [new uint[15] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, (ushort)sbyte.MaxValue }], // 1byte
        [new ulong[15] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, (ushort)sbyte.MaxValue }], // 1byte

        [new bool[32] { true, true, false, false, true, false, false, false, false, true, true, true, true, true, true, false, true, true, false, false, true, false, false, false, false, true, true, true, true, true, true, false }],
        [new sbyte[16] { 3, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, }], // 1byte
        [new short[16] { 3, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, }], // 1byte
        [new int[16] { 3, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, }], // 1byte
        [new long[16] { 3, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, }], // 1byte
        [new sbyte[16] { 120, -128, 127, -126, 126, -124, 125, -122, 124, -120, 123, -118, 122, -116, 121, -114, }], // 2byte, 1byte
        [new short[16] { 120, -128, 127, -126, 126, -124, 125, -122, 124, -120, 123, -118, 122, -116, 121, -114, }], // 2byte, 1byte
        [new int[16] { 120, -128, 127, -126, 126, -124, 125, -122, 124, -120, 123, -118, 122, -116, 121, -114, }], // 2byte, 1byte
        [new long[16] { 120, -128, 127, -126, 126, -124, 125, -122, 124, -120, 123, -118, 122, -116, 121, -114, }], // 2byte, 1byte
        [new short[16] { short.MinValue, -128, 127, short.MaxValue, -124, 125, 129, 124, -120, 255, -118, 122, 256, -116, 121, 1024 }], // 3byte, 2byte, 1byte
        [new int[16] { short.MinValue, -128, 127, short.MaxValue, -124, 125, 129, 124, -120, 255, -118, 122, 256, -116, 121, 1024 }], // 3byte, 2byte, 1byte
        [new long[16] { short.MinValue, -128, 127, short.MaxValue, -124, 125, 129, 124, -120, 255, -118, 122, 256, -116, 121, 1024 }], // 3byte, 2byte, 1byte
        [new int[16] { int.MinValue, ushort.MaxValue, sbyte.MinValue, sbyte.MaxValue, int.MaxValue, short.MinValue, byte.MaxValue, 0, int.MaxValue / 2, ushort.MaxValue - 4, -100, -1, int.MinValue + 4, short.MaxValue - 4, 1, -65539 }], // 5byte, 3byte, 2byte, 1byte
        [new long[16] { int.MinValue, ushort.MaxValue, sbyte.MinValue, sbyte.MaxValue, int.MaxValue, short.MinValue, byte.MaxValue, 0, int.MaxValue / 2, ushort.MaxValue - 4, -100, -1, int.MinValue + 4, short.MaxValue - 4, 1, -65539 }], // 5byte, 3byte, 2byte, 1byte
        [new long[16] { long.MinValue, uint.MaxValue, short.MinValue, byte.MaxValue, sbyte.MaxValue, long.MaxValue, int.MinValue, short.MaxValue, sbyte.MinValue, -2, long.MaxValue - 1000L, uint.MaxValue - 1000L, short.MinValue + 1000L, sbyte.MaxValue + 10L, 0, -65539 }],
        [new ushort[16] { sbyte.MaxValue - 1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, (ushort)sbyte.MaxValue, }], // 1byte
        [new uint[16] { sbyte.MaxValue - 1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, (uint)sbyte.MaxValue, }], // 1byte
        [new ulong[16] { sbyte.MaxValue - 1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, (ulong)sbyte.MaxValue, }], // 1byte

        [new bool[33] { true, true, false, false, true, false, false, false, false, true, true, true, true, true, true, false, false, true, true, false, false, true, false, false, false, false, true, true, true, true, true, true, false }],
        [new sbyte[17] { 4, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, }], // 1byte
        [new short[17] { 4, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, }], // 1byte
        [new int[17] { 4, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, }], // 1byte
        [new long[17] { 4, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, }], // 1byte
        [new sbyte[17] { -32, -128, 127, -126, 126, -124, 125, -122, 124, -120, 123, -118, 122, -117, 121, -114, 120, }], // 2byte, 1byte
        [new short[17] { -32, -128, 127, -126, 126, -124, 125, -122, 124, -120, 123, -118, 122, -117, 121, -114, 120, }], // 2byte, 1byte
        [new int[17] { -32, -128, 127, -126, 126, -124, 125, -122, 124, -120, 123, -118, 122, -117, 121, -114, 120, }], // 2byte, 1byte
        [new long[17] { -32, -128, 127, -126, 126, -124, 125, -122, 124, -120, 123, -118, 122, -117, 121, -114, 120, }], // 2byte, 1byte
        [new short[17] { short.MinValue, -128, 127, short.MaxValue, -124, 125, 129, 124, -120, 255, -118, 122, 256, -117, 121, 1024, 240, }], // 3byte, 2byte, 1byte
        [new int[17] { short.MinValue, -128, 127, short.MaxValue, -124, 125, 129, 124, -120, 255, -118, 122, 256, -117, 121, 1024, 240, }], // 3byte, 2byte, 1byte
        [new long[17] { short.MinValue, -128, 127, short.MaxValue, -124, 125, 129, 124, -120, 255, -118, 122, 256, -117, 121, 1024, 240, }], // 3byte, 2byte, 1byte
        [new int[17] { int.MinValue, ushort.MaxValue, sbyte.MinValue, sbyte.MaxValue, int.MaxValue, short.MinValue, byte.MaxValue, 0, int.MaxValue / 2, ushort.MaxValue - 4, -100, -1, int.MinValue + 4, short.MaxValue - 4, 1, -65539, -31, }], // 5byte, 3byte, 2byte, 1byte
        [new long[17] { int.MinValue, ushort.MaxValue, sbyte.MinValue, sbyte.MaxValue, int.MaxValue, short.MinValue, byte.MaxValue, 0, int.MaxValue / 2, ushort.MaxValue - 4, -100, -1, int.MinValue + 4, short.MaxValue - 4, 1, -65539, -31, }], // 5byte, 3byte, 2byte, 1byte
        [new long[17] { long.MinValue, uint.MaxValue, short.MinValue, byte.MaxValue, sbyte.MaxValue, long.MaxValue, int.MinValue, short.MaxValue, sbyte.MinValue, -2, long.MaxValue - 1000L, uint.MaxValue - 1000L, short.MinValue + 1000L, sbyte.MaxValue + 10L, 0, -65539, -31, }],
        [new ushort[17] { sbyte.MaxValue - 2, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, (ushort)sbyte.MaxValue, sbyte.MaxValue - 1, }], // 1byte
        [new uint[17] { sbyte.MaxValue - 2, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, (uint)sbyte.MaxValue, sbyte.MaxValue - 1, }], // 1byte
        [new ulong[17] { sbyte.MaxValue - 2, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, (ulong)sbyte.MaxValue, sbyte.MaxValue - 1, }], // 1byte
    ];

    [Theory]
    [MemberData(nameof(CollectionTestData))]
    public void ConcreteCollectionTest<T>(T[] x)
    {
        Convert(x).IsStructuralEqual(x);
        var list = new List<T>(x);
        Convert(list).IsStructuralEqual(list);
    }
}

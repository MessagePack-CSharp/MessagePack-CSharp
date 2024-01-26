// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

/*
    **** REFERENCE BLOG POST ****
    About SIMD(Single Instruction Multiple Data). See: https://en.wikipedia.org/wiki/SIMD
    In August 2020, .NET Core 3.1 provides the `Hardware Intrinsics` for x86/x64 processor.
        Blog Post : https://devblogs.microsoft.com/dotnet/hardware-intrinsics-in-net-core/
        API Document : https://docs.microsoft.com/ja-jp/dotnet/api/system.runtime.intrinsics.X86
    Incoming .NET 5 will provide the Hardware Intrinsics API for Arm processor.
        Blog Post : https://devblogs.microsoft.com/dotnet/announcing-net-5-preview-4-and-our-journey-to-one-net/#user-content-arm64-performance
        API Document : https://docs.microsoft.com/ja-jp/dotnet/api/system.runtime.intrinsics.Arm
*/

/*
    In x86/x64, there are several streaming SIMD extensions.
        Official Site: https://software.intel.com/sites/landingpage/IntrinsicsGuide/

    .NET Core 3.1 only provides up to AVX2 streaming SIMD extension.
    AVX2 is supproted since Intel's Haswell micro architecture(2013/6).
    Sse4.2 and PopCnt are supported since Intel's Nehalem micro architecture(2008/11).

    Because of a problem in serializing MessagePack, I implemented all of them using SSE4.2 features instead of AVX2.
    In MessagePack, the size of the resultant encoded numeric value varies according to the range to which the numeric value belongs.

    |            max excluded            |              min excluded              | encoded byte length | MessagePack code |
    | ---------------------------------- | -------------------------------------- | ------------------- | ---------------- |
    |           short.MinValue           |            int.MinValue - 1            |          5          |      Int32       |
    | MessagePackRange.MinFixNegativeInt |           sbyte.MinValue - 1           |          2          |       Int8       |
    |           sbyte.MinValue           |           short.MinValue - 1           |          3          |      Int16       |
    |         sbyte.MaxValue + 1         | MessagePackRange.MinFixNegativeInt - 1 |          1          |      FixNum      |
    |          byte.MaxValue + 1         |             sbyte.MaxValue             |          2          |      UInt8       |
    |         ushort.MaxValue + 1        |             byte.MaxValue              |          3          |      UInt16      |
    |          uint.MaxValue + 1         |            ushort.MaxValue             |          5          |      UInt32      |

    We will insert appropriate MessagePackCode with appropriate reordering of the input byte sequence to correspond to the variable-length output results.
    In SIMD programming circles, "reordering" is referred to as "shuffle" or "permute".

    MessagePack serialization using SIMD consists of the following steps.

    - Load the Input Values.
    - Classify the Input Values.
    - Calculate Total Output Byte Count.
    - Get the destination span from writer with the Total Output Byte Count.

    In subsequent steps, the Input Values are divided into halves and treated as the Input Values.
    There are 16 input values in the sbyte type, 8 in the short type, and 4 in the int type.

    - Shuffle the Input Values to make the Output Byte Sequence.
    - Place the appropriate MessagePack Codes in the Output Byte Sequence according to the classification.
    - Store the Output Byte Sequence in the destination span.
*/

/*
    How to classify the input values?

    Conditional branching is not favored in SIMD programming.
    The technique of Shader programming is similar to that of SIMD.

    I use only Sse2.CompareGreaterThan(left, right). See : https://software.intel.com/sites/landingpage/IntrinsicsGuide/#text=_mm_cmpgt_epi8&expand=915

    public static System.Runtime.Intrinsics.Vector128<sbyte> CompareGreaterThan (System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right);

    Pseudo Code:
        System.Runtime.Intrinsics.Vector128<sbyte> CompareGreaterThan (System.Runtime.Intrinsics.Vector128<sbyte> left, System.Runtime.Intrinsics.Vector128<sbyte> right)
        {
            Vector128<sbyte> answer = default;
            sbyte* leftPointer = (sbyte*)&left;
            sbyte* rightPointer = (sbyte*)&right;
            sbyte* answerPointer = (sbyte*)&answer;
            for (int i = 0; i < 16; i++)
            {
                if (*leftPointer++ > *rightPointer++)
                {
                    *answer++ = -1;
                }
                else
                {
                    *answer++ = 0;
                }
            }

            return answer;
        }

    If the condition is true, the corresponding bit intervals are all 1.
    This state where all the bits are 1 is sometimes regarded as -1, and sometimes it is simply used as a bit mask.
*/

/*
    What is the `shuffle`?
    See : https://software.intel.com/sites/landingpage/IntrinsicsGuide/#text=_mm_shuffle_epi8&expand=5153
    See : https://www.officedaytime.com/simd512e/simdimg/si.php?f=pshufb

    public static System.Runtime.Intrinsics.Vector128<byte> Shuffle (System.Runtime.Intrinsics.Vector128<byte> value, System.Runtime.Intrinsics.Vector128<byte> mask);
    `shuffle` packed 8-bit integers in value according to shuffle control mask in the corresponding 8-bit element of mask, and returns.

    Example(pseudo code):
        Vector128<byte> input   = Vector128.Create(0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10);
        Vector128<byte> shuffle = Vector128.Create(0x80, 0, 15, 0x80, 2, 2, 3, 0x80, 0x80, 14, 13, 12, 0, 5, 1, 6);
        Vector128<byte> result = Ssse3.Shuffle(input, shuffle);

        Console.WriteLine(result.ToString());
        // <0, 0x01, 0x10, 0, 0x03, 0x03, 0x04, 0, 0, 0x0F, 0x0E, 0x0D, 0x01, 0x06, 0x02, 0x07>
*/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable CS0436 // The same name of the primary package
#pragma warning disable IDE0049
#pragma warning disable SA1121

namespace MessagePack.Formatters;

public sealed partial class SByteArrayFormatter : IMessagePackFormatter<SByte[]?>
{
    public static readonly SByteArrayFormatter Instance = new();

    private SByteArrayFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, SByte[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
    }

    public SByte[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return [];
        }

        var array = new SByte[len];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = reader.ReadSByte();
        }

        return array;
    }
}

public sealed partial class Int16ArrayFormatter : IMessagePackFormatter<Int16[]?>
{
    public static readonly Int16ArrayFormatter Instance = new();

    private Int16ArrayFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, Int16[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
    }

    public Int16[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return [];
        }

        var array = new Int16[len];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = reader.ReadInt16();
        }

        return array;
    }
}

public sealed partial class Int32ArrayFormatter : IMessagePackFormatter<Int32[]?>
{
    public static readonly Int32ArrayFormatter Instance = new();

    private Int32ArrayFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, Int32[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
    }

    public Int32[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return [];
        }

        var array = new Int32[len];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = reader.ReadInt32();
        }

        return array;
    }
}

public sealed partial class Int64ArrayFormatter : IMessagePackFormatter<Int64[]?>
{
    public static readonly Int64ArrayFormatter Instance = new();

    private Int64ArrayFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, Int64[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
    }

    public Int64[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return [];
        }

        var array = new Int64[len];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = reader.ReadInt64();
        }

        return array;
    }
}

public sealed partial class UInt16ArrayFormatter : IMessagePackFormatter<UInt16[]?>
{
    public static readonly UInt16ArrayFormatter Instance = new();

    private UInt16ArrayFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, UInt16[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
    }

    public UInt16[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return [];
        }

        var array = new UInt16[len];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = reader.ReadUInt16();
        }

        return array;
    }
}

public sealed partial class UInt32ArrayFormatter : IMessagePackFormatter<UInt32[]?>
{
    public static readonly UInt32ArrayFormatter Instance = new();

    private UInt32ArrayFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, UInt32[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
    }

    public UInt32[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return [];
        }

        var array = new UInt32[len];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = reader.ReadUInt32();
        }

        return array;
    }
}

public sealed partial class UInt64ArrayFormatter : IMessagePackFormatter<UInt64[]?>
{
    public static readonly UInt64ArrayFormatter Instance = new();

    private UInt64ArrayFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, UInt64[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
    }

    public UInt64[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return [];
        }

        var array = new UInt64[len];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = reader.ReadUInt64();
        }

        return array;
    }
}

public sealed partial class SingleArrayFormatter : IMessagePackFormatter<Single[]?>
{
    public static readonly SingleArrayFormatter Instance = new();

    private SingleArrayFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, Single[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
    }

    public Single[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return [];
        }

        var array = new Single[len];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = reader.ReadSingle();
        }

        return array;
    }
}

public sealed partial class DoubleArrayFormatter : IMessagePackFormatter<Double[]?>
{
    public static readonly DoubleArrayFormatter Instance = new();

    private DoubleArrayFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, Double[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
    }

    public Double[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return [];
        }

        var array = new Double[len];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = reader.ReadDouble();
        }

        return array;
    }
}

public sealed partial class BooleanArrayFormatter : IMessagePackFormatter<Boolean[]?>
{
    public static readonly BooleanArrayFormatter Instance = new();

    private BooleanArrayFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, Boolean[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
    }
}

public sealed partial class CharArrayFormatter : IMessagePackFormatter<Char[]?>
{
    public static readonly CharArrayFormatter Instance = new();

    private CharArrayFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, Char[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetArrayDataReference(value)), value.Length);
    }

    public Char[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return [];
        }

        var array = new Char[len];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = reader.ReadChar();
        }

        return array;
    }
}

public sealed partial class DateTimeArrayFormatter : IMessagePackFormatter<DateTime[]?>
{
    public static readonly DateTimeArrayFormatter Instance = new();

    private DateTimeArrayFormatter()
    {
    }

    public void Serialize(ref MessagePackWriter writer, DateTime[]? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpanSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
    }

    public DateTime[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        var len = reader.ReadArrayHeader();
        if (len == 0)
        {
            return [];
        }

        var array = new DateTime[len];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = reader.ReadDateTime();
        }

        return array;
    }
}

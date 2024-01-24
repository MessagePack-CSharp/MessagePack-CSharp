// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias e;

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace MessagePack.Experimental.Tests;

public class ArrayTests
{
    private static readonly MessagePack.Formatters.ArrayFormatter<SByte> SByteArrayFormatter = new();

    [Test]
    public void EmptySByteArrayTests()
    {
        var array = Array.Empty<SByte>();
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.SByteArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        MessagePackReader reader = new(encoded);
        SByte[]? decoded = e::MessagePack.Formatters.SByteArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(0, decoded!.Length);
    }

    [Test]
    public void NullSByteArrayTests()
    {
        var array = default(SByte[]);
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.SByteArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);
        MessagePackReader reader = new(encoded);
        SByte[]? decoded = e::MessagePack.Formatters.SByteArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNull(decoded);
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void DefaultSByteArrayTests(int length)
    {
        var array = new SByte[length];
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.SByteArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            SByteArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        SByte[]? decoded = e::MessagePack.Formatters.SByteArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void MinValueSByteArrayTests(int length)
    {
        var array = new SByte[length];
        for (var index = 0; index < array.Length; index++)
        {
            array[index] = SByte.MinValue;
        }

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.SByteArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            SByteArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        SByte[]? decoded = e::MessagePack.Formatters.SByteArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void RandomValueSByteArrayTests(int length)
    {
        var array = new SByte[length];
        var r = new Random();
        r.NextBytes(MemoryMarshal.AsBytes(array.AsSpan()));

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.SByteArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            SByteArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        SByte[]? decoded = e::MessagePack.Formatters.SByteArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(length, decoded!.Length);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    private static readonly MessagePack.Formatters.ArrayFormatter<Int16> Int16ArrayFormatter = new();

    [Test]
    public void EmptyInt16ArrayTests()
    {
        var array = Array.Empty<Int16>();
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int16ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        MessagePackReader reader = new(encoded);
        Int16[]? decoded = e::MessagePack.Formatters.Int16ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(0, decoded!.Length);
    }

    [Test]
    public void NullInt16ArrayTests()
    {
        var array = default(Int16[]);
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int16ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);
        MessagePackReader reader = new(encoded);
        Int16[]? decoded = e::MessagePack.Formatters.Int16ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNull(decoded);
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void DefaultInt16ArrayTests(int length)
    {
        var array = new Int16[length];
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int16ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            Int16ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Int16[]? decoded = e::MessagePack.Formatters.Int16ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void MinValueInt16ArrayTests(int length)
    {
        var array = new Int16[length];
        for (var index = 0; index < array.Length; index++)
        {
            array[index] = Int16.MinValue;
        }

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int16ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            Int16ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Int16[]? decoded = e::MessagePack.Formatters.Int16ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void RandomValueInt16ArrayTests(int length)
    {
        var array = new Int16[length];
        var r = new Random();
        r.NextBytes(MemoryMarshal.AsBytes(array.AsSpan()));

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int16ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            Int16ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Int16[]? decoded = e::MessagePack.Formatters.Int16ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(length, decoded!.Length);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    private static readonly MessagePack.Formatters.ArrayFormatter<Int32> Int32ArrayFormatter = new();

    [Test]
    public void EmptyInt32ArrayTests()
    {
        var array = Array.Empty<Int32>();
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int32ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        MessagePackReader reader = new(encoded);
        Int32[]? decoded = e::MessagePack.Formatters.Int32ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(0, decoded!.Length);
    }

    [Test]
    public void NullInt32ArrayTests()
    {
        var array = default(Int32[]);
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int32ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);
        MessagePackReader reader = new(encoded);
        Int32[]? decoded = e::MessagePack.Formatters.Int32ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNull(decoded);
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void DefaultInt32ArrayTests(int length)
    {
        var array = new Int32[length];
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int32ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            Int32ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Int32[]? decoded = e::MessagePack.Formatters.Int32ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void MinValueInt32ArrayTests(int length)
    {
        var array = new Int32[length];
        for (var index = 0; index < array.Length; index++)
        {
            array[index] = Int32.MinValue;
        }

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int32ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            Int32ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Int32[]? decoded = e::MessagePack.Formatters.Int32ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void RandomValueInt32ArrayTests(int length)
    {
        var array = new Int32[length];
        var r = new Random();
        r.NextBytes(MemoryMarshal.AsBytes(array.AsSpan()));

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int32ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            Int32ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Int32[]? decoded = e::MessagePack.Formatters.Int32ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(length, decoded!.Length);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    private static readonly MessagePack.Formatters.ArrayFormatter<Int64> Int64ArrayFormatter = new();

    [Test]
    public void EmptyInt64ArrayTests()
    {
        var array = Array.Empty<Int64>();
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int64ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        MessagePackReader reader = new(encoded);
        Int64[]? decoded = e::MessagePack.Formatters.Int64ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(0, decoded!.Length);
    }

    [Test]
    public void NullInt64ArrayTests()
    {
        var array = default(Int64[]);
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int64ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);
        MessagePackReader reader = new(encoded);
        Int64[]? decoded = e::MessagePack.Formatters.Int64ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNull(decoded);
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void DefaultInt64ArrayTests(int length)
    {
        var array = new Int64[length];
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int64ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            Int64ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Int64[]? decoded = e::MessagePack.Formatters.Int64ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void MinValueInt64ArrayTests(int length)
    {
        var array = new Int64[length];
        for (var index = 0; index < array.Length; index++)
        {
            array[index] = Int64.MinValue;
        }

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int64ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            Int64ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Int64[]? decoded = e::MessagePack.Formatters.Int64ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void RandomValueInt64ArrayTests(int length)
    {
        var array = new Int64[length];
        var r = new Random();
        r.NextBytes(MemoryMarshal.AsBytes(array.AsSpan()));

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.Int64ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            Int64ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Int64[]? decoded = e::MessagePack.Formatters.Int64ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(length, decoded!.Length);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    private static readonly MessagePack.Formatters.ArrayFormatter<UInt16> UInt16ArrayFormatter = new();

    [Test]
    public void EmptyUInt16ArrayTests()
    {
        var array = Array.Empty<UInt16>();
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt16ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        MessagePackReader reader = new(encoded);
        UInt16[]? decoded = e::MessagePack.Formatters.UInt16ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(0, decoded!.Length);
    }

    [Test]
    public void NullUInt16ArrayTests()
    {
        var array = default(UInt16[]);
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt16ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);
        MessagePackReader reader = new(encoded);
        UInt16[]? decoded = e::MessagePack.Formatters.UInt16ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNull(decoded);
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void DefaultUInt16ArrayTests(int length)
    {
        var array = new UInt16[length];
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt16ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            UInt16ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        UInt16[]? decoded = e::MessagePack.Formatters.UInt16ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void MinValueUInt16ArrayTests(int length)
    {
        var array = new UInt16[length];
        for (var index = 0; index < array.Length; index++)
        {
            array[index] = UInt16.MinValue;
        }

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt16ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            UInt16ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        UInt16[]? decoded = e::MessagePack.Formatters.UInt16ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void RandomValueUInt16ArrayTests(int length)
    {
        var array = new UInt16[length];
        var r = new Random();
        r.NextBytes(MemoryMarshal.AsBytes(array.AsSpan()));

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt16ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            UInt16ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        UInt16[]? decoded = e::MessagePack.Formatters.UInt16ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(length, decoded!.Length);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    private static readonly MessagePack.Formatters.ArrayFormatter<UInt32> UInt32ArrayFormatter = new();

    [Test]
    public void EmptyUInt32ArrayTests()
    {
        var array = Array.Empty<UInt32>();
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt32ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        MessagePackReader reader = new(encoded);
        UInt32[]? decoded = e::MessagePack.Formatters.UInt32ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(0, decoded!.Length);
    }

    [Test]
    public void NullUInt32ArrayTests()
    {
        var array = default(UInt32[]);
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt32ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);
        MessagePackReader reader = new(encoded);
        UInt32[]? decoded = e::MessagePack.Formatters.UInt32ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNull(decoded);
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void DefaultUInt32ArrayTests(int length)
    {
        var array = new UInt32[length];
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt32ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            UInt32ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        UInt32[]? decoded = e::MessagePack.Formatters.UInt32ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void MinValueUInt32ArrayTests(int length)
    {
        var array = new UInt32[length];
        for (var index = 0; index < array.Length; index++)
        {
            array[index] = UInt32.MinValue;
        }

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt32ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            UInt32ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        UInt32[]? decoded = e::MessagePack.Formatters.UInt32ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void RandomValueUInt32ArrayTests(int length)
    {
        var array = new UInt32[length];
        var r = new Random();
        r.NextBytes(MemoryMarshal.AsBytes(array.AsSpan()));

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt32ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            UInt32ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        UInt32[]? decoded = e::MessagePack.Formatters.UInt32ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(length, decoded!.Length);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    private static readonly MessagePack.Formatters.ArrayFormatter<UInt64> UInt64ArrayFormatter = new();

    [Test]
    public void EmptyUInt64ArrayTests()
    {
        var array = Array.Empty<UInt64>();
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt64ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        MessagePackReader reader = new(encoded);
        UInt64[]? decoded = e::MessagePack.Formatters.UInt64ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(0, decoded!.Length);
    }

    [Test]
    public void NullUInt64ArrayTests()
    {
        var array = default(UInt64[]);
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt64ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);
        MessagePackReader reader = new(encoded);
        UInt64[]? decoded = e::MessagePack.Formatters.UInt64ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNull(decoded);
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void DefaultUInt64ArrayTests(int length)
    {
        var array = new UInt64[length];
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt64ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            UInt64ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        UInt64[]? decoded = e::MessagePack.Formatters.UInt64ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void MinValueUInt64ArrayTests(int length)
    {
        var array = new UInt64[length];
        for (var index = 0; index < array.Length; index++)
        {
            array[index] = UInt64.MinValue;
        }

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt64ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            UInt64ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        UInt64[]? decoded = e::MessagePack.Formatters.UInt64ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void RandomValueUInt64ArrayTests(int length)
    {
        var array = new UInt64[length];
        var r = new Random();
        r.NextBytes(MemoryMarshal.AsBytes(array.AsSpan()));

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.UInt64ArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            UInt64ArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        UInt64[]? decoded = e::MessagePack.Formatters.UInt64ArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(length, decoded!.Length);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    private static readonly MessagePack.Formatters.ArrayFormatter<Single> SingleArrayFormatter = new();

    [Test]
    public void EmptySingleArrayTests()
    {
        var array = Array.Empty<Single>();
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.SingleArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        MessagePackReader reader = new(encoded);
        Single[]? decoded = e::MessagePack.Formatters.SingleArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(0, decoded!.Length);
    }

    [Test]
    public void NullSingleArrayTests()
    {
        var array = default(Single[]);
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.SingleArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);
        MessagePackReader reader = new(encoded);
        Single[]? decoded = e::MessagePack.Formatters.SingleArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNull(decoded);
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void DefaultSingleArrayTests(int length)
    {
        var array = new Single[length];
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.SingleArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            SingleArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Single[]? decoded = e::MessagePack.Formatters.SingleArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void MinValueSingleArrayTests(int length)
    {
        var array = new Single[length];
        for (var index = 0; index < array.Length; index++)
        {
            array[index] = Single.MinValue;
        }

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.SingleArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            SingleArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Single[]? decoded = e::MessagePack.Formatters.SingleArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void RandomValueSingleArrayTests(int length)
    {
        var array = new Single[length];
        for (var index = 0; index < array.Length; index++)
        {
            array[index] = Random.Shared.NextSingle();
        }

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.SingleArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            SingleArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Single[]? decoded = e::MessagePack.Formatters.SingleArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    private static readonly MessagePack.Formatters.ArrayFormatter<Double> DoubleArrayFormatter = new();

    [Test]
    public void EmptyDoubleArrayTests()
    {
        var array = Array.Empty<Double>();
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.DoubleArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        MessagePackReader reader = new(encoded);
        Double[]? decoded = e::MessagePack.Formatters.DoubleArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(0, decoded!.Length);
    }

    [Test]
    public void NullDoubleArrayTests()
    {
        var array = default(Double[]);
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.DoubleArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);
        MessagePackReader reader = new(encoded);
        Double[]? decoded = e::MessagePack.Formatters.DoubleArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNull(decoded);
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void DefaultDoubleArrayTests(int length)
    {
        var array = new Double[length];
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.DoubleArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            DoubleArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Double[]? decoded = e::MessagePack.Formatters.DoubleArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void MinValueDoubleArrayTests(int length)
    {
        var array = new Double[length];
        for (var index = 0; index < array.Length; index++)
        {
            array[index] = Double.MinValue;
        }

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.DoubleArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            DoubleArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Double[]? decoded = e::MessagePack.Formatters.DoubleArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void RandomValueDoubleArrayTests(int length)
    {
        var array = new Double[length];
        for (var index = 0; index < array.Length; index++)
        {
            array[index] = Random.Shared.NextDouble();
        }

        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.DoubleArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            DoubleArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Double[]? decoded = e::MessagePack.Formatters.DoubleArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }

    private static readonly MessagePack.Formatters.ArrayFormatter<Boolean> BooleanArrayFormatter = new();

    [Test]
    public void EmptyBooleanArrayTests()
    {
        var array = Array.Empty<Boolean>();
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.BooleanArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        MessagePackReader reader = new(encoded);
        Boolean[]? decoded = e::MessagePack.Formatters.BooleanArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(0, decoded!.Length);
    }

    [Test]
    public void NullBooleanArrayTests()
    {
        var array = default(Boolean[]);
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.BooleanArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);
        MessagePackReader reader = new(encoded);
        Boolean[]? decoded = e::MessagePack.Formatters.BooleanArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNull(decoded);
    }

    [TestCase(1)]
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(4096)]
    public void DefaultBooleanArrayTests(int length)
    {
        var array = new Boolean[length];
        ArrayBufferWriter<byte> bufferWriter = new();
        {
            MessagePackWriter writer = new(bufferWriter);
            e::MessagePack.Formatters.BooleanArrayFormatter.Instance.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var encoded = bufferWriter.WrittenMemory.ToArray();
        Assert.IsNotNull(encoded);

        bufferWriter.Clear();
        {
            MessagePackWriter writer = new(bufferWriter);
            BooleanArrayFormatter.Serialize(ref writer, array, default!);
            writer.Flush();
        }

        var defaultEncoded = bufferWriter.WrittenMemory.ToArray();
        Assert.AreEqual(encoded.Length, defaultEncoded.Length);
        Assert.That(encoded.AsSpan().SequenceEqual(defaultEncoded));
        MessagePackReader reader = new(encoded);
        Boolean[]? decoded = e::MessagePack.Formatters.BooleanArrayFormatter.Instance.Deserialize(ref reader, default!);
        Assert.IsNotNull(decoded);
        Assert.That(decoded.AsSpan().SequenceEqual(array));
    }
}

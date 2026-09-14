// Formatters for the UnityEngine value types, the wire form of MessagePack-CSharp v3's MessagePack.Unity
// (every type is an array of its members in the same order; class types write nil for null).
//
// This file is compiled twice: inside Unity against the real UnityEngine (the MessagePack.Unity package), and
// inside MessagePack.UnityShims against the shim structs (the same names in the UnityEngine namespace), so the
// server side reads and writes the same wire. Only members both sides expose are used.
//
// Language level is C# 9 (Unity's compiler), so: block namespaces, no collection expressions, no file-scoped
// types, and the `allows ref struct` constraint only under NET9_0_OR_GREATER, which Unity never defines.
// Every formatter is flat (it writes nested arrays inline rather than descending into another formatter), so none
// needs the resolver and none charges the depth budget.
#nullable enable
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using MessagePack;
using SerializerFoundation;
using UnityEngine;
using static MessagePack.MessagePackPrimitives;

namespace MessagePack.Unity
{
    // shared pieces of the wire: fixarray headers of float32 / int32 members, the exact-shape fast reads, and the
    // tolerant slow reads (v3 semantics: extra elements are skipped, missing ones stay default, nil is an error for
    // structs)
    internal static class UnityCodec
    {
        public const int Float32Size = 5; // code + 4 bytes

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingles2<TWriteBuffer>(ref TWriteBuffer buffer, float a, float b)
            where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            ref var r = ref buffer.GetReference(1 + 2 * Float32Size);
            r = (byte)(MessagePackCode.MinFixArray | 2);
            UnsafeWriteSingle(ref Unsafe.Add(ref r, 1), a);
            UnsafeWriteSingle(ref Unsafe.Add(ref r, 6), b);
            buffer.Advance(1 + 2 * Float32Size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingles3<TWriteBuffer>(ref TWriteBuffer buffer, float a, float b, float c)
            where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            ref var r = ref buffer.GetReference(1 + 3 * Float32Size);
            r = (byte)(MessagePackCode.MinFixArray | 3);
            UnsafeWriteSingle(ref Unsafe.Add(ref r, 1), a);
            UnsafeWriteSingle(ref Unsafe.Add(ref r, 6), b);
            UnsafeWriteSingle(ref Unsafe.Add(ref r, 11), c);
            buffer.Advance(1 + 3 * Float32Size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingles4<TWriteBuffer>(ref TWriteBuffer buffer, float a, float b, float c, float d)
            where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            ref var r = ref buffer.GetReference(1 + 4 * Float32Size);
            r = (byte)(MessagePackCode.MinFixArray | 4);
            UnsafeWriteSingle(ref Unsafe.Add(ref r, 1), a);
            UnsafeWriteSingle(ref Unsafe.Add(ref r, 6), b);
            UnsafeWriteSingle(ref Unsafe.Add(ref r, 11), c);
            UnsafeWriteSingle(ref Unsafe.Add(ref r, 16), d);
            buffer.Advance(1 + 4 * Float32Size);
        }

        // exact shape: fixarray(count) followed by count float32 tokens; anything else (ints, nil, other widths) takes
        // the slow read
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFixArrayOfSingles(ReadOnlySpan<byte> span, int count)
        {
            var total = 1 + count * Float32Size;
            if (span.Length < total || span[0] != (byte)(MessagePackCode.MinFixArray | count))
            {
                return false;
            }
            for (var i = 0; i < count; i++)
            {
                if (span[1 + i * Float32Size] != MessagePackCode.Float32)
                {
                    return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadSingleAt(ReadOnlySpan<byte> span, int index)
        {
            // BitConverter.Int32BitsToSingle is missing on netstandard2.0; the reinterpret through Unsafe is the same instruction
            var bits = BinaryPrimitives.ReadInt32BigEndian(span.Slice(2 + index * Float32Size));
            return Unsafe.As<int, float>(ref bits);
        }

        // the tolerant read: fills `values` from the array in order, skipping extra elements.
        // `scoped` (C# 11) tells the ref-safety analysis that the caller's stackalloc span never escapes into the
        // buffer, which only matters once TReadBuffer may itself be a ref struct (net9+); Unity's C# 9 never sees it.
        public static void ReadSingles<TReadBuffer>(
            ref TReadBuffer buffer,
#if NET9_0_OR_GREATER
            scoped
#endif
            Span<float> values)
            where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            if (buffer.TryReadNil())
            {
                ThrowNilForStruct();
            }
            var count = buffer.ReadArrayHeader();
            for (var i = 0; i < count; i++)
            {
                if (i < values.Length)
                {
                    values[i] = buffer.ReadSingle();
                }
                else
                {
                    buffer.Skip();
                }
            }
        }

        public static void ReadInt32s<TReadBuffer>(
            ref TReadBuffer buffer,
#if NET9_0_OR_GREATER
            scoped
#endif
            Span<int> values)
            where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            if (buffer.TryReadNil())
            {
                ThrowNilForStruct();
            }
            var count = buffer.ReadArrayHeader();
            for (var i = 0; i < count; i++)
            {
                if (i < values.Length)
                {
                    values[i] = buffer.ReadInt32();
                }
                else
                {
                    buffer.Skip();
                }
            }
        }

        // the element count of a nested array, or -1 for nil; extra elements past what the caller reads must be
        // skipped by the caller (see SkipRest)
        public static int ReadNestedArrayHeader<TReadBuffer>(ref TReadBuffer buffer)
            where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            return buffer.TryReadNil() ? -1 : buffer.ReadArrayHeader();
        }

        public static void SkipRest<TReadBuffer>(ref TReadBuffer buffer, int count, int consumed)
            where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
            , allows ref struct
#endif
        {
            for (var i = consumed; i < count; i++)
            {
                buffer.Skip();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNilForStruct()
        {
            throw new MessagePackSerializationException("A nil value cannot be read into a UnityEngine struct.");
        }
    }

    /// <summary>[x, y]</summary>
    public sealed class Vector2Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Vector2>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Vector2 value)
            => UnityCodec.WriteSingles2(ref buffer, value.x, value.y);

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Vector2 value)
        {
            var span = buffer.GetUnreadSpan();
            if (UnityCodec.IsFixArrayOfSingles(span, 2))
            {
                value = new Vector2(UnityCodec.ReadSingleAt(span, 0), UnityCodec.ReadSingleAt(span, 1));
                buffer.Advance(1 + 2 * UnityCodec.Float32Size);
                return;
            }
            Span<float> v = stackalloc float[2];
            UnityCodec.ReadSingles(ref buffer, v);
            value = new Vector2(v[0], v[1]);
        }
    }

    /// <summary>[x, y, z]</summary>
    public sealed class Vector3Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Vector3>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Vector3 value)
            => UnityCodec.WriteSingles3(ref buffer, value.x, value.y, value.z);

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Vector3 value)
        {
            var span = buffer.GetUnreadSpan();
            if (UnityCodec.IsFixArrayOfSingles(span, 3))
            {
                value = new Vector3(UnityCodec.ReadSingleAt(span, 0), UnityCodec.ReadSingleAt(span, 1), UnityCodec.ReadSingleAt(span, 2));
                buffer.Advance(1 + 3 * UnityCodec.Float32Size);
                return;
            }
            Span<float> v = stackalloc float[3];
            UnityCodec.ReadSingles(ref buffer, v);
            value = new Vector3(v[0], v[1], v[2]);
        }
    }

    /// <summary>[x, y, z, w]</summary>
    public sealed class Vector4Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Vector4>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Vector4 value)
            => UnityCodec.WriteSingles4(ref buffer, value.x, value.y, value.z, value.w);

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Vector4 value)
        {
            var span = buffer.GetUnreadSpan();
            if (UnityCodec.IsFixArrayOfSingles(span, 4))
            {
                value = new Vector4(UnityCodec.ReadSingleAt(span, 0), UnityCodec.ReadSingleAt(span, 1), UnityCodec.ReadSingleAt(span, 2), UnityCodec.ReadSingleAt(span, 3));
                buffer.Advance(1 + 4 * UnityCodec.Float32Size);
                return;
            }
            Span<float> v = stackalloc float[4];
            UnityCodec.ReadSingles(ref buffer, v);
            value = new Vector4(v[0], v[1], v[2], v[3]);
        }
    }

    /// <summary>[x, y, z, w]</summary>
    public sealed class QuaternionFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Quaternion>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Quaternion value)
            => UnityCodec.WriteSingles4(ref buffer, value.x, value.y, value.z, value.w);

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Quaternion value)
        {
            var span = buffer.GetUnreadSpan();
            if (UnityCodec.IsFixArrayOfSingles(span, 4))
            {
                value = new Quaternion(UnityCodec.ReadSingleAt(span, 0), UnityCodec.ReadSingleAt(span, 1), UnityCodec.ReadSingleAt(span, 2), UnityCodec.ReadSingleAt(span, 3));
                buffer.Advance(1 + 4 * UnityCodec.Float32Size);
                return;
            }
            Span<float> v = stackalloc float[4];
            UnityCodec.ReadSingles(ref buffer, v);
            value = new Quaternion(v[0], v[1], v[2], v[3]);
        }
    }

    /// <summary>[r, g, b, a]</summary>
    public sealed class ColorFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Color>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Color value)
            => UnityCodec.WriteSingles4(ref buffer, value.r, value.g, value.b, value.a);

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Color value)
        {
            var span = buffer.GetUnreadSpan();
            if (UnityCodec.IsFixArrayOfSingles(span, 4))
            {
                value = new Color(UnityCodec.ReadSingleAt(span, 0), UnityCodec.ReadSingleAt(span, 1), UnityCodec.ReadSingleAt(span, 2), UnityCodec.ReadSingleAt(span, 3));
                buffer.Advance(1 + 4 * UnityCodec.Float32Size);
                return;
            }
            Span<float> v = stackalloc float[4];
            UnityCodec.ReadSingles(ref buffer, v);
            value = new Color(v[0], v[1], v[2], v[3]);
        }
    }

    /// <summary>[x, y, width, height]</summary>
    public sealed class RectFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Rect>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Rect value)
            => UnityCodec.WriteSingles4(ref buffer, value.x, value.y, value.width, value.height);

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Rect value)
        {
            var span = buffer.GetUnreadSpan();
            if (UnityCodec.IsFixArrayOfSingles(span, 4))
            {
                value = new Rect(UnityCodec.ReadSingleAt(span, 0), UnityCodec.ReadSingleAt(span, 1), UnityCodec.ReadSingleAt(span, 2), UnityCodec.ReadSingleAt(span, 3));
                buffer.Advance(1 + 4 * UnityCodec.Float32Size);
                return;
            }
            Span<float> v = stackalloc float[4];
            UnityCodec.ReadSingles(ref buffer, v);
            value = new Rect(v[0], v[1], v[2], v[3]);
        }
    }

    /// <summary>[time, value, inTangent, outTangent] (weights are not on the wire, as in v3)</summary>
    public sealed class KeyframeFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Keyframe>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Keyframe value)
            => UnityCodec.WriteSingles4(ref buffer, value.time, value.value, value.inTangent, value.outTangent);

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Keyframe value)
        {
            var span = buffer.GetUnreadSpan();
            if (UnityCodec.IsFixArrayOfSingles(span, 4))
            {
                value = new Keyframe(UnityCodec.ReadSingleAt(span, 0), UnityCodec.ReadSingleAt(span, 1), UnityCodec.ReadSingleAt(span, 2), UnityCodec.ReadSingleAt(span, 3));
                buffer.Advance(1 + 4 * UnityCodec.Float32Size);
                return;
            }
            Span<float> v = stackalloc float[4];
            UnityCodec.ReadSingles(ref buffer, v);
            value = new Keyframe(v[0], v[1], v[2], v[3]);
        }
    }

    /// <summary>[[center x, y, z], [size x, y, z]]</summary>
    public sealed class BoundsFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Bounds>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Bounds value)
        {
            buffer.WriteArrayHeader(2);
            var center = value.center;
            UnityCodec.WriteSingles3(ref buffer, center.x, center.y, center.z);
            var size = value.size;
            UnityCodec.WriteSingles3(ref buffer, size.x, size.y, size.z);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Bounds value)
        {
            if (buffer.TryReadNil())
            {
                UnityCodec.ThrowNilForStruct();
            }
            var count = buffer.ReadArrayHeader();
            var center = default(Vector3);
            var size = default(Vector3);
            if (count > 0) center = ReadVector3(ref buffer);
            if (count > 1) size = ReadVector3(ref buffer);
            UnityCodec.SkipRest(ref buffer, count, 2);
            value = new Bounds(center, size);
        }

        static Vector3 ReadVector3(ref TReadBuffer buffer)
        {
            Span<float> v = stackalloc float[3];
            UnityCodec.ReadSingles(ref buffer, v);
            return new Vector3(v[0], v[1], v[2]);
        }
    }

    /// <summary>[m00, m10, m20, m30, m01, m11, m21, m31, m02, m12, m22, m32, m03, m13, m23, m33] (column-major field order, as in v3)</summary>
    public sealed class Matrix4x4Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Matrix4x4>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        // 16 elements is one past fixarray's 15, so the header is array16 (3 bytes)
        const int HeaderSize = 3;
        const int Size = HeaderSize + 16 * UnityCodec.Float32Size;

        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Matrix4x4 value)
        {
            ref var r = ref buffer.GetReference(Size);
            var o = UnsafeWriteArrayHeader(ref r, 16);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m00);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m10);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m20);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m30);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m01);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m11);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m21);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m31);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m02);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m12);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m22);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m32);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m03);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m13);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m23);
            o += UnsafeWriteSingle(ref Unsafe.Add(ref r, o), value.m33);
            buffer.Advance(o);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Matrix4x4 value)
        {
            Span<float> v = stackalloc float[16];
            var span = buffer.GetUnreadSpan();
            if (IsArray16OfSingles(span))
            {
                for (var i = 0; i < 16; i++)
                {
                    var bits = BinaryPrimitives.ReadInt32BigEndian(span.Slice(HeaderSize + 1 + i * UnityCodec.Float32Size));
                    v[i] = Unsafe.As<int, float>(ref bits);
                }
                buffer.Advance(Size);
            }
            else
            {
                UnityCodec.ReadSingles(ref buffer, v);
            }
            var m = default(Matrix4x4);
            AssignColumnMajor(ref m, v);
            value = m;
        }

        static bool IsArray16OfSingles(ReadOnlySpan<byte> span)
        {
            if (span.Length < Size || span[0] != MessagePackCode.Array16 || span[1] != 0 || span[2] != 16)
            {
                return false;
            }
            for (var i = 0; i < 16; i++)
            {
                if (span[HeaderSize + i * UnityCodec.Float32Size] != MessagePackCode.Float32)
                {
                    return false;
                }
            }
            return true;
        }

        static void AssignColumnMajor(ref Matrix4x4 m, ReadOnlySpan<float> v)
        {
            m.m00 = v[0]; m.m10 = v[1]; m.m20 = v[2]; m.m30 = v[3];
            m.m01 = v[4]; m.m11 = v[5]; m.m21 = v[6]; m.m31 = v[7];
            m.m02 = v[8]; m.m12 = v[9]; m.m22 = v[10]; m.m32 = v[11];
            m.m03 = v[12]; m.m13 = v[13]; m.m23 = v[14]; m.m33 = v[15];
        }
    }

    /// <summary>[r, g, b, a] as uint8</summary>
    public sealed class Color32Formatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Color32>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Color32 value)
        {
            buffer.WriteArrayHeader(4);
            buffer.WriteByte(value.r);
            buffer.WriteByte(value.g);
            buffer.WriteByte(value.b);
            buffer.WriteByte(value.a);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Color32 value)
        {
            if (buffer.TryReadNil())
            {
                UnityCodec.ThrowNilForStruct();
            }
            var count = buffer.ReadArrayHeader();
            byte r = 0, g = 0, b = 0, a = 0;
            if (count > 0) r = buffer.ReadByte();
            if (count > 1) g = buffer.ReadByte();
            if (count > 2) b = buffer.ReadByte();
            if (count > 3) a = buffer.ReadByte();
            UnityCodec.SkipRest(ref buffer, count, 4);
            value = new Color32(r, g, b, a);
        }
    }

    /// <summary>[value]</summary>
    public sealed class LayerMaskFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, LayerMask>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, LayerMask value)
        {
            buffer.WriteArrayHeader(1);
            buffer.WriteInt32(value.value);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref LayerMask value)
        {
            Span<int> v = stackalloc int[1];
            UnityCodec.ReadInt32s(ref buffer, v);
            var mask = default(LayerMask);
            mask.value = v[0];
            value = mask;
        }
    }

    /// <summary>[x, y]</summary>
    public sealed class Vector2IntFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Vector2Int>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Vector2Int value)
        {
            buffer.WriteArrayHeader(2);
            buffer.WriteInt32(value.x);
            buffer.WriteInt32(value.y);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Vector2Int value)
        {
            Span<int> v = stackalloc int[2];
            UnityCodec.ReadInt32s(ref buffer, v);
            value = new Vector2Int(v[0], v[1]);
        }
    }

    /// <summary>[x, y, z]</summary>
    public sealed class Vector3IntFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Vector3Int>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Vector3Int value)
        {
            buffer.WriteArrayHeader(3);
            buffer.WriteInt32(value.x);
            buffer.WriteInt32(value.y);
            buffer.WriteInt32(value.z);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Vector3Int value)
        {
            Span<int> v = stackalloc int[3];
            UnityCodec.ReadInt32s(ref buffer, v);
            value = new Vector3Int(v[0], v[1], v[2]);
        }
    }

    /// <summary>[start, length]</summary>
    public sealed class RangeIntFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, RangeInt>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, RangeInt value)
        {
            buffer.WriteArrayHeader(2);
            buffer.WriteInt32(value.start);
            buffer.WriteInt32(value.length);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref RangeInt value)
        {
            Span<int> v = stackalloc int[2];
            UnityCodec.ReadInt32s(ref buffer, v);
            value = new RangeInt(v[0], v[1]);
        }
    }

    /// <summary>[x, y, width, height]</summary>
    public sealed class RectIntFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, RectInt>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, RectInt value)
        {
            buffer.WriteArrayHeader(4);
            buffer.WriteInt32(value.x);
            buffer.WriteInt32(value.y);
            buffer.WriteInt32(value.width);
            buffer.WriteInt32(value.height);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref RectInt value)
        {
            Span<int> v = stackalloc int[4];
            UnityCodec.ReadInt32s(ref buffer, v);
            value = new RectInt(v[0], v[1], v[2], v[3]);
        }
    }

    /// <summary>[[position x, y, z], [size x, y, z]]</summary>
    public sealed class BoundsIntFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, BoundsInt>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, BoundsInt value)
        {
            buffer.WriteArrayHeader(2);
            WriteVector3Int(ref buffer, value.position);
            WriteVector3Int(ref buffer, value.size);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref BoundsInt value)
        {
            if (buffer.TryReadNil())
            {
                UnityCodec.ThrowNilForStruct();
            }
            var count = buffer.ReadArrayHeader();
            var position = default(Vector3Int);
            var size = default(Vector3Int);
            if (count > 0) position = ReadVector3Int(ref buffer);
            if (count > 1) size = ReadVector3Int(ref buffer);
            UnityCodec.SkipRest(ref buffer, count, 2);
            value = new BoundsInt(position, size);
        }

        static void WriteVector3Int(ref TWriteBuffer buffer, Vector3Int v)
        {
            buffer.WriteArrayHeader(3);
            buffer.WriteInt32(v.x);
            buffer.WriteInt32(v.y);
            buffer.WriteInt32(v.z);
        }

        static Vector3Int ReadVector3Int(ref TReadBuffer buffer)
        {
            Span<int> v = stackalloc int[3];
            UnityCodec.ReadInt32s(ref buffer, v);
            return new Vector3Int(v[0], v[1], v[2]);
        }
    }

    /// <summary>[[color r, g, b, a], time]</summary>
    public sealed class GradientColorKeyFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, GradientColorKey>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, GradientColorKey value)
        {
            buffer.WriteArrayHeader(2);
            var color = value.color;
            UnityCodec.WriteSingles4(ref buffer, color.r, color.g, color.b, color.a);
            buffer.WriteSingle(value.time);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref GradientColorKey value)
        {
            if (buffer.TryReadNil())
            {
                UnityCodec.ThrowNilForStruct();
            }
            var count = buffer.ReadArrayHeader();
            var color = default(Color);
            var time = 0f;
            if (count > 0)
            {
                Span<float> v = stackalloc float[4];
                UnityCodec.ReadSingles(ref buffer, v);
                color = new Color(v[0], v[1], v[2], v[3]);
            }
            if (count > 1) time = buffer.ReadSingle();
            UnityCodec.SkipRest(ref buffer, count, 2);
            value = new GradientColorKey(color, time);
        }
    }

    /// <summary>[alpha, time]</summary>
    public sealed class GradientAlphaKeyFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, GradientAlphaKey>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, GradientAlphaKey value)
            => UnityCodec.WriteSingles2(ref buffer, value.alpha, value.time);

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref GradientAlphaKey value)
        {
            var span = buffer.GetUnreadSpan();
            if (UnityCodec.IsFixArrayOfSingles(span, 2))
            {
                value = new GradientAlphaKey(UnityCodec.ReadSingleAt(span, 0), UnityCodec.ReadSingleAt(span, 1));
                buffer.Advance(1 + 2 * UnityCodec.Float32Size);
                return;
            }
            Span<float> v = stackalloc float[2];
            UnityCodec.ReadSingles(ref buffer, v);
            value = new GradientAlphaKey(v[0], v[1]);
        }
    }

    /// <summary>nil, or [[keyframe]..., postWrapMode, preWrapMode] (post before pre, as in v3)</summary>
    public sealed class AnimationCurveFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, AnimationCurve?>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, AnimationCurve? value)
        {
            if (value == null)
            {
                buffer.WriteNil();
                return;
            }
            buffer.WriteArrayHeader(3);
            var keys = value.keys;
            if (keys == null)
            {
                buffer.WriteNil();
            }
            else
            {
                buffer.WriteArrayHeader(keys.Length);
                for (var i = 0; i < keys.Length; i++)
                {
                    var k = keys[i];
                    UnityCodec.WriteSingles4(ref buffer, k.time, k.value, k.inTangent, k.outTangent);
                }
            }
            buffer.WriteInt32((int)value.postWrapMode);
            buffer.WriteInt32((int)value.preWrapMode);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref AnimationCurve? value)
        {
            if (buffer.TryReadNil())
            {
                value = null;
                return;
            }
            var count = buffer.ReadArrayHeader();
            Keyframe[]? keys = null;
            var postWrapMode = default(WrapMode);
            var preWrapMode = default(WrapMode);
            if (count > 0)
            {
                var keyCount = UnityCodec.ReadNestedArrayHeader(ref buffer);
                if (keyCount >= 0)
                {
                    keys = new Keyframe[keyCount];
                    Span<float> v = stackalloc float[4];
                    for (var i = 0; i < keyCount; i++)
                    {
                        v.Clear();
                        UnityCodec.ReadSingles(ref buffer, v);
                        keys[i] = new Keyframe(v[0], v[1], v[2], v[3]);
                    }
                }
            }
            if (count > 1) postWrapMode = (WrapMode)buffer.ReadInt32();
            if (count > 2) preWrapMode = (WrapMode)buffer.ReadInt32();
            UnityCodec.SkipRest(ref buffer, count, 3);
            var curve = keys == null ? new AnimationCurve() : new AnimationCurve(keys);
            curve.postWrapMode = postWrapMode;
            curve.preWrapMode = preWrapMode;
            value = curve;
        }
    }

    /// <summary>nil, or [[colorKey]..., [alphaKey]..., mode]</summary>
    public sealed class GradientFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Gradient?>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Gradient? value)
        {
            if (value == null)
            {
                buffer.WriteNil();
                return;
            }
            buffer.WriteArrayHeader(3);
            var colorKeys = value.colorKeys;
            if (colorKeys == null)
            {
                buffer.WriteNil();
            }
            else
            {
                buffer.WriteArrayHeader(colorKeys.Length);
                for (var i = 0; i < colorKeys.Length; i++)
                {
                    var key = colorKeys[i];
                    buffer.WriteArrayHeader(2);
                    var color = key.color;
                    UnityCodec.WriteSingles4(ref buffer, color.r, color.g, color.b, color.a);
                    buffer.WriteSingle(key.time);
                }
            }
            var alphaKeys = value.alphaKeys;
            if (alphaKeys == null)
            {
                buffer.WriteNil();
            }
            else
            {
                buffer.WriteArrayHeader(alphaKeys.Length);
                for (var i = 0; i < alphaKeys.Length; i++)
                {
                    var key = alphaKeys[i];
                    UnityCodec.WriteSingles2(ref buffer, key.alpha, key.time);
                }
            }
            buffer.WriteInt32((int)value.mode);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Gradient? value)
        {
            if (buffer.TryReadNil())
            {
                value = null;
                return;
            }
            var count = buffer.ReadArrayHeader();
            GradientColorKey[]? colorKeys = null;
            GradientAlphaKey[]? alphaKeys = null;
            var mode = default(GradientMode);
            if (count > 0)
            {
                var n = UnityCodec.ReadNestedArrayHeader(ref buffer);
                if (n >= 0)
                {
                    colorKeys = new GradientColorKey[n];
                    Span<float> v = stackalloc float[4];
                    for (var i = 0; i < n; i++)
                    {
                        if (buffer.TryReadNil())
                        {
                            UnityCodec.ThrowNilForStruct();
                        }
                        var keyCount = buffer.ReadArrayHeader();
                        var color = default(Color);
                        var time = 0f;
                        if (keyCount > 0)
                        {
                            v.Clear();
                            UnityCodec.ReadSingles(ref buffer, v);
                            color = new Color(v[0], v[1], v[2], v[3]);
                        }
                        if (keyCount > 1) time = buffer.ReadSingle();
                        UnityCodec.SkipRest(ref buffer, keyCount, 2);
                        colorKeys[i] = new GradientColorKey(color, time);
                    }
                }
            }
            if (count > 1)
            {
                var n = UnityCodec.ReadNestedArrayHeader(ref buffer);
                if (n >= 0)
                {
                    alphaKeys = new GradientAlphaKey[n];
                    Span<float> v = stackalloc float[2];
                    for (var i = 0; i < n; i++)
                    {
                        v.Clear();
                        UnityCodec.ReadSingles(ref buffer, v);
                        alphaKeys[i] = new GradientAlphaKey(v[0], v[1]);
                    }
                }
            }
            if (count > 2) mode = (GradientMode)buffer.ReadInt32();
            UnityCodec.SkipRest(ref buffer, count, 3);
            var gradient = new Gradient();
            if (colorKeys != null) gradient.colorKeys = colorKeys;
            if (alphaKeys != null) gradient.alphaKeys = alphaKeys;
            gradient.mode = mode;
            value = gradient;
        }
    }

    /// <summary>nil, or [left, right, top, bottom]</summary>
    public sealed class RectOffsetFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, RectOffset?>
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        public void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, RectOffset? value)
        {
            if (value == null)
            {
                buffer.WriteNil();
                return;
            }
            buffer.WriteArrayHeader(4);
            buffer.WriteInt32(value.left);
            buffer.WriteInt32(value.right);
            buffer.WriteInt32(value.top);
            buffer.WriteInt32(value.bottom);
        }

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref RectOffset? value)
        {
            if (buffer.TryReadNil())
            {
                value = null;
                return;
            }
            var count = buffer.ReadArrayHeader();
            int left = 0, right = 0, top = 0, bottom = 0;
            if (count > 0) left = buffer.ReadInt32();
            if (count > 1) right = buffer.ReadInt32();
            if (count > 2) top = buffer.ReadInt32();
            if (count > 3) bottom = buffer.ReadInt32();
            UnityCodec.SkipRest(ref buffer, count, 4);
            value = new RectOffset(left, right, top, bottom);
        }
    }
}

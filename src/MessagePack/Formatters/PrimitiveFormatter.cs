// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MessagePack.Internal;

#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    public sealed class Int16Formatter : IMessagePackFormatter<Int16>
    {
        public static readonly Int16Formatter Instance = new Int16Formatter();

        private Int16Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int16 value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public Int16 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadInt16();
        }
    }

    public sealed class NullableInt16Formatter : IMessagePackFormatter<Int16?>
    {
        public static readonly NullableInt16Formatter Instance = new NullableInt16Formatter();

        private NullableInt16Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int16? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public Int16? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadInt16();
            }
        }
    }

    public sealed class Int16ArrayFormatter : IMessagePackFormatter<Int16[]?>
    {
        public static readonly Int16ArrayFormatter Instance = new Int16ArrayFormatter();

        private Int16ArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int16[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                if (value.Length > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
#else
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref value[0], value.Length);
#endif
                }
            }
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
                return Array.Empty<Int16>();
            }

            var array = new Int16[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadInt16();
            }

            return array;
        }
    }

#if NET8_0_OR_GREATER
    public sealed class Int16ListFormatter : IMessagePackFormatter<List<Int16>?>
    {
        public static readonly Int16ListFormatter Instance = new Int16ListFormatter();

        private Int16ListFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, List<Int16>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Count);
                if (value.Count > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(value)), value.Count);
                }
            }
        }

        public List<Int16>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            var list = new List<Int16>(len);
            CollectionsMarshal.SetCount(list, len);
            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < len; i++)
            {
                span[i] = reader.ReadInt16();
            }

            return list;
        }
    }

#endif

    public sealed class Int32Formatter : IMessagePackFormatter<Int32>
    {
        public static readonly Int32Formatter Instance = new Int32Formatter();

        private Int32Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int32 value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public Int32 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadInt32();
        }
    }

    public sealed class NullableInt32Formatter : IMessagePackFormatter<Int32?>
    {
        public static readonly NullableInt32Formatter Instance = new NullableInt32Formatter();

        private NullableInt32Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int32? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public Int32? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadInt32();
            }
        }
    }

    public sealed class Int32ArrayFormatter : IMessagePackFormatter<Int32[]?>
    {
        public static readonly Int32ArrayFormatter Instance = new Int32ArrayFormatter();

        private Int32ArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int32[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                if (value.Length > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
#else
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref value[0], value.Length);
#endif
                }
            }
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
                return Array.Empty<Int32>();
            }

            var array = new Int32[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadInt32();
            }

            return array;
        }
    }

#if NET8_0_OR_GREATER
    public sealed class Int32ListFormatter : IMessagePackFormatter<List<Int32>?>
    {
        public static readonly Int32ListFormatter Instance = new Int32ListFormatter();

        private Int32ListFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, List<Int32>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Count);
                if (value.Count > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(value)), value.Count);
                }
            }
        }

        public List<Int32>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            var list = new List<Int32>(len);
            CollectionsMarshal.SetCount(list, len);
            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < len; i++)
            {
                span[i] = reader.ReadInt32();
            }

            return list;
        }
    }

#endif

    public sealed class Int64Formatter : IMessagePackFormatter<Int64>
    {
        public static readonly Int64Formatter Instance = new Int64Formatter();

        private Int64Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int64 value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public Int64 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadInt64();
        }
    }

    public sealed class NullableInt64Formatter : IMessagePackFormatter<Int64?>
    {
        public static readonly NullableInt64Formatter Instance = new NullableInt64Formatter();

        private NullableInt64Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int64? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public Int64? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadInt64();
            }
        }
    }

    public sealed class Int64ArrayFormatter : IMessagePackFormatter<Int64[]?>
    {
        public static readonly Int64ArrayFormatter Instance = new Int64ArrayFormatter();

        private Int64ArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Int64[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                if (value.Length > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
#else
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref value[0], value.Length);
#endif
                }
            }
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
                return Array.Empty<Int64>();
            }

            var array = new Int64[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadInt64();
            }

            return array;
        }
    }

#if NET8_0_OR_GREATER
    public sealed class Int64ListFormatter : IMessagePackFormatter<List<Int64>?>
    {
        public static readonly Int64ListFormatter Instance = new Int64ListFormatter();

        private Int64ListFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, List<Int64>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Count);
                if (value.Count > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(value)), value.Count);
                }
            }
        }

        public List<Int64>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            var list = new List<Int64>(len);
            CollectionsMarshal.SetCount(list, len);
            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < len; i++)
            {
                span[i] = reader.ReadInt64();
            }

            return list;
        }
    }

#endif

    public sealed class UInt16Formatter : IMessagePackFormatter<UInt16>
    {
        public static readonly UInt16Formatter Instance = new UInt16Formatter();

        private UInt16Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt16 value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public UInt16 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadUInt16();
        }
    }

    public sealed class NullableUInt16Formatter : IMessagePackFormatter<UInt16?>
    {
        public static readonly NullableUInt16Formatter Instance = new NullableUInt16Formatter();

        private NullableUInt16Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt16? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public UInt16? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadUInt16();
            }
        }
    }

    public sealed class UInt16ArrayFormatter : IMessagePackFormatter<UInt16[]?>
    {
        public static readonly UInt16ArrayFormatter Instance = new UInt16ArrayFormatter();

        private UInt16ArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt16[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                if (value.Length > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
#else
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref value[0], value.Length);
#endif
                }
            }
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
                return Array.Empty<UInt16>();
            }

            var array = new UInt16[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadUInt16();
            }

            return array;
        }
    }

#if NET8_0_OR_GREATER
    public sealed class UInt16ListFormatter : IMessagePackFormatter<List<UInt16>?>
    {
        public static readonly UInt16ListFormatter Instance = new UInt16ListFormatter();

        private UInt16ListFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, List<UInt16>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Count);
                if (value.Count > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(value)), value.Count);
                }
            }
        }

        public List<UInt16>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            var list = new List<UInt16>(len);
            CollectionsMarshal.SetCount(list, len);
            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < len; i++)
            {
                span[i] = reader.ReadUInt16();
            }

            return list;
        }
    }

#endif

    public sealed class UInt32Formatter : IMessagePackFormatter<UInt32>
    {
        public static readonly UInt32Formatter Instance = new UInt32Formatter();

        private UInt32Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt32 value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public UInt32 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadUInt32();
        }
    }

    public sealed class NullableUInt32Formatter : IMessagePackFormatter<UInt32?>
    {
        public static readonly NullableUInt32Formatter Instance = new NullableUInt32Formatter();

        private NullableUInt32Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt32? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public UInt32? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadUInt32();
            }
        }
    }

    public sealed class UInt32ArrayFormatter : IMessagePackFormatter<UInt32[]?>
    {
        public static readonly UInt32ArrayFormatter Instance = new UInt32ArrayFormatter();

        private UInt32ArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt32[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                if (value.Length > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
#else
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref value[0], value.Length);
#endif
                }
            }
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
                return Array.Empty<UInt32>();
            }

            var array = new UInt32[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadUInt32();
            }

            return array;
        }
    }

#if NET8_0_OR_GREATER
    public sealed class UInt32ListFormatter : IMessagePackFormatter<List<UInt32>?>
    {
        public static readonly UInt32ListFormatter Instance = new UInt32ListFormatter();

        private UInt32ListFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, List<UInt32>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Count);
                if (value.Count > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(value)), value.Count);
                }
            }
        }

        public List<UInt32>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            var list = new List<UInt32>(len);
            CollectionsMarshal.SetCount(list, len);
            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < len; i++)
            {
                span[i] = reader.ReadUInt32();
            }

            return list;
        }
    }

#endif

    public sealed class UInt64Formatter : IMessagePackFormatter<UInt64>
    {
        public static readonly UInt64Formatter Instance = new UInt64Formatter();

        private UInt64Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt64 value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public UInt64 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadUInt64();
        }
    }

    public sealed class NullableUInt64Formatter : IMessagePackFormatter<UInt64?>
    {
        public static readonly NullableUInt64Formatter Instance = new NullableUInt64Formatter();

        private NullableUInt64Formatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt64? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public UInt64? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadUInt64();
            }
        }
    }

    public sealed class UInt64ArrayFormatter : IMessagePackFormatter<UInt64[]?>
    {
        public static readonly UInt64ArrayFormatter Instance = new UInt64ArrayFormatter();

        private UInt64ArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, UInt64[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                if (value.Length > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
#else
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref value[0], value.Length);
#endif
                }
            }
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
                return Array.Empty<UInt64>();
            }

            var array = new UInt64[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadUInt64();
            }

            return array;
        }
    }

#if NET8_0_OR_GREATER
    public sealed class UInt64ListFormatter : IMessagePackFormatter<List<UInt64>?>
    {
        public static readonly UInt64ListFormatter Instance = new UInt64ListFormatter();

        private UInt64ListFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, List<UInt64>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Count);
                if (value.Count > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(value)), value.Count);
                }
            }
        }

        public List<UInt64>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            var list = new List<UInt64>(len);
            CollectionsMarshal.SetCount(list, len);
            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < len; i++)
            {
                span[i] = reader.ReadUInt64();
            }

            return list;
        }
    }

#endif

    public sealed class SingleFormatter : IMessagePackFormatter<Single>
    {
        public static readonly SingleFormatter Instance = new SingleFormatter();

        private SingleFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Single value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public Single Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadSingle();
        }
    }

    public sealed class NullableSingleFormatter : IMessagePackFormatter<Single?>
    {
        public static readonly NullableSingleFormatter Instance = new NullableSingleFormatter();

        private NullableSingleFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Single? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public Single? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadSingle();
            }
        }
    }

    public sealed class SingleArrayFormatter : IMessagePackFormatter<Single[]?>
    {
        public static readonly SingleArrayFormatter Instance = new SingleArrayFormatter();

        private SingleArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Single[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                if (value.Length > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
#else
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref value[0], value.Length);
#endif
                }
            }
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
                return Array.Empty<Single>();
            }

            var array = new Single[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadSingle();
            }

            return array;
        }
    }

#if NET8_0_OR_GREATER
    public sealed class SingleListFormatter : IMessagePackFormatter<List<Single>?>
    {
        public static readonly SingleListFormatter Instance = new SingleListFormatter();

        private SingleListFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, List<Single>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Count);
                if (value.Count > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(value)), value.Count);
                }
            }
        }

        public List<Single>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            var list = new List<Single>(len);
            CollectionsMarshal.SetCount(list, len);
            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < len; i++)
            {
                span[i] = reader.ReadSingle();
            }

            return list;
        }
    }

#endif

    public sealed class DoubleFormatter : IMessagePackFormatter<Double>
    {
        public static readonly DoubleFormatter Instance = new DoubleFormatter();

        private DoubleFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Double value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public Double Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadDouble();
        }
    }

    public sealed class NullableDoubleFormatter : IMessagePackFormatter<Double?>
    {
        public static readonly NullableDoubleFormatter Instance = new NullableDoubleFormatter();

        private NullableDoubleFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Double? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public Double? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadDouble();
            }
        }
    }

    public sealed class DoubleArrayFormatter : IMessagePackFormatter<Double[]?>
    {
        public static readonly DoubleArrayFormatter Instance = new DoubleArrayFormatter();

        private DoubleArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Double[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                if (value.Length > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
#else
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref value[0], value.Length);
#endif
                }
            }
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
                return Array.Empty<Double>();
            }

            var array = new Double[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadDouble();
            }

            return array;
        }
    }

#if NET8_0_OR_GREATER
    public sealed class DoubleListFormatter : IMessagePackFormatter<List<Double>?>
    {
        public static readonly DoubleListFormatter Instance = new DoubleListFormatter();

        private DoubleListFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, List<Double>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Count);
                if (value.Count > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(value)), value.Count);
                }
            }
        }

        public List<Double>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            var list = new List<Double>(len);
            CollectionsMarshal.SetCount(list, len);
            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < len; i++)
            {
                span[i] = reader.ReadDouble();
            }

            return list;
        }
    }

#endif

    public sealed class BooleanFormatter : IMessagePackFormatter<Boolean>
    {
        public static readonly BooleanFormatter Instance = new BooleanFormatter();

        private BooleanFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Boolean value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public Boolean Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadBoolean();
        }
    }

    public sealed class NullableBooleanFormatter : IMessagePackFormatter<Boolean?>
    {
        public static readonly NullableBooleanFormatter Instance = new NullableBooleanFormatter();

        private NullableBooleanFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Boolean? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public Boolean? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadBoolean();
            }
        }
    }

    public sealed class BooleanArrayFormatter : IMessagePackFormatter<Boolean[]?>
    {
        public static readonly BooleanArrayFormatter Instance = new BooleanArrayFormatter();

        private BooleanArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Boolean[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                if (value.Length > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
#else
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref value[0], value.Length);
#endif
                }
            }
        }

        public Boolean[]? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            var array = new Boolean[len];
#if NET6_0_OR_GREATER
            ref var output = ref MemoryMarshal.GetArrayDataReference(array);
#else
            ref var output = ref array[0];
#endif

            var inputOffset = 0;
            var sequence = reader.ReadRaw(len);
            foreach (var memory in sequence)
            {
                var inputSpan = memory.Span;
                if (inputSpan.IsEmpty)
                {
                    continue;
                }

#if NET6_0_OR_GREATER
                var errorIndex = UnsafeRefDeserializeHelper.Deserialize(ref MemoryMarshal.GetReference(inputSpan), inputSpan.Length, ref Unsafe.Add(ref output, inputOffset));
#else
                var errorIndex = UnsafeRefDeserializeHelper.Deserialize(ref Unsafe.AsRef(in inputSpan[0]), inputSpan.Length, ref Unsafe.Add(ref output, inputOffset));
#endif
                if (errorIndex >= 0)
                {
                    throw new MessagePackSerializationException($"Unexpected msgpack code {inputSpan[errorIndex]} ({MessagePackCode.ToFormatName(inputSpan[errorIndex])}) at {errorIndex + inputOffset} encountered.");
                }

                inputOffset += inputSpan.Length;
            }

            return array;
        }
    }

#if NET8_0_OR_GREATER
    public sealed class BooleanListFormatter : IMessagePackFormatter<List<Boolean>?>
    {
        public static readonly BooleanListFormatter Instance = new BooleanListFormatter();

        private BooleanListFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, List<Boolean>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Count);
                if (value.Count > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(value)), value.Count);
                }
            }
        }

        public List<Boolean>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            var list = new List<Boolean>(len);
            CollectionsMarshal.SetCount(list, len);
            ref var output = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(list));
            var inputOffset = 0;
            var sequence = reader.ReadRaw(len);
            foreach (var memory in sequence)
            {
                var inputSpan = memory.Span;
                if (inputSpan.IsEmpty)
                {
                    continue;
                }

                var errorIndex = UnsafeRefDeserializeHelper.Deserialize(ref MemoryMarshal.GetReference(inputSpan), inputSpan.Length, ref Unsafe.Add(ref output, inputOffset));
                if (errorIndex >= 0)
                {
                    throw new MessagePackSerializationException($"Unexpected msgpack code {inputSpan[errorIndex]} ({MessagePackCode.ToFormatName(inputSpan[errorIndex])}) at {errorIndex + inputOffset} encountered.");
                }

                inputOffset += inputSpan.Length;
            }

            return list;
        }
    }
#endif

    public sealed class ByteFormatter : IMessagePackFormatter<Byte>
    {
        public static readonly ByteFormatter Instance = new ByteFormatter();

        private ByteFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Byte value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public Byte Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadByte();
        }
    }

    public sealed class NullableByteFormatter : IMessagePackFormatter<Byte?>
    {
        public static readonly NullableByteFormatter Instance = new NullableByteFormatter();

        private NullableByteFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Byte? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public Byte? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadByte();
            }
        }
    }

    public sealed class SByteFormatter : IMessagePackFormatter<SByte>
    {
        public static readonly SByteFormatter Instance = new SByteFormatter();

        private SByteFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, SByte value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public SByte Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadSByte();
        }
    }

    public sealed class NullableSByteFormatter : IMessagePackFormatter<SByte?>
    {
        public static readonly NullableSByteFormatter Instance = new NullableSByteFormatter();

        private NullableSByteFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, SByte? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public SByte? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadSByte();
            }
        }
    }

    public sealed class SByteArrayFormatter : IMessagePackFormatter<SByte[]?>
    {
        public static readonly SByteArrayFormatter Instance = new SByteArrayFormatter();

        private SByteArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, SByte[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                if (value.Length > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
#else
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref value[0], value.Length);
#endif
                }
            }
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
                return Array.Empty<SByte>();
            }

            var array = new SByte[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadSByte();
            }

            return array;
        }
    }

#if NET8_0_OR_GREATER
    public sealed class SByteListFormatter : IMessagePackFormatter<List<SByte>?>
    {
        public static readonly SByteListFormatter Instance = new SByteListFormatter();

        private SByteListFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, List<SByte>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Count);
                if (value.Count > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(value)), value.Count);
                }
            }
        }

        public List<SByte>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            var list = new List<SByte>(len);
            CollectionsMarshal.SetCount(list, len);
            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < len; i++)
            {
                span[i] = reader.ReadSByte();
            }

            return list;
        }
    }

#endif

    public sealed class CharFormatter : IMessagePackFormatter<Char>
    {
        public static readonly CharFormatter Instance = new CharFormatter();

        private CharFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Char value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public Char Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadChar();
        }
    }

    public sealed class NullableCharFormatter : IMessagePackFormatter<Char?>
    {
        public static readonly NullableCharFormatter Instance = new NullableCharFormatter();

        private NullableCharFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Char? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public Char? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadChar();
            }
        }
    }

    public sealed class CharArrayFormatter : IMessagePackFormatter<Char[]?>
    {
        public static readonly CharArrayFormatter Instance = new CharArrayFormatter();

        private CharArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Char[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                if (value.Length > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
#if NET6_0_OR_GREATER
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetArrayDataReference(value), value.Length);
#else
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref value[0], value.Length);
#endif
                }
            }
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
                return Array.Empty<Char>();
            }

            var array = new Char[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadChar();
            }

            return array;
        }
    }

#if NET8_0_OR_GREATER
    public sealed class CharListFormatter : IMessagePackFormatter<List<Char>?>
    {
        public static readonly CharListFormatter Instance = new CharListFormatter();

        private CharListFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, List<Char>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Count);
                if (value.Count > 0)
                {
                    writer.CancellationToken.ThrowIfCancellationRequested();
                    UnsafeRefSerializeHelper.Serialize(ref writer, ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(value)), value.Count);
                }
            }
        }

        public List<Char>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            var list = new List<Char>(len);
            CollectionsMarshal.SetCount(list, len);
            var span = CollectionsMarshal.AsSpan(list);
            for (int i = 0; i < len; i++)
            {
                span[i] = reader.ReadChar();
            }

            return list;
        }
    }

#endif

    public sealed class DateTimeFormatter : IMessagePackFormatter<DateTime>
    {
        public static readonly DateTimeFormatter Instance = new DateTimeFormatter();

        private DateTimeFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, DateTime value, MessagePackSerializerOptions options)
        {
            writer.Write(value);
        }

        public DateTime Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            return reader.ReadDateTime();
        }
    }

    public sealed class NullableDateTimeFormatter : IMessagePackFormatter<DateTime?>
    {
        public static readonly NullableDateTimeFormatter Instance = new NullableDateTimeFormatter();

        private NullableDateTimeFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, DateTime? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(value.Value);
            }
        }

        public DateTime? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }
            else
            {
                return reader.ReadDateTime();
            }
        }
    }

    public sealed class DateTimeArrayFormatter : IMessagePackFormatter<DateTime[]?>
    {
        public static readonly DateTimeArrayFormatter Instance = new DateTimeArrayFormatter();

        private DateTimeArrayFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, DateTime[]? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    writer.Write(value[i]);
                }
            }
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
                return Array.Empty<DateTime>();
            }

            var array = new DateTime[len];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = reader.ReadDateTime();
            }

            return array;
        }
    }
}

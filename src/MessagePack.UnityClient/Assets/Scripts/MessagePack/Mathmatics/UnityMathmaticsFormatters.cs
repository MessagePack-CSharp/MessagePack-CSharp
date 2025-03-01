// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#nullable enable
#pragma warning disable SA1513
#pragma warning disable SA1516

using System;
using System.Buffers;
using MessagePack;
using MessagePack.Formatters;

#pragma warning disable SA1312 // variable naming
#pragma warning disable SA1402 // one type per file
#pragma warning disable SA1649 // file name matches type name

namespace MessagePack.Unity
{
    public sealed class Bool2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::Unity.Mathematics.bool2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(bool);
            var y = default(bool);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadBoolean();
                        break;
                    case 1:
                        y = reader.ReadBoolean();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::Unity.Mathematics.bool2(x, y);
            return result;
        }
    }

    public sealed class Bool3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.bool3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.bool3 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::Unity.Mathematics.bool3 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(bool);
            var y = default(bool);
            var z = default(bool);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadBoolean();
                        break;
                    case 1:
                        y = reader.ReadBoolean();
                        break;
                    case 2:
                        z = reader.ReadBoolean();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::Unity.Mathematics.bool3(x, y, z);
            return result;
        }
    }

    public sealed class Double2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::Unity.Mathematics.double2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(double);
            var y = default(double);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadDouble();
                        break;
                    case 1:
                        y = reader.ReadDouble();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::Unity.Mathematics.double2(x, y);
            return result;
        }
    }

    public sealed class Double3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.double3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.double3 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::Unity.Mathematics.double3 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(double);
            var y = default(double);
            var z = default(double);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadDouble();
                        break;
                    case 1:
                        y = reader.ReadDouble();
                        break;
                    case 2:
                        z = reader.ReadDouble();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::Unity.Mathematics.double3(x, y, z);
            return result;
        }
    }

    public sealed class Float2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::Unity.Mathematics.float2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(float);
            var y = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::Unity.Mathematics.float2(x, y);
            return result;
        }
    }

    public sealed class Float3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.float3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.float3 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::Unity.Mathematics.float3 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(float);
            var y = default(float);
            var z = default(float);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadSingle();
                        break;
                    case 1:
                        y = reader.ReadSingle();
                        break;
                    case 2:
                        z = reader.ReadSingle();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::Unity.Mathematics.float3(x, y, z);
            return result;
        }
    }

    public sealed class Int2Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int2>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int2 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);
            writer.Write(value.x);
            writer.Write(value.y);
        }

        public global::Unity.Mathematics.int2 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(int);
            var y = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadInt32();
                        break;
                    case 1:
                        y = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::Unity.Mathematics.int2(x, y);
            return result;
        }
    }

    public sealed class Int3Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Unity.Mathematics.int3>
    {
        public void Serialize(ref MessagePackWriter writer, global::Unity.Mathematics.int3 value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public global::Unity.Mathematics.int3 Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            var length = reader.ReadArrayHeader();
            var x = default(int);
            var y = default(int);
            var z = default(int);
            for (int i = 0; i < length; i++)
            {
                var key = i;
                switch (key)
                {
                    case 0:
                        x = reader.ReadInt32();
                        break;
                    case 1:
                        y = reader.ReadInt32();
                        break;
                    case 2:
                        z = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var result = new global::Unity.Mathematics.int3(x, y, z);
            return result;
        }
    }
}

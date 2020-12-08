// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using Benchmark.Serializers;

#pragma warning disable SA1649 // File name should match first type name

public class MessagePack_v1 : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return oldmsgpack::MessagePack.MessagePackSerializer.Serialize<T>(input);
    }

    public override string ToString()
    {
        return "MessagePack_v1";
    }
}

public class MessagePack_v2 : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Serialize<T>(input);
    }

    public override string ToString()
    {
        return "MessagePack_v2";
    }
}

public class MsgPack_v1_string : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return oldmsgpack::MessagePack.MessagePackSerializer.Deserialize<T>((byte[])input, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
    }

    public override object Serialize<T>(T input)
    {
        return oldmsgpack::MessagePack.MessagePackSerializer.Serialize<T>(input, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
    }

    public override string ToString()
    {
        return "MsgPack_v1_string";
    }
}

public class MsgPack_v2_string : SerializerBase
{
    private static readonly newmsgpack::MessagePack.MessagePackSerializerOptions Options = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);

    public override T Deserialize<T>(object input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<T>((byte[])input, options: Options);
    }

    public override object Serialize<T>(T input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Serialize<T>(input, options: Options);
    }

    public override string ToString()
    {
        return "MsgPack_v2_string";
    }
}

public class MessagePackLz4_v1 : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return oldmsgpack::MessagePack.LZ4MessagePackSerializer.Deserialize<T>((byte[])input);
    }

    public override object Serialize<T>(T input)
    {
        return oldmsgpack::MessagePack.LZ4MessagePackSerializer.Serialize<T>(input);
    }

    public override string ToString()
    {
        return "MessagePackLz4_v1";
    }
}

public class MessagePackLz4_v2 : SerializerBase
{
    private static readonly newmsgpack::MessagePack.MessagePackSerializerOptions LZ4BlockArray = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithCompression(newmsgpack::MessagePack.MessagePackCompression.Lz4BlockArray);

    public override T Deserialize<T>(object input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<T>((byte[])input, LZ4BlockArray);
    }

    public override object Serialize<T>(T input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Serialize<T>(input, LZ4BlockArray);
    }

    public override string ToString()
    {
        return "MessagePackLz4_v2";
    }
}

public class MsgPack_v1_str_lz4 : SerializerBase
{
    public override T Deserialize<T>(object input)
    {
        return oldmsgpack::MessagePack.LZ4MessagePackSerializer.Deserialize<T>((byte[])input, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
    }

    public override object Serialize<T>(T input)
    {
        return oldmsgpack::MessagePack.LZ4MessagePackSerializer.Serialize<T>(input, oldmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance);
    }

    public override string ToString()
    {
        return "MsgPack_v1_str_lz4";
    }
}

public class MsgPack_v2_str_lz4 : SerializerBase
{
    private static readonly newmsgpack::MessagePack.MessagePackSerializerOptions Options = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(newmsgpack::MessagePack.Resolvers.ContractlessStandardResolver.Instance).WithCompression(newmsgpack::MessagePack.MessagePackCompression.Lz4BlockArray);

    public override T Deserialize<T>(object input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<T>((byte[])input, Options);
    }

    public override object Serialize<T>(T input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Serialize<T>(input, Options);
    }

    public override string ToString()
    {
        return "MsgPack_v2_str_lz4";
    }
}

public class MsgPack_v2_opt : SerializerBase
{
    private static readonly newmsgpack::MessagePack.MessagePackSerializerOptions Options = newmsgpack::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(OptimizedResolver.Instance);

    public override T Deserialize<T>(object input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Deserialize<T>((byte[])input, Options);
    }

    public override object Serialize<T>(T input)
    {
        return newmsgpack::MessagePack.MessagePackSerializer.Serialize<T>(input, Options);
    }

    public override string ToString()
    {
        return "MsgPack_v2_opt";
    }
}

public class OptimizedResolver : newmsgpack::MessagePack.IFormatterResolver
{
    public static readonly newmsgpack::MessagePack.IFormatterResolver Instance = new OptimizedResolver();

    // configure your custom resolvers.
    private static readonly newmsgpack::MessagePack.IFormatterResolver[] Resolvers = new newmsgpack::MessagePack.IFormatterResolver[]
    {
        newmsgpack::MessagePack.Resolvers.NativeGuidResolver.Instance,
        newmsgpack::MessagePack.Resolvers.NativeDecimalResolver.Instance,
        newmsgpack::MessagePack.Resolvers.NativeDateTimeResolver.Instance,
        newmsgpack::MessagePack.Resolvers.StandardResolver.Instance,
    };

    private OptimizedResolver()
    {
    }

    public newmsgpack::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()
    {
        return Cache<T>.Formatter;
    }

    private static class Cache<T>
    {
#pragma warning disable SA1401 // Fields should be private
        public static newmsgpack::MessagePack.Formatters.IMessagePackFormatter<T> Formatter;
#pragma warning restore SA1401 // Fields should be private

        static Cache()
        {
            foreach (var resolver in Resolvers)
            {
                var f = resolver.GetFormatter<T>();
                if (f != null)
                {
                    Formatter = f;
                    return;
                }
            }
        }
    }
}

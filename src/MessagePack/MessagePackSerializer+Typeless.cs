// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePack
{
    public partial class MessagePackSerializer
    {
        /// <summary>
        /// A convenience wrapper around <see cref="MessagePackSerializer"/> that assumes all generic type arguments are <see cref="object"/>
        /// causing the type of top-level objects to be recorded in the MessagePack stream and thus deserialized to the original type automatically.
        /// </summary>
        public static class Typeless
        {
            /// <summary>
            /// The default set of options to run with.
            /// </summary>
            public static readonly MessagePackSerializerOptions DefaultOptions = Resolvers.TypelessContractlessStandardResolver.Options;

            public static void Serialize(ref MessagePackWriter writer, object obj, MessagePackSerializerOptions options = null) => Serialize<object>(ref writer, obj, options ?? DefaultOptions);

            public static void Serialize(IBufferWriter<byte> writer, object obj, MessagePackSerializerOptions options = null) => Serialize<object>(writer, obj, options ?? DefaultOptions);

            public static byte[] Serialize(object obj, MessagePackSerializerOptions options = null) => Serialize<object>(obj, options ?? DefaultOptions);

            public static void Serialize(Stream stream, object obj, MessagePackSerializerOptions options = null) => Serialize<object>(stream, obj, options ?? DefaultOptions);

            public static ValueTask SerializeAsync(Stream stream, object obj, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default) => SerializeAsync<object>(stream, obj, options ?? DefaultOptions, cancellationToken);

            public static object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options = null) => Deserialize<object>(ref reader, options ?? DefaultOptions);

            public static object Deserialize(in ReadOnlySequence<byte> byteSequence, MessagePackSerializerOptions options = null) => Deserialize<object>(byteSequence, options ?? DefaultOptions);

            public static object Deserialize(Stream stream, MessagePackSerializerOptions options = null) => Deserialize<object>(stream, options ?? DefaultOptions);

            public static object Deserialize(Memory<byte> bytes, MessagePackSerializerOptions options = null) => Deserialize<object>(bytes, options ?? DefaultOptions);

            public static ValueTask<object> DeserializeAsync(Stream stream, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default) => DeserializeAsync<object>(stream, options ?? DefaultOptions, cancellationToken);
        }
    }
}

#endif

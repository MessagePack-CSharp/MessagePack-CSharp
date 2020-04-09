// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY_2018_3_OR_NEWER

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
            /// Gets or sets the default set of options to use when not explicitly specified for a method call.
            /// </summary>
            /// <value>The default value is <see cref="Resolvers.TypelessContractlessStandardResolver.Options"/>.</value>
            /// <remarks>
            /// This is an AppDomain or process-wide setting.
            /// If you're writing a library, you should NOT set or rely on this property but should instead pass
            /// in <see cref="MessagePackSerializerOptions.Standard"/> (or the required options) explicitly to every method call
            /// to guarantee appropriate behavior in any application.
            /// If you are an app author, realize that setting this property impacts the entire application so it should only be
            /// set once, and before any use of <see cref="MessagePackSerializer"/> occurs.
            /// </remarks>
            public static MessagePackSerializerOptions DefaultOptions { get; set; } = Resolvers.TypelessContractlessStandardResolver.Options;

            public static void Serialize(ref MessagePackWriter writer, object obj, MessagePackSerializerOptions options = null) => Serialize<object>(ref writer, obj, options ?? DefaultOptions);

            public static void Serialize(IBufferWriter<byte> writer, object obj, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default) => Serialize<object>(writer, obj, options ?? DefaultOptions, cancellationToken);

            public static byte[] Serialize(object obj, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default) => Serialize<object>(obj, options ?? DefaultOptions, cancellationToken);

            public static void Serialize(Stream stream, object obj, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default) => Serialize<object>(stream, obj, options ?? DefaultOptions, cancellationToken);

            public static Task SerializeAsync(Stream stream, object obj, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default) => SerializeAsync<object>(stream, obj, options ?? DefaultOptions, cancellationToken);

            public static object Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options = null) => Deserialize<object>(ref reader, options ?? DefaultOptions);

            public static object Deserialize(in ReadOnlySequence<byte> byteSequence, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default) => Deserialize<object>(byteSequence, options ?? DefaultOptions, cancellationToken);

            public static object Deserialize(Stream stream, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default) => Deserialize<object>(stream, options ?? DefaultOptions, cancellationToken);

            public static object Deserialize(Memory<byte> bytes, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default) => Deserialize<object>(bytes, options ?? DefaultOptions, cancellationToken);

            public static ValueTask<object> DeserializeAsync(Stream stream, MessagePackSerializerOptions options = null, CancellationToken cancellationToken = default) => DeserializeAsync<object>(stream, options ?? DefaultOptions, cancellationToken);
        }
    }
}

#endif

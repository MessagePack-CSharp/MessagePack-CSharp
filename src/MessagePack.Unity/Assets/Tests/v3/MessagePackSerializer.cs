using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePackv3
{
    public static class MessagePackSerializer
    {
        /// <summary>
        /// A thread-local, recyclable array that may be used for short bursts of code.
        /// </summary>
        [ThreadStatic]
        private static byte[]? s_scratchArray;

        static MessagePackSerializerOptions defaultOptions = default!;

        public static byte[] Serialize<T>(in T value, MessagePackSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            byte[]? array = s_scratchArray;
            if (array == null)
            {
                s_scratchArray = array = new byte[65536];
            }

            IMessagePackFormatter<T> formatter = default!; // TODO:

            // TODO:rent from SequencePool
            var writer = new MessagePackWriter(array);
            formatter.Serialize(ref writer, ref Unsafe.AsRef(value), options ?? defaultOptions);

            // writer.Write...
            return writer.FlushAndGetArray();
        }

        public static T Deserialize<T>(byte[] source, MessagePackSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var reader = new MessagePackReader(source);

            IMessagePackFormatter<T> formatter = default!; // TODO:

            T result = default;
            formatter.Deserialize(ref reader, ref result, options ?? defaultOptions);
            return result;
        }


        public static void DeserializeTo<T>(byte[] source, ref T result, MessagePackSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            var reader = new MessagePackReader(source);

            IMessagePackFormatter<T> formatter = default!; // TODO:

            formatter.Deserialize(ref reader, ref result, options ?? defaultOptions);
        }
    }
}

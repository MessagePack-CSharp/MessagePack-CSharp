#if NETSTANDARD || NETFRAMEWORK

using System;
using System.IO;

namespace MessagePack
{
    public partial class MessagePackSerializer
    {
        /// <summary>
        /// A convenience wrapper around <see cref="MessagePackSerializer"/> that assumes all generic type arguments are <see cref="object"/>.
        /// </summary>
        public class Typeless
        {
            private readonly MessagePackSerializer serializer;

            public Typeless()
                : this(new MessagePackSerializer(new Resolvers.TypelessContractlessStandardResolver()))
            {
            }

            public Typeless(MessagePackSerializer serializer)
            {
                this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            }

            public byte[] Serialize(object obj) => serializer.Serialize(obj);

            public void Serialize(Stream stream, object obj) => serializer.Serialize(stream, obj);

            public System.Threading.Tasks.Task SerializeAsync(Stream stream, object obj) => serializer.SerializeAsync(stream, obj);

            public object Deserialize(byte[] bytes) => serializer.Deserialize<object>(bytes);

            public object Deserialize(Stream stream) => serializer.Deserialize<object>(stream);

            public object Deserialize(Stream stream, bool readStrict) => serializer.Deserialize<object>(stream, readStrict);

            public System.Threading.Tasks.Task<object> DeserializeAsync(Stream stream) => serializer.DeserializeAsync<object>(stream);
        }
    }
}

#endif
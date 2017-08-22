using MessagePack.Formatters;
using System;

namespace MessagePack
{
    public interface IResolverConfiguration
    {
        CollectionDeserializeToBehaviour CollectionDeserializeToBehaviour { get; }
    }

    public enum CollectionDeserializeToBehaviour
    {
        OverwriteReplace,
        Add
    }

    public class MessagePackConfiguration : IResolverConfiguration, IMessagePackFormatter<IResolverConfiguration>
    {
        public static readonly MessagePackConfiguration Default = new MessagePackConfiguration(CollectionDeserializeToBehaviour.OverwriteReplace);

        public CollectionDeserializeToBehaviour CollectionDeserializeToBehaviour { get; private set; }

        public MessagePackConfiguration(CollectionDeserializeToBehaviour collectionDeserializeToBehaviour)
        {
            this.CollectionDeserializeToBehaviour = collectionDeserializeToBehaviour;
        }

        int IMessagePackFormatter<IResolverConfiguration>.Serialize(ref byte[] bytes, int offset, IResolverConfiguration value, IFormatterResolver formatterResolver)
        {
            throw new NotSupportedException("IMessagePackFormatter method is dummy, please call AsConfiguration().");
        }

        IResolverConfiguration IMessagePackFormatter<IResolverConfiguration>.Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            throw new NotSupportedException("IMessagePackFormatter method is dummy, please call AsConfiguration().");
        }
    }

    public static class ResolverConfigurationExtensions
    {
        public static IResolverConfiguration GetConfiguration(this IFormatterResolver formatterResolver)
        {
            var configuration = formatterResolver.GetFormatter<IResolverConfiguration>();
            return (configuration == null) ? MessagePackConfiguration.Default : configuration.AsConfiguration();
        }

        public static IResolverConfiguration AsConfiguration(this IMessagePackFormatter<IResolverConfiguration> configurationFormatter)
        {
            return (IResolverConfiguration)configurationFormatter;
        }
    }
}
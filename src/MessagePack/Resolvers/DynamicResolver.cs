using System;
using System.Collections.Generic;
using System.Text;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public class DynamicResolver : IFormatterResolver
    {
        IMessagePackFormatter<T> IFormatterResolver.GetFormatter<T>()
        {
            throw new NotImplementedException();
        }
    }
}

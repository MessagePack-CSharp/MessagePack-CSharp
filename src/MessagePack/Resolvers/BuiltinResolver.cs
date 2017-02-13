using System;
using System.Collections.Generic;
using System.Text;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public class BuiltinResolver : IFormatterResolver
    {
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            throw new NotImplementedException();
        }
    }
}

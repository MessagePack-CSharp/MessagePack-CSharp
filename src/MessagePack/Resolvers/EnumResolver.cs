using System;
using System.Collections.Generic;
using System.Text;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public class EnumResolver : IFormatterResolver
    {
        IMessagePackFormatter<T> IFormatterResolver.GetFormatter<T>()
        {
            throw new NotImplementedException();
        }
    }
}

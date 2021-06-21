using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePackv3
{
    public interface IMessagePackFormatter<T>
    {
        void Serialize(ref MessagePackWriter writer, ref T value, MessagePackSerializerOptions options);
        void Deserialize(ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options);
    }
}

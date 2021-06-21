using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePackv3.Formatters
{
    public sealed class Int32MessagePackFormatter : IMessagePackFormatter<int>
    {
        public void Serialize(ref MessagePackWriter writer, ref int value, MessagePackSerializerOptions options)
        {
            writer.WriteInt32(value);
        }

        public void Deserialize(ref MessagePackReader reader, ref int value, MessagePackSerializerOptions options)
        {
            value = reader.ReadInt32();
        }
    }
}

using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.LZ4;
using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;

namespace MessagePack
{
    // JSON API
    public partial class LZ4MessagePackSerializer
    {
        /// <summary>
        /// Dump message-pack binary to JSON string.
        /// </summary>
        public override void ConvertToJson(ref MessagePackReader reader, TextWriter jsonWriter)
        {
            using (var scratch = new Nerdbank.Streams.Sequence<byte>())
            {
                if (TryDecompress(ref reader, scratch))
                {
                    var scratchReader = new MessagePackReader(scratch.AsReadOnlySequence);
                    base.ConvertToJson(ref scratchReader, jsonWriter);
                }
                else
                {
                    base.ConvertToJson(ref reader, jsonWriter);
                }
            }
        }

        /// <summary>
        /// From Json String to LZ4MessagePack binary
        /// </summary>
        public override byte[] ConvertFromJson(TextReader reader)
        {
            var buffer = MessagePackSerializer.FromJsonUnsafe(reader); // offset is guranteed from 0
            return LZ4MessagePackSerializer.ToLZ4Binary(buffer);
        }
    }
}

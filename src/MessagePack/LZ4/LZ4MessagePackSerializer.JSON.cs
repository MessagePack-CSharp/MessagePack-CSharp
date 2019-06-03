using System.IO;

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
        public override void ConvertFromJson(TextReader reader, ref MessagePackWriter writer)
        {
            using (var scratch = new Nerdbank.Streams.Sequence<byte>())
            {
                var scratchWriter = writer.Clone(scratch);
                base.ConvertFromJson(reader, ref scratchWriter);
                scratchWriter.Flush();
                ToLZ4BinaryCore(scratch.AsReadOnlySequence, ref writer);
            }
        }
    }
}

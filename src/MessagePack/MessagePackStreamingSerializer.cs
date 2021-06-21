using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePackv3
{
    public sealed class FlushStrategy
    {
        public static FlushStrategy FlushOnEveryWritten = new FlushStrategy { FlushPerItemCount = true, ItemCount = 1 };
        public static FlushStrategy FlushOnEvery64K = new FlushStrategy { FlushPerBytesWritten = true, BytesWritten = 65536 };

        public bool FlushPerItemCount { get; set; } // { get; init; }
        public bool FlushPerBytesWritten { get; set; }
        public int ItemCount { get; set; }
        public int BytesWritten { get; set; }

        public bool ShouldFlush(int itemCount, int bytesWritten)
        {
            if (FlushPerItemCount && (ItemCount <= itemCount))
            {
                return true;
            }
            if (FlushPerBytesWritten && (BytesWritten <= bytesWritten))
            {
                return true;
            }
            return false;
        }
    }

    public static class MessagePackStreamingSerializer
    {
        // public static async ValueTask SerializeAsync<T>(PipeWriter pipeWriter, IReadOnlyCollection<T?> values, MessagePackOptions options)

        public static async ValueTask SerializeAsync<T>(PipeWriter pipeWriter, IEnumerable<T?> values, int count, MessagePackSerializerOptions options, FlushStrategy flushStrategy, CancellationToken cancellationToken = default)
        {
            static int WriteCore(PipeWriter pipeWriter, T? item, IMessagePackFormatter<T> formatter, MessagePackSerializerOptions options)
            {
                var writer = new MessagePackWriter(pipeWriter);
                formatter.Serialize(ref writer, ref item, options);
                return writer.TotalWritten;
            }

            // NOTE* writearrayheader
            // var span... = pipeWriter.GetSpan(4);
            // Advance and Flush

            IMessagePackFormatter<T> formatter = default!;
            int bytesWritten = 0;
            var itemCount = 0;
            foreach (var item in values)
            {
                bytesWritten += WriteCore(pipeWriter, item, formatter, options);
                itemCount++;
                if (flushStrategy.ShouldFlush(itemCount, bytesWritten))
                {
                    await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            await pipeWriter.FlushAsync().ConfigureAwait(false);
        }

        public static ValueTask SerializeMapAsync<TKey, TValue>(PipeWriter pipeWriter, int count, IEnumerable<KeyValuePair<TKey, TValue>> value)
        {
            throw new NotImplementedException();
        }

        public static async IAsyncEnumerable<T?> DeserializeAsync<T>(PipeReader pipeReader, MessagePackSerializerOptions options)
        {
            static T? ReadCore(ReadOnlySequence<byte> buffer, IMessagePackFormatter<T> formatter, MessagePackSerializerOptions options)
            {
                var reader = new MessagePackReader(buffer);
                // Unsafe.SkipInit<T?>(out var value); // #if NET5_0
                T? value = default;
                formatter.Deserialize(ref reader, ref value, options);
                return value;
            }

            IMessagePackFormatter<T> formatter = default!;

            var arrayCount = 0; // array or map count]
            //ReadResult result;
            for (int i = 0; i < arrayCount; i++)
            {
                if (pipeReader.TryRead(out var result))
                {
                    //if (result.Buffer.IsSingleSegment)
                    //{
                    //}

                    //var rr = new SequenceReader<T>(result.Buffer);

                    // TODO:use reader.TrySkip and get buffers...

                    yield return ReadCore(result.Buffer, formatter, options);


                    //var reader = new MessagePackReader(result.Buffer);

                    //T? value;
                    //formatter.Deserialize(ref reader, ref value, options);


                }
                // pipeReader.ReadAsync(
                // 


                //new MessagePackReader(

                // TrySkip(calculate requires buffer size)


            }

            await pipeReader.ReadAsync();

            // TODO: set read buffer end(end?)
            // pipeReader.AdvanceTo(
            //await pipeReader.CompleteAsync();
        }
    }
}
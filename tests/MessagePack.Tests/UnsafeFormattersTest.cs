using MessagePack.Formatters;
using Nerdbank.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class UnsafeFormattersTest
    {
        [Fact]
        public void GuidTest()
        {
            var guid = Guid.NewGuid();
            var sequence = new Sequence<byte>();
            var sequenceWriter = new MessagePackWriter(sequence);
            BinaryGuidFormatter.Instance.Serialize(ref sequenceWriter, guid, null);
            sequenceWriter.Flush();
            sequence.Length.Is(18);

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            var nguid = BinaryGuidFormatter.Instance.Deserialize(ref sequenceReader, null);
            Assert.True(sequenceReader.End);

            guid.Is(nguid);
        }

        [Fact]
        public void DecimalTest()
        {
            var d = new Decimal(1341, 53156, 61, true, 3);
            var sequence = new Sequence<byte>();
            var sequenceWriter = new MessagePackWriter(sequence);
            BinaryDecimalFormatter.Instance.Serialize(ref sequenceWriter, d, null);
            sequenceWriter.Flush();
            sequence.Length.Is(18);

            var sequenceReader = new MessagePackReader(sequence.AsReadOnlySequence);
            var nd = BinaryDecimalFormatter.Instance.Deserialize(ref sequenceReader, null);
            Assert.True(sequenceReader.End);

            d.Is(nd);
        }
    }
}

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
            var sequenceWriter = new MessagePackWriter();
            BinaryGuidFormatter.Instance.Serialize(ref sequenceWriter, guid, null);
            sequenceWriter.Flush();
            sequenceWriter.WrittenBytes.Length.Is(18);

            var sequenceReader = new MessagePackReader(sequenceWriter.WrittenBytes);
            var nguid = BinaryGuidFormatter.Instance.Deserialize(ref sequenceReader, null);
            Assert.True(sequenceReader.End);

            guid.Is(nguid);
        }

        [Fact]
        public void DecimalTest()
        {
            var d = new Decimal(1341, 53156, 61, true, 3);
            var sequenceWriter = new MessagePackWriter();
            BinaryDecimalFormatter.Instance.Serialize(ref sequenceWriter, d, null);
            sequenceWriter.Flush();
            sequenceWriter.WrittenBytes.Length.Is(18);

            var sequenceReader = new MessagePackReader(sequenceWriter.WrittenBytes);
            var nd = BinaryDecimalFormatter.Instance.Deserialize(ref sequenceReader, null);
            Assert.True(sequenceReader.End);

            d.Is(nd);
        }
    }
}

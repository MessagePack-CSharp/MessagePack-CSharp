using MessagePack.Formatters;
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
            byte[] bin = null;
            BinaryGuidFormatter.Instance.Serialize(ref bin, 0, guid, null).Is(18);

            int readSize;
            var nguid = BinaryGuidFormatter.Instance.Deserialize(bin, 0, null, out readSize);
            readSize.Is(18);

            guid.Is(nguid);
        }

        [Fact]
        public void DecimalTest()
        {
            var d = new Decimal(1341, 53156, 61, true, 3);
            byte[] bin = null;
            BinaryDecimalFormatter.Instance.Serialize(ref bin, 0, d, null).Is(18);

            int readSize;
            var nd = BinaryDecimalFormatter.Instance.Deserialize(bin, 0, null, out readSize);
            readSize.Is(18);

            d.Is(nd);
        }
    }
}

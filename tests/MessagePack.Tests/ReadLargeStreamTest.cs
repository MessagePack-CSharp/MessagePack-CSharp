using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class ReadLargeStreamTest
    {
        private MessagePackSerializer serializer = new MessagePackSerializer();

        [Fact]
        public void Large()
        {
            var bytesA = new byte[131066];
            var bytesB = new byte[31072];
            var bytesC = new byte[131066];
            for (int i = 0; i < bytesA.Length; i++)
            {
                bytesA[i] = 1;
                // bytesB[i] = 1;
                bytesC[i] = 1;
            }

            var bin = serializer.Serialize(new[] { bytesA, bytesB, bytesC });
            var ms = new MemoryStream(bin, 0, bin.Length, false, false);

            var foo = serializer.Deserialize<byte[][]>(ms);

            for (int i = 0; i < foo[0].Length; i++)
            {
                foo[0][i].Is((byte)1);
                // foo[1][i].Is((byte)1);
                foo[2][i].Is((byte)1);
            }
        }
    }
}

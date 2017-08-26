using MessagePack.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class AutomataSafe
    {
        [Theory]
        [InlineData("a")]
        [InlineData("ab")]
        [InlineData("abc")]
        [InlineData("abcd")]
        [InlineData("abcde")]
        [InlineData("abcdef")]
        [InlineData("abcdefg")]
        [InlineData("abcdefgh")]
        public unsafe void KeyGen(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            var rest = bytes.Length;
            fixed (byte* buf = bytes)
            {
                var p = buf;
                var l1 = AutomataKeyGen.GetKey(ref p, ref rest);

                var offset = 0;
                rest = bytes.Length;
                var l2 = AutomataKeyGen.GetKeySafe(bytes, ref offset, ref rest);

                l1.Is(l2);
            }
        }

        [Fact]
        public void TryGetValueSafe()
        {
            var keys = new[]
            {
               "abcdefgh",
               "abcdefghabcd1",
               "abcdefghabcd2",
               "zeregzfw",
               "takohogergahu",
               "rezhgoerghouerhgouerhozughoreughozheorugheoghozuehrouehogreuhgoeuz",
               "mp1",
               "mp2",
               "mp3",
               "mp4",
               "mp5"
            };

            var automata = new AutomataDictionary();
            for (int i = 0; i < keys.Length; i++)
            {
                automata.Add(keys[i], i);
            }

            for (int i = 0; i < keys.Length; i++)
            {
                var enc = Encoding.UTF8.GetBytes(keys[i]);
                int v;
                automata.TryGetValueSafe(new ArraySegment<byte>(enc, 0, enc.Length), out v).IsTrue();
                v.Is(i);
            }
        }
    }
}

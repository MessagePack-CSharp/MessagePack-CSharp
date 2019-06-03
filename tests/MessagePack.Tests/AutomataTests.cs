using MessagePack.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class AutomataTests
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
            ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(str);
            var l1 = AutomataKeyGen.GetKey(ref bytes);
        }

        [Fact]
        public void TryGetValue()
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
                automata.TryGetValue(enc, out v).IsTrue();
                v.Is(i);
            }
        }
    }
}

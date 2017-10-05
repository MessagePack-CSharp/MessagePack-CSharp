using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePack.Tests
{
    using MessagePack.Internal;
    using Xunit;
    public class UnicodeUtilityTest
    {
        [Theory]
        // U+0,U+7F
        [InlineData(new char[] { (char)0x00, (char)0x7f, (char)0x3f })]
        // U+80,U+7FF
        [InlineData(new char[] { (char)0x123, (char)0x80, (char)0x7ff })]
        // U+800,U+FFFF
        [InlineData(new char[] { (char)0x1234, (char)0x800, (char)0xffff })]
        // U+10000,U+10FFFF
        [InlineData(new char[] { (char)0xdbc0, (char)0xdc00, (char)0xdbc0, (char)0xdc30, (char)0xdbff, (char)0xdfff })]
        public void Utf8ToUtf16_U_7F(char[] expected)
        {
            var bytes = Encoding.UTF8.GetBytes(expected);
            var actual = UnicodeUtility.GetStringFromUtf8(bytes, 0, bytes.Length).ToArray();
            Assert.Equal(expected.Select(x => (int)x), actual.Select(x => (int)x));
        }
        [Fact]
        public void Utf8ToUtf16_IncludeBOM()
        {
            var expected = new char[] { (char)0xfeff, (char)0x12 };
            var bytes = Encoding.UTF8.GetBytes(expected);
            var actual = UnicodeUtility.GetStringFromUtf8(bytes, 0, bytes.Length).ToArray();
            Assert.Equal(expected, actual);
        }
        [Theory]
        // 0x80 is invalid as first byte
        [InlineData(new byte[] { 0x80, 0x80 })]
        // second byte is invalid as second utf-8 data, but valid as first byte
        [InlineData(new byte[] { 0xc2, 0x10 })]
        // invalid third byte data
        [InlineData(new byte[] { 0xe0, 0xa0, 0x7f })]
        // U+1FFFFF should be retrived as invalid unicode code point
        [InlineData(new byte[] { 0xf7, 0xbf, 0xbf, 0xbf })]
        // invalid third byte data, short of data length
        [InlineData(new byte[] { 0xe0, 0xa0, 0xe0, 0xa0 })]
        // retrieve as error when U+20000,U+3FFFFFF codepoint has come
        [InlineData(new byte[] { 0xf8, 0x88, 0x80, 0x80, 0x80, 0xfb, 0xbf, 0xbf, 0xbf, 0xbf})]
        // retrieve as error when U+4000000,U+7FFFFFFF codepoint has come
        [InlineData(new byte[] { 0xfc, 0x84, 0x80, 0x80, 0x80, 0x80, 0xfd, 0xbf, 0xbf, 0xbf, 0xbf, 0xbf})]
        public void Utf8ToUtf16_InvalidByteSequence(byte[] bytes)
        {
            var expected = Encoding.UTF8.GetString(bytes).ToCharArray();
            var actual = UnicodeUtility.GetStringFromUtf8(bytes, 0, bytes.Length).ToArray();
            Assert.Equal(expected.Select(x => (int)x), actual.Select(x => (int)x));
        }
    }
}

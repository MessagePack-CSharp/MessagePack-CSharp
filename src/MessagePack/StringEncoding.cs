using System.Text;

namespace MessagePack
{
    internal static class StringEncoding
    {
        public static readonly Encoding UTF8 = new UTF8Encoding(false);
    }
}

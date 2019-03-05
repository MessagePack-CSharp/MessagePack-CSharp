using System;
using System.Buffers;
using MessagePack.Formatters;
using Microsoft;
using Nerdbank.Streams;

namespace MessagePack
{
    /// <summary>
    /// Internal utilities and extension methods for various external types.
    /// </summary>
    internal static class Utilities
    {
        internal delegate void GetWriterBytesAction<TArg>(ref MessagePackWriter writer, TArg argument);

        internal static byte[] GetWriterBytes<TArg>(TArg arg, GetWriterBytesAction<TArg> action)
        {
            var writer = new MessagePackWriter();
            action(ref writer, arg);
            writer.Flush();
            return writer.WrittenBytes.ToArray();
        }
    }
}

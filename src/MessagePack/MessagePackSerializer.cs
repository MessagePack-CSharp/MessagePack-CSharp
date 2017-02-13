using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack
{
    public static class MessagePackSerializer
    {
    }

    internal static class InternalMemoryPool
    {
        [ThreadStatic]
        public static readonly byte[] Buffer = new byte[4096];
    }
}

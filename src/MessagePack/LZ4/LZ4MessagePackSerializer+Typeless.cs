#if !UNITY

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace MessagePack
{
    // Typeless API
    public partial class LZ4MessagePackSerializer
    {
        public new class Typeless : MessagePackSerializer.Typeless
        {
            public Typeless()
                : base(new LZ4MessagePackSerializer(new Resolvers.TypelessContractlessStandardResolver()))
            {
            }
        }
    }
}

#endif
#if !UNITY

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace MessagePack
{
    // NonGeneric API
    public partial class LZ4MessagePackSerializer
    {
        public new class NonGeneric : MessagePackSerializer.NonGeneric
        {
            public NonGeneric()
                : base(new LZ4MessagePackSerializer())
            {
            }

            public NonGeneric(IFormatterResolver resolver)
                : base(new LZ4MessagePackSerializer(resolver))
            {
            }
        }
    }
}

#endif
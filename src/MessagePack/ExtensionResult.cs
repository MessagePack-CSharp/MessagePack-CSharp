using System;
using System.Buffers;

namespace MessagePack
{
#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    struct ExtensionResult
    {
        public sbyte TypeCode { get; private set; }

        public ReadOnlySequence<byte> Data { get; private set; }

        public ExtensionResult(sbyte typeCode, Memory<byte> data)
        {
            TypeCode = typeCode;
            Data = new ReadOnlySequence<byte>(data);
        }

        public ExtensionResult(sbyte typeCode, ReadOnlySequence<byte> data)
        {
            TypeCode = typeCode;
            Data = data;
        }

        public ExtensionHeader Header => new ExtensionHeader(this.TypeCode, (uint)this.Data.Length);
    }
}

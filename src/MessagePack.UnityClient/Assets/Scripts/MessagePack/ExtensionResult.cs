// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public ExtensionResult(sbyte typeCode, Memory<byte> data)
        {
            this.TypeCode = typeCode;
            this.Data = new ReadOnlySequence<byte>(data);
        }

        public ExtensionResult(sbyte typeCode, ReadOnlySequence<byte> data)
        {
            this.TypeCode = typeCode;
            this.Data = data;
        }

        public sbyte TypeCode { get; private set; }

        public ReadOnlySequence<byte> Data { get; private set; }

        public ExtensionHeader Header => new ExtensionHeader(this.TypeCode, (uint)this.Data.Length);
    }
}

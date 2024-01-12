// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MessagePack
{
#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    struct ExtensionHeader : IEquatable<ExtensionHeader>
    {
        public sbyte TypeCode { get; private set; }

        public uint Length { get; private set; }

        public ExtensionHeader(sbyte typeCode, uint length)
        {
            this.TypeCode = typeCode;
            this.Length = length;
        }

        public ExtensionHeader(sbyte typeCode, int length)
        {
            this.TypeCode = typeCode;
            this.Length = (uint)length;
        }

        public bool Equals(ExtensionHeader other) => this.TypeCode == other.TypeCode && this.Length == other.Length;
    }
}

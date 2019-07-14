// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack
{
    /* safe accessor of Single/Double's underlying byte.
     * This code is borrowed from MsgPack-Cli https://github.com/msgpack/msgpack-cli */

    [StructLayout(LayoutKind.Explicit)]
    internal struct Float32Bits
    {
        [FieldOffset(0)]
        public readonly float Value;

        [FieldOffset(0)]
        public readonly Byte Byte0;

        [FieldOffset(1)]
        public readonly Byte Byte1;

        [FieldOffset(2)]
        public readonly Byte Byte2;

        [FieldOffset(3)]
        public readonly Byte Byte3;

        public Float32Bits(float value)
        {
            this = default(Float32Bits);
            this.Value = value;
        }

        public Float32Bits(ReadOnlySpan<byte> bigEndianBytes)
        {
            this = default(Float32Bits);

            if (BitConverter.IsLittleEndian)
            {
                this.Byte0 = bigEndianBytes[3];
                this.Byte1 = bigEndianBytes[2];
                this.Byte2 = bigEndianBytes[1];
                this.Byte3 = bigEndianBytes[0];
            }
            else
            {
                this.Byte0 = bigEndianBytes[0];
                this.Byte1 = bigEndianBytes[1];
                this.Byte2 = bigEndianBytes[2];
                this.Byte3 = bigEndianBytes[3];
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct Float64Bits
    {
        [FieldOffset(0)]
        public readonly double Value;

        [FieldOffset(0)]
        public readonly Byte Byte0;

        [FieldOffset(1)]
        public readonly Byte Byte1;

        [FieldOffset(2)]
        public readonly Byte Byte2;

        [FieldOffset(3)]
        public readonly Byte Byte3;

        [FieldOffset(4)]
        public readonly Byte Byte4;

        [FieldOffset(5)]
        public readonly Byte Byte5;

        [FieldOffset(6)]
        public readonly Byte Byte6;

        [FieldOffset(7)]
        public readonly Byte Byte7;

        public Float64Bits(double value)
        {
            this = default(Float64Bits);
            this.Value = value;
        }

        public Float64Bits(ReadOnlySpan<byte> bigEndianBytes)
        {
            this = default(Float64Bits);

            if (BitConverter.IsLittleEndian)
            {
                this.Byte0 = bigEndianBytes[7];
                this.Byte1 = bigEndianBytes[6];
                this.Byte2 = bigEndianBytes[5];
                this.Byte3 = bigEndianBytes[4];
                this.Byte4 = bigEndianBytes[3];
                this.Byte5 = bigEndianBytes[2];
                this.Byte6 = bigEndianBytes[1];
                this.Byte7 = bigEndianBytes[0];
            }
            else
            {
                this.Byte0 = bigEndianBytes[0];
                this.Byte1 = bigEndianBytes[1];
                this.Byte2 = bigEndianBytes[2];
                this.Byte3 = bigEndianBytes[3];
                this.Byte4 = bigEndianBytes[4];
                this.Byte5 = bigEndianBytes[5];
                this.Byte6 = bigEndianBytes[6];
                this.Byte7 = bigEndianBytes[7];
            }
        }
    }
}

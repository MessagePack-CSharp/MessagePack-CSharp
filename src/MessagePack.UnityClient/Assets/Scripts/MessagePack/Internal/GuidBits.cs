// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace MessagePack.Internal
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal struct GuidBits
    {
        [FieldOffset(0)]
        public readonly Guid Value;

        [FieldOffset(0)]
        public readonly byte Byte0;
        [FieldOffset(1)]
        public readonly byte Byte1;
        [FieldOffset(2)]
        public readonly byte Byte2;
        [FieldOffset(3)]
        public readonly byte Byte3;
        [FieldOffset(4)]
        public readonly byte Byte4;
        [FieldOffset(5)]
        public readonly byte Byte5;
        [FieldOffset(6)]
        public readonly byte Byte6;
        [FieldOffset(7)]
        public readonly byte Byte7;
        [FieldOffset(8)]
        public readonly byte Byte8;
        [FieldOffset(9)]
        public readonly byte Byte9;
        [FieldOffset(10)]
        public readonly byte Byte10;
        [FieldOffset(11)]
        public readonly byte Byte11;
        [FieldOffset(12)]
        public readonly byte Byte12;
        [FieldOffset(13)]
        public readonly byte Byte13;
        [FieldOffset(14)]
        public readonly byte Byte14;
        [FieldOffset(15)]
        public readonly byte Byte15;

        // string.Join(", ", Enumerable.Range(0, 256).Select(x => (int)BitConverter.ToString(new byte[] { (byte)x }).ToLower()[0]))
        private static byte[] byteToHexStringHigh = new byte[256] { 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 51, 51, 51, 51, 51, 51, 51, 51, 51, 51, 51, 51, 51, 51, 51, 51, 52, 52, 52, 52, 52, 52, 52, 52, 52, 52, 52, 52, 52, 52, 52, 52, 53, 53, 53, 53, 53, 53, 53, 53, 53, 53, 53, 53, 53, 53, 53, 53, 54, 54, 54, 54, 54, 54, 54, 54, 54, 54, 54, 54, 54, 54, 54, 54, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 57, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 97, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 98, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 101, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102, 102 };

        // string.Join(", ", Enumerable.Range(0, 256).Select(x => (int)BitConverter.ToString(new byte[] { (byte)x }).ToLower()[1]))
        private static byte[] byteToHexStringLow = new byte[256] { 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 97, 98, 99, 100, 101, 102 };

        public GuidBits(ref Guid value)
        {
            this = default(GuidBits);
            this.Value = value;
        }

        // 4-pattern, lower/upper and '-' or no
        public GuidBits(ReadOnlySpan<byte> utf8string)
        {
            this = default(GuidBits);

            // 32
            if (utf8string.Length == 32)
            {
                if (BitConverter.IsLittleEndian)
                {
                    this.Byte0 = Parse(utf8string, 6);
                    this.Byte1 = Parse(utf8string, 4);
                    this.Byte2 = Parse(utf8string, 2);
                    this.Byte3 = Parse(utf8string, 0);

                    this.Byte4 = Parse(utf8string, 10);
                    this.Byte5 = Parse(utf8string, 8);

                    this.Byte6 = Parse(utf8string, 14);
                    this.Byte7 = Parse(utf8string, 12);
                }
                else
                {
                    this.Byte0 = Parse(utf8string, 0);
                    this.Byte1 = Parse(utf8string, 2);
                    this.Byte2 = Parse(utf8string, 4);
                    this.Byte3 = Parse(utf8string, 6);

                    this.Byte4 = Parse(utf8string, 8);
                    this.Byte5 = Parse(utf8string, 10);

                    this.Byte6 = Parse(utf8string, 12);
                    this.Byte7 = Parse(utf8string, 14);
                }

                this.Byte8 = Parse(utf8string, 16);
                this.Byte9 = Parse(utf8string, 18);

                this.Byte10 = Parse(utf8string, 20);
                this.Byte11 = Parse(utf8string, 22);
                this.Byte12 = Parse(utf8string, 24);
                this.Byte13 = Parse(utf8string, 26);
                this.Byte14 = Parse(utf8string, 28);
                this.Byte15 = Parse(utf8string, 30);
                return;
            }
            else if (utf8string.Length == 36)
            {
                // '-' => 45
                if (BitConverter.IsLittleEndian)
                {
                    this.Byte0 = Parse(utf8string, 6);
                    this.Byte1 = Parse(utf8string, 4);
                    this.Byte2 = Parse(utf8string, 2);
                    this.Byte3 = Parse(utf8string, 0);

                    if (utf8string[8] != '-')
                    {
                        goto ERROR;
                    }

                    this.Byte4 = Parse(utf8string, 11);
                    this.Byte5 = Parse(utf8string, 9);

                    if (utf8string[13] != '-')
                    {
                        goto ERROR;
                    }

                    this.Byte6 = Parse(utf8string, 16);
                    this.Byte7 = Parse(utf8string, 14);
                }
                else
                {
                    this.Byte0 = Parse(utf8string, 0);
                    this.Byte1 = Parse(utf8string, 2);
                    this.Byte2 = Parse(utf8string, 4);
                    this.Byte3 = Parse(utf8string, 6);

                    if (utf8string[8] != '-')
                    {
                        goto ERROR;
                    }

                    this.Byte4 = Parse(utf8string, 9);
                    this.Byte5 = Parse(utf8string, 11);

                    if (utf8string[13] != '-')
                    {
                        goto ERROR;
                    }

                    this.Byte6 = Parse(utf8string, 14);
                    this.Byte7 = Parse(utf8string, 16);
                }

                if (utf8string[18] != '-')
                {
                    goto ERROR;
                }

                this.Byte8 = Parse(utf8string, 19);
                this.Byte9 = Parse(utf8string, 21);

                if (utf8string[23] != '-')
                {
                    goto ERROR;
                }

                this.Byte10 = Parse(utf8string, 24);
                this.Byte11 = Parse(utf8string, 26);
                this.Byte12 = Parse(utf8string, 28);
                this.Byte13 = Parse(utf8string, 30);
                this.Byte14 = Parse(utf8string, 32);
                this.Byte15 = Parse(utf8string, 34);
                return;
            }

ERROR:
            throw new MessagePackSerializationException("Invalid Guid Pattern.");
        }

#if !UNITY_2018_3_OR_NEWER
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        private static byte Parse(ReadOnlySpan<byte> bytes, int highOffset)
        {
            return unchecked((byte)((SwitchParse(bytes[highOffset]) * 16) + SwitchParse(bytes[highOffset + 1])));
        }

#if !UNITY_2018_3_OR_NEWER
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        private static byte SwitchParse(byte b)
        {
            // '0'(48) ~ '9'(57) => -48
            // 'A'(65) ~ 'F'(70) => -55
            // 'a'(97) ~ 'f'(102) => -87
            switch (b)
            {
                case 48:
                case 49:
                case 50:
                case 51:
                case 52:
                case 53:
                case 54:
                case 55:
                case 56:
                case 57:
                    return unchecked((byte)(b - 48));
                case 65:
                case 66:
                case 67:
                case 68:
                case 69:
                case 70:
                    return unchecked((byte)(b - 55));
                case 97:
                case 98:
                case 99:
                case 100:
                case 101:
                case 102:
                    return unchecked((byte)(b - 87));
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 17:
                case 18:
                case 19:
                case 20:
                case 21:
                case 22:
                case 23:
                case 24:
                case 25:
                case 26:
                case 27:
                case 28:
                case 29:
                case 30:
                case 31:
                case 32:
                case 33:
                case 34:
                case 35:
                case 36:
                case 37:
                case 38:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                case 44:
                case 45:
                case 46:
                case 47:
                case 58:
                case 59:
                case 60:
                case 61:
                case 62:
                case 63:
                case 64:
                case 71:
                case 72:
                case 73:
                case 74:
                case 75:
                case 76:
                case 77:
                case 78:
                case 79:
                case 80:
                case 81:
                case 82:
                case 83:
                case 84:
                case 85:
                case 86:
                case 87:
                case 88:
                case 89:
                case 90:
                case 91:
                case 92:
                case 93:
                case 94:
                case 95:
                case 96:
                default:
                    throw new MessagePackSerializationException("Invalid Guid Pattern.");
            }
        }

        // 4(x2) - 2(x2) - 2(x2) - 2(x2) - 6(x2)
        public void Write(Span<byte> buffer)
        {
            if (BitConverter.IsLittleEndian)
            {
                // int(_a)
                buffer[6] = byteToHexStringHigh[this.Byte0];
                buffer[7] = byteToHexStringLow[this.Byte0];
                buffer[4] = byteToHexStringHigh[this.Byte1];
                buffer[5] = byteToHexStringLow[this.Byte1];
                buffer[2] = byteToHexStringHigh[this.Byte2];
                buffer[3] = byteToHexStringLow[this.Byte2];
                buffer[0] = byteToHexStringHigh[this.Byte3];
                buffer[1] = byteToHexStringLow[this.Byte3];

                buffer[8] = (byte)'-';

                // short(_b)
                buffer[11] = byteToHexStringHigh[this.Byte4];
                buffer[12] = byteToHexStringLow[this.Byte4];
                buffer[9] = byteToHexStringHigh[this.Byte5];
                buffer[10] = byteToHexStringLow[this.Byte5];

                buffer[13] = (byte)'-';

                // short(_c)
                buffer[16] = byteToHexStringHigh[this.Byte6];
                buffer[17] = byteToHexStringLow[this.Byte6];
                buffer[14] = byteToHexStringHigh[this.Byte7];
                buffer[15] = byteToHexStringLow[this.Byte7];
            }
            else
            {
                buffer[0] = byteToHexStringHigh[this.Byte0];
                buffer[1] = byteToHexStringLow[this.Byte0];
                buffer[2] = byteToHexStringHigh[this.Byte1];
                buffer[3] = byteToHexStringLow[this.Byte1];
                buffer[4] = byteToHexStringHigh[this.Byte2];
                buffer[5] = byteToHexStringLow[this.Byte2];
                buffer[6] = byteToHexStringHigh[this.Byte3];
                buffer[7] = byteToHexStringLow[this.Byte3];

                buffer[8] = (byte)'-';

                buffer[9] = byteToHexStringHigh[this.Byte4];
                buffer[10] = byteToHexStringLow[this.Byte4];
                buffer[11] = byteToHexStringHigh[this.Byte5];
                buffer[12] = byteToHexStringLow[this.Byte5];

                buffer[13] = (byte)'-';

                buffer[14] = byteToHexStringHigh[this.Byte6];
                buffer[15] = byteToHexStringLow[this.Byte6];
                buffer[16] = byteToHexStringHigh[this.Byte7];
                buffer[17] = byteToHexStringLow[this.Byte7];
            }

            buffer[18] = (byte)'-';

            buffer[19] = byteToHexStringHigh[this.Byte8];
            buffer[20] = byteToHexStringLow[this.Byte8];
            buffer[21] = byteToHexStringHigh[this.Byte9];
            buffer[22] = byteToHexStringLow[this.Byte9];

            buffer[23] = (byte)'-';

            buffer[24] = byteToHexStringHigh[this.Byte10];
            buffer[25] = byteToHexStringLow[this.Byte10];
            buffer[26] = byteToHexStringHigh[this.Byte11];
            buffer[27] = byteToHexStringLow[this.Byte11];
            buffer[28] = byteToHexStringHigh[this.Byte12];
            buffer[29] = byteToHexStringLow[this.Byte12];
            buffer[30] = byteToHexStringHigh[this.Byte13];
            buffer[31] = byteToHexStringLow[this.Byte13];
            buffer[32] = byteToHexStringHigh[this.Byte14];
            buffer[33] = byteToHexStringLow[this.Byte14];
            buffer[34] = byteToHexStringHigh[this.Byte15];
            buffer[35] = byteToHexStringLow[this.Byte15];
        }
    }
}

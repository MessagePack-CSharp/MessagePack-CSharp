// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Tests
{
    internal static class TestConstants
    {
        internal const string MultibyteCharString = "にほんごにほんごにほんごにほんごにほんご";

        internal static readonly ReadOnlyMemory<byte> MsgPackEncodedMultibyteCharString = GetMsgPackEncodedMultibyteCharString();

        private static byte[] GetMsgPackEncodedMultibyteCharString()
        {
            byte[] encodedString = Encoding.UTF8.GetBytes(MultibyteCharString);
            byte[] expectedBytes = new byte[2 + encodedString.Length];
            expectedBytes[0] = MessagePackCode.Str8;
            expectedBytes[1] = checked((byte)encodedString.Length);
            Array.Copy(encodedString, 0, expectedBytes, 2, encodedString.Length);
            return expectedBytes;
        }
    }
}

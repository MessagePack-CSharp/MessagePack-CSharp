// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace MessagePack.UWPTests
{
    public class BasicUWPTests
    {
        [Fact]
        public void BasicIntRoundtrip()
        {
            byte[] buffer = MessagePackSerializer.Serialize(5);
            Assert.Equal(5, MessagePackSerializer.Deserialize<int>(buffer));
        }
    }
}

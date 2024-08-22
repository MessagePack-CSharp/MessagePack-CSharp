// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace MessagePack.Tests
{
    public class ExtensionHeaderTests
    {
        [Fact]
        public void Ctor_SByte_Int()
        {
            var eh = new ExtensionHeader(1, 2);
            Assert.Equal(1, eh.TypeCode);
            Assert.Equal(2u, eh.Length);
        }

        [Fact]
        public void Ctor_SByte_UInt()
        {
            var eh = new ExtensionHeader(1, 2u);
            Assert.Equal(1, eh.TypeCode);
            Assert.Equal(2u, eh.Length);
        }

        [Fact]
        public void Equality()
        {
            var eh1 = new ExtensionHeader(1, 2);
            var eh2 = new ExtensionHeader(2, 2);
            var eh3 = new ExtensionHeader(1, 1);

            Assert.True(eh1.Equals(eh1));
            Assert.False(eh1.Equals(eh2));
            Assert.False(eh1.Equals(eh3));
            Assert.False(eh2.Equals(eh3));
        }
    }
}

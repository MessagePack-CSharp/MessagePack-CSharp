// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if NET8_0_OR_GREATER
#nullable enable

using System.Collections.Frozen;
using System.Collections.Generic;
using Xunit;

namespace MessagePack.Tests.ExtensionTests
{
    public class FrozenCollectionTest
    {
        private T Convert<T>(T value)
        {
            MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard;
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, options), options);
        }

        [Fact]
        public void FrozenSet_WithContent()
        {
            FrozenSet<int> populated = new[] { 1, 10, 100 }.ToFrozenSet();
            this.Convert(populated).Is(1, 10, 100);
        }

        [Fact]
        public void FrozenSet_Nullable_WithContent()
        {
            FrozenSet<int>? populatedNullable = new[] { 1, 10, 100 }.ToFrozenSet();
            this.Convert(populatedNullable).Is(1, 10, 100);
        }

        [Fact]
        public void FrozenSet_Nullable_Null()
        {
            FrozenSet<int>? nullNullable = null;
            Assert.Null(this.Convert(nullNullable));
        }

        [Fact]
        public void FrozenSet_Empty()
        {
            FrozenSet<int> defaultArray = FrozenSet<int>.Empty;
            Assert.Equal(FrozenSet<int>.Empty, this.Convert(defaultArray));
        }

        [Fact]
        public void FrozenDictionary_WithContent()
        {
            FrozenDictionary<int, int> populated = new Dictionary<int, int> { { 1, 10 }, { 2, 10 }, { 3, 100 } }.ToFrozenDictionary();
            this.Convert(populated).IsStructuralEqualIgnoreCollectionOrder(populated);
        }

        [Fact]
        public void FrozenDictionary_Nullable_WithContent()
        {
            FrozenDictionary<int, int>? populatedNullable = new Dictionary<int, int> { { 1, 10 }, { 2, 10 }, { 3, 100 } }.ToFrozenDictionary();
            this.Convert(populatedNullable).IsStructuralEqualIgnoreCollectionOrder(populatedNullable);
        }

        [Fact]
        public void FrozenDictionary_Nullable_Null()
        {
            FrozenDictionary<int, int>? nullNullable = null;
            Assert.Null(this.Convert(nullNullable));
        }

        [Fact]
        public void FrozenDictionary_Empty()
        {
            FrozenDictionary<int, int> defaultArray = FrozenDictionary<int, int>.Empty;
            Assert.Equal(FrozenDictionary<int, int>.Empty, this.Convert(defaultArray));
        }
    }
}

#endif

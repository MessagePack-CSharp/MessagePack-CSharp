// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Benchmark.Fixture
{
    public class LongValueFixture : IValueFixture
    {
        private readonly Random prng = new Random();

        public Type Type { get; } = typeof(long);

        public object Generate()
        {
            return ((long)this.prng.Next() << 32) | (uint)this.prng.Next();
        }
    }
}

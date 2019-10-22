// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Benchmark.Fixture
{
    public class ShortValueFixture : IValueFixture
    {
        private readonly Random prng = new Random();

        public Type Type { get; } = typeof(short);

        public object Generate()
        {
            return (short)(this.prng.Next() & 0xFFFF);
        }
    }
}

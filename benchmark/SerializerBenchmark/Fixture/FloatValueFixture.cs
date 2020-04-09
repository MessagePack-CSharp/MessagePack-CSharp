// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Benchmark.Fixture
{
    public class FloatValueFixture : IValueFixture
    {
        private readonly Random prng = new Random();

        public Type Type { get; } = typeof(float);

        public object Generate()
        {
            return ((float)this.prng.Next() % 10000) + 0.5f;
        }
    }
}

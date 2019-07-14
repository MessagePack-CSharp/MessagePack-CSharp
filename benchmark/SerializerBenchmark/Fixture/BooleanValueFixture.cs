// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Benchmark.Fixture
{
    public class BooleanValueFixture : IValueFixture
    {
        private readonly Random prng = new Random();

        public Type Type { get; } = typeof(bool);

        public object Generate()
        {
            return this.prng.Next() % 2 == 1;
        }
    }
}

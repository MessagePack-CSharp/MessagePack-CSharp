// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Benchmark.Fixture
{
    public class DateTimeOffsetValueFixture : IValueFixture
    {
        private long lastValue;

        public Type Type { get; } = typeof(DateTimeOffset);

        public object Generate()
        {
            this.lastValue += 1000;
            return DateTimeOffset.FromUnixTimeMilliseconds(this.lastValue);
        }
    }
}

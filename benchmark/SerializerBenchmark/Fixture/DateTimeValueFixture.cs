// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Benchmark.Fixture
{
    public class DateTimeValueFixture : IValueFixture
    {
        private long lastValue;
        private static readonly long Offset = new DateTime(1970, 1, 1, 0, 0, 0).ToFileTime();

        public Type Type { get; } = typeof(DateTime);

        public object Generate()
        {
            this.lastValue += 1000;
            var dt = DateTime.FromFileTime(this.lastValue + Offset);
            return dt;
        }
    }
}

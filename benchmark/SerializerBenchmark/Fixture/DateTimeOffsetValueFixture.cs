using System;

namespace Benchmark.Fixture
{
    public class DateTimeOffsetValueFixture : IValueFixture
    {
        private long _lastValue;
        public Type Type { get; } = typeof(DateTimeOffset);

        public object Generate()
        {
            _lastValue += 1000;
            return DateTimeOffset.FromUnixTimeMilliseconds(_lastValue);
        }
    }
}
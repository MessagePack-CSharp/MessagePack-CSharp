using System;

namespace Benchmark.Fixture
{
    public class DateTimeValueFixture : IValueFixture
    {
        private long _lastValue;
        private static readonly long Offset = new DateTime(1970, 1,1,0,0,0).ToFileTime();
        public Type Type { get; } = typeof(DateTime);

        public object Generate()
        {
            _lastValue += 1000;
            var dt =  DateTime.FromFileTime(_lastValue+Offset);
            return dt;
        }
    }
}
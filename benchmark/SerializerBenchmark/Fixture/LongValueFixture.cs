using System;

namespace Benchmark.Fixture
{
    public class LongValueFixture : IValueFixture
    {
        private readonly Random _prng = new Random();
        public Type Type { get; } = typeof(long);

        public object Generate()
        {
            return ((long) _prng.Next() << 32) | (uint) _prng.Next();
        }
    }
}
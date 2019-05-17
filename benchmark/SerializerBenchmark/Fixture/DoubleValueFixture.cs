using System;

namespace Benchmark.Fixture
{
    public class DoubleValueFixture : IValueFixture
    {
        private readonly Random _prng = new Random();
        public Type Type { get; } = typeof(double);

        public object Generate()
        {
            return _prng.Next() + 0.5d;
        }
    }
}
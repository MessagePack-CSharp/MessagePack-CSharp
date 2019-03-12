using System;

namespace Benchmark.Fixture
{
    public class FloatValueFixture : IValueFixture
    {
        private readonly Random _prng = new Random();
        public Type Type { get; } = typeof(float);

        public object Generate()
        {
            return (float) _prng.Next() % 10000 + 0.5f;
        }
    }
}
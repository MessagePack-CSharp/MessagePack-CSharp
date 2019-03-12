using System;

namespace Benchmark.Fixture
{
    public class BooleanValueFixture : IValueFixture
    {
        private readonly Random _prng = new Random();
        public Type Type { get; } = typeof(bool);

        public object Generate()
        {
            return _prng.Next() % 2 == 1;
        }
    }
}
using System;

namespace Benchmark.Fixture
{
    public class ShortValueFixture : IValueFixture
    {
        private readonly Random _prng = new Random();
        public Type Type { get; } = typeof(short);

        public object Generate()
        {
            return (short) (_prng.Next() & 0xFFFF);
        }
    }
}
using System;

namespace Benchmark.Fixture
{
    public class ByteValueFixture : IValueFixture
    {
        private readonly Random _prng = new Random();
        public Type Type { get; } = typeof(byte);

        public object Generate()
        {
            return (byte) (_prng.Next() & 0xFF);
        }
    }
}
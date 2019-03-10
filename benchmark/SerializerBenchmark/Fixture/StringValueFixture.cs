using System;

namespace Benchmark.Fixture
{
    public class StringValueFixture : IValueFixture
    {
        private readonly Random _prng = new Random();

        public Type Type { get; } = typeof(string);

        public object Generate()
        {
            return Generate(8);
        }

        private string Generate(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var cArray = new char[length];
            for (var i = 0; i < length; i++)
            {
                cArray[i] = chars[_prng.Next(chars.Length)];
            }
            return new string(cArray);
        }
    }
}
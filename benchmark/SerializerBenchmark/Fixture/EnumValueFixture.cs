using System;

namespace Benchmark.Fixture
{
    public class EnumValueFixture : IValueFixture
    {
        private readonly Random _prng = new Random();
        private readonly string[] _values;

        public EnumValueFixture(Type type)
        {
            Type = type;
            _values = Enum.GetNames(Type);
        }

        public Type Type { get; }

        public object Generate()
        {
            return Enum.Parse(Type, _values[_prng.Next(_values.Length)]);
        }
    }
}
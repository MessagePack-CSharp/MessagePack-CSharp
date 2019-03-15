using System;

namespace Benchmark.Fixture
{
    public class GuidValueFixture : IValueFixture
    {
        public Type Type { get; } = typeof(Guid);

        public object Generate()
        {
            return Guid.NewGuid();
        }
    }
}
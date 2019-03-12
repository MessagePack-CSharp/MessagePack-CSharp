using System;

namespace Benchmark.Fixture
{
    public interface IValueFixture
    {
        Type Type { get; }
        object Generate();
    }
}
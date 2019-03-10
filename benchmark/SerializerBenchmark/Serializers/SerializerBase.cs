using System;

namespace Benchmark.Serializers
{
    public abstract class SerializerBase
    {
        public abstract object Serialize<T>(T input);

        public abstract T Deserialize<T>(object input);


    }
}
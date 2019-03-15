using System;
using System.Runtime.CompilerServices;

namespace Benchmark.Fixture
{
    public delegate T Converter<T>(ReadOnlySpan<byte> buffer);

    public abstract class PrimitiveFixtureBase<T> : IValueFixture
        where T : unmanaged
    {
        private readonly Random rand = new Random();

        public Type Type => typeof(T);

        public abstract object Generate();

        protected unsafe object GenerateBytes(Converter<T> converter)
        {
            Span<byte> buffer = stackalloc byte[sizeof(T)];
            rand.NextBytes(buffer);
            return converter(buffer);
        }
    }

    public class SByteValueFixture : PrimitiveFixtureBase<sbyte>
    {
        public override object Generate()
        {
            return GenerateBytes(x => unchecked((sbyte)x[0]));
        }
    }

    public class UShortValueFixture : PrimitiveFixtureBase<ushort>
    {
        public override object Generate() => GenerateBytes(BitConverter.ToUInt16);
    }

    public class UInt32ValueFixture : PrimitiveFixtureBase<UInt32>
    {
        public override object Generate() => GenerateBytes(BitConverter.ToUInt32);
    }

    public class UInt64ValueFixture : PrimitiveFixtureBase<UInt64>
    {
        public override object Generate() => GenerateBytes(BitConverter.ToUInt64);
    }

    public class CharValueFixture : PrimitiveFixtureBase<Char>
    {
        public override object Generate() => GenerateBytes(BitConverter.ToChar);
    }

    public class ByteArrayFixture : IValueFixture
    {
        private readonly Random _prng = new Random();
        public Type Type { get; } = typeof(byte[]);

        public object Generate()
        {
            var bytes = new byte[100];
            _prng.NextBytes(bytes);
            return bytes;
        }
    }
}
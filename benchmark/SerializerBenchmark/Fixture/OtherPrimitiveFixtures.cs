// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

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
            this.rand.NextBytes(buffer);
            return converter(buffer);
        }
    }

    public class SByteValueFixture : PrimitiveFixtureBase<sbyte>
    {
        public override object Generate()
        {
            return this.GenerateBytes(x => unchecked((sbyte)x[0]));
        }
    }

    public class UShortValueFixture : PrimitiveFixtureBase<ushort>
    {
        public override object Generate() => this.GenerateBytes(BitConverter.ToUInt16);
    }

    public class UInt32ValueFixture : PrimitiveFixtureBase<UInt32>
    {
        public override object Generate() => this.GenerateBytes(BitConverter.ToUInt32);
    }

    public class UInt64ValueFixture : PrimitiveFixtureBase<UInt64>
    {
        public override object Generate() => this.GenerateBytes(BitConverter.ToUInt64);
    }

    public class CharValueFixture : PrimitiveFixtureBase<Char>
    {
        public override object Generate() => this.GenerateBytes(BitConverter.ToChar);
    }

    public class ByteArrayFixture : IValueFixture
    {
        private readonly Random prng = new Random();

        public Type Type { get; } = typeof(byte[]);

        public object Generate()
        {
            var bytes = new byte[100];
            this.prng.NextBytes(bytes);
            return bytes;
        }
    }
}

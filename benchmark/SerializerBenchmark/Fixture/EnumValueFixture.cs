// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Benchmark.Fixture
{
    public class EnumValueFixture : IValueFixture
    {
        private readonly Random prng = new Random();
        private readonly string[] values;

        public EnumValueFixture(Type type)
        {
            this.Type = type;
            this.values = Enum.GetNames(this.Type);
        }

        public Type Type { get; }

        public object Generate()
        {
            return Enum.Parse(this.Type, this.values[this.prng.Next(this.values.Length)]);
        }
    }
}

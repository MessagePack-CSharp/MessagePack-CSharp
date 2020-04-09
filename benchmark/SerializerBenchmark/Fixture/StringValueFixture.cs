// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Benchmark.Fixture
{
    public class StringValueFixture : IValueFixture
    {
        private readonly Random prng = new Random();

        public Type Type { get; } = typeof(string);

        public object Generate()
        {
            return this.Generate(8);
        }

        private string Generate(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var cArray = new char[length];
            for (var i = 0; i < length; i++)
            {
                cArray[i] = chars[this.prng.Next(chars.Length)];
            }

            return new string(cArray);
        }
    }
}

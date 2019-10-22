// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

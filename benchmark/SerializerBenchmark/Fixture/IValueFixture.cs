// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Benchmark.Fixture
{
    public interface IValueFixture
    {
        Type Type { get; }

        object Generate();
    }
}

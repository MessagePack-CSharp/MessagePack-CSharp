// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Benchmark.Serializers
{
    public abstract class SerializerBase
    {
        public abstract object Serialize<T>(T input);

        public abstract T Deserialize<T>(object input);
    }
}

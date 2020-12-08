// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Benchmark.Serializers
{
    public class SpanJsonSerializer : SerializerBase
    {
        public override object Serialize<T>(T input)
        {
            return SpanJson.JsonSerializer.Generic.Utf8.Serialize(input);
        }

        public override T Deserialize<T>(object input)
        {
            return SpanJson.JsonSerializer.Generic.Utf8.Deserialize<T>((byte[])input);
        }

        public override string ToString()
        {
            return "SpanJson";
        }
    }
}

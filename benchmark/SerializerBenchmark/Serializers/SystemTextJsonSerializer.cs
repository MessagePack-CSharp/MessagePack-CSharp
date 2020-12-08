// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Benchmark.Serializers
{
    public class SystemTextJsonSerializer : SerializerBase
    {
        public override object Serialize<T>(T input)
        {
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(input);
        }

        public override T Deserialize<T>(object input)
        {
            var span = (byte[])input;
            return System.Text.Json.JsonSerializer.Deserialize<T>(span);
        }

        public override string ToString()
        {
            return "SystemTextJson";
        }
    }
}

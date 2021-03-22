// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Benchmark.Serializers
{
    public class BinaryFormatterSerializer : SerializerBase
    {
        public override T Deserialize<T>(object input)
        {
            using (var ms = new MemoryStream((byte[])input))
            {
                return (T)new BinaryFormatter().Deserialize(ms);
            }
        }

        public override object Serialize<T>(T input)
        {
            using (var ms = new MemoryStream())
            {
                new BinaryFormatter().Serialize(ms, input);
                ms.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            return "BinaryFormatter";
        }
    }
}

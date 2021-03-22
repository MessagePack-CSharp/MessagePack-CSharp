// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Benchmark.Serializers
{
    public class JsonNetSerializer : SerializerBase
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer();

        public override T Deserialize<T>(object input)
        {
            using (var ms = new MemoryStream((byte[])input))
            using (var sr = new StreamReader(ms, Encoding.UTF8))
            using (var jr = new JsonTextReader(sr))
            {
                return Serializer.Deserialize<T>(jr);
            }
        }

        public override object Serialize<T>(T input)
        {
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms, Encoding.UTF8))
                using (var jw = new JsonTextWriter(sw))
                {
                    Serializer.Serialize(jw, input);
                }

                ms.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            return "JsonNet";
        }
    }
}

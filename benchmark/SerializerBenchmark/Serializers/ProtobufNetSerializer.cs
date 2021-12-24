﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using ProtoBuf;

namespace Benchmark.Serializers
{
    public class ProtobufNetSerializer : SerializerBase
    {
        public override T Deserialize<T>(object input)
        {
            using (var ms = new MemoryStream((byte[])input))
            {
                return Serializer.Deserialize<T>(ms);
            }
        }

        public override object Serialize<T>(T input)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, input);
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            return "ProtobufNet";
        }
    }
}

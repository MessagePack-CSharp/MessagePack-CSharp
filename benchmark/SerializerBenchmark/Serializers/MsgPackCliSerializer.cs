// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Benchmark.Serializers
{
    public class MsgPackCliSerializer : SerializerBase
    {
        public override T Deserialize<T>(object input)
        {
            return MsgPack.Serialization.MessagePackSerializer.Get<T>().UnpackSingleObject((byte[])input);
        }

        public override object Serialize<T>(T input)
        {
            return MsgPack.Serialization.MessagePackSerializer.Get<T>().PackSingleObject(input);
        }

        public override string ToString()
        {
            return "MsgPackCli";
        }
    }
}

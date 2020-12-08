// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Benchmark.Serializers
{
    public class CerasSerializer : SerializerBase
    {
        private Ceras.CerasSerializer ceras = new Ceras.CerasSerializer();

        public override T Deserialize<T>(object input)
        {
            return this.ceras.Deserialize<T>((byte[])input);
        }

        public override object Serialize<T>(T input)
        {
            return this.ceras.Serialize(input);
        }

        public override string ToString()
        {
            return "Ceras";
        }
    }
}

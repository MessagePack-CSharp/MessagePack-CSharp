// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Privilege : IGenericEquality<Privilege>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public string short_description { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public string description { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? reputation { get; set; }

        public bool Equals(Privilege obj)
        {
            return
                this.description.TrueEqualsString(obj.description) &&
                this.reputation.TrueEquals(obj.reputation) &&
                this.short_description.TrueEqualsString(obj.short_description);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.description.TrueEqualsString((string)obj.description) &&
                this.reputation.TrueEquals((int?)obj.reputation) &&
                this.short_description.TrueEqualsString((string)obj.short_description);
        }
    }
}

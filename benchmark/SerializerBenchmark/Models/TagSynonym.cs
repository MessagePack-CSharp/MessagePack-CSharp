// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using System;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class TagSynonym : IGenericEquality<TagSynonym>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public string from_tag { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public string to_tag { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? applied_count { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public DateTime? last_applied_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public DateTime? creation_date { get; set; }

        public bool Equals(TagSynonym obj)
        {
            return
                this.applied_count.TrueEquals(obj.applied_count) &&
                this.creation_date.TrueEquals(obj.creation_date) &&
                this.from_tag.TrueEqualsString(obj.from_tag) &&
                this.last_applied_date.TrueEquals(obj.last_applied_date) &&
                this.to_tag.TrueEqualsString(obj.to_tag);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.applied_count.TrueEquals((int?)obj.applied_count) &&
                this.creation_date.TrueEquals((DateTime?)obj.creation_date) &&
                this.from_tag.TrueEqualsString((string)obj.from_tag) &&
                this.last_applied_date.TrueEquals((DateTime?)obj.last_applied_date) &&
                this.to_tag.TrueEqualsString((string)obj.to_tag);
        }
    }
}

﻿extern alias oldmsgpack;
extern alias newmsgpack;
using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Tag : IGenericEquality<Tag>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public string name { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public int? count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public bool? is_required { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public bool? is_moderator_only { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public int? user_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public bool? has_synonyms { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public DateTime? last_activity_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public List<string> synonyms { get; set; }

        public bool Equals(Tag obj)
        {
            return
                count.TrueEquals(obj.count) &&
                has_synonyms.TrueEquals(obj.has_synonyms) &&
                is_moderator_only.TrueEquals(obj.is_moderator_only) &&
                is_required.TrueEquals(obj.is_required) &&
                last_activity_date.TrueEquals(obj.last_activity_date) &&
                name.TrueEqualsString(obj.name) &&
                synonyms.TrueEqualsString(obj.synonyms) &&
                user_id.TrueEquals(obj.user_id);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                count.TrueEquals((int?) obj.count) &&
                has_synonyms.TrueEquals((bool?) obj.has_synonyms) &&
                is_moderator_only.TrueEquals((bool?) obj.is_moderator_only) &&
                is_required.TrueEquals((bool?) obj.is_required) &&
                last_activity_date.TrueEquals((DateTime?) obj.last_activity_date) &&
                name.TrueEqualsString((string) obj.name) &&
                synonyms.TrueEqualsString((IEnumerable<string>) obj.synonyms) &&
                user_id.TrueEquals((int?) obj.user_id);
        }
    }
}
﻿extern alias oldmsgpack;
extern alias newmsgpack;
using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Benchmark.Models
{
    public enum RevisionType : byte
    {
        single_user = 1,
        vote_based = 2
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Revision : IGenericEquality<Revision>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public string revision_guid { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public int? revision_number { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public RevisionType? revision_type { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public PostType? post_type { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public int? post_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public string comment { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public DateTime? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public bool? is_rollback { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public string last_body { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public string last_title { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public List<string> last_tags { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(12), oldmsgpack::MessagePack.Key(12 - 1), newmsgpack::MessagePack.Key(12 - 1)]
        public string body { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(13), oldmsgpack::MessagePack.Key(13 - 1), newmsgpack::MessagePack.Key(13 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(14), oldmsgpack::MessagePack.Key(14 - 1), newmsgpack::MessagePack.Key(14 - 1)]
        public List<string> tags { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(15), oldmsgpack::MessagePack.Key(15 - 1), newmsgpack::MessagePack.Key(15 - 1)]
        public bool? set_community_wiki { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(16), oldmsgpack::MessagePack.Key(16 - 1), newmsgpack::MessagePack.Key(16 - 1)]
        public ShallowUser user { get; set; }

        public bool Equals(Revision obj)
        {
            return
                body.TrueEqualsString(obj.body) &&
                comment.TrueEqualsString(obj.comment) &&
                creation_date.TrueEquals(obj.creation_date) &&
                is_rollback.TrueEquals(obj.is_rollback) &&
                last_body.TrueEqualsString(obj.last_body) &&
                last_tags.TrueEqualsString(obj.last_tags) &&
                last_title.TrueEqualsString(obj.last_title) &&
                post_id.TrueEquals(obj.post_id) &&
                post_type.TrueEquals(obj.post_type) &&
                revision_guid.TrueEqualsString(obj.revision_guid) &&
                revision_number.TrueEquals(obj.revision_number) &&
                revision_type.TrueEquals(obj.revision_type) &&
                set_community_wiki.TrueEquals(obj.set_community_wiki) &&
                tags.TrueEqualsString(obj.tags) &&
                title.TrueEqualsString(obj.title) &&
                user.TrueEquals(obj.user);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                body.TrueEqualsString((string) obj.body) &&
                comment.TrueEqualsString((string) obj.comment) &&
                creation_date.TrueEquals((DateTime?) obj.creation_date) &&
                is_rollback.TrueEquals((bool?) obj.is_rollback) &&
                last_body.TrueEqualsString((string) obj.last_body) &&
                last_tags.TrueEqualsString((IEnumerable<string>) obj.last_tags) &&
                last_title.TrueEqualsString((string) obj.last_title) &&
                post_id.TrueEquals((int?) obj.post_id) &&
                post_type.TrueEquals((PostType?) obj.post_type) &&
                revision_guid.TrueEqualsString((string) obj.revision_guid) &&
                revision_number.TrueEquals((int?) obj.revision_number) &&
                revision_type.TrueEquals((RevisionType?) obj.revision_type) &&
                set_community_wiki.TrueEquals((bool?) obj.set_community_wiki) &&
                tags.TrueEqualsString((IEnumerable<string>) obj.tags) &&
                title.TrueEqualsString((string) obj.title) &&
                (user == null && obj.user == null || user.EqualsDynamic(obj.user));
        }
    }
}
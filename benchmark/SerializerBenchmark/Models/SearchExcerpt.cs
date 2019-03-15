extern alias oldmsgpack;
extern alias newmsgpack;
using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Benchmark.Models
{
    public enum SearchExcerptItemType : byte
    {
        question = 1,
        answer = 2
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class SearchExcerpt : IGenericEquality<SearchExcerpt>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public string excerpt { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public DateTime? community_owned_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public DateTime? locked_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public DateTime? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public DateTime? last_activity_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public ShallowUser owner { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public ShallowUser last_activity_user { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public int? score { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public SearchExcerptItemType? item_type { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public string body { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(12), oldmsgpack::MessagePack.Key(12 - 1), newmsgpack::MessagePack.Key(12 - 1)]
        public int? question_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(13), oldmsgpack::MessagePack.Key(13 - 1), newmsgpack::MessagePack.Key(13 - 1)]
        public bool? is_answered { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(14), oldmsgpack::MessagePack.Key(14 - 1), newmsgpack::MessagePack.Key(14 - 1)]
        public int? answer_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(15), oldmsgpack::MessagePack.Key(15 - 1), newmsgpack::MessagePack.Key(15 - 1)]
        public List<string> tags { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(16), oldmsgpack::MessagePack.Key(16 - 1), newmsgpack::MessagePack.Key(16 - 1)]
        public DateTime? closed_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(17), oldmsgpack::MessagePack.Key(17 - 1), newmsgpack::MessagePack.Key(17 - 1)]
        public int? answer_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(18), oldmsgpack::MessagePack.Key(18 - 1), newmsgpack::MessagePack.Key(18 - 1)]
        public bool? is_accepted { get; set; }

        public bool Equals(SearchExcerpt obj)
        {
            return
                answer_count.TrueEquals(obj.answer_count) &&
                answer_id.TrueEquals(obj.answer_id) &&
                body.TrueEqualsString(obj.body) &&
                closed_date.TrueEquals(obj.closed_date) &&
                community_owned_date.TrueEquals(obj.community_owned_date) &&
                creation_date.TrueEquals(obj.creation_date) &&
                excerpt.TrueEqualsString(obj.excerpt) &&
                is_accepted.TrueEquals(obj.is_accepted) &&
                is_answered.TrueEquals(obj.is_answered) &&
                item_type.TrueEquals(obj.item_type) &&
                last_activity_date.TrueEquals(obj.last_activity_date) &&
                last_activity_user.TrueEquals(obj.last_activity_user) &&
                locked_date.TrueEquals(obj.locked_date) &&
                owner.TrueEquals(obj.owner) &&
                question_id.TrueEquals(obj.question_id) &&
                score.TrueEquals(obj.score) &&
                tags.TrueEqualsString(obj.tags) &&
                title.TrueEqualsString(obj.title);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                answer_count.TrueEquals((int?) obj.answer_count) &&
                answer_id.TrueEquals((int?) obj.answer_id) &&
                body.TrueEqualsString((string) obj.body) &&
                closed_date.TrueEquals((DateTime?) obj.closed_date) &&
                community_owned_date.TrueEquals((DateTime?) obj.community_owned_date) &&
                creation_date.TrueEquals((DateTime?) obj.creation_date) &&
                excerpt.TrueEqualsString((string) obj.excerpt) &&
                is_accepted.TrueEquals((bool?) obj.is_accepted) &&
                is_answered.TrueEquals((bool?) obj.is_answered) &&
                item_type.TrueEquals((SearchExcerptItemType?) obj.item_type) &&
                last_activity_date.TrueEquals((DateTime?) obj.last_activity_date) &&
                (last_activity_user == null && obj.last_activity_user == null ||
                 last_activity_user.EqualsDynamic(obj.last_activity_user)) &&
                locked_date.TrueEquals((DateTime?) obj.locked_date) &&
                (owner == null && obj.owner == null || owner.EqualsDynamic(obj.owner)) &&
                question_id.TrueEquals((int?) obj.question_id) &&
                score.TrueEquals((int?) obj.score) &&
                tags.TrueEqualsString((IEnumerable<string>) obj.tags) &&
                title.TrueEqualsString((string) obj.title);
        }
    }
}
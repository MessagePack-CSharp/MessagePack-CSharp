extern alias oldmsgpack;
extern alias newmsgpack;
using System;
using ProtoBuf;

namespace Benchmark.Models
{
    public enum QuestionTimelineAction : byte
    {
        question = 1,
        answer = 2,
        comment = 3,
        unaccepted_answer = 4,
        accepted_answer = 5,
        vote_aggregate = 6,
        revision = 7,
        post_state_changed = 8
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class QuestionTimeline : IGenericEquality<QuestionTimeline>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public QuestionTimelineAction? timeline_type { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public int? question_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? post_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public int? comment_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string revision_guid { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public int? up_vote_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? down_vote_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public DateTime? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public ShallowUser user { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public ShallowUser owner { get; set; }

        public bool Equals(QuestionTimeline obj)
        {
            return
                comment_id.TrueEquals(obj.comment_id) &&
                creation_date.TrueEquals(obj.creation_date) &&
                down_vote_count.TrueEquals(obj.down_vote_count) &&
                owner.TrueEquals(obj.owner) &&
                post_id.TrueEquals(obj.post_id) &&
                question_id.TrueEquals(obj.question_id) &&
                revision_guid.TrueEqualsString(obj.revision_guid) &&
                timeline_type.TrueEquals(obj.timeline_type) &&
                up_vote_count.TrueEquals(obj.up_vote_count) &&
                user.TrueEquals(obj.user);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                comment_id.TrueEquals((int?) obj.comment_id) &&
                creation_date.TrueEquals((DateTime?) obj.creation_date) &&
                down_vote_count.TrueEquals((int?) obj.down_vote_count) &&
                (owner == null && obj.owner == null || owner.EqualsDynamic(obj.owner)) &&
                post_id.TrueEquals((int?) obj.post_id) &&
                question_id.TrueEquals((int?) obj.question_id) &&
                revision_guid.TrueEqualsString((string) obj.revision_guid) &&
                timeline_type.TrueEquals((QuestionTimelineAction?) obj.timeline_type) &&
                up_vote_count.TrueEquals((int?) obj.up_vote_count) &&
                (user == null && obj.user == null || user.EqualsDynamic(obj.user));
        }
    }
}
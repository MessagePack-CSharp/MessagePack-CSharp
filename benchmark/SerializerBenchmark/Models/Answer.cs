extern alias oldmsgpack;
extern alias newmsgpack;
using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Answer : IGenericEquality<Answer>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? question_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public int? answer_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public DateTime? locked_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public DateTime? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public DateTime? last_edit_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public DateTime? last_activity_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? score { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public DateTime? community_owned_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public bool? is_accepted { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public string body { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public ShallowUser owner { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(12), oldmsgpack::MessagePack.Key(12 - 1), newmsgpack::MessagePack.Key(12 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(13), oldmsgpack::MessagePack.Key(13 - 1), newmsgpack::MessagePack.Key(13 - 1)]
        public int? up_vote_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(14), oldmsgpack::MessagePack.Key(14 - 1), newmsgpack::MessagePack.Key(14 - 1)]
        public int? down_vote_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(15), oldmsgpack::MessagePack.Key(15 - 1), newmsgpack::MessagePack.Key(15 - 1)]
        public List<Comment> comments { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(16), oldmsgpack::MessagePack.Key(16 - 1), newmsgpack::MessagePack.Key(16 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(17), oldmsgpack::MessagePack.Key(17 - 1), newmsgpack::MessagePack.Key(17 - 1)]
        public List<string> tags { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(18), oldmsgpack::MessagePack.Key(18 - 1), newmsgpack::MessagePack.Key(18 - 1)]
        public bool? upvoted { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(19), oldmsgpack::MessagePack.Key(19 - 1), newmsgpack::MessagePack.Key(19 - 1)]
        public bool? downvoted { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(20), oldmsgpack::MessagePack.Key(20 - 1), newmsgpack::MessagePack.Key(20 - 1)]
        public bool? accepted { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(21), oldmsgpack::MessagePack.Key(21 - 1), newmsgpack::MessagePack.Key(21 - 1)]
        public ShallowUser last_editor { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(22), oldmsgpack::MessagePack.Key(22 - 1), newmsgpack::MessagePack.Key(22 - 1)]
        public int? comment_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(23), oldmsgpack::MessagePack.Key(23 - 1), newmsgpack::MessagePack.Key(23 - 1)]
        public string body_markdown { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(24), oldmsgpack::MessagePack.Key(24 - 1), newmsgpack::MessagePack.Key(24 - 1)]
        public string share_link { get; set; }

        public bool Equals(Answer obj)
        {
            return
                accepted.TrueEquals(obj.accepted) &&
                answer_id.TrueEquals(obj.answer_id) &&
                body.TrueEqualsString(obj.body) &&
                body_markdown.TrueEqualsString(obj.body_markdown) &&
                comment_count.TrueEquals(obj.comment_count) &&
                comments.TrueEqualsList(obj.comments) &&
                community_owned_date.TrueEquals(obj.community_owned_date) &&
                creation_date.TrueEquals(obj.creation_date) &&
                down_vote_count.TrueEquals(obj.down_vote_count) &&
                downvoted.TrueEquals(obj.downvoted) &&
                is_accepted.TrueEquals(obj.is_accepted) &&
                last_activity_date.TrueEquals(obj.last_activity_date) &&
                last_edit_date.TrueEquals(obj.last_edit_date) &&
                last_editor.TrueEquals(obj.last_editor) &&
                link.TrueEqualsString(obj.link) &&
                locked_date.TrueEquals(obj.locked_date) &&
                owner.TrueEquals(obj.owner) &&
                question_id.TrueEquals(obj.question_id) &&
                score.TrueEquals(obj.score) &&
                share_link.TrueEqualsString(obj.share_link) &&
                tags.TrueEqualsString(obj.tags) &&
                title.TrueEqualsString(obj.title) &&
                up_vote_count.TrueEquals(obj.up_vote_count) &&
                upvoted.TrueEquals(obj.upvoted);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                accepted.TrueEquals((bool?) obj.accepted) &&
                answer_id.TrueEquals((int?) obj.answer_id) &&
                body.TrueEqualsString((string) obj.body) &&
                body_markdown.TrueEqualsString((string) obj.body_markdown) &&
                comment_count.TrueEquals((int?) obj.comment_count) &&
                comments.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.comments) &&
                community_owned_date.TrueEquals((DateTime?) obj.community_owned_date) &&
                creation_date.TrueEquals((DateTime?) obj.creation_date) &&
                down_vote_count.TrueEquals((int?) obj.down_vote_count) &&
                downvoted.TrueEquals((bool?) obj.downvoted) &&
                is_accepted.TrueEquals((bool?) obj.is_accepted) &&
                last_activity_date.TrueEquals((DateTime?) obj.last_activity_date) &&
                last_edit_date.TrueEquals((DateTime?) obj.last_edit_date) &&
                (last_editor == null && obj.last_editor == null || last_editor.EqualsDynamic(obj.last_editor)) &&
                link.TrueEqualsString((string) obj.link) &&
                locked_date.TrueEquals((DateTime?) obj.locked_date) &&
                (owner == null && obj.owner == null || owner.EqualsDynamic(obj.owner)) &&
                question_id.TrueEquals((int?) obj.question_id) &&
                score.TrueEquals((int?) obj.score) &&
                share_link.TrueEqualsString((string) obj.share_link) &&
                tags.TrueEqualsString((IEnumerable<string>) obj.tags) &&
                title.TrueEqualsString((string) obj.title) &&
                up_vote_count.TrueEquals((int?) obj.up_vote_count) &&
                upvoted.TrueEquals((bool?) obj.upvoted);
        }
    }
}
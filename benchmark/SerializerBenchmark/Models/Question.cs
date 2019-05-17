extern alias oldmsgpack;
extern alias newmsgpack;
using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Question : IGenericEquality<Question>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? question_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public DateTime? last_edit_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public DateTime? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public DateTime? last_activity_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public DateTime? locked_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public int? score { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public DateTime? community_owned_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public int? answer_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public int? accepted_answer_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public MigrationInfo migrated_to { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public MigrationInfo migrated_from { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(12), oldmsgpack::MessagePack.Key(12 - 1), newmsgpack::MessagePack.Key(12 - 1)]
        public DateTime? bounty_closes_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(13), oldmsgpack::MessagePack.Key(13 - 1), newmsgpack::MessagePack.Key(13 - 1)]
        public int? bounty_amount { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(14), oldmsgpack::MessagePack.Key(14 - 1), newmsgpack::MessagePack.Key(14 - 1)]
        public DateTime? closed_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(15), oldmsgpack::MessagePack.Key(15 - 1), newmsgpack::MessagePack.Key(15 - 1)]
        public DateTime? protected_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(16), oldmsgpack::MessagePack.Key(16 - 1), newmsgpack::MessagePack.Key(16 - 1)]
        public string body { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(17), oldmsgpack::MessagePack.Key(17 - 1), newmsgpack::MessagePack.Key(17 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(18), oldmsgpack::MessagePack.Key(18 - 1), newmsgpack::MessagePack.Key(18 - 1)]
        public List<string> tags { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(19), oldmsgpack::MessagePack.Key(19 - 1), newmsgpack::MessagePack.Key(19 - 1)]
        public string closed_reason { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(20), oldmsgpack::MessagePack.Key(20 - 1), newmsgpack::MessagePack.Key(20 - 1)]
        public int? up_vote_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(21), oldmsgpack::MessagePack.Key(21 - 1), newmsgpack::MessagePack.Key(21 - 1)]
        public int? down_vote_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(22), oldmsgpack::MessagePack.Key(22 - 1), newmsgpack::MessagePack.Key(22 - 1)]
        public int? favorite_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(23), oldmsgpack::MessagePack.Key(23 - 1), newmsgpack::MessagePack.Key(23 - 1)]
        public int? view_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(24), oldmsgpack::MessagePack.Key(24 - 1), newmsgpack::MessagePack.Key(24 - 1)]
        public ShallowUser owner { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(25), oldmsgpack::MessagePack.Key(25 - 1), newmsgpack::MessagePack.Key(25 - 1)]
        public List<Comment> comments { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(26), oldmsgpack::MessagePack.Key(26 - 1), newmsgpack::MessagePack.Key(26 - 1)]
        public List<Answer> answers { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(27), oldmsgpack::MessagePack.Key(27 - 1), newmsgpack::MessagePack.Key(27 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(28), oldmsgpack::MessagePack.Key(28 - 1), newmsgpack::MessagePack.Key(28 - 1)]
        public bool? is_answered { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(29), oldmsgpack::MessagePack.Key(29 - 1), newmsgpack::MessagePack.Key(29 - 1)]
        public int? close_vote_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(30), oldmsgpack::MessagePack.Key(30 - 1), newmsgpack::MessagePack.Key(30 - 1)]
        public int? reopen_vote_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(31), oldmsgpack::MessagePack.Key(31 - 1), newmsgpack::MessagePack.Key(31 - 1)]
        public int? delete_vote_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(32), oldmsgpack::MessagePack.Key(32 - 1), newmsgpack::MessagePack.Key(32 - 1)]
        public Notice notice { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(33), oldmsgpack::MessagePack.Key(33 - 1), newmsgpack::MessagePack.Key(33 - 1)]
        public bool? upvoted { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(34), oldmsgpack::MessagePack.Key(34 - 1), newmsgpack::MessagePack.Key(34 - 1)]
        public bool? downvoted { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(35), oldmsgpack::MessagePack.Key(35 - 1), newmsgpack::MessagePack.Key(35 - 1)]
        public bool? favorited { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(36), oldmsgpack::MessagePack.Key(36 - 1), newmsgpack::MessagePack.Key(36 - 1)]
        public ShallowUser last_editor { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(37), oldmsgpack::MessagePack.Key(37 - 1), newmsgpack::MessagePack.Key(37 - 1)]
        public int? comment_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(38), oldmsgpack::MessagePack.Key(38 - 1), newmsgpack::MessagePack.Key(38 - 1)]
        public string body_markdown { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(39), oldmsgpack::MessagePack.Key(39 - 1), newmsgpack::MessagePack.Key(39 - 1)]
        public ClosedDetails closed_details { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(40), oldmsgpack::MessagePack.Key(40 - 1), newmsgpack::MessagePack.Key(40 - 1)]
        public string share_link { get; set; }

        public bool Equals(Question obj)
        {
            return
                accepted_answer_id.TrueEquals(obj.accepted_answer_id) &&
                answer_count.TrueEquals(obj.answer_count) &&
                answers.TrueEqualsList(obj.answers) &&
                body.TrueEqualsString(obj.body) &&
                body_markdown.TrueEqualsString(obj.body_markdown) &&
                bounty_amount.TrueEquals(obj.bounty_amount) &&
                bounty_closes_date.TrueEquals(obj.bounty_closes_date) &&
                close_vote_count.TrueEquals(obj.close_vote_count) &&
                closed_date.TrueEquals(obj.closed_date) &&
                closed_details.TrueEquals(obj.closed_details) &&
                closed_reason.TrueEqualsString(obj.closed_reason) &&
                comment_count.TrueEquals(obj.comment_count) &&
                comments.TrueEqualsList(obj.comments) &&
                community_owned_date.TrueEquals(obj.community_owned_date) &&
                creation_date.TrueEquals(obj.creation_date) &&
                delete_vote_count.TrueEquals(obj.delete_vote_count) &&
                down_vote_count.TrueEquals(obj.down_vote_count) &&
                downvoted.TrueEquals(obj.downvoted) &&
                favorite_count.TrueEquals(obj.favorite_count) &&
                favorited.TrueEquals(obj.favorited) &&
                is_answered.TrueEquals(obj.is_answered) &&
                last_activity_date.TrueEquals(obj.last_activity_date) &&
                last_edit_date.TrueEquals(obj.last_edit_date) &&
                last_editor.TrueEquals(obj.last_editor) &&
                link.TrueEqualsString(obj.link) &&
                locked_date.TrueEquals(obj.locked_date) &&
                migrated_from.TrueEquals(obj.migrated_from) &&
                migrated_to.TrueEquals(obj.migrated_to) &&
                notice.TrueEquals(obj.notice) &&
                owner.TrueEquals(obj.owner) &&
                protected_date.TrueEquals(obj.protected_date) &&
                question_id.TrueEquals(obj.question_id) &&
                reopen_vote_count.TrueEquals(obj.reopen_vote_count) &&
                score.TrueEquals(obj.score) &&
                share_link.TrueEqualsString(obj.share_link) &&
                tags.TrueEqualsString(obj.tags) &&
                title.TrueEqualsString(obj.title) &&
                up_vote_count.TrueEquals(obj.up_vote_count) &&
                upvoted.TrueEquals(obj.upvoted) &&
                view_count.TrueEquals(obj.view_count);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                accepted_answer_id.TrueEquals((int?) obj.accepted_answer_id) &&
                answer_count.TrueEquals((int?) obj.answer_count) &&
                answers.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.answers) &&
                body.TrueEqualsString((string) obj.body) &&
                body_markdown.TrueEqualsString((string) obj.body_markdown) &&
                bounty_amount.TrueEquals((int?) obj.bounty_amount) &&
                bounty_closes_date.TrueEquals((DateTime?) obj.bounty_closes_date) &&
                close_vote_count.TrueEquals((int?) obj.close_vote_count) &&
                closed_date.TrueEquals((DateTime?) obj.closed_date) &&
                (closed_details == null && obj.closed_details == null ||
                 closed_details.EqualsDynamic(obj.closed_details)) &&
                closed_reason.TrueEqualsString((string) obj.closed_reason) &&
                comment_count.TrueEquals((int?) obj.comment_count) &&
                comments.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.comments) &&
                community_owned_date.TrueEquals((DateTime?) obj.community_owned_date) &&
                creation_date.TrueEquals((DateTime?) obj.creation_date) &&
                delete_vote_count.TrueEquals((int?) obj.delete_vote_count) &&
                down_vote_count.TrueEquals((int?) obj.down_vote_count) &&
                downvoted.TrueEquals((bool?) obj.downvoted) &&
                favorite_count.TrueEquals((int?) obj.favorite_count) &&
                favorited.TrueEquals((bool?) obj.favorited) &&
                is_answered.TrueEquals((bool?) obj.is_answered) &&
                last_activity_date.TrueEquals((DateTime?) obj.last_activity_date) &&
                last_edit_date.TrueEquals((DateTime?) obj.last_edit_date) &&
                (last_editor == null && obj.last_editor == null || last_editor.EqualsDynamic(obj.last_editor)) &&
                link.TrueEqualsString((string) obj.link) &&
                locked_date.TrueEquals((DateTime?) obj.locked_date) &&
                (migrated_from == null && obj.migrated_from == null ||
                 migrated_from.EqualsDynamic(obj.migrated_from)) &&
                (migrated_to == null && obj.migrated_to == null || migrated_to.EqualsDynamic(obj.migrated_to)) &&
                (notice == null && obj.notice == null || notice.EqualsDynamic(obj.notice)) &&
                (owner == null && obj.owner == null || owner.EqualsDynamic(obj.owner)) &&
                protected_date.TrueEquals((DateTime?) obj.protected_date) &&
                question_id.TrueEquals((int?) obj.question_id) &&
                reopen_vote_count.TrueEquals((int?) obj.reopen_vote_count) &&
                score.TrueEquals((int?) obj.score) &&
                share_link.TrueEqualsString((string) obj.share_link) &&
                tags.TrueEqualsString((IEnumerable<string>) obj.tags) &&
                title.TrueEqualsString((string) obj.title) &&
                up_vote_count.TrueEquals((int?) obj.up_vote_count) &&
                upvoted.TrueEquals((bool?) obj.upvoted) &&
                view_count.TrueEquals((int?) obj.view_count);
        }

        [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
        public class ClosedDetails : IGenericEquality<ClosedDetails>
        {
            [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
            public bool? on_hold { get; set; }

            [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
            public string reason { get; set; }

            [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
            public string description { get; set; }

            [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
            public List<ShallowUser> by_users { get; set; }

            [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
            public List<OriginalQuestion> original_questions { get; set; }

            public bool Equals(ClosedDetails obj)
            {
                return
                    by_users.TrueEqualsList(obj.by_users) &&
                    description.TrueEqualsString(obj.description) &&
                    on_hold.TrueEquals(obj.on_hold) &&
                    original_questions.TrueEqualsList(obj.original_questions) &&
                    reason.TrueEqualsString(obj.reason);
            }

            public bool EqualsDynamic(dynamic obj)
            {
                var oq = obj.original_questions;
                var oqI = (IEnumerable<dynamic>) oq;

                return
                    by_users.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.by_users) &&
                    description.TrueEqualsString((string) obj.description) &&
                    on_hold.TrueEquals((bool?) obj.on_hold) &&
                    //this.original_questions.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.original_questions) &&
                    original_questions.TrueEqualsListDynamic(oqI) &&
                    reason.TrueEqualsString((string) obj.reason);
            }

            [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
            public class OriginalQuestion : IGenericEquality<OriginalQuestion>
            {
                [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
                public int? question_id { get; set; }

                [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
                public string title { get; set; }

                [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
                public int? answer_count { get; set; }

                [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
                public int? accepted_answer_id { get; set; }

                public bool Equals(OriginalQuestion obj)
                {
                    return
                        accepted_answer_id.TrueEquals(obj.accepted_answer_id) &&
                        answer_count.TrueEquals(obj.answer_count) &&
                        question_id.TrueEquals(obj.question_id) &&
                        title.TrueEqualsString(obj.title);
                }

                public bool EqualsDynamic(dynamic obj)
                {
                    return
                        accepted_answer_id.TrueEquals((int?) obj.accepted_answer_id) &&
                        answer_count.TrueEquals((int?) obj.answer_count) &&
                        question_id.TrueEquals((int?) obj.question_id) &&
                        title.TrueEqualsString((string) obj.title);
                }
            }
        }

        [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
        public class Notice : IGenericEquality<Notice>
        {
            [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
            public string body { get; set; }

            [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
            public DateTime? creation_date { get; set; }

            [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
            public int? owner_user_id { get; set; }

            public bool Equals(Notice obj)
            {
                return
                    body.TrueEqualsString(obj.body) &&
                    creation_date.TrueEquals(obj.creation_date) &&
                    owner_user_id.TrueEquals(obj.owner_user_id);
            }

            public bool EqualsDynamic(dynamic obj)
            {
                return
                    body.TrueEqualsString((string) obj.body) &&
                    creation_date.TrueEquals((DateTime?) obj.creation_date) &&
                    owner_user_id.TrueEquals((int?) obj.owner_user_id);
            }
        }

        [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
        public class MigrationInfo : IGenericEquality<MigrationInfo>
        {
            [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
            public int? question_id { get; set; }

            [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
            public Info.Site other_site { get; set; }

            [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
            public DateTime? on_date { get; set; }

            public bool Equals(MigrationInfo obj)
            {
                return
                    on_date.TrueEquals(obj.on_date) &&
                    other_site.TrueEquals(obj.other_site) &&
                    question_id.TrueEquals(obj.question_id);
            }

            public bool EqualsDynamic(dynamic obj)
            {
                return
                    on_date.TrueEquals((DateTime?) obj.on_date) &&
                    (other_site == null && obj.other_site == null || other_site.EqualsDynamic(obj.other_site)) &&
                    question_id.TrueEquals((int?) obj.question_id);
            }
        }
    }
}
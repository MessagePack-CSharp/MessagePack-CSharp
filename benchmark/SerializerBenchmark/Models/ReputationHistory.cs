// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using System;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class ReputationHistory : IGenericEquality<ReputationHistory>
    {
        public enum ReputationHistoryType : byte
        {
            asker_accepts_answer = 1,
            asker_unaccept_answer = 2,
            answer_accepted = 3,
            answer_unaccepted = 4,

            voter_downvotes = 5,
            voter_undownvotes = 6,
            post_downvoted = 7,
            post_undownvoted = 8,

            post_upvoted = 9,
            post_unupvoted = 10,

            suggested_edit_approval_received = 11,

            post_flagged_as_spam = 12,
            post_flagged_as_offensive = 13,

            bounty_given = 14,
            bounty_earned = 15,
            bounty_cancelled = 16,

            post_deleted = 17,
            post_undeleted = 18,

            association_bonus = 19,
            arbitrary_reputation_change = 20,

            vote_fraud_reversal = 21,

            post_migrated = 22,

            user_deleted = 23,
        }

        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? user_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public DateTime? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? post_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public int? reputation_change { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public ReputationHistoryType? reputation_history_type { get; set; }

        public bool Equals(ReputationHistory obj)
        {
            return
                this.creation_date.TrueEquals(obj.creation_date) &&
                this.post_id.TrueEquals(obj.post_id) &&
                this.reputation_change.TrueEquals(obj.reputation_change) &&
                this.reputation_history_type.TrueEquals(obj.reputation_history_type) &&
                this.user_id.TrueEquals(obj.user_id);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.creation_date.TrueEquals((DateTime?)obj.creation_date) &&
                this.post_id.TrueEquals((int?)obj.post_id) &&
                this.reputation_change.TrueEquals((int?)obj.reputation_change) &&
                this.reputation_history_type.TrueEquals((ReputationHistoryType?)obj.reputation_history_type) &&
                this.user_id.TrueEquals((int?)obj.user_id);
        }
    }
}

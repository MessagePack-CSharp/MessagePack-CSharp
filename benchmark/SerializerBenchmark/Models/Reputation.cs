// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using System;
using ProtoBuf;

namespace Benchmark.Models
{
    public enum VoteType : byte
    {
        up_votes = 2,
        down_votes = 3,
        spam = 12,
        accepts = 1,
        bounties_won = 9,
        bounties_offered = 8,
        suggested_edits = 16,
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Reputation : IGenericEquality<Reputation>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? user_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public int? post_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public PostType? post_type { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public VoteType? vote_type { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? reputation_change { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public DateTime? on_date { get; set; }

        public bool Equals(Reputation obj)
        {
            return
                this.link.TrueEqualsString(obj.link) &&
                this.on_date.TrueEquals(obj.on_date) &&
                this.post_id.TrueEquals(obj.post_id) &&
                this.post_type.TrueEquals(obj.post_type) &&
                this.reputation_change.TrueEquals(obj.reputation_change) &&
                this.title.TrueEqualsString(obj.title) &&
                this.user_id.TrueEquals(obj.user_id) &&
                this.vote_type.TrueEquals(obj.vote_type);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.link.TrueEqualsString((string)obj.link) &&
                this.on_date.TrueEquals((DateTime?)obj.on_date) &&
                this.post_id.TrueEquals((int?)obj.post_id) &&
                this.post_type.TrueEquals((PostType?)obj.post_type) &&
                this.reputation_change.TrueEquals((int?)obj.reputation_change) &&
                this.title.TrueEqualsString((string)obj.title) &&
                this.user_id.TrueEquals((int?)obj.user_id) &&
                this.vote_type.TrueEquals((VoteType?)obj.vote_type);
        }
    }
}

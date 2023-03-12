// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable IDE1006
#pragma warning disable SA1516

extern alias oldmsgpack;
extern alias newmsgpack;

using System;
using ProtoBuf;

namespace Benchmark.Models
{
    public enum PostType : byte
    {
        question = 1,
        answer = 2,
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Comment : IGenericEquality<Comment>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? comment_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public int? post_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public DateTime? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public PostType? post_type { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public int? score { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public bool? edited { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public string body { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public ShallowUser owner { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public ShallowUser reply_to_user { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public string body_markdown { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(12), oldmsgpack::MessagePack.Key(12 - 1), newmsgpack::MessagePack.Key(12 - 1)]
        public bool? upvoted { get; set; }

        public bool Equals(Comment obj)
        {
            return
                this.body.TrueEqualsString(obj.body) &&
                this.body_markdown.TrueEqualsString(obj.body_markdown) &&
                this.comment_id.TrueEquals(obj.comment_id) &&
                this.creation_date.TrueEquals(obj.creation_date) &&
                this.edited.TrueEquals(obj.edited) &&
                this.link.TrueEqualsString(obj.link) &&
                this.owner.TrueEquals(obj.owner) &&
                this.post_id.TrueEquals(obj.post_id) &&
                this.post_type.TrueEquals(obj.post_type) &&
                this.reply_to_user.TrueEquals(obj.reply_to_user) &&
                this.score.TrueEquals(obj.score) &&
                this.upvoted.TrueEquals(obj.upvoted);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.body.TrueEqualsString((string)obj.body) &&
                this.body_markdown.TrueEqualsString((string)obj.body_markdown) &&
                this.comment_id.TrueEquals((int?)obj.comment_id) &&
                this.creation_date.TrueEquals((DateTime?)obj.creation_date) &&
                this.edited.TrueEquals((bool?)obj.edited) &&
                this.link.TrueEqualsString((string)obj.link) &&
                ((this.owner == null && obj.owner == null) || this.owner.EqualsDynamic(obj.owner)) &&
                this.post_id.TrueEquals((int?)obj.post_id) &&
                this.post_type.TrueEquals((PostType?)obj.post_type) &&
                ((this.reply_to_user == null && obj.reply_to_user == null) ||
                 this.reply_to_user.EqualsDynamic(obj.reply_to_user)) &&
                this.score.TrueEquals((int?)obj.score) &&
                this.upvoted.TrueEquals((bool?)obj.upvoted);
        }
    }

    [oldmsgpack::MessagePack.MessagePackObject(true), newmsgpack::MessagePack.MessagePackObject(true)]
    public class Comment2 : IGenericEquality<Comment2>
    {
        public int? comment_id { get; set; }
        public int? post_id { get; set; }
        public DateTime? creation_date { get; set; }
        public PostType? post_type { get; set; }
        public int? score { get; set; }
        public bool? edited { get; set; }
        public string body { get; set; }
        public ShallowUser2 owner { get; set; }
        public ShallowUser2 reply_to_user { get; set; }
        public string link { get; set; }
        public string body_markdown { get; set; }
        public bool? upvoted { get; set; }

        public bool Equals(Comment2 obj)
        {
            return
                this.body.TrueEqualsString(obj.body) &&
                this.body_markdown.TrueEqualsString(obj.body_markdown) &&
                this.comment_id.TrueEquals(obj.comment_id) &&
                this.creation_date.TrueEquals(obj.creation_date) &&
                this.edited.TrueEquals(obj.edited) &&
                this.link.TrueEqualsString(obj.link) &&
                this.owner.TrueEquals(obj.owner) &&
                this.post_id.TrueEquals(obj.post_id) &&
                this.post_type.TrueEquals(obj.post_type) &&
                this.reply_to_user.TrueEquals(obj.reply_to_user) &&
                this.score.TrueEquals(obj.score) &&
                this.upvoted.TrueEquals(obj.upvoted);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.body.TrueEqualsString((string)obj.body) &&
                this.body_markdown.TrueEqualsString((string)obj.body_markdown) &&
                this.comment_id.TrueEquals((int?)obj.comment_id) &&
                this.creation_date.TrueEquals((DateTime?)obj.creation_date) &&
                this.edited.TrueEquals((bool?)obj.edited) &&
                this.link.TrueEqualsString((string)obj.link) &&
                ((this.owner == null && obj.owner == null) || this.owner.EqualsDynamic(obj.owner)) &&
                this.post_id.TrueEquals((int?)obj.post_id) &&
                this.post_type.TrueEquals((PostType?)obj.post_type) &&
                ((this.reply_to_user == null && obj.reply_to_user == null) ||
                 this.reply_to_user.EqualsDynamic(obj.reply_to_user)) &&
                this.score.TrueEquals((int?)obj.score) &&
                this.upvoted.TrueEquals((bool?)obj.upvoted);
        }
    }
}

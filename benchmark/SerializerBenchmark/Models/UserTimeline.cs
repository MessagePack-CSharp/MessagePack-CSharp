// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using System;
using ProtoBuf;

namespace Benchmark.Models
{
    public enum UserTimelineType : byte
    {
        commented = 1,
        asked = 2,
        answered = 3,
        badge = 4,
        revision = 5,
        accepted = 6,
        reviewed = 7,
        suggested = 8,
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class UserTimeline : IGenericEquality<UserTimeline>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public DateTime? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public PostType? post_type { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public UserTimelineType? timeline_type { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public int? user_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public int? post_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public int? comment_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? suggested_edit_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public int? badge_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public string detail { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public string link { get; set; }

        public bool Equals(UserTimeline obj)
        {
            return
                this.badge_id.TrueEquals(obj.badge_id) &&
                this.comment_id.TrueEquals(obj.comment_id) &&
                this.creation_date.TrueEquals(obj.creation_date) &&
                this.detail.TrueEqualsString(obj.detail) &&
                this.link.TrueEqualsString(obj.link) &&
                this.post_id.TrueEquals(obj.post_id) &&
                this.post_type.TrueEquals(obj.post_type) &&
                this.suggested_edit_id.TrueEquals(obj.suggested_edit_id) &&
                this.timeline_type.TrueEquals(obj.timeline_type) &&
                this.title.TrueEqualsString(obj.title) &&
                this.user_id.TrueEquals(obj.user_id);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.badge_id.TrueEquals((int?)obj.badge_id) &&
                this.comment_id.TrueEquals((int?)obj.comment_id) &&
                this.creation_date.TrueEquals((DateTime?)obj.creation_date) &&
                this.detail.TrueEqualsString((string)obj.detail) &&
                this.link.TrueEqualsString((string)obj.link) &&
                this.post_id.TrueEquals((int?)obj.post_id) &&
                this.post_type.TrueEquals((PostType?)obj.post_type) &&
                this.suggested_edit_id.TrueEquals((int?)obj.suggested_edit_id) &&
                this.timeline_type.TrueEquals((UserTimelineType?)obj.timeline_type) &&
                this.title.TrueEqualsString((string)obj.title) &&
                this.user_id.TrueEquals((int?)obj.user_id);
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using System;
using ProtoBuf;

namespace Benchmark.Models
{
    public enum NotificationType : byte
    {
        generic = 1,
        accounts_associated = 8,
        badge_earned = 5,
        profile_activity = 2,
        bounty_expired = 3,
        bounty_expires_in_one_day = 4,
        bounty_expires_in_three_days = 6,
        edit_suggested = 22,
        new_privilege = 9,
        post_migrated = 10,
        moderator_message = 11,
        registration_reminder = 12,
        substantive_edit = 23,
        reputation_bonus = 7,
        bounty_grace_period_started = 24,
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Notification : IGenericEquality<Notification>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public NotificationType? notification_type { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public Info.Site site { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public DateTime? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string body { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public int? post_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public bool? is_unread { get; set; }

        public bool Equals(Notification obj)
        {
            return
                this.body.TrueEqualsString(obj.body) &&
                this.site.TrueEquals(obj.site) &&
                this.creation_date.TrueEquals(obj.creation_date) &&
                this.post_id.TrueEquals(obj.post_id) &&
                this.is_unread.TrueEquals(obj.is_unread);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.body.TrueEqualsString((string)obj.body) &&
                ((this.site == null && obj.site == null) || this.site.EqualsDynamic(obj.site)) &&
                this.creation_date.TrueEquals((DateTime?)obj.creation_date) &&
                this.post_id.TrueEquals((int?)obj.post_id) &&
                this.is_unread.TrueEquals((bool?)obj.is_unread);
        }
    }
}

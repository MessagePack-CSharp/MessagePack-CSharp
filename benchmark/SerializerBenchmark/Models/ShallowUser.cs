// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable IDE1006
#pragma warning disable SA1516

extern alias oldmsgpack;
extern alias newmsgpack;

using ProtoBuf;

namespace Benchmark.Models
{
    public enum UserType : byte
    {
        unregistered = 2,
        registered = 3,
        moderator = 4,
        does_not_exist = 255,
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class ShallowUser : IGenericEquality<ShallowUser>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? user_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public string display_name { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? reputation { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public UserType? user_type { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string profile_image { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? accept_rate { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public User.BadgeCount badge_counts { get; set; }

        public bool Equals(ShallowUser obj)
        {
            return
                this.accept_rate.TrueEquals(obj.accept_rate) &&
                this.badge_counts.TrueEquals(obj.badge_counts) &&
                this.display_name.TrueEqualsString(obj.display_name) &&
                this.link.TrueEqualsString(obj.link) &&
                this.profile_image.TrueEqualsString(obj.profile_image) &&
                this.reputation.TrueEquals(obj.reputation) &&
                this.user_id.TrueEquals(obj.user_id) &&
                this.user_type.TrueEquals(obj.user_type);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.accept_rate.TrueEquals((int?)obj.accept_rate) &&
                ((this.badge_counts == null && obj.badge_counts == null) || this.badge_counts.EqualsDynamic(obj.badge_counts)) &&
                this.display_name.TrueEqualsString((string)obj.display_name) &&
                this.link.TrueEqualsString((string)obj.link) &&
                this.profile_image.TrueEqualsString((string)obj.profile_image) &&
                this.reputation.TrueEquals((int?)obj.reputation) &&
                this.user_id.TrueEquals((int?)obj.user_id) &&
                this.user_type.TrueEquals((UserType?)obj.user_type);
        }
    }

    [oldmsgpack::MessagePack.MessagePackObject(true), newmsgpack::MessagePack.MessagePackObject(true)]
    public class ShallowUser2 : IGenericEquality<ShallowUser2>
    {
        public int? user_id { get; set; }
        public string display_name { get; set; }
        public int? reputation { get; set; }
        public UserType? user_type { get; set; }
        public string profile_image { get; set; }
        public string link { get; set; }
        public int? accept_rate { get; set; }
        public User.BadgeCount2 badge_counts { get; set; }

        public bool Equals(ShallowUser2 obj)
        {
            return
                this.accept_rate.TrueEquals(obj.accept_rate) &&
                this.badge_counts.TrueEquals(obj.badge_counts) &&
                this.display_name.TrueEqualsString(obj.display_name) &&
                this.link.TrueEqualsString(obj.link) &&
                this.profile_image.TrueEqualsString(obj.profile_image) &&
                this.reputation.TrueEquals(obj.reputation) &&
                this.user_id.TrueEquals(obj.user_id) &&
                this.user_type.TrueEquals(obj.user_type);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.accept_rate.TrueEquals((int?)obj.accept_rate) &&
                ((this.badge_counts == null && obj.badge_counts == null) || this.badge_counts.EqualsDynamic(obj.badge_counts)) &&
                this.display_name.TrueEqualsString((string)obj.display_name) &&
                this.link.TrueEqualsString((string)obj.link) &&
                this.profile_image.TrueEqualsString((string)obj.profile_image) &&
                this.reputation.TrueEquals((int?)obj.reputation) &&
                this.user_id.TrueEquals((int?)obj.user_id) &&
                this.user_type.TrueEquals((UserType?)obj.user_type);
        }
    }
}

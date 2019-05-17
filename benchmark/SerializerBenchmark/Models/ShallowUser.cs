﻿extern alias oldmsgpack;
extern alias newmsgpack;
using ProtoBuf;

namespace Benchmark.Models
{
    public enum UserType : byte
    {
        unregistered = 2,
        registered = 3,
        moderator = 4,
        does_not_exist = 255
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class ShallowUser : IGenericEquality<ShallowUser>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? user_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public string display_name { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? reputation { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public UserType? user_type { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string profile_image { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? accept_rate { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public User.BadgeCount badge_counts { get; set; }

        public bool Equals(ShallowUser obj)
        {
            return
                accept_rate.TrueEquals(obj.accept_rate) &&
                badge_counts.TrueEquals(obj.badge_counts) &&
                display_name.TrueEqualsString(obj.display_name) &&
                link.TrueEqualsString(obj.link) &&
                profile_image.TrueEqualsString(obj.profile_image) &&
                reputation.TrueEquals(obj.reputation) &&
                user_id.TrueEquals(obj.user_id) &&
                user_type.TrueEquals(obj.user_type);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                accept_rate.TrueEquals((int?) obj.accept_rate) &&
                (badge_counts == null && obj.badge_counts == null || badge_counts.EqualsDynamic(obj.badge_counts)) &&
                display_name.TrueEqualsString((string) obj.display_name) &&
                link.TrueEqualsString((string) obj.link) &&
                profile_image.TrueEqualsString((string) obj.profile_image) &&
                reputation.TrueEquals((int?) obj.reputation) &&
                user_id.TrueEquals((int?) obj.user_id) &&
                user_type.TrueEquals((UserType?) obj.user_type);
        }
    }
}
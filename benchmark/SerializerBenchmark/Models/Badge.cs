extern alias oldmsgpack;
extern alias newmsgpack;
using ProtoBuf;

namespace Benchmark.Models
{
    public enum BadgeRank : byte
    {
        bronze = 3,
        silver = 2,
        gold = 1
    }

    public enum BadgeType
    {
        named = 1,
        tag_based = 2
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Badge : IGenericEquality<Badge>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? badge_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public BadgeRank? rank { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string name { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string description { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public int? award_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public BadgeType? badge_type { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public ShallowUser user { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public string link { get; set; }

        public bool Equals(Badge obj)
        {
            return
                award_count.TrueEquals(obj.award_count) &&
                badge_id.TrueEquals(obj.badge_id) &&
                badge_type.TrueEquals(obj.badge_type) &&
                description.TrueEqualsString(obj.description) &&
                link.TrueEqualsString(obj.link) &&
                name.TrueEqualsString(obj.name) &&
                rank.TrueEquals(obj.rank) &&
                user.TrueEquals(obj.user);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                award_count.TrueEquals((int?) obj.award_count) &&
                badge_id.TrueEquals((int?) obj.badge_id) &&
                badge_type.TrueEquals((BadgeType?) obj.badge_type) &&
                description.TrueEqualsString((string) obj.description) &&
                link.TrueEqualsString((string) obj.link) &&
                name.TrueEqualsString((string) obj.name) &&
                rank.TrueEquals((BadgeRank?) obj.rank) &&
                (user == null && obj.user == null || user.EqualsDynamic(obj.user));
        }
    }
}
extern alias oldmsgpack;
extern alias newmsgpack;
using System.Collections.Generic;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class MobileFeed : IGenericEquality<MobileFeed>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public List<MobileQuestion> hot_questions { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public List<MobileInboxItem> inbox_items { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public List<MobileQuestion> likely_to_answer_questions { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public List<MobileRepChange> reputation_events { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public List<MobileQuestion> cross_site_interesting_questions { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public List<MobileBadgeAward> badges { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public List<MobilePrivilege> earned_privileges { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public List<MobilePrivilege> upcoming_privileges { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public List<MobileCommunityBulletin> community_bulletins { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public List<MobileAssociationBonus> association_bonuses { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public List<MobileCareersJobAd> careers_job_ads { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(12), oldmsgpack::MessagePack.Key(12 - 1), newmsgpack::MessagePack.Key(12 - 1)]
        public List<MobileBannerAd> banner_ads { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(13), oldmsgpack::MessagePack.Key(13 - 1), newmsgpack::MessagePack.Key(13 - 1)]
        public long? before { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(14), oldmsgpack::MessagePack.Key(14 - 1), newmsgpack::MessagePack.Key(14 - 1)]
        public long? since { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(15), oldmsgpack::MessagePack.Key(15 - 1), newmsgpack::MessagePack.Key(15 - 1)]
        public int? account_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(16), oldmsgpack::MessagePack.Key(16 - 1), newmsgpack::MessagePack.Key(16 - 1)]
        public MobileUpdateNotice update_notice { get; set; }

        public bool Equals(MobileFeed obj)
        {
            return
                account_id == obj.account_id &&
                association_bonuses.TrueEqualsList(obj.association_bonuses) &&
                badges.TrueEqualsList(obj.badges) &&
                banner_ads.TrueEqualsList(obj.banner_ads) &&
                before == obj.before &&
                careers_job_ads.TrueEqualsList(obj.careers_job_ads) &&
                community_bulletins.TrueEqualsList(obj.community_bulletins) &&
                cross_site_interesting_questions.TrueEqualsList(obj.cross_site_interesting_questions) &&
                earned_privileges.TrueEqualsList(obj.earned_privileges) &&
                hot_questions.TrueEqualsList(obj.hot_questions) &&
                inbox_items.TrueEqualsList(obj.inbox_items) &&
                likely_to_answer_questions.TrueEqualsList(obj.likely_to_answer_questions) &&
                reputation_events.TrueEqualsList(obj.reputation_events) &&
                since == obj.since &&
                upcoming_privileges.TrueEqualsList(obj.upcoming_privileges) &&
                update_notice.TrueEquals(obj.update_notice);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                account_id == (int?) obj.account_id &&
                association_bonuses.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.association_bonuses) &&
                badges.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.badges) &&
                banner_ads.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.banner_ads) &&
                before == (long?) obj.before &&
                careers_job_ads.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.careers_job_ads) &&
                community_bulletins.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.community_bulletins) &&
                cross_site_interesting_questions.TrueEqualsListDynamic(
                    (IEnumerable<dynamic>) obj.cross_site_interesting_questions) &&
                earned_privileges.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.earned_privileges) &&
                hot_questions.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.hot_questions) &&
                inbox_items.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.inbox_items) &&
                likely_to_answer_questions.TrueEqualsListDynamic(
                    (IEnumerable<dynamic>) obj.likely_to_answer_questions) &&
                reputation_events.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.reputation_events) &&
                since == (long?) obj.since &&
                upcoming_privileges.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.upcoming_privileges) &&
                (update_notice == null && obj.update_notice == null || update_notice.EqualsDynamic(obj.update_notice));
        }
    }

    public interface IMobileFeedBase<T> : IGenericEquality<T>
    {
        int? group_id { get; set; }
        long? added_date { get; set; }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileQuestion : IMobileFeedBase<MobileQuestion>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? question_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public long? question_creation_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public long? last_activity_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public List<string> tags { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public bool? is_deleted { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public bool? has_accepted_answer { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public int? answer_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileQuestion obj)
        {
            return
                added_date == obj.added_date &&
                answer_count == obj.answer_count &&
                group_id == obj.group_id &&
                has_accepted_answer == obj.has_accepted_answer &&
                is_deleted == obj.is_deleted &&
                last_activity_date == obj.last_activity_date &&
                question_creation_date == obj.question_creation_date &&
                question_id == obj.question_id &&
                site == obj.site &&
                tags.TrueEqualsString(obj.tags) &&
                title == obj.title;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                added_date == (long?) obj.added_date &&
                answer_count == (int?) obj.answer_count &&
                group_id == (int?) obj.group_id &&
                has_accepted_answer == (bool?) obj.has_accepted_answer &&
                is_deleted == (bool?) obj.is_deleted &&
                last_activity_date == (long?) obj.last_activity_date &&
                question_creation_date == (long?) obj.question_creation_date &&
                question_id == (int?) obj.question_id &&
                site == (string) obj.site &&
                tags.TrueEqualsString((IEnumerable<string>) obj.tags) &&
                title == (string) obj.title;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileRepChange : IMobileFeedBase<MobileRepChange>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public int? rep_change { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileRepChange obj)
        {
            return
                added_date == obj.added_date &&
                group_id == obj.group_id &&
                link == obj.link &&
                rep_change == obj.rep_change &&
                site == obj.site &&
                title == obj.title;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                added_date == (long?) obj.added_date &&
                group_id == (int?) obj.group_id &&
                link == (string) obj.link &&
                rep_change == (int?) obj.rep_change &&
                site == (string) obj.site &&
                title == (string) obj.title;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileInboxItem : IMobileFeedBase<MobileInboxItem>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? answer_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string body { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public int? comment_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public long? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public string item_type { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public int? question_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileInboxItem obj)
        {
            return
                added_date == obj.added_date &&
                answer_id == obj.answer_id &&
                body == obj.body &&
                comment_id == obj.comment_id &&
                creation_date == obj.creation_date &&
                group_id == obj.group_id &&
                item_type == obj.item_type &&
                link == obj.link &&
                question_id == obj.question_id &&
                site == obj.site &&
                title == obj.title;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                added_date == (long?) obj.added_date &&
                answer_id == (int?) obj.answer_id &&
                body == (string) obj.body &&
                comment_id == (int?) obj.comment_id &&
                creation_date == (long?) obj.creation_date &&
                group_id == (int?) obj.group_id &&
                item_type == (string) obj.item_type &&
                link == (string) obj.link &&
                question_id == (int?) obj.question_id &&
                site == (string) obj.site &&
                title == (string) obj.title;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileBadgeAward : IMobileFeedBase<MobileBadgeAward>
    {
        public enum BadgeRank : byte
        {
            bronze = 1,
            silver = 2,
            gold = 3
        }

        public enum BadgeType
        {
            named = 1,
            tag_based = 2
        }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string badge_name { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string badge_description { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public int? badge_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? post_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public BadgeRank? rank { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public BadgeType? badge_type { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileBadgeAward obj)
        {
            return
                added_date == obj.added_date &&
                badge_description == obj.badge_description &&
                badge_id == obj.badge_id &&
                badge_name == obj.badge_name &&
                badge_type == obj.badge_type &&
                group_id == obj.group_id &&
                link == obj.link &&
                post_id == obj.post_id &&
                rank == obj.rank &&
                site == obj.site;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                added_date == (long?) obj.added_date &&
                badge_description == (string) obj.badge_description &&
                badge_id == (int?) obj.badge_id &&
                badge_name == (string) obj.badge_name &&
                badge_type == (BadgeType?) obj.badge_type &&
                group_id == (int?) obj.group_id &&
                link == (string) obj.link &&
                post_id == (int?) obj.post_id &&
                rank == (BadgeRank?) obj.rank &&
                site == (string) obj.site;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobilePrivilege : IMobileFeedBase<MobilePrivilege>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string privilege_short_description { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string privilege_long_description { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public int? privilege_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? reputation_required { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobilePrivilege obj)
        {
            return
                added_date == obj.added_date &&
                group_id == obj.group_id &&
                link == obj.link &&
                privilege_id == obj.privilege_id &&
                privilege_long_description == obj.privilege_long_description &&
                privilege_short_description == obj.privilege_short_description &&
                reputation_required == obj.reputation_required &&
                site == obj.site;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                added_date == (long?) obj.added_date &&
                group_id == (int?) obj.group_id &&
                link == (string) obj.link &&
                privilege_id == (int?) obj.privilege_id &&
                privilege_long_description == (string) obj.privilege_long_description &&
                privilege_short_description == (string) obj.privilege_short_description &&
                reputation_required == (int?) obj.reputation_required &&
                site == (string) obj.site;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileCommunityBulletin : IMobileFeedBase<MobileCommunityBulletin>
    {
        public enum CommunityBulletinType : byte
        {
            blog_post = 1,
            featured_meta_question = 2,
            upcoming_event = 3
        }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public CommunityBulletinType? bulletin_type { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public long? begin_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public long? end_date { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public string custom_date_string { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public List<string> tags { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public bool? is_deleted { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(12), oldmsgpack::MessagePack.Key(12 - 1), newmsgpack::MessagePack.Key(12 - 1)]
        public bool? has_accepted_answer { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(13), oldmsgpack::MessagePack.Key(13 - 1), newmsgpack::MessagePack.Key(13 - 1)]
        public int? answer_count { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(14), oldmsgpack::MessagePack.Key(14 - 1), newmsgpack::MessagePack.Key(14 - 1)]
        public bool? is_promoted { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileCommunityBulletin obj)
        {
            return
                added_date == obj.added_date &&
                answer_count == obj.answer_count &&
                begin_date == obj.begin_date &&
                bulletin_type == obj.bulletin_type &&
                custom_date_string == obj.custom_date_string &&
                end_date == obj.end_date &&
                group_id == obj.group_id &&
                has_accepted_answer == obj.has_accepted_answer &&
                is_deleted == obj.is_deleted &&
                is_promoted == obj.is_promoted &&
                link == obj.link &&
                site == obj.site &&
                tags.TrueEqualsString(obj.tags) &&
                title == obj.title;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                added_date == (long?) obj.added_date &&
                answer_count == (int?) obj.answer_count &&
                begin_date == (long?) obj.begin_date &&
                bulletin_type == (CommunityBulletinType?) obj.bulletin_type &&
                custom_date_string == (string) obj.custom_date_string &&
                end_date == (long?) obj.end_date &&
                group_id == (int?) obj.group_id &&
                has_accepted_answer == (bool?) obj.has_accepted_answer &&
                is_deleted == (bool?) obj.is_deleted &&
                is_promoted == (bool?) obj.is_promoted &&
                link == (string) obj.link &&
                site == (string) obj.site &&
                tags.TrueEqualsString((IEnumerable<string>) obj.tags) &&
                title == (string) obj.title;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileAssociationBonus : IMobileFeedBase<MobileAssociationBonus>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public int? amount { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileAssociationBonus obj)
        {
            return
                added_date == obj.added_date &&
                amount == obj.amount &&
                group_id == obj.group_id &&
                site == obj.site;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                added_date == (long?) obj.added_date &&
                amount == (int?) obj.amount &&
                group_id == (int?) obj.group_id &&
                site == (string) obj.site;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileCareersJobAd : IMobileFeedBase<MobileCareersJobAd>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? job_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string company_name { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public string location { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileCareersJobAd obj)
        {
            return
                added_date == obj.added_date &&
                company_name == obj.company_name &&
                group_id == obj.group_id &&
                job_id == obj.job_id &&
                link == obj.link &&
                location == obj.location &&
                title == obj.title;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                added_date == (long?) obj.added_date &&
                company_name == (string) obj.company_name &&
                group_id == (int?) obj.group_id &&
                job_id == (int?) obj.job_id &&
                link == (string) obj.link &&
                location == (string) obj.location &&
                title == (string) obj.title;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileBannerAd : IMobileFeedBase<MobileBannerAd>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public List<MobileBannerAdImage> images { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileBannerAd obj)
        {
            return
                added_date == obj.added_date &&
                group_id == obj.group_id &&
                images.TrueEqualsList(obj.images) &&
                link == obj.link;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                added_date == (long?) obj.added_date &&
                group_id == (int?) obj.group_id &&
                images.TrueEqualsListDynamic((IEnumerable<dynamic>) obj.images) &&
                link == (string) obj.link;
        }

        [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
        public sealed class MobileBannerAdImage : IGenericEquality<MobileBannerAdImage>
        {
            [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
            public string image_url { get; set; }

            [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
            public int? width { get; set; }

            [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
            public int? height { get; set; }

            public bool Equals(MobileBannerAdImage obj)
            {
                return
                    height == obj.height &&
                    image_url == obj.image_url &&
                    width == obj.width;
            }

            public bool EqualsDynamic(dynamic obj)
            {
                return
                    height == (int?) obj.height &&
                    image_url == (string) obj.image_url &&
                    width == (int?) obj.width;
            }
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileUpdateNotice : IGenericEquality<MobileUpdateNotice>
    {
        [System.Runtime.Serialization.DataMember(), ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public bool? should_update { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public string message { get; set; }

        [System.Runtime.Serialization.DataMember(), ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string minimum_supported_version { get; set; }

        public bool Equals(MobileUpdateNotice obj)
        {
            return
                message == obj.message &&
                minimum_supported_version == obj.minimum_supported_version &&
                should_update == obj.should_update;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                message == (string) obj.message &&
                minimum_supported_version == (string) obj.minimum_supported_version &&
                should_update == (bool?) obj.should_update;
        }
    }
}
// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using System.Collections.Generic;
using ProtoBuf;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class MobileFeed : IGenericEquality<MobileFeed>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public List<MobileQuestion> hot_questions { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public List<MobileInboxItem> inbox_items { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public List<MobileQuestion> likely_to_answer_questions { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public List<MobileRepChange> reputation_events { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public List<MobileQuestion> cross_site_interesting_questions { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public List<MobileBadgeAward> badges { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public List<MobilePrivilege> earned_privileges { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public List<MobilePrivilege> upcoming_privileges { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public List<MobileCommunityBulletin> community_bulletins { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public List<MobileAssociationBonus> association_bonuses { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public List<MobileCareersJobAd> careers_job_ads { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(12), oldmsgpack::MessagePack.Key(12 - 1), newmsgpack::MessagePack.Key(12 - 1)]
        public List<MobileBannerAd> banner_ads { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(13), oldmsgpack::MessagePack.Key(13 - 1), newmsgpack::MessagePack.Key(13 - 1)]
        public long? before { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(14), oldmsgpack::MessagePack.Key(14 - 1), newmsgpack::MessagePack.Key(14 - 1)]
        public long? since { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(15), oldmsgpack::MessagePack.Key(15 - 1), newmsgpack::MessagePack.Key(15 - 1)]
        public int? account_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(16), oldmsgpack::MessagePack.Key(16 - 1), newmsgpack::MessagePack.Key(16 - 1)]
        public MobileUpdateNotice update_notice { get; set; }

        public bool Equals(MobileFeed obj)
        {
            return
                this.account_id == obj.account_id &&
                this.association_bonuses.TrueEqualsList(obj.association_bonuses) &&
                this.badges.TrueEqualsList(obj.badges) &&
                this.banner_ads.TrueEqualsList(obj.banner_ads) &&
                this.before == obj.before &&
                this.careers_job_ads.TrueEqualsList(obj.careers_job_ads) &&
                this.community_bulletins.TrueEqualsList(obj.community_bulletins) &&
                this.cross_site_interesting_questions.TrueEqualsList(obj.cross_site_interesting_questions) &&
                this.earned_privileges.TrueEqualsList(obj.earned_privileges) &&
                this.hot_questions.TrueEqualsList(obj.hot_questions) &&
                this.inbox_items.TrueEqualsList(obj.inbox_items) &&
                this.likely_to_answer_questions.TrueEqualsList(obj.likely_to_answer_questions) &&
                this.reputation_events.TrueEqualsList(obj.reputation_events) &&
                this.since == obj.since &&
                this.upcoming_privileges.TrueEqualsList(obj.upcoming_privileges) &&
                this.update_notice.TrueEquals(obj.update_notice);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.account_id == (int?)obj.account_id &&
                this.association_bonuses.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.association_bonuses) &&
                this.badges.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.badges) &&
                this.banner_ads.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.banner_ads) &&
                this.before == (long?)obj.before &&
                this.careers_job_ads.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.careers_job_ads) &&
                this.community_bulletins.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.community_bulletins) &&
                this.cross_site_interesting_questions.TrueEqualsListDynamic(
                    (IEnumerable<dynamic>)obj.cross_site_interesting_questions) &&
                this.earned_privileges.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.earned_privileges) &&
                this.hot_questions.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.hot_questions) &&
                this.inbox_items.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.inbox_items) &&
                this.likely_to_answer_questions.TrueEqualsListDynamic(
                    (IEnumerable<dynamic>)obj.likely_to_answer_questions) &&
                this.reputation_events.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.reputation_events) &&
                this.since == (long?)obj.since &&
                this.upcoming_privileges.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.upcoming_privileges) &&
                ((this.update_notice == null && obj.update_notice == null) || this.update_notice.EqualsDynamic(obj.update_notice));
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
        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? question_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public long? question_creation_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public long? last_activity_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public List<string> tags { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public bool? is_deleted { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public bool? has_accepted_answer { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public int? answer_count { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileQuestion obj)
        {
            return
                this.added_date == obj.added_date &&
                this.answer_count == obj.answer_count &&
                this.group_id == obj.group_id &&
                this.has_accepted_answer == obj.has_accepted_answer &&
                this.is_deleted == obj.is_deleted &&
                this.last_activity_date == obj.last_activity_date &&
                this.question_creation_date == obj.question_creation_date &&
                this.question_id == obj.question_id &&
                this.site == obj.site &&
                this.tags.TrueEqualsString(obj.tags) &&
                this.title == obj.title;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.added_date == (long?)obj.added_date &&
                this.answer_count == (int?)obj.answer_count &&
                this.group_id == (int?)obj.group_id &&
                this.has_accepted_answer == (bool?)obj.has_accepted_answer &&
                this.is_deleted == (bool?)obj.is_deleted &&
                this.last_activity_date == (long?)obj.last_activity_date &&
                this.question_creation_date == (long?)obj.question_creation_date &&
                this.question_id == (int?)obj.question_id &&
                this.site == (string)obj.site &&
                this.tags.TrueEqualsString((IEnumerable<string>)obj.tags) &&
                this.title == (string)obj.title;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileRepChange : IMobileFeedBase<MobileRepChange>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public int? rep_change { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileRepChange obj)
        {
            return
                this.added_date == obj.added_date &&
                this.group_id == obj.group_id &&
                this.link == obj.link &&
                this.rep_change == obj.rep_change &&
                this.site == obj.site &&
                this.title == obj.title;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.added_date == (long?)obj.added_date &&
                this.group_id == (int?)obj.group_id &&
                this.link == (string)obj.link &&
                this.rep_change == (int?)obj.rep_change &&
                this.site == (string)obj.site &&
                this.title == (string)obj.title;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileInboxItem : IMobileFeedBase<MobileInboxItem>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? answer_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string body { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public int? comment_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public long? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public string item_type { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public int? question_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileInboxItem obj)
        {
            return
                this.added_date == obj.added_date &&
                this.answer_id == obj.answer_id &&
                this.body == obj.body &&
                this.comment_id == obj.comment_id &&
                this.creation_date == obj.creation_date &&
                this.group_id == obj.group_id &&
                this.item_type == obj.item_type &&
                this.link == obj.link &&
                this.question_id == obj.question_id &&
                this.site == obj.site &&
                this.title == obj.title;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.added_date == (long?)obj.added_date &&
                this.answer_id == (int?)obj.answer_id &&
                this.body == (string)obj.body &&
                this.comment_id == (int?)obj.comment_id &&
                this.creation_date == (long?)obj.creation_date &&
                this.group_id == (int?)obj.group_id &&
                this.item_type == (string)obj.item_type &&
                this.link == (string)obj.link &&
                this.question_id == (int?)obj.question_id &&
                this.site == (string)obj.site &&
                this.title == (string)obj.title;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileBadgeAward : IMobileFeedBase<MobileBadgeAward>
    {
        public enum BadgeRank : byte
        {
            bronze = 1,
            silver = 2,
            gold = 3,
        }

        public enum BadgeType
        {
            named = 1,
            tag_based = 2,
        }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string badge_name { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string badge_description { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public int? badge_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? post_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public BadgeRank? rank { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public BadgeType? badge_type { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileBadgeAward obj)
        {
            return
                this.added_date == obj.added_date &&
                this.badge_description == obj.badge_description &&
                this.badge_id == obj.badge_id &&
                this.badge_name == obj.badge_name &&
                this.badge_type == obj.badge_type &&
                this.group_id == obj.group_id &&
                this.link == obj.link &&
                this.post_id == obj.post_id &&
                this.rank == obj.rank &&
                this.site == obj.site;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.added_date == (long?)obj.added_date &&
                this.badge_description == (string)obj.badge_description &&
                this.badge_id == (int?)obj.badge_id &&
                this.badge_name == (string)obj.badge_name &&
                this.badge_type == (BadgeType?)obj.badge_type &&
                this.group_id == (int?)obj.group_id &&
                this.link == (string)obj.link &&
                this.post_id == (int?)obj.post_id &&
                this.rank == (BadgeRank?)obj.rank &&
                this.site == (string)obj.site;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobilePrivilege : IMobileFeedBase<MobilePrivilege>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string privilege_short_description { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string privilege_long_description { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public int? privilege_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? reputation_required { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobilePrivilege obj)
        {
            return
                this.added_date == obj.added_date &&
                this.group_id == obj.group_id &&
                this.link == obj.link &&
                this.privilege_id == obj.privilege_id &&
                this.privilege_long_description == obj.privilege_long_description &&
                this.privilege_short_description == obj.privilege_short_description &&
                this.reputation_required == obj.reputation_required &&
                this.site == obj.site;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.added_date == (long?)obj.added_date &&
                this.group_id == (int?)obj.group_id &&
                this.link == (string)obj.link &&
                this.privilege_id == (int?)obj.privilege_id &&
                this.privilege_long_description == (string)obj.privilege_long_description &&
                this.privilege_short_description == (string)obj.privilege_short_description &&
                this.reputation_required == (int?)obj.reputation_required &&
                this.site == (string)obj.site;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileCommunityBulletin : IMobileFeedBase<MobileCommunityBulletin>
    {
        public enum CommunityBulletinType : byte
        {
            blog_post = 1,
            featured_meta_question = 2,
            upcoming_event = 3,
        }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public CommunityBulletinType? bulletin_type { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public long? begin_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public long? end_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public string custom_date_string { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public List<string> tags { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public bool? is_deleted { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(12), oldmsgpack::MessagePack.Key(12 - 1), newmsgpack::MessagePack.Key(12 - 1)]
        public bool? has_accepted_answer { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(13), oldmsgpack::MessagePack.Key(13 - 1), newmsgpack::MessagePack.Key(13 - 1)]
        public int? answer_count { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(14), oldmsgpack::MessagePack.Key(14 - 1), newmsgpack::MessagePack.Key(14 - 1)]
        public bool? is_promoted { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileCommunityBulletin obj)
        {
            return
                this.added_date == obj.added_date &&
                this.answer_count == obj.answer_count &&
                this.begin_date == obj.begin_date &&
                this.bulletin_type == obj.bulletin_type &&
                this.custom_date_string == obj.custom_date_string &&
                this.end_date == obj.end_date &&
                this.group_id == obj.group_id &&
                this.has_accepted_answer == obj.has_accepted_answer &&
                this.is_deleted == obj.is_deleted &&
                this.is_promoted == obj.is_promoted &&
                this.link == obj.link &&
                this.site == obj.site &&
                this.tags.TrueEqualsString(obj.tags) &&
                this.title == obj.title;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.added_date == (long?)obj.added_date &&
                this.answer_count == (int?)obj.answer_count &&
                this.begin_date == (long?)obj.begin_date &&
                this.bulletin_type == (CommunityBulletinType?)obj.bulletin_type &&
                this.custom_date_string == (string)obj.custom_date_string &&
                this.end_date == (long?)obj.end_date &&
                this.group_id == (int?)obj.group_id &&
                this.has_accepted_answer == (bool?)obj.has_accepted_answer &&
                this.is_deleted == (bool?)obj.is_deleted &&
                this.is_promoted == (bool?)obj.is_promoted &&
                this.link == (string)obj.link &&
                this.site == (string)obj.site &&
                this.tags.TrueEqualsString((IEnumerable<string>)obj.tags) &&
                this.title == (string)obj.title;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileAssociationBonus : IMobileFeedBase<MobileAssociationBonus>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string site { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public int? amount { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileAssociationBonus obj)
        {
            return
                this.added_date == obj.added_date &&
                this.amount == obj.amount &&
                this.group_id == obj.group_id &&
                this.site == obj.site;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.added_date == (long?)obj.added_date &&
                this.amount == (int?)obj.amount &&
                this.group_id == (int?)obj.group_id &&
                this.site == (string)obj.site;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileCareersJobAd : IMobileFeedBase<MobileCareersJobAd>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? job_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string company_name { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public string location { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public string title { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileCareersJobAd obj)
        {
            return
                this.added_date == obj.added_date &&
                this.company_name == obj.company_name &&
                this.group_id == obj.group_id &&
                this.job_id == obj.job_id &&
                this.link == obj.link &&
                this.location == obj.location &&
                this.title == obj.title;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.added_date == (long?)obj.added_date &&
                this.company_name == (string)obj.company_name &&
                this.group_id == (int?)obj.group_id &&
                this.job_id == (int?)obj.job_id &&
                this.link == (string)obj.link &&
                this.location == (string)obj.location &&
                this.title == (string)obj.title;
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileBannerAd : IMobileFeedBase<MobileBannerAd>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public List<MobileBannerAdImage> images { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? group_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public long? added_date { get; set; }

        public bool Equals(MobileBannerAd obj)
        {
            return
                this.added_date == obj.added_date &&
                this.group_id == obj.group_id &&
                this.images.TrueEqualsList(obj.images) &&
                this.link == obj.link;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.added_date == (long?)obj.added_date &&
                this.group_id == (int?)obj.group_id &&
                this.images.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.images) &&
                this.link == (string)obj.link;
        }

        [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
        public sealed class MobileBannerAdImage : IGenericEquality<MobileBannerAdImage>
        {
            [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
            public string image_url { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
            public int? width { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
            public int? height { get; set; }

            public bool Equals(MobileBannerAdImage obj)
            {
                return
                    this.height == obj.height &&
                    this.image_url == obj.image_url &&
                    this.width == obj.width;
            }

            public bool EqualsDynamic(dynamic obj)
            {
                return
                    this.height == (int?)obj.height &&
                    this.image_url == (string)obj.image_url &&
                    this.width == (int?)obj.width;
            }
        }
    }

    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public sealed class MobileUpdateNotice : IGenericEquality<MobileUpdateNotice>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public bool? should_update { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public string message { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public string minimum_supported_version { get; set; }

        public bool Equals(MobileUpdateNotice obj)
        {
            return
                this.message == obj.message &&
                this.minimum_supported_version == obj.minimum_supported_version &&
                this.should_update == obj.should_update;
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.message == (string)obj.message &&
                this.minimum_supported_version == (string)obj.minimum_supported_version &&
                this.should_update == (bool?)obj.should_update;
        }
    }
}

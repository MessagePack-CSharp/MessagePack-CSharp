// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE1006
#pragma warning disable SA1516

extern alias oldmsgpack;
extern alias newmsgpack;

using System;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class User : IGenericEquality<User>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? user_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public UserType? user_type { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public DateTime? creation_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public string display_name { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public string profile_image { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public int? reputation { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? reputation_change_day { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public int? reputation_change_week { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public int? reputation_change_month { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public int? reputation_change_quarter { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public int? reputation_change_year { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(12), oldmsgpack::MessagePack.Key(12 - 1), newmsgpack::MessagePack.Key(12 - 1)]
        public int? age { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(13), oldmsgpack::MessagePack.Key(13 - 1), newmsgpack::MessagePack.Key(13 - 1)]
        public DateTime? last_access_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(14), oldmsgpack::MessagePack.Key(14 - 1), newmsgpack::MessagePack.Key(14 - 1)]
        public DateTime? last_modified_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(15), oldmsgpack::MessagePack.Key(15 - 1), newmsgpack::MessagePack.Key(15 - 1)]
        public bool? is_employee { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(16), oldmsgpack::MessagePack.Key(16 - 1), newmsgpack::MessagePack.Key(16 - 1)]
        public string link { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(17), oldmsgpack::MessagePack.Key(17 - 1), newmsgpack::MessagePack.Key(17 - 1)]
        public string website_url { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(18), oldmsgpack::MessagePack.Key(18 - 1), newmsgpack::MessagePack.Key(18 - 1)]
        public string location { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(19), oldmsgpack::MessagePack.Key(19 - 1), newmsgpack::MessagePack.Key(19 - 1)]
        public int? account_id { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(20), oldmsgpack::MessagePack.Key(20 - 1), newmsgpack::MessagePack.Key(20 - 1)]
        public DateTime? timed_penalty_date { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(21), oldmsgpack::MessagePack.Key(21 - 1), newmsgpack::MessagePack.Key(21 - 1)]
        public BadgeCount badge_counts { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(22), oldmsgpack::MessagePack.Key(22 - 1), newmsgpack::MessagePack.Key(22 - 1)]
        public int? question_count { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(23), oldmsgpack::MessagePack.Key(23 - 1), newmsgpack::MessagePack.Key(23 - 1)]
        public int? answer_count { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(24), oldmsgpack::MessagePack.Key(24 - 1), newmsgpack::MessagePack.Key(24 - 1)]
        public int? up_vote_count { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(25), oldmsgpack::MessagePack.Key(25 - 1), newmsgpack::MessagePack.Key(25 - 1)]
        public int? down_vote_count { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(26), oldmsgpack::MessagePack.Key(26 - 1), newmsgpack::MessagePack.Key(26 - 1)]
        public string about_me { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(27), oldmsgpack::MessagePack.Key(27 - 1), newmsgpack::MessagePack.Key(27 - 1)]
        public int? view_count { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(28), oldmsgpack::MessagePack.Key(28 - 1), newmsgpack::MessagePack.Key(28 - 1)]
        public int? accept_rate { get; set; }

        public bool Equals(User obj)
        {
            return
                this.about_me.TrueEqualsString(obj.about_me) &&
                this.accept_rate.TrueEquals(obj.accept_rate) &&
                this.account_id.TrueEquals(obj.account_id) &&
                this.age.TrueEquals(obj.age) &&
                this.answer_count.TrueEquals(obj.answer_count) &&
                this.badge_counts.TrueEquals(obj.badge_counts) &&
                this.creation_date.TrueEquals(obj.creation_date) &&
                this.display_name.TrueEqualsString(obj.display_name) &&
                this.down_vote_count.TrueEquals(obj.down_vote_count) &&
                this.is_employee.TrueEquals(obj.is_employee) &&
                this.last_access_date.TrueEquals(obj.last_access_date) &&
                this.last_modified_date.TrueEquals(obj.last_modified_date) &&
                this.link.TrueEqualsString(obj.link) &&
                this.location.TrueEqualsString(obj.location) &&
                this.profile_image.TrueEqualsString(obj.profile_image) &&
                this.question_count.TrueEquals(obj.question_count) &&
                this.reputation.TrueEquals(obj.reputation) &&
                this.reputation_change_day.TrueEquals(obj.reputation_change_day) &&
                this.reputation_change_month.TrueEquals(obj.reputation_change_month) &&
                this.reputation_change_quarter.TrueEquals(obj.reputation_change_quarter) &&
                this.reputation_change_week.TrueEquals(obj.reputation_change_week) &&
                this.reputation_change_year.TrueEquals(obj.reputation_change_year) &&
                this.timed_penalty_date.TrueEquals(obj.timed_penalty_date) &&
                this.up_vote_count.TrueEquals(obj.up_vote_count) &&
                this.user_id.TrueEquals(obj.user_id) &&
                this.user_type.TrueEquals(obj.user_type) &&
                this.view_count.TrueEquals(obj.view_count) &&
                this.website_url.TrueEqualsString(obj.website_url);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.about_me.TrueEqualsString((string)obj.about_me) &&
                this.accept_rate.TrueEquals((int?)obj.accept_rate) &&
                this.account_id.TrueEquals((int?)obj.account_id) &&
                this.age.TrueEquals((int?)obj.age) &&
                this.answer_count.TrueEquals((int?)obj.answer_count) &&
                ((this.badge_counts == null && obj.badge_counts == null) || this.badge_counts.EqualsDynamic(obj.badge_counts)) &&
                this.creation_date.TrueEquals((DateTime?)obj.creation_date) &&
                this.display_name.TrueEqualsString((string)obj.display_name) &&
                this.down_vote_count.TrueEquals((int?)obj.down_vote_count) &&
                this.is_employee.TrueEquals((bool?)obj.is_employee) &&
                this.last_access_date.TrueEquals((DateTime?)obj.last_access_date) &&
                this.last_modified_date.TrueEquals((DateTime?)obj.last_modified_date) &&
                this.link.TrueEqualsString((string)obj.link) &&
                this.location.TrueEqualsString((string)obj.location) &&
                this.profile_image.TrueEqualsString((string)obj.profile_image) &&
                this.question_count.TrueEquals((int?)obj.question_count) &&
                this.reputation.TrueEquals((int?)obj.reputation) &&
                this.reputation_change_day.TrueEquals((int?)obj.reputation_change_day) &&
                this.reputation_change_month.TrueEquals((int?)obj.reputation_change_month) &&
                this.reputation_change_quarter.TrueEquals((int?)obj.reputation_change_quarter) &&
                this.reputation_change_week.TrueEquals((int?)obj.reputation_change_week) &&
                this.reputation_change_year.TrueEquals((int?)obj.reputation_change_year) &&
                this.timed_penalty_date.TrueEquals((DateTime?)obj.timed_penalty_date) &&
                this.up_vote_count.TrueEquals((int?)obj.up_vote_count) &&
                this.user_id.TrueEquals((int?)obj.user_id) &&
                this.user_type.TrueEquals((UserType?)obj.user_type) &&
                this.view_count.TrueEquals((int?)obj.view_count) &&
                this.website_url.TrueEqualsString((string)obj.website_url);
        }

        [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
        public class BadgeCount : IGenericEquality<BadgeCount>
        {
            [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
            public int? gold { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
            public int? silver { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
            public int? bronze { get; set; }

            public bool Equals(BadgeCount obj)
            {
                return
                    this.bronze.TrueEquals(obj.bronze) &&
                    this.silver.TrueEquals(obj.silver) &&
                    this.gold.TrueEquals(obj.gold);
            }

            public bool EqualsDynamic(dynamic obj)
            {
                return
                    this.bronze.TrueEquals((int?)obj.bronze) &&
                    this.silver.TrueEquals((int?)obj.silver) &&
                    this.gold.TrueEquals((int?)obj.gold);
            }
        }

        [oldmsgpack::MessagePack.MessagePackObject(true), newmsgpack::MessagePack.MessagePackObject(true)]
        public class BadgeCount2 : IGenericEquality<BadgeCount2>
        {
            public int? gold { get; set; }
            public int? silver { get; set; }
            public int? bronze { get; set; }

            public bool Equals(BadgeCount2 obj)
            {
                return
                    this.bronze.TrueEquals(obj.bronze) &&
                    this.silver.TrueEquals(obj.silver) &&
                    this.gold.TrueEquals(obj.gold);
            }

            public bool EqualsDynamic(dynamic obj)
            {
                return
                    this.bronze.TrueEquals((int?)obj.bronze) &&
                    this.silver.TrueEquals((int?)obj.silver) &&
                    this.gold.TrueEquals((int?)obj.gold);
            }
        }
    }
}

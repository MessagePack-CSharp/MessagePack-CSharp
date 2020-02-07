// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias oldmsgpack;
extern alias newmsgpack;

using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Benchmark.Models
{
    [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
    public class Info : IGenericEquality<Info>
    {
        [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
        public int? total_questions { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
        public int? total_unanswered { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
        public int? total_accepted { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
        public int? total_answers { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
        public decimal? questions_per_minute { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
        public decimal? answers_per_minute { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
        public int? total_comments { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
        public int? total_votes { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
        public int? total_badges { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
        public decimal? badges_per_minute { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
        public int? total_users { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(12), oldmsgpack::MessagePack.Key(12 - 1), newmsgpack::MessagePack.Key(12 - 1)]
        public int? new_active_users { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(13), oldmsgpack::MessagePack.Key(13 - 1), newmsgpack::MessagePack.Key(13 - 1)]
        public string api_revision { get; set; }

        [System.Runtime.Serialization.DataMember, ProtoMember(14), oldmsgpack::MessagePack.Key(14 - 1), newmsgpack::MessagePack.Key(14 - 1)]
        public Site site { get; set; }

        public bool Equals(Info obj)
        {
            return
                this.answers_per_minute.TrueEquals(obj.answers_per_minute) &&
                this.api_revision.TrueEqualsString(obj.api_revision) &&
                this.badges_per_minute.TrueEquals(obj.badges_per_minute) &&
                this.new_active_users.TrueEquals(obj.new_active_users) &&
                this.questions_per_minute.TrueEquals(obj.questions_per_minute) &&
                this.site.TrueEquals(obj.site) &&
                this.total_accepted.TrueEquals(obj.total_accepted) &&
                this.total_answers.TrueEquals(obj.total_answers) &&
                this.total_badges.TrueEquals(obj.total_badges) &&
                this.total_comments.TrueEquals(obj.total_comments) &&
                this.total_questions.TrueEquals(obj.total_questions) &&
                this.total_unanswered.TrueEquals(obj.total_unanswered) &&
                this.total_users.TrueEquals(obj.total_users) &&
                this.total_votes.TrueEquals(obj.total_votes);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.answers_per_minute.TrueEquals((decimal?)obj.answers_per_minute) &&
                this.api_revision.TrueEqualsString((string)obj.api_revision) &&
                this.badges_per_minute.TrueEquals((decimal?)obj.badges_per_minute) &&
                this.new_active_users.TrueEquals((int?)obj.new_active_users) &&
                this.questions_per_minute.TrueEquals((decimal?)obj.questions_per_minute) &&
                ((this.site == null && obj.site == null) || this.site.EqualsDynamic(obj.site)) &&
                this.total_accepted.TrueEquals((int?)obj.total_accepted) &&
                this.total_answers.TrueEquals((int?)obj.total_answers) &&
                this.total_badges.TrueEquals((int?)obj.total_badges) &&
                this.total_comments.TrueEquals((int?)obj.total_comments) &&
                this.total_questions.TrueEquals((int?)obj.total_questions) &&
                this.total_unanswered.TrueEquals((int?)obj.total_unanswered) &&
                this.total_users.TrueEquals((int?)obj.total_users) &&
                this.total_votes.TrueEquals((int?)obj.total_votes);
        }

        [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
        public class Site : IGenericEquality<Site>
        {
            public enum SiteState
            {
                normal,
                closed_beta,
                open_beta,
                linked_meta,
            }

            [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
            public string site_type { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
            public string name { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
            public string logo_url { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
            public string api_site_parameter { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(5), oldmsgpack::MessagePack.Key(5 - 1), newmsgpack::MessagePack.Key(5 - 1)]
            public string site_url { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(6), oldmsgpack::MessagePack.Key(6 - 1), newmsgpack::MessagePack.Key(6 - 1)]
            public string audience { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(7), oldmsgpack::MessagePack.Key(7 - 1), newmsgpack::MessagePack.Key(7 - 1)]
            public string icon_url { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(8), oldmsgpack::MessagePack.Key(8 - 1), newmsgpack::MessagePack.Key(8 - 1)]
            public List<string> aliases { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(9), oldmsgpack::MessagePack.Key(9 - 1), newmsgpack::MessagePack.Key(9 - 1)]
            public SiteState? site_state { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(10), oldmsgpack::MessagePack.Key(10 - 1), newmsgpack::MessagePack.Key(10 - 1)]
            public Styling styling { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(11), oldmsgpack::MessagePack.Key(11 - 1), newmsgpack::MessagePack.Key(11 - 1)]
            public DateTime? closed_beta_date { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(12), oldmsgpack::MessagePack.Key(12 - 1), newmsgpack::MessagePack.Key(12 - 1)]
            public DateTime? open_beta_date { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(13), oldmsgpack::MessagePack.Key(13 - 1), newmsgpack::MessagePack.Key(13 - 1)]
            public DateTime? launch_date { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(14), oldmsgpack::MessagePack.Key(14 - 1), newmsgpack::MessagePack.Key(14 - 1)]
            public string favicon_url { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(15), oldmsgpack::MessagePack.Key(15 - 1), newmsgpack::MessagePack.Key(15 - 1)]
            public List<RelatedSite> related_sites { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(16), oldmsgpack::MessagePack.Key(16 - 1), newmsgpack::MessagePack.Key(16 - 1)]
            public string twitter_account { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(17), oldmsgpack::MessagePack.Key(17 - 1), newmsgpack::MessagePack.Key(17 - 1)]
            public List<string> markdown_extensions { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(18), oldmsgpack::MessagePack.Key(18 - 1), newmsgpack::MessagePack.Key(18 - 1)]
            public string high_resolution_icon_url { get; set; }

            public bool Equals(Site obj)
            {
                return
                    this.aliases.TrueEqualsString(obj.aliases) &&
                    this.api_site_parameter.TrueEqualsString(obj.api_site_parameter) &&
                    this.audience.TrueEqualsString(obj.audience) &&
                    this.closed_beta_date.TrueEquals(obj.closed_beta_date) &&
                    this.favicon_url.TrueEqualsString(obj.favicon_url) &&
                    this.high_resolution_icon_url.TrueEqualsString(obj.high_resolution_icon_url) &&
                    this.icon_url.TrueEqualsString(obj.icon_url) &&
                    this.launch_date.TrueEquals(obj.launch_date) &&
                    this.logo_url.TrueEqualsString(obj.logo_url) &&
                    this.markdown_extensions.TrueEqualsString(obj.markdown_extensions) &&
                    this.name.TrueEqualsString(obj.name) &&
                    this.open_beta_date.TrueEquals(obj.open_beta_date) &&
                    this.related_sites.TrueEqualsList(obj.related_sites) &&
                    this.site_state.TrueEquals(obj.site_state) &&
                    this.site_type.TrueEqualsString(obj.site_type) &&
                    this.site_url.TrueEqualsString(obj.site_url) &&
                    this.styling.TrueEquals(obj.styling) &&
                    this.twitter_account.TrueEqualsString(obj.twitter_account);
            }

            public bool EqualsDynamic(dynamic obj)
            {
                return
                    this.aliases.TrueEqualsString((IEnumerable<string>)obj.aliases) &&
                    this.api_site_parameter.TrueEqualsString((string)obj.api_site_parameter) &&
                    this.audience.TrueEqualsString((string)obj.audience) &&
                    this.closed_beta_date.TrueEquals((DateTime?)obj.closed_beta_date) &&
                    this.favicon_url.TrueEqualsString((string)obj.favicon_url) &&
                    this.high_resolution_icon_url.TrueEqualsString((string)obj.high_resolution_icon_url) &&
                    this.icon_url.TrueEqualsString((string)obj.icon_url) &&
                    this.launch_date.TrueEquals((DateTime?)obj.launch_date) &&
                    this.logo_url.TrueEqualsString((string)obj.logo_url) &&
                    this.markdown_extensions.TrueEqualsString((IEnumerable<string>)obj.markdown_extensions) &&
                    this.name.TrueEqualsString((string)obj.name) &&
                    this.open_beta_date.TrueEquals((DateTime?)obj.open_beta_date) &&
                    this.related_sites.TrueEqualsListDynamic((IEnumerable<dynamic>)obj.related_sites) &&
                    this.site_state.TrueEquals((SiteState?)obj.site_state) &&
                    this.site_type.TrueEqualsString((string)obj.site_type) &&
                    this.site_url.TrueEqualsString((string)obj.site_url) &&
                    ((this.styling == null && obj.styling == null) || this.styling.EqualsDynamic(obj.styling)) &&
                    this.twitter_account.TrueEqualsString((string)obj.twitter_account);
            }

            [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
            public class Styling : IGenericEquality<Styling>
            {
                [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
                public string link_color { get; set; }

                [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
                public string tag_foreground_color { get; set; }

                [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
                public string tag_background_color { get; set; }

                public bool Equals(Styling obj)
                {
                    return
                        this.link_color.TrueEqualsString(obj.link_color) &&
                        this.tag_background_color.TrueEqualsString(obj.tag_background_color) &&
                        this.tag_foreground_color.TrueEqualsString(obj.tag_foreground_color);
                }

                public bool EqualsDynamic(dynamic obj)
                {
                    return
                        this.link_color.TrueEqualsString((string)obj.link_color) &&
                        this.tag_background_color.TrueEqualsString((string)obj.tag_background_color) &&
                        this.tag_foreground_color.TrueEqualsString((string)obj.tag_foreground_color);
                }
            }
        }

        [ProtoContract, System.Serializable, System.Runtime.Serialization.DataContract, oldmsgpack::MessagePack.MessagePackObject, newmsgpack::MessagePack.MessagePackObject]
        public class RelatedSite : IGenericEquality<RelatedSite>
        {
            public enum SiteRelation
            {
                parent,
                meta,
                chat,
            }

            [System.Runtime.Serialization.DataMember, ProtoMember(1), oldmsgpack::MessagePack.Key(1 - 1), newmsgpack::MessagePack.Key(1 - 1)]
            public string name { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(2), oldmsgpack::MessagePack.Key(2 - 1), newmsgpack::MessagePack.Key(2 - 1)]
            public string site_url { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(3), oldmsgpack::MessagePack.Key(3 - 1), newmsgpack::MessagePack.Key(3 - 1)]
            public SiteRelation? relation { get; set; }

            [System.Runtime.Serialization.DataMember, ProtoMember(4), oldmsgpack::MessagePack.Key(4 - 1), newmsgpack::MessagePack.Key(4 - 1)]
            public string api_site_parameter { get; set; }

            public bool Equals(RelatedSite obj)
            {
                return
                    this.name.TrueEqualsString(obj.name) &&
                    this.relation.TrueEquals(obj.relation) &&
                    this.api_site_parameter.TrueEqualsString(obj.api_site_parameter);
            }

            public bool EqualsDynamic(dynamic obj)
            {
                return
                    this.name.TrueEqualsString((string)obj.name) &&
                    this.relation.TrueEquals((SiteRelation?)obj.relation) &&
                    this.api_site_parameter.TrueEqualsString((string)obj.api_site_parameter);
            }
        }
    }
}

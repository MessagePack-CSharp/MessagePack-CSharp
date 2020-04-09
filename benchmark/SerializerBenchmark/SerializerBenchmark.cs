// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Benchmark.Fixture;
using Benchmark.Models;
using Benchmark.Serializers;
using BenchmarkDotNet.Attributes;

#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1649 // File name should match first type name

namespace Benchmark
{
    [Config(typeof(BenchmarkConfig))]
    public class AllSerializerBenchmark_BytesInOut
    {
        [ParamsSource(nameof(Serializers))]
        public SerializerBase Serializer;

        // Currently BenchmarkdDotNet does not detect inherited ParamsSource so use copy and paste:)
        public IEnumerable<SerializerBase> Serializers => new SerializerBase[]
        {
            new MessagePack_v1(),
            new MessagePack_v2(),
            new MessagePackLz4_v1(),
            new MessagePackLz4_v2(),
            new MsgPack_v2_opt(),
            //new MsgPack_v2_string(),
            //new MsgPack_v2_str_lz4(),
            new ProtobufNet(),
            new JsonNet(),
            new BinaryFormatter_(),
            new DataContract_(),
            new Hyperion_(),
            new Jil_(),
            new SpanJson_(),
            new Utf8Json_(),
            new SystemTextJson(),
            new MsgPackCli(),
            new FsPickler_(),
            new Ceras_(),
        };

        protected static readonly ExpressionTreeFixture ExpressionTreeFixture = new ExpressionTreeFixture();

        // primitives
        protected static readonly sbyte SByteInput = ExpressionTreeFixture.Create<sbyte>();
        protected static readonly short ShortInput = ExpressionTreeFixture.Create<short>();
        protected static readonly int IntInput = ExpressionTreeFixture.Create<int>();
        protected static readonly long LongInput = ExpressionTreeFixture.Create<long>();
        protected static readonly byte ByteInput = ExpressionTreeFixture.Create<byte>();
        protected static readonly ushort UShortInput = ExpressionTreeFixture.Create<ushort>();
        protected static readonly uint UIntInput = ExpressionTreeFixture.Create<uint>();
        protected static readonly ulong ULongInput = ExpressionTreeFixture.Create<ulong>();
        protected static readonly bool BoolInput = ExpressionTreeFixture.Create<bool>();
        protected static readonly string StringInput = ExpressionTreeFixture.Create<string>();
        protected static readonly char CharInput = ExpressionTreeFixture.Create<char>();
        protected static readonly DateTime DateTimeInput = ExpressionTreeFixture.Create<DateTime>();
        protected static readonly Guid GuidInput = ExpressionTreeFixture.Create<Guid>();
        protected static readonly byte[] BytesInput = ExpressionTreeFixture.Create<byte[]>();

        // models
        protected static readonly Benchmark.Models.AccessToken AccessTokenInput = ExpressionTreeFixture.Create<Benchmark.Models.AccessToken>();

        protected static readonly Benchmark.Models.AccountMerge AccountMergeInput = ExpressionTreeFixture.Create<Benchmark.Models.AccountMerge>();

        protected static readonly Benchmark.Models.Answer AnswerInput = ExpressionTreeFixture.Create<Benchmark.Models.Answer>();

        protected static readonly Benchmark.Models.Badge BadgeInput = ExpressionTreeFixture.Create<Benchmark.Models.Badge>();

        protected static readonly Benchmark.Models.Comment CommentInput = ExpressionTreeFixture.Create<Benchmark.Models.Comment>();

        protected static readonly Benchmark.Models.Error ErrorInput = ExpressionTreeFixture.Create<Benchmark.Models.Error>();

        protected static readonly Benchmark.Models.Event EventInput = ExpressionTreeFixture.Create<Benchmark.Models.Event>();

        protected static readonly Benchmark.Models.MobileFeed MobileFeedInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileFeed>();

        protected static readonly Benchmark.Models.MobileQuestion MobileQuestionInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileQuestion>();

        protected static readonly Benchmark.Models.MobileRepChange MobileRepChangeInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileRepChange>();

        protected static readonly Benchmark.Models.MobileInboxItem MobileInboxItemInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileInboxItem>();

        protected static readonly Benchmark.Models.MobileBadgeAward MobileBadgeAwardInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileBadgeAward>();

        protected static readonly Benchmark.Models.MobilePrivilege MobilePrivilegeInput = ExpressionTreeFixture.Create<Benchmark.Models.MobilePrivilege>();

        protected static readonly Benchmark.Models.MobileCommunityBulletin MobileCommunityBulletinInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileCommunityBulletin>();

        protected static readonly Benchmark.Models.MobileAssociationBonus MobileAssociationBonusInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileAssociationBonus>();

        protected static readonly Benchmark.Models.MobileCareersJobAd MobileCareersJobAdInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileCareersJobAd>();

        protected static readonly Benchmark.Models.MobileBannerAd MobileBannerAdInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileBannerAd>();

        protected static readonly Benchmark.Models.MobileUpdateNotice MobileUpdateNoticeInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileUpdateNotice>();

        protected static readonly Benchmark.Models.FlagOption FlagOptionInput = ExpressionTreeFixture.Create<Benchmark.Models.FlagOption>();

        protected static readonly Benchmark.Models.InboxItem InboxItemInput = ExpressionTreeFixture.Create<Benchmark.Models.InboxItem>();

        protected static readonly Benchmark.Models.Info InfoInput = ExpressionTreeFixture.Create<Benchmark.Models.Info>();

        protected static readonly Benchmark.Models.NetworkUser NetworkUserInput = ExpressionTreeFixture.Create<Benchmark.Models.NetworkUser>();

        protected static readonly Benchmark.Models.Notification NotificationInput = ExpressionTreeFixture.Create<Benchmark.Models.Notification>();

        protected static readonly Benchmark.Models.Post PostInput = ExpressionTreeFixture.Create<Benchmark.Models.Post>();

        protected static readonly Benchmark.Models.Privilege PrivilegeInput = ExpressionTreeFixture.Create<Benchmark.Models.Privilege>();

        protected static readonly Benchmark.Models.Question QuestionInput = ExpressionTreeFixture.Create<Benchmark.Models.Question>();

        protected static readonly Benchmark.Models.QuestionTimeline QuestionTimelineInput = ExpressionTreeFixture.Create<Benchmark.Models.QuestionTimeline>();

        protected static readonly Benchmark.Models.Reputation ReputationInput = ExpressionTreeFixture.Create<Benchmark.Models.Reputation>();

        protected static readonly Benchmark.Models.ReputationHistory ReputationHistoryInput = ExpressionTreeFixture.Create<Benchmark.Models.ReputationHistory>();

        protected static readonly Benchmark.Models.Revision RevisionInput = ExpressionTreeFixture.Create<Benchmark.Models.Revision>();

        protected static readonly Benchmark.Models.SearchExcerpt SearchExcerptInput = ExpressionTreeFixture.Create<Benchmark.Models.SearchExcerpt>();

        protected static readonly Benchmark.Models.ShallowUser ShallowUserInput = ExpressionTreeFixture.Create<Benchmark.Models.ShallowUser>();

        protected static readonly Benchmark.Models.SuggestedEdit SuggestedEditInput = ExpressionTreeFixture.Create<Benchmark.Models.SuggestedEdit>();

        protected static readonly Benchmark.Models.Tag TagInput = ExpressionTreeFixture.Create<Benchmark.Models.Tag>();

        protected static readonly Benchmark.Models.TagScore TagScoreInput = ExpressionTreeFixture.Create<Benchmark.Models.TagScore>();

        protected static readonly Benchmark.Models.TagSynonym TagSynonymInput = ExpressionTreeFixture.Create<Benchmark.Models.TagSynonym>();

        protected static readonly Benchmark.Models.TagWiki TagWikiInput = ExpressionTreeFixture.Create<Benchmark.Models.TagWiki>();

        protected static readonly Benchmark.Models.TopTag TopTagInput = ExpressionTreeFixture.Create<Benchmark.Models.TopTag>();

        protected static readonly Benchmark.Models.User UserInput = ExpressionTreeFixture.Create<Benchmark.Models.User>();

        protected static readonly Benchmark.Models.UserTimeline UserTimelineInput = ExpressionTreeFixture.Create<Benchmark.Models.UserTimeline>();

        protected static readonly Benchmark.Models.WritePermission WritePermissionInput = ExpressionTreeFixture.Create<Benchmark.Models.WritePermission>();

        protected static readonly Benchmark.Models.MobileBannerAd.MobileBannerAdImage MobileBannerAdImageInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileBannerAd.MobileBannerAdImage>();

        protected static readonly Benchmark.Models.Info.Site SiteInput = ExpressionTreeFixture.Create<Benchmark.Models.Info.Site>();

        protected static readonly Benchmark.Models.Info.RelatedSite RelatedSiteInput = ExpressionTreeFixture.Create<Benchmark.Models.Info.RelatedSite>();

        protected static readonly Benchmark.Models.Question.ClosedDetails ClosedDetailsInput = ExpressionTreeFixture.Create<Benchmark.Models.Question.ClosedDetails>();

        protected static readonly Benchmark.Models.Question.Notice NoticeInput = ExpressionTreeFixture.Create<Benchmark.Models.Question.Notice>();

        protected static readonly Benchmark.Models.Question.MigrationInfo MigrationInfoInput = ExpressionTreeFixture.Create<Benchmark.Models.Question.MigrationInfo>();

        protected static readonly Benchmark.Models.User.BadgeCount BadgeCountInput = ExpressionTreeFixture.Create<Benchmark.Models.User.BadgeCount>();

        protected static readonly Benchmark.Models.Info.Site.Styling StylingInput = ExpressionTreeFixture.Create<Benchmark.Models.Info.Site.Styling>();

        protected static readonly Benchmark.Models.Question.ClosedDetails.OriginalQuestion OriginalQuestionInput = ExpressionTreeFixture.Create<Benchmark.Models.Question.ClosedDetails.OriginalQuestion>();

        private object SByteOutput;
        private object ShortOutput;
        private object IntOutput;
        private object LongOutput;
        private object ByteOutput;
        private object UShortOutput;
        private object UIntOutput;
        private object ULongOutput;
        private object BoolOutput;
        private object StringOutput;
        private object CharOutput;
        private object DateTimeOutput;
        private object GuidOutput;
        private object BytesOutput;

        private object AccessTokenOutput;
        private object AccountMergeOutput;
        private object AnswerOutput;
        private object BadgeOutput;
        private object CommentOutput;
        private object ErrorOutput;
        private object EventOutput;
        private object MobileFeedOutput;
        private object MobileQuestionOutput;
        private object MobileRepChangeOutput;
        private object MobileInboxItemOutput;
        private object MobileBadgeAwardOutput;
        private object MobilePrivilegeOutput;
        private object MobileCommunityBulletinOutput;
        private object MobileAssociationBonusOutput;
        private object MobileCareersJobAdOutput;
        private object MobileBannerAdOutput;
        private object MobileUpdateNoticeOutput;
        private object FlagOptionOutput;
        private object InboxItemOutput;
        private object InfoOutput;
        private object NetworkUserOutput;
        private object NotificationOutput;
        private object PostOutput;
        private object PrivilegeOutput;
        private object QuestionOutput;
        private object QuestionTimelineOutput;
        private object ReputationOutput;
        private object ReputationHistoryOutput;
        private object RevisionOutput;
        private object SearchExcerptOutput;
        private object ShallowUserOutput;
        private object SuggestedEditOutput;
        private object TagOutput;
        private object TagScoreOutput;
        private object TagSynonymOutput;
        private object TagWikiOutput;
        private object TopTagOutput;
        private object UserOutput;
        private object UserTimelineOutput;
        private object WritePermissionOutput;
        private object MobileBannerAdImageOutput;
        private object SiteOutput;
        private object RelatedSiteOutput;
        private object ClosedDetailsOutput;
        private object NoticeOutput;
        private object MigrationInfoOutput;
        private object BadgeCountOutput;
        private object StylingOutput;
        private object OriginalQuestionOutput;

        [GlobalSetup]
        public void Setup()
        {
            // primitives
            this.SByteOutput = this.Serializer.Serialize(SByteInput);
            this.ShortOutput = this.Serializer.Serialize(ShortInput);
            this.IntOutput = this.Serializer.Serialize(IntInput);
            this.LongOutput = this.Serializer.Serialize(LongInput);
            this.ByteOutput = this.Serializer.Serialize(ByteInput);
            this.UShortOutput = this.Serializer.Serialize(UShortInput);
            this.UIntOutput = this.Serializer.Serialize(UIntInput);
            this.ULongOutput = this.Serializer.Serialize(ULongInput);
            this.BoolOutput = this.Serializer.Serialize(BoolInput);
            this.StringOutput = this.Serializer.Serialize(StringInput);
            this.CharOutput = this.Serializer.Serialize(CharInput);
            this.DateTimeOutput = this.Serializer.Serialize(DateTimeInput);
            this.GuidOutput = this.Serializer.Serialize(GuidInput);
            this.BytesOutput = this.Serializer.Serialize(BytesInput);

            // models
            this.AccessTokenOutput = this.Serializer.Serialize(AccessTokenInput);
            this.AccountMergeOutput = this.Serializer.Serialize(AccountMergeInput);
            this.AnswerOutput = this.Serializer.Serialize(AnswerInput);
            this.BadgeOutput = this.Serializer.Serialize(BadgeInput);
            this.CommentOutput = this.Serializer.Serialize(CommentInput);
            this.ErrorOutput = this.Serializer.Serialize(ErrorInput);
            this.EventOutput = this.Serializer.Serialize(EventInput);
            this.MobileFeedOutput = this.Serializer.Serialize(MobileFeedInput);
            this.MobileQuestionOutput = this.Serializer.Serialize(MobileQuestionInput);
            this.MobileRepChangeOutput = this.Serializer.Serialize(MobileRepChangeInput);
            this.MobileInboxItemOutput = this.Serializer.Serialize(MobileInboxItemInput);
            this.MobileBadgeAwardOutput = this.Serializer.Serialize(MobileBadgeAwardInput);
            this.MobilePrivilegeOutput = this.Serializer.Serialize(MobilePrivilegeInput);
            this.MobileCommunityBulletinOutput = this.Serializer.Serialize(MobileCommunityBulletinInput);
            this.MobileAssociationBonusOutput = this.Serializer.Serialize(MobileAssociationBonusInput);
            this.MobileCareersJobAdOutput = this.Serializer.Serialize(MobileCareersJobAdInput);
            this.MobileBannerAdOutput = this.Serializer.Serialize(MobileBannerAdInput);
            this.MobileUpdateNoticeOutput = this.Serializer.Serialize(MobileUpdateNoticeInput);
            this.FlagOptionOutput = this.Serializer.Serialize(FlagOptionInput);
            this.InboxItemOutput = this.Serializer.Serialize(InboxItemInput);
            this.InfoOutput = this.Serializer.Serialize(InfoInput);
            this.NetworkUserOutput = this.Serializer.Serialize(NetworkUserInput);
            this.NotificationOutput = this.Serializer.Serialize(NotificationInput);
            this.PostOutput = this.Serializer.Serialize(PostInput);
            this.PrivilegeOutput = this.Serializer.Serialize(PrivilegeInput);
            this.QuestionOutput = this.Serializer.Serialize(QuestionInput);
            this.QuestionTimelineOutput = this.Serializer.Serialize(QuestionTimelineInput);
            this.ReputationOutput = this.Serializer.Serialize(ReputationInput);
            this.ReputationHistoryOutput = this.Serializer.Serialize(ReputationHistoryInput);
            this.RevisionOutput = this.Serializer.Serialize(RevisionInput);
            this.SearchExcerptOutput = this.Serializer.Serialize(SearchExcerptInput);
            this.ShallowUserOutput = this.Serializer.Serialize(ShallowUserInput);
            this.SuggestedEditOutput = this.Serializer.Serialize(SuggestedEditInput);
            this.TagOutput = this.Serializer.Serialize(TagInput);
            this.TagScoreOutput = this.Serializer.Serialize(TagScoreInput);
            this.TagSynonymOutput = this.Serializer.Serialize(TagSynonymInput);
            this.TagWikiOutput = this.Serializer.Serialize(TagWikiInput);
            this.TopTagOutput = this.Serializer.Serialize(TopTagInput);
            this.UserOutput = this.Serializer.Serialize(UserInput);
            this.UserTimelineOutput = this.Serializer.Serialize(UserTimelineInput);
            this.WritePermissionOutput = this.Serializer.Serialize(WritePermissionInput);
            this.MobileBannerAdImageOutput = this.Serializer.Serialize(MobileBannerAdImageInput);
            this.SiteOutput = this.Serializer.Serialize(SiteInput);
            this.RelatedSiteOutput = this.Serializer.Serialize(RelatedSiteInput);
            this.ClosedDetailsOutput = this.Serializer.Serialize(ClosedDetailsInput);
            this.NoticeOutput = this.Serializer.Serialize(NoticeInput);
            this.MigrationInfoOutput = this.Serializer.Serialize(MigrationInfoInput);
            this.BadgeCountOutput = this.Serializer.Serialize(BadgeCountInput);
            this.StylingOutput = this.Serializer.Serialize(StylingInput);
            this.OriginalQuestionOutput = this.Serializer.Serialize(OriginalQuestionInput);
        }

        // Serialize
        [Benchmark] public object _PrimitiveSByteSerialize() => this.Serializer.Serialize(SByteInput);

        [Benchmark] public object _PrimitiveShortSerialize() => this.Serializer.Serialize(ShortInput);

        [Benchmark] public object _PrimitiveIntSerialize() => this.Serializer.Serialize(IntInput);

        [Benchmark] public object _PrimitiveLongSerialize() => this.Serializer.Serialize(LongInput);

        [Benchmark] public object _PrimitiveByteSerialize() => this.Serializer.Serialize(ByteInput);

        [Benchmark] public object _PrimitiveUShortSerialize() => this.Serializer.Serialize(UShortInput);

        [Benchmark] public object _PrimitiveUIntSerialize() => this.Serializer.Serialize(UIntInput);

        [Benchmark] public object _PrimitiveULongSerialize() => this.Serializer.Serialize(ULongInput);

        [Benchmark] public object _PrimitiveBoolSerialize() => this.Serializer.Serialize(BoolInput);

        [Benchmark] public object _PrimitiveStringSerialize() => this.Serializer.Serialize(StringInput);

        [Benchmark] public object _PrimitiveCharSerialize() => this.Serializer.Serialize(CharInput);

        [Benchmark] public object _PrimitiveDateTimeSerialize() => this.Serializer.Serialize(DateTimeInput);

        [Benchmark] public object _PrimitiveGuidSerialize() => this.Serializer.Serialize(GuidInput);

        [Benchmark] public object _PrimitiveBytesSerialize() => this.Serializer.Serialize(BytesInput);

        [Benchmark] public object AccessTokenSerialize() => this.Serializer.Serialize(AccessTokenInput);

        [Benchmark] public object AccountMergeSerialize() => this.Serializer.Serialize(AccountMergeInput);

        [Benchmark] public object AnswerSerialize() => this.Serializer.Serialize(AnswerInput);

        [Benchmark] public object BadgeSerialize() => this.Serializer.Serialize(BadgeInput);

        [Benchmark] public object CommentSerialize() => this.Serializer.Serialize(CommentInput);

        [Benchmark] public object ErrorSerialize() => this.Serializer.Serialize(ErrorInput);

        [Benchmark] public object EventSerialize() => this.Serializer.Serialize(EventInput);

        [Benchmark] public object MobileFeedSerialize() => this.Serializer.Serialize(MobileFeedInput);

        [Benchmark] public object MobileQuestionSerialize() => this.Serializer.Serialize(MobileQuestionInput);

        [Benchmark] public object MobileRepChangeSerialize() => this.Serializer.Serialize(MobileRepChangeInput);

        [Benchmark] public object MobileInboxItemSerialize() => this.Serializer.Serialize(MobileInboxItemInput);

        [Benchmark] public object MobileBadgeAwardSerialize() => this.Serializer.Serialize(MobileBadgeAwardInput);

        [Benchmark] public object MobilePrivilegeSerialize() => this.Serializer.Serialize(MobilePrivilegeInput);

        [Benchmark] public object MobileCommunityBulletinSerialize() => this.Serializer.Serialize(MobileCommunityBulletinInput);

        [Benchmark] public object MobileAssociationBonusSerialize() => this.Serializer.Serialize(MobileAssociationBonusInput);

        [Benchmark] public object MobileCareersJobAdSerialize() => this.Serializer.Serialize(MobileCareersJobAdInput);

        [Benchmark] public object MobileBannerAdSerialize() => this.Serializer.Serialize(MobileBannerAdInput);

        [Benchmark] public object MobileUpdateNoticeSerialize() => this.Serializer.Serialize(MobileUpdateNoticeInput);

        [Benchmark] public object FlagOptionSerialize() => this.Serializer.Serialize(FlagOptionInput);

        [Benchmark] public object InboxItemSerialize() => this.Serializer.Serialize(InboxItemInput);

        [Benchmark] public object InfoSerialize() => this.Serializer.Serialize(InfoInput);

        [Benchmark] public object NetworkUserSerialize() => this.Serializer.Serialize(NetworkUserInput);

        [Benchmark] public object NotificationSerialize() => this.Serializer.Serialize(NotificationInput);

        [Benchmark] public object PostSerialize() => this.Serializer.Serialize(PostInput);

        [Benchmark] public object PrivilegeSerialize() => this.Serializer.Serialize(PrivilegeInput);

        [Benchmark] public object QuestionSerialize() => this.Serializer.Serialize(QuestionInput);

        [Benchmark] public object QuestionTimelineSerialize() => this.Serializer.Serialize(QuestionTimelineInput);

        [Benchmark] public object ReputationSerialize() => this.Serializer.Serialize(ReputationInput);

        [Benchmark] public object ReputationHistorySerialize() => this.Serializer.Serialize(ReputationHistoryInput);

        [Benchmark] public object RevisionSerialize() => this.Serializer.Serialize(RevisionInput);

        [Benchmark] public object SearchExcerptSerialize() => this.Serializer.Serialize(SearchExcerptInput);

        [Benchmark] public object ShallowUserSerialize() => this.Serializer.Serialize(ShallowUserInput);

        [Benchmark] public object SuggestedEditSerialize() => this.Serializer.Serialize(SuggestedEditInput);

        [Benchmark] public object TagSerialize() => this.Serializer.Serialize(TagInput);

        [Benchmark] public object TagScoreSerialize() => this.Serializer.Serialize(TagScoreInput);

        [Benchmark] public object TagSynonymSerialize() => this.Serializer.Serialize(TagSynonymInput);

        [Benchmark] public object TagWikiSerialize() => this.Serializer.Serialize(TagWikiInput);

        [Benchmark] public object TopTagSerialize() => this.Serializer.Serialize(TopTagInput);

        [Benchmark] public object UserSerialize() => this.Serializer.Serialize(UserInput);

        [Benchmark] public object UserTimelineSerialize() => this.Serializer.Serialize(UserTimelineInput);

        [Benchmark] public object WritePermissionSerialize() => this.Serializer.Serialize(WritePermissionInput);

        [Benchmark] public object MobileBannerAdImageSerialize() => this.Serializer.Serialize(MobileBannerAdImageInput);

        [Benchmark] public object SiteSerialize() => this.Serializer.Serialize(SiteInput);

        [Benchmark] public object RelatedSiteSerialize() => this.Serializer.Serialize(RelatedSiteInput);

        [Benchmark] public object ClosedDetailsSerialize() => this.Serializer.Serialize(ClosedDetailsInput);

        [Benchmark] public object NoticeSerialize() => this.Serializer.Serialize(NoticeInput);

        [Benchmark] public object MigrationInfoSerialize() => this.Serializer.Serialize(MigrationInfoInput);

        [Benchmark] public object BadgeCountSerialize() => this.Serializer.Serialize(BadgeCountInput);

        [Benchmark] public object StylingSerialize() => this.Serializer.Serialize(StylingInput);

        [Benchmark] public object OriginalQuestionSerialize() => this.Serializer.Serialize(OriginalQuestionInput);

        // Deserialize
        [Benchmark] public SByte _PrimitiveSByteDeserialize() => this.Serializer.Deserialize<SByte>(this.SByteOutput);

        [Benchmark] public short _PrimitiveShortDeserialize() => this.Serializer.Deserialize<short>(this.ShortOutput);

        [Benchmark] public Int32 _PrimitiveIntDeserialize() => this.Serializer.Deserialize<Int32>(this.IntOutput);

        [Benchmark] public Int64 _PrimitiveLongDeserialize() => this.Serializer.Deserialize<Int64>(this.LongOutput);

        [Benchmark] public Byte _PrimitiveByteDeserialize() => this.Serializer.Deserialize<Byte>(this.ByteOutput);

        [Benchmark] public ushort _PrimitiveUShortDeserialize() => this.Serializer.Deserialize<ushort>(this.UShortOutput);

        [Benchmark] public uint _PrimitiveUIntDeserialize() => this.Serializer.Deserialize<uint>(this.UIntOutput);

        [Benchmark] public ulong _PrimitiveULongDeserialize() => this.Serializer.Deserialize<ulong>(this.ULongOutput);

        [Benchmark] public bool _PrimitiveBoolDeserialize() => this.Serializer.Deserialize<bool>(this.BoolOutput);

        [Benchmark] public String _PrimitiveStringDeserialize() => this.Serializer.Deserialize<String>(this.StringOutput);

        [Benchmark] public Char _PrimitiveCharDeserialize() => this.Serializer.Deserialize<Char>(this.CharOutput);

        [Benchmark] public DateTime _PrimitiveDateTimeDeserialize() => this.Serializer.Deserialize<DateTime>(this.DateTimeOutput);

        [Benchmark] public Guid _PrimitiveGuidDeserialize() => this.Serializer.Deserialize<Guid>(this.GuidOutput);

        [Benchmark] public byte[] _PrimitiveBytesDeserialize() => this.Serializer.Deserialize<byte[]>(this.BytesOutput);

        [Benchmark] public AccessToken AccessTokenDeserialize() => this.Serializer.Deserialize<AccessToken>(this.AccessTokenOutput);

        [Benchmark] public AccountMerge AccountMergeDeserialize() => this.Serializer.Deserialize<AccountMerge>(this.AccountMergeOutput);

        [Benchmark] public Answer AnswerDeserialize() => this.Serializer.Deserialize<Answer>(this.AnswerOutput);

        [Benchmark] public Badge BadgeDeserialize() => this.Serializer.Deserialize<Badge>(this.BadgeOutput);

        [Benchmark] public Comment CommentDeserialize() => this.Serializer.Deserialize<Comment>(this.CommentOutput);

        [Benchmark] public Error ErrorDeserialize() => this.Serializer.Deserialize<Error>(this.ErrorOutput);

        [Benchmark] public Event EventDeserialize() => this.Serializer.Deserialize<Event>(this.EventOutput);

        [Benchmark] public MobileFeed MobileFeedDeserialize() => this.Serializer.Deserialize<MobileFeed>(this.MobileFeedOutput);

        [Benchmark] public MobileQuestion MobileQuestionDeserialize() => this.Serializer.Deserialize<MobileQuestion>(this.MobileQuestionOutput);

        [Benchmark] public MobileRepChange MobileRepChangeDeserialize() => this.Serializer.Deserialize<MobileRepChange>(this.MobileRepChangeOutput);

        [Benchmark] public MobileInboxItem MobileInboxItemDeserialize() => this.Serializer.Deserialize<MobileInboxItem>(this.MobileInboxItemOutput);

        [Benchmark] public MobileBadgeAward MobileBadgeAwardDeserialize() => this.Serializer.Deserialize<MobileBadgeAward>(this.MobileBadgeAwardOutput);

        [Benchmark] public MobilePrivilege MobilePrivilegeDeserialize() => this.Serializer.Deserialize<MobilePrivilege>(this.MobilePrivilegeOutput);

        [Benchmark] public MobileCommunityBulletin MobileCommunityBulletinDeserialize() => this.Serializer.Deserialize<MobileCommunityBulletin>(this.MobileCommunityBulletinOutput);

        [Benchmark] public MobileAssociationBonus MobileAssociationBonusDeserialize() => this.Serializer.Deserialize<MobileAssociationBonus>(this.MobileAssociationBonusOutput);

        [Benchmark] public MobileCareersJobAd MobileCareersJobAdDeserialize() => this.Serializer.Deserialize<MobileCareersJobAd>(this.MobileCareersJobAdOutput);

        [Benchmark] public MobileBannerAd MobileBannerAdDeserialize() => this.Serializer.Deserialize<MobileBannerAd>(this.MobileBannerAdOutput);

        [Benchmark] public MobileUpdateNotice MobileUpdateNoticeDeserialize() => this.Serializer.Deserialize<MobileUpdateNotice>(this.MobileUpdateNoticeOutput);

        [Benchmark] public FlagOption FlagOptionDeserialize() => this.Serializer.Deserialize<FlagOption>(this.FlagOptionOutput);

        [Benchmark] public InboxItem InboxItemDeserialize() => this.Serializer.Deserialize<InboxItem>(this.InboxItemOutput);

        [Benchmark] public Info InfoDeserialize() => this.Serializer.Deserialize<Info>(this.InfoOutput);

        [Benchmark] public NetworkUser NetworkUserDeserialize() => this.Serializer.Deserialize<NetworkUser>(this.NetworkUserOutput);

        [Benchmark] public Notification NotificationDeserialize() => this.Serializer.Deserialize<Notification>(this.NotificationOutput);

        [Benchmark] public Post PostDeserialize() => this.Serializer.Deserialize<Post>(this.PostOutput);

        [Benchmark] public Privilege PrivilegeDeserialize() => this.Serializer.Deserialize<Privilege>(this.PrivilegeOutput);

        [Benchmark] public Question QuestionDeserialize() => this.Serializer.Deserialize<Question>(this.QuestionOutput);

        [Benchmark] public QuestionTimeline QuestionTimelineDeserialize() => this.Serializer.Deserialize<QuestionTimeline>(this.QuestionTimelineOutput);

        [Benchmark] public Reputation ReputationDeserialize() => this.Serializer.Deserialize<Reputation>(this.ReputationOutput);

        [Benchmark] public ReputationHistory ReputationHistoryDeserialize() => this.Serializer.Deserialize<ReputationHistory>(this.ReputationHistoryOutput);

        [Benchmark] public Revision RevisionDeserialize() => this.Serializer.Deserialize<Revision>(this.RevisionOutput);

        [Benchmark] public SearchExcerpt SearchExcerptDeserialize() => this.Serializer.Deserialize<SearchExcerpt>(this.SearchExcerptOutput);

        [Benchmark] public ShallowUser ShallowUserDeserialize() => this.Serializer.Deserialize<ShallowUser>(this.ShallowUserOutput);

        [Benchmark] public SuggestedEdit SuggestedEditDeserialize() => this.Serializer.Deserialize<SuggestedEdit>(this.SuggestedEditOutput);

        [Benchmark] public Tag TagDeserialize() => this.Serializer.Deserialize<Tag>(this.TagOutput);

        [Benchmark] public TagScore TagScoreDeserialize() => this.Serializer.Deserialize<TagScore>(this.TagScoreOutput);

        [Benchmark] public TagSynonym TagSynonymDeserialize() => this.Serializer.Deserialize<TagSynonym>(this.TagSynonymOutput);

        [Benchmark] public TagWiki TagWikiDeserialize() => this.Serializer.Deserialize<TagWiki>(this.TagWikiOutput);

        [Benchmark] public TopTag TopTagDeserialize() => this.Serializer.Deserialize<TopTag>(this.TopTagOutput);

        [Benchmark] public User UserDeserialize() => this.Serializer.Deserialize<User>(this.UserOutput);

        [Benchmark] public UserTimeline UserTimelineDeserialize() => this.Serializer.Deserialize<UserTimeline>(this.UserTimelineOutput);

        [Benchmark] public WritePermission WritePermissionDeserialize() => this.Serializer.Deserialize<WritePermission>(this.WritePermissionOutput);

        [Benchmark] public MobileBannerAd.MobileBannerAdImage MobileBannerAdImageDeserialize() => this.Serializer.Deserialize<MobileBannerAd.MobileBannerAdImage>(this.MobileBannerAdImageOutput);

        [Benchmark] public Info.Site SiteDeserialize() => this.Serializer.Deserialize<Info.Site>(this.SiteOutput);

        [Benchmark] public Info.RelatedSite RelatedSiteDeserialize() => this.Serializer.Deserialize<Info.RelatedSite>(this.RelatedSiteOutput);

        [Benchmark] public Question.ClosedDetails ClosedDetailsDeserialize() => this.Serializer.Deserialize<Question.ClosedDetails>(this.ClosedDetailsOutput);

        [Benchmark] public Question.Notice NoticeDeserialize() => this.Serializer.Deserialize<Question.Notice>(this.NoticeOutput);

        [Benchmark] public Question.MigrationInfo MigrationInfoDeserialize() => this.Serializer.Deserialize<Question.MigrationInfo>(this.MigrationInfoOutput);

        [Benchmark] public User.BadgeCount BadgeCountDeserialize() => this.Serializer.Deserialize<User.BadgeCount>(this.BadgeCountOutput);

        [Benchmark] public Info.Site.Styling StylingDeserialize() => this.Serializer.Deserialize<Info.Site.Styling>(this.StylingOutput);

        [Benchmark] public Question.ClosedDetails.OriginalQuestion OriginalQuestionDeserialize() => this.Serializer.Deserialize<Question.ClosedDetails.OriginalQuestion>(this.OriginalQuestionOutput);
    }

    [Config(typeof(BenchmarkConfig))]
    public class MsgPackV1_Vs_MsgPackV2_BytesInOut // : AllSerializerBenchmark
    {
        [ParamsSource(nameof(Serializers))]
        public SerializerBase Serializer;

        // Currently BenchmarkdDotNet does not detect inherited ParamsSource so use copy and paste:)
        public IEnumerable<SerializerBase> Serializers => new SerializerBase[]
        {
            new MessagePack_v1(),
            new MessagePack_v2(),
        };

        protected static readonly ExpressionTreeFixture ExpressionTreeFixture = new ExpressionTreeFixture();

        // primitives
        protected static readonly sbyte SByteInput = ExpressionTreeFixture.Create<sbyte>();
        protected static readonly short ShortInput = ExpressionTreeFixture.Create<short>();
        protected static readonly int IntInput = ExpressionTreeFixture.Create<int>();
        protected static readonly long LongInput = ExpressionTreeFixture.Create<long>();
        protected static readonly byte ByteInput = ExpressionTreeFixture.Create<byte>();
        protected static readonly ushort UShortInput = ExpressionTreeFixture.Create<ushort>();
        protected static readonly uint UIntInput = ExpressionTreeFixture.Create<uint>();
        protected static readonly ulong ULongInput = ExpressionTreeFixture.Create<ulong>();
        protected static readonly bool BoolInput = ExpressionTreeFixture.Create<bool>();
        protected static readonly string StringInput = ExpressionTreeFixture.Create<string>();
        protected static readonly char CharInput = ExpressionTreeFixture.Create<char>();
        protected static readonly DateTime DateTimeInput = ExpressionTreeFixture.Create<DateTime>();
        protected static readonly Guid GuidInput = ExpressionTreeFixture.Create<Guid>();
        protected static readonly byte[] BytesInput = ExpressionTreeFixture.Create<byte[]>();

        // models
        protected static readonly Benchmark.Models.AccessToken AccessTokenInput = ExpressionTreeFixture.Create<Benchmark.Models.AccessToken>();

        protected static readonly Benchmark.Models.AccountMerge AccountMergeInput = ExpressionTreeFixture.Create<Benchmark.Models.AccountMerge>();

        protected static readonly Benchmark.Models.Answer AnswerInput = ExpressionTreeFixture.Create<Benchmark.Models.Answer>();

        protected static readonly Benchmark.Models.Badge BadgeInput = ExpressionTreeFixture.Create<Benchmark.Models.Badge>();

        protected static readonly Benchmark.Models.Comment CommentInput = ExpressionTreeFixture.Create<Benchmark.Models.Comment>();

        protected static readonly Benchmark.Models.Error ErrorInput = ExpressionTreeFixture.Create<Benchmark.Models.Error>();

        protected static readonly Benchmark.Models.Event EventInput = ExpressionTreeFixture.Create<Benchmark.Models.Event>();

        protected static readonly Benchmark.Models.MobileFeed MobileFeedInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileFeed>();

        protected static readonly Benchmark.Models.MobileQuestion MobileQuestionInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileQuestion>();

        protected static readonly Benchmark.Models.MobileRepChange MobileRepChangeInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileRepChange>();

        protected static readonly Benchmark.Models.MobileInboxItem MobileInboxItemInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileInboxItem>();

        protected static readonly Benchmark.Models.MobileBadgeAward MobileBadgeAwardInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileBadgeAward>();

        protected static readonly Benchmark.Models.MobilePrivilege MobilePrivilegeInput = ExpressionTreeFixture.Create<Benchmark.Models.MobilePrivilege>();

        protected static readonly Benchmark.Models.MobileCommunityBulletin MobileCommunityBulletinInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileCommunityBulletin>();

        protected static readonly Benchmark.Models.MobileAssociationBonus MobileAssociationBonusInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileAssociationBonus>();

        protected static readonly Benchmark.Models.MobileCareersJobAd MobileCareersJobAdInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileCareersJobAd>();

        protected static readonly Benchmark.Models.MobileBannerAd MobileBannerAdInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileBannerAd>();

        protected static readonly Benchmark.Models.MobileUpdateNotice MobileUpdateNoticeInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileUpdateNotice>();

        protected static readonly Benchmark.Models.FlagOption FlagOptionInput = ExpressionTreeFixture.Create<Benchmark.Models.FlagOption>();

        protected static readonly Benchmark.Models.InboxItem InboxItemInput = ExpressionTreeFixture.Create<Benchmark.Models.InboxItem>();

        protected static readonly Benchmark.Models.Info InfoInput = ExpressionTreeFixture.Create<Benchmark.Models.Info>();

        protected static readonly Benchmark.Models.NetworkUser NetworkUserInput = ExpressionTreeFixture.Create<Benchmark.Models.NetworkUser>();

        protected static readonly Benchmark.Models.Notification NotificationInput = ExpressionTreeFixture.Create<Benchmark.Models.Notification>();

        protected static readonly Benchmark.Models.Post PostInput = ExpressionTreeFixture.Create<Benchmark.Models.Post>();

        protected static readonly Benchmark.Models.Privilege PrivilegeInput = ExpressionTreeFixture.Create<Benchmark.Models.Privilege>();

        protected static readonly Benchmark.Models.Question QuestionInput = ExpressionTreeFixture.Create<Benchmark.Models.Question>();

        protected static readonly Benchmark.Models.QuestionTimeline QuestionTimelineInput = ExpressionTreeFixture.Create<Benchmark.Models.QuestionTimeline>();

        protected static readonly Benchmark.Models.Reputation ReputationInput = ExpressionTreeFixture.Create<Benchmark.Models.Reputation>();

        protected static readonly Benchmark.Models.ReputationHistory ReputationHistoryInput = ExpressionTreeFixture.Create<Benchmark.Models.ReputationHistory>();

        protected static readonly Benchmark.Models.Revision RevisionInput = ExpressionTreeFixture.Create<Benchmark.Models.Revision>();

        protected static readonly Benchmark.Models.SearchExcerpt SearchExcerptInput = ExpressionTreeFixture.Create<Benchmark.Models.SearchExcerpt>();

        protected static readonly Benchmark.Models.ShallowUser ShallowUserInput = ExpressionTreeFixture.Create<Benchmark.Models.ShallowUser>();

        protected static readonly Benchmark.Models.SuggestedEdit SuggestedEditInput = ExpressionTreeFixture.Create<Benchmark.Models.SuggestedEdit>();

        protected static readonly Benchmark.Models.Tag TagInput = ExpressionTreeFixture.Create<Benchmark.Models.Tag>();

        protected static readonly Benchmark.Models.TagScore TagScoreInput = ExpressionTreeFixture.Create<Benchmark.Models.TagScore>();

        protected static readonly Benchmark.Models.TagSynonym TagSynonymInput = ExpressionTreeFixture.Create<Benchmark.Models.TagSynonym>();

        protected static readonly Benchmark.Models.TagWiki TagWikiInput = ExpressionTreeFixture.Create<Benchmark.Models.TagWiki>();

        protected static readonly Benchmark.Models.TopTag TopTagInput = ExpressionTreeFixture.Create<Benchmark.Models.TopTag>();

        protected static readonly Benchmark.Models.User UserInput = ExpressionTreeFixture.Create<Benchmark.Models.User>();

        protected static readonly Benchmark.Models.UserTimeline UserTimelineInput = ExpressionTreeFixture.Create<Benchmark.Models.UserTimeline>();

        protected static readonly Benchmark.Models.WritePermission WritePermissionInput = ExpressionTreeFixture.Create<Benchmark.Models.WritePermission>();

        protected static readonly Benchmark.Models.MobileBannerAd.MobileBannerAdImage MobileBannerAdImageInput = ExpressionTreeFixture.Create<Benchmark.Models.MobileBannerAd.MobileBannerAdImage>();

        protected static readonly Benchmark.Models.Info.Site SiteInput = ExpressionTreeFixture.Create<Benchmark.Models.Info.Site>();

        protected static readonly Benchmark.Models.Info.RelatedSite RelatedSiteInput = ExpressionTreeFixture.Create<Benchmark.Models.Info.RelatedSite>();

        protected static readonly Benchmark.Models.Question.ClosedDetails ClosedDetailsInput = ExpressionTreeFixture.Create<Benchmark.Models.Question.ClosedDetails>();

        protected static readonly Benchmark.Models.Question.Notice NoticeInput = ExpressionTreeFixture.Create<Benchmark.Models.Question.Notice>();

        protected static readonly Benchmark.Models.Question.MigrationInfo MigrationInfoInput = ExpressionTreeFixture.Create<Benchmark.Models.Question.MigrationInfo>();

        protected static readonly Benchmark.Models.User.BadgeCount BadgeCountInput = ExpressionTreeFixture.Create<Benchmark.Models.User.BadgeCount>();

        protected static readonly Benchmark.Models.Info.Site.Styling StylingInput = ExpressionTreeFixture.Create<Benchmark.Models.Info.Site.Styling>();

        protected static readonly Benchmark.Models.Question.ClosedDetails.OriginalQuestion OriginalQuestionInput = ExpressionTreeFixture.Create<Benchmark.Models.Question.ClosedDetails.OriginalQuestion>();

        private object SByteOutput;
        private object ShortOutput;
        private object IntOutput;
        private object LongOutput;
        private object ByteOutput;
        private object UShortOutput;
        private object UIntOutput;
        private object ULongOutput;
        private object BoolOutput;
        private object StringOutput;
        private object CharOutput;
        private object DateTimeOutput;
        private object GuidOutput;
        private object BytesOutput;

        private object AccessTokenOutput;
        private object AccountMergeOutput;
        private object AnswerOutput;
        private object BadgeOutput;
        private object CommentOutput;
        private object ErrorOutput;
        private object EventOutput;
        private object MobileFeedOutput;
        private object MobileQuestionOutput;
        private object MobileRepChangeOutput;
        private object MobileInboxItemOutput;
        private object MobileBadgeAwardOutput;
        private object MobilePrivilegeOutput;
        private object MobileCommunityBulletinOutput;
        private object MobileAssociationBonusOutput;
        private object MobileCareersJobAdOutput;
        private object MobileBannerAdOutput;
        private object MobileUpdateNoticeOutput;
        private object FlagOptionOutput;
        private object InboxItemOutput;
        private object InfoOutput;
        private object NetworkUserOutput;
        private object NotificationOutput;
        private object PostOutput;
        private object PrivilegeOutput;
        private object QuestionOutput;
        private object QuestionTimelineOutput;
        private object ReputationOutput;
        private object ReputationHistoryOutput;
        private object RevisionOutput;
        private object SearchExcerptOutput;
        private object ShallowUserOutput;
        private object SuggestedEditOutput;
        private object TagOutput;
        private object TagScoreOutput;
        private object TagSynonymOutput;
        private object TagWikiOutput;
        private object TopTagOutput;
        private object UserOutput;
        private object UserTimelineOutput;
        private object WritePermissionOutput;
        private object MobileBannerAdImageOutput;
        private object SiteOutput;
        private object RelatedSiteOutput;
        private object ClosedDetailsOutput;
        private object NoticeOutput;
        private object MigrationInfoOutput;
        private object BadgeCountOutput;
        private object StylingOutput;
        private object OriginalQuestionOutput;

        [GlobalSetup]
        public void Setup()
        {
            // primitives
            this.SByteOutput = this.Serializer.Serialize(SByteInput);
            this.ShortOutput = this.Serializer.Serialize(ShortInput);
            this.IntOutput = this.Serializer.Serialize(IntInput);
            this.LongOutput = this.Serializer.Serialize(LongInput);
            this.ByteOutput = this.Serializer.Serialize(ByteInput);
            this.UShortOutput = this.Serializer.Serialize(UShortInput);
            this.UIntOutput = this.Serializer.Serialize(UIntInput);
            this.ULongOutput = this.Serializer.Serialize(ULongInput);
            this.BoolOutput = this.Serializer.Serialize(BoolInput);
            this.StringOutput = this.Serializer.Serialize(StringInput);
            this.CharOutput = this.Serializer.Serialize(CharInput);
            this.DateTimeOutput = this.Serializer.Serialize(DateTimeInput);
            this.GuidOutput = this.Serializer.Serialize(GuidInput);
            this.BytesOutput = this.Serializer.Serialize(BytesInput);

            // models
            this.AccessTokenOutput = this.Serializer.Serialize(AccessTokenInput);
            this.AccountMergeOutput = this.Serializer.Serialize(AccountMergeInput);
            this.AnswerOutput = this.Serializer.Serialize(AnswerInput);
            this.BadgeOutput = this.Serializer.Serialize(BadgeInput);
            this.CommentOutput = this.Serializer.Serialize(CommentInput);
            this.ErrorOutput = this.Serializer.Serialize(ErrorInput);
            this.EventOutput = this.Serializer.Serialize(EventInput);
            this.MobileFeedOutput = this.Serializer.Serialize(MobileFeedInput);
            this.MobileQuestionOutput = this.Serializer.Serialize(MobileQuestionInput);
            this.MobileRepChangeOutput = this.Serializer.Serialize(MobileRepChangeInput);
            this.MobileInboxItemOutput = this.Serializer.Serialize(MobileInboxItemInput);
            this.MobileBadgeAwardOutput = this.Serializer.Serialize(MobileBadgeAwardInput);
            this.MobilePrivilegeOutput = this.Serializer.Serialize(MobilePrivilegeInput);
            this.MobileCommunityBulletinOutput = this.Serializer.Serialize(MobileCommunityBulletinInput);
            this.MobileAssociationBonusOutput = this.Serializer.Serialize(MobileAssociationBonusInput);
            this.MobileCareersJobAdOutput = this.Serializer.Serialize(MobileCareersJobAdInput);
            this.MobileBannerAdOutput = this.Serializer.Serialize(MobileBannerAdInput);
            this.MobileUpdateNoticeOutput = this.Serializer.Serialize(MobileUpdateNoticeInput);
            this.FlagOptionOutput = this.Serializer.Serialize(FlagOptionInput);
            this.InboxItemOutput = this.Serializer.Serialize(InboxItemInput);
            this.InfoOutput = this.Serializer.Serialize(InfoInput);
            this.NetworkUserOutput = this.Serializer.Serialize(NetworkUserInput);
            this.NotificationOutput = this.Serializer.Serialize(NotificationInput);
            this.PostOutput = this.Serializer.Serialize(PostInput);
            this.PrivilegeOutput = this.Serializer.Serialize(PrivilegeInput);
            this.QuestionOutput = this.Serializer.Serialize(QuestionInput);
            this.QuestionTimelineOutput = this.Serializer.Serialize(QuestionTimelineInput);
            this.ReputationOutput = this.Serializer.Serialize(ReputationInput);
            this.ReputationHistoryOutput = this.Serializer.Serialize(ReputationHistoryInput);
            this.RevisionOutput = this.Serializer.Serialize(RevisionInput);
            this.SearchExcerptOutput = this.Serializer.Serialize(SearchExcerptInput);
            this.ShallowUserOutput = this.Serializer.Serialize(ShallowUserInput);
            this.SuggestedEditOutput = this.Serializer.Serialize(SuggestedEditInput);
            this.TagOutput = this.Serializer.Serialize(TagInput);
            this.TagScoreOutput = this.Serializer.Serialize(TagScoreInput);
            this.TagSynonymOutput = this.Serializer.Serialize(TagSynonymInput);
            this.TagWikiOutput = this.Serializer.Serialize(TagWikiInput);
            this.TopTagOutput = this.Serializer.Serialize(TopTagInput);
            this.UserOutput = this.Serializer.Serialize(UserInput);
            this.UserTimelineOutput = this.Serializer.Serialize(UserTimelineInput);
            this.WritePermissionOutput = this.Serializer.Serialize(WritePermissionInput);
            this.MobileBannerAdImageOutput = this.Serializer.Serialize(MobileBannerAdImageInput);
            this.SiteOutput = this.Serializer.Serialize(SiteInput);
            this.RelatedSiteOutput = this.Serializer.Serialize(RelatedSiteInput);
            this.ClosedDetailsOutput = this.Serializer.Serialize(ClosedDetailsInput);
            this.NoticeOutput = this.Serializer.Serialize(NoticeInput);
            this.MigrationInfoOutput = this.Serializer.Serialize(MigrationInfoInput);
            this.BadgeCountOutput = this.Serializer.Serialize(BadgeCountInput);
            this.StylingOutput = this.Serializer.Serialize(StylingInput);
            this.OriginalQuestionOutput = this.Serializer.Serialize(OriginalQuestionInput);
        }

        // Serialize
        [Benchmark] public object _PrimitiveSByteSerialize() => this.Serializer.Serialize(SByteInput);

        [Benchmark] public object _PrimitiveShortSerialize() => this.Serializer.Serialize(ShortInput);

        [Benchmark] public object _PrimitiveIntSerialize() => this.Serializer.Serialize(IntInput);

        [Benchmark] public object _PrimitiveLongSerialize() => this.Serializer.Serialize(LongInput);

        [Benchmark] public object _PrimitiveByteSerialize() => this.Serializer.Serialize(ByteInput);

        [Benchmark] public object _PrimitiveUShortSerialize() => this.Serializer.Serialize(UShortInput);

        [Benchmark] public object _PrimitiveUIntSerialize() => this.Serializer.Serialize(UIntInput);

        [Benchmark] public object _PrimitiveULongSerialize() => this.Serializer.Serialize(ULongInput);

        [Benchmark] public object _PrimitiveBoolSerialize() => this.Serializer.Serialize(BoolInput);

        [Benchmark] public object _PrimitiveStringSerialize() => this.Serializer.Serialize(StringInput);

        [Benchmark] public object _PrimitiveCharSerialize() => this.Serializer.Serialize(CharInput);

        [Benchmark] public object _PrimitiveDateTimeSerialize() => this.Serializer.Serialize(DateTimeInput);

        [Benchmark] public object _PrimitiveGuidSerialize() => this.Serializer.Serialize(GuidInput);

        [Benchmark] public object _PrimitiveBytesSerialize() => this.Serializer.Serialize(BytesInput);

        [Benchmark] public object AccessTokenSerialize() => this.Serializer.Serialize(AccessTokenInput);

        [Benchmark] public object AccountMergeSerialize() => this.Serializer.Serialize(AccountMergeInput);

        [Benchmark] public object AnswerSerialize() => this.Serializer.Serialize(AnswerInput);

        [Benchmark] public object BadgeSerialize() => this.Serializer.Serialize(BadgeInput);

        [Benchmark] public object CommentSerialize() => this.Serializer.Serialize(CommentInput);

        [Benchmark] public object ErrorSerialize() => this.Serializer.Serialize(ErrorInput);

        [Benchmark] public object EventSerialize() => this.Serializer.Serialize(EventInput);

        [Benchmark] public object MobileFeedSerialize() => this.Serializer.Serialize(MobileFeedInput);

        [Benchmark] public object MobileQuestionSerialize() => this.Serializer.Serialize(MobileQuestionInput);

        [Benchmark] public object MobileRepChangeSerialize() => this.Serializer.Serialize(MobileRepChangeInput);

        [Benchmark] public object MobileInboxItemSerialize() => this.Serializer.Serialize(MobileInboxItemInput);

        [Benchmark] public object MobileBadgeAwardSerialize() => this.Serializer.Serialize(MobileBadgeAwardInput);

        [Benchmark] public object MobilePrivilegeSerialize() => this.Serializer.Serialize(MobilePrivilegeInput);

        [Benchmark] public object MobileCommunityBulletinSerialize() => this.Serializer.Serialize(MobileCommunityBulletinInput);

        [Benchmark] public object MobileAssociationBonusSerialize() => this.Serializer.Serialize(MobileAssociationBonusInput);

        [Benchmark] public object MobileCareersJobAdSerialize() => this.Serializer.Serialize(MobileCareersJobAdInput);

        [Benchmark] public object MobileBannerAdSerialize() => this.Serializer.Serialize(MobileBannerAdInput);

        [Benchmark] public object MobileUpdateNoticeSerialize() => this.Serializer.Serialize(MobileUpdateNoticeInput);

        [Benchmark] public object FlagOptionSerialize() => this.Serializer.Serialize(FlagOptionInput);

        [Benchmark] public object InboxItemSerialize() => this.Serializer.Serialize(InboxItemInput);

        [Benchmark] public object InfoSerialize() => this.Serializer.Serialize(InfoInput);

        [Benchmark] public object NetworkUserSerialize() => this.Serializer.Serialize(NetworkUserInput);

        [Benchmark] public object NotificationSerialize() => this.Serializer.Serialize(NotificationInput);

        [Benchmark] public object PostSerialize() => this.Serializer.Serialize(PostInput);

        [Benchmark] public object PrivilegeSerialize() => this.Serializer.Serialize(PrivilegeInput);

        [Benchmark] public object QuestionSerialize() => this.Serializer.Serialize(QuestionInput);

        [Benchmark] public object QuestionTimelineSerialize() => this.Serializer.Serialize(QuestionTimelineInput);

        [Benchmark] public object ReputationSerialize() => this.Serializer.Serialize(ReputationInput);

        [Benchmark] public object ReputationHistorySerialize() => this.Serializer.Serialize(ReputationHistoryInput);

        [Benchmark] public object RevisionSerialize() => this.Serializer.Serialize(RevisionInput);

        [Benchmark] public object SearchExcerptSerialize() => this.Serializer.Serialize(SearchExcerptInput);

        [Benchmark] public object ShallowUserSerialize() => this.Serializer.Serialize(ShallowUserInput);

        [Benchmark] public object SuggestedEditSerialize() => this.Serializer.Serialize(SuggestedEditInput);

        [Benchmark] public object TagSerialize() => this.Serializer.Serialize(TagInput);

        [Benchmark] public object TagScoreSerialize() => this.Serializer.Serialize(TagScoreInput);

        [Benchmark] public object TagSynonymSerialize() => this.Serializer.Serialize(TagSynonymInput);

        [Benchmark] public object TagWikiSerialize() => this.Serializer.Serialize(TagWikiInput);

        [Benchmark] public object TopTagSerialize() => this.Serializer.Serialize(TopTagInput);

        [Benchmark] public object UserSerialize() => this.Serializer.Serialize(UserInput);

        [Benchmark] public object UserTimelineSerialize() => this.Serializer.Serialize(UserTimelineInput);

        [Benchmark] public object WritePermissionSerialize() => this.Serializer.Serialize(WritePermissionInput);

        [Benchmark] public object MobileBannerAdImageSerialize() => this.Serializer.Serialize(MobileBannerAdImageInput);

        [Benchmark] public object SiteSerialize() => this.Serializer.Serialize(SiteInput);

        [Benchmark] public object RelatedSiteSerialize() => this.Serializer.Serialize(RelatedSiteInput);

        [Benchmark] public object ClosedDetailsSerialize() => this.Serializer.Serialize(ClosedDetailsInput);

        [Benchmark] public object NoticeSerialize() => this.Serializer.Serialize(NoticeInput);

        [Benchmark] public object MigrationInfoSerialize() => this.Serializer.Serialize(MigrationInfoInput);

        [Benchmark] public object BadgeCountSerialize() => this.Serializer.Serialize(BadgeCountInput);

        [Benchmark] public object StylingSerialize() => this.Serializer.Serialize(StylingInput);

        [Benchmark] public object OriginalQuestionSerialize() => this.Serializer.Serialize(OriginalQuestionInput);

        // Deserialize
        [Benchmark] public SByte _PrimitiveSByteDeserialize() => this.Serializer.Deserialize<SByte>(this.SByteOutput);

        [Benchmark] public short _PrimitiveShortDeserialize() => this.Serializer.Deserialize<short>(this.ShortOutput);

        [Benchmark] public Int32 _PrimitiveIntDeserialize() => this.Serializer.Deserialize<Int32>(this.IntOutput);

        [Benchmark] public Int64 _PrimitiveLongDeserialize() => this.Serializer.Deserialize<Int64>(this.LongOutput);

        [Benchmark] public Byte _PrimitiveByteDeserialize() => this.Serializer.Deserialize<Byte>(this.ByteOutput);

        [Benchmark] public ushort _PrimitiveUShortDeserialize() => this.Serializer.Deserialize<ushort>(this.UShortOutput);

        [Benchmark] public uint _PrimitiveUIntDeserialize() => this.Serializer.Deserialize<uint>(this.UIntOutput);

        [Benchmark] public ulong _PrimitiveULongDeserialize() => this.Serializer.Deserialize<ulong>(this.ULongOutput);

        [Benchmark] public bool _PrimitiveBoolDeserialize() => this.Serializer.Deserialize<bool>(this.BoolOutput);

        [Benchmark] public String _PrimitiveStringDeserialize() => this.Serializer.Deserialize<String>(this.StringOutput);

        [Benchmark] public Char _PrimitiveCharDeserialize() => this.Serializer.Deserialize<Char>(this.CharOutput);

        [Benchmark] public DateTime _PrimitiveDateTimeDeserialize() => this.Serializer.Deserialize<DateTime>(this.DateTimeOutput);

        [Benchmark] public Guid _PrimitiveGuidDeserialize() => this.Serializer.Deserialize<Guid>(this.GuidOutput);

        [Benchmark] public byte[] _PrimitiveBytesDeserialize() => this.Serializer.Deserialize<byte[]>(this.BytesOutput);

        [Benchmark] public AccessToken AccessTokenDeserialize() => this.Serializer.Deserialize<AccessToken>(this.AccessTokenOutput);

        [Benchmark] public AccountMerge AccountMergeDeserialize() => this.Serializer.Deserialize<AccountMerge>(this.AccountMergeOutput);

        [Benchmark] public Answer AnswerDeserialize() => this.Serializer.Deserialize<Answer>(this.AnswerOutput);

        [Benchmark] public Badge BadgeDeserialize() => this.Serializer.Deserialize<Badge>(this.BadgeOutput);

        [Benchmark] public Comment CommentDeserialize() => this.Serializer.Deserialize<Comment>(this.CommentOutput);

        [Benchmark] public Error ErrorDeserialize() => this.Serializer.Deserialize<Error>(this.ErrorOutput);

        [Benchmark] public Event EventDeserialize() => this.Serializer.Deserialize<Event>(this.EventOutput);

        [Benchmark] public MobileFeed MobileFeedDeserialize() => this.Serializer.Deserialize<MobileFeed>(this.MobileFeedOutput);

        [Benchmark] public MobileQuestion MobileQuestionDeserialize() => this.Serializer.Deserialize<MobileQuestion>(this.MobileQuestionOutput);

        [Benchmark] public MobileRepChange MobileRepChangeDeserialize() => this.Serializer.Deserialize<MobileRepChange>(this.MobileRepChangeOutput);

        [Benchmark] public MobileInboxItem MobileInboxItemDeserialize() => this.Serializer.Deserialize<MobileInboxItem>(this.MobileInboxItemOutput);

        [Benchmark] public MobileBadgeAward MobileBadgeAwardDeserialize() => this.Serializer.Deserialize<MobileBadgeAward>(this.MobileBadgeAwardOutput);

        [Benchmark] public MobilePrivilege MobilePrivilegeDeserialize() => this.Serializer.Deserialize<MobilePrivilege>(this.MobilePrivilegeOutput);

        [Benchmark] public MobileCommunityBulletin MobileCommunityBulletinDeserialize() => this.Serializer.Deserialize<MobileCommunityBulletin>(this.MobileCommunityBulletinOutput);

        [Benchmark] public MobileAssociationBonus MobileAssociationBonusDeserialize() => this.Serializer.Deserialize<MobileAssociationBonus>(this.MobileAssociationBonusOutput);

        [Benchmark] public MobileCareersJobAd MobileCareersJobAdDeserialize() => this.Serializer.Deserialize<MobileCareersJobAd>(this.MobileCareersJobAdOutput);

        [Benchmark] public MobileBannerAd MobileBannerAdDeserialize() => this.Serializer.Deserialize<MobileBannerAd>(this.MobileBannerAdOutput);

        [Benchmark] public MobileUpdateNotice MobileUpdateNoticeDeserialize() => this.Serializer.Deserialize<MobileUpdateNotice>(this.MobileUpdateNoticeOutput);

        [Benchmark] public FlagOption FlagOptionDeserialize() => this.Serializer.Deserialize<FlagOption>(this.FlagOptionOutput);

        [Benchmark] public InboxItem InboxItemDeserialize() => this.Serializer.Deserialize<InboxItem>(this.InboxItemOutput);

        [Benchmark] public Info InfoDeserialize() => this.Serializer.Deserialize<Info>(this.InfoOutput);

        [Benchmark] public NetworkUser NetworkUserDeserialize() => this.Serializer.Deserialize<NetworkUser>(this.NetworkUserOutput);

        [Benchmark] public Notification NotificationDeserialize() => this.Serializer.Deserialize<Notification>(this.NotificationOutput);

        [Benchmark] public Post PostDeserialize() => this.Serializer.Deserialize<Post>(this.PostOutput);

        [Benchmark] public Privilege PrivilegeDeserialize() => this.Serializer.Deserialize<Privilege>(this.PrivilegeOutput);

        [Benchmark] public Question QuestionDeserialize() => this.Serializer.Deserialize<Question>(this.QuestionOutput);

        [Benchmark] public QuestionTimeline QuestionTimelineDeserialize() => this.Serializer.Deserialize<QuestionTimeline>(this.QuestionTimelineOutput);

        [Benchmark] public Reputation ReputationDeserialize() => this.Serializer.Deserialize<Reputation>(this.ReputationOutput);

        [Benchmark] public ReputationHistory ReputationHistoryDeserialize() => this.Serializer.Deserialize<ReputationHistory>(this.ReputationHistoryOutput);

        [Benchmark] public Revision RevisionDeserialize() => this.Serializer.Deserialize<Revision>(this.RevisionOutput);

        [Benchmark] public SearchExcerpt SearchExcerptDeserialize() => this.Serializer.Deserialize<SearchExcerpt>(this.SearchExcerptOutput);

        [Benchmark] public ShallowUser ShallowUserDeserialize() => this.Serializer.Deserialize<ShallowUser>(this.ShallowUserOutput);

        [Benchmark] public SuggestedEdit SuggestedEditDeserialize() => this.Serializer.Deserialize<SuggestedEdit>(this.SuggestedEditOutput);

        [Benchmark] public Tag TagDeserialize() => this.Serializer.Deserialize<Tag>(this.TagOutput);

        [Benchmark] public TagScore TagScoreDeserialize() => this.Serializer.Deserialize<TagScore>(this.TagScoreOutput);

        [Benchmark] public TagSynonym TagSynonymDeserialize() => this.Serializer.Deserialize<TagSynonym>(this.TagSynonymOutput);

        [Benchmark] public TagWiki TagWikiDeserialize() => this.Serializer.Deserialize<TagWiki>(this.TagWikiOutput);

        [Benchmark] public TopTag TopTagDeserialize() => this.Serializer.Deserialize<TopTag>(this.TopTagOutput);

        [Benchmark] public User UserDeserialize() => this.Serializer.Deserialize<User>(this.UserOutput);

        [Benchmark] public UserTimeline UserTimelineDeserialize() => this.Serializer.Deserialize<UserTimeline>(this.UserTimelineOutput);

        [Benchmark] public WritePermission WritePermissionDeserialize() => this.Serializer.Deserialize<WritePermission>(this.WritePermissionOutput);

        [Benchmark] public MobileBannerAd.MobileBannerAdImage MobileBannerAdImageDeserialize() => this.Serializer.Deserialize<MobileBannerAd.MobileBannerAdImage>(this.MobileBannerAdImageOutput);

        [Benchmark] public Info.Site SiteDeserialize() => this.Serializer.Deserialize<Info.Site>(this.SiteOutput);

        [Benchmark] public Info.RelatedSite RelatedSiteDeserialize() => this.Serializer.Deserialize<Info.RelatedSite>(this.RelatedSiteOutput);

        [Benchmark] public Question.ClosedDetails ClosedDetailsDeserialize() => this.Serializer.Deserialize<Question.ClosedDetails>(this.ClosedDetailsOutput);

        [Benchmark] public Question.Notice NoticeDeserialize() => this.Serializer.Deserialize<Question.Notice>(this.NoticeOutput);

        [Benchmark] public Question.MigrationInfo MigrationInfoDeserialize() => this.Serializer.Deserialize<Question.MigrationInfo>(this.MigrationInfoOutput);

        [Benchmark] public User.BadgeCount BadgeCountDeserialize() => this.Serializer.Deserialize<User.BadgeCount>(this.BadgeCountOutput);

        [Benchmark] public Info.Site.Styling StylingDeserialize() => this.Serializer.Deserialize<Info.Site.Styling>(this.StylingOutput);

        [Benchmark] public Question.ClosedDetails.OriginalQuestion OriginalQuestionDeserialize() => this.Serializer.Deserialize<Question.ClosedDetails.OriginalQuestion>(this.OriginalQuestionOutput);
    }

    [Config(typeof(BenchmarkConfig))]
    public class ShortRun_AllSerializerBenchmark_BytesInOut
    {
        [ParamsSource(nameof(Serializers))]
        public SerializerBase Serializer;

        private bool isContractless;

        // Currently BenchmarkdDotNet does not detect inherited ParamsSource so use copy and paste:)
        public IEnumerable<SerializerBase> Serializers => new SerializerBase[]
        {
            new MessagePack_v1(),
            new MessagePack_v2(),
            new MsgPack_v2_opt(),
            new MessagePackLz4_v1(),
            new MessagePackLz4_v2(),
            new MsgPack_v1_string(),
            new MsgPack_v2_string(),
            new MsgPack_v1_str_lz4(),
            new MsgPack_v2_str_lz4(),
            new ProtobufNet(),
            new JsonNet(),
            new BinaryFormatter_(),
            new DataContract_(),
            new Hyperion_(),
            new Jil_(),
            new SpanJson_(),
            new Utf8Json_(),
            new SystemTextJson(),
            new MsgPackCli(),
            new FsPickler_(),
            new Ceras_(),
        };

        protected static readonly ExpressionTreeFixture ExpressionTreeFixture = new ExpressionTreeFixture();

        // primitives
        protected static readonly int IntInput = ExpressionTreeFixture.Create<int>();

        // models
        protected static readonly Benchmark.Models.Answer AnswerInput = ExpressionTreeFixture.Create<Benchmark.Models.Answer>();
        // not same data so does not gurantee correctly.
        protected static readonly Benchmark.Models.Answer2 Answer2Input = ExpressionTreeFixture.Create<Benchmark.Models.Answer2>();

        private object IntOutput;
        private object AnswerOutput;

        [GlobalSetup]
        public void Setup()
        {
            this.isContractless = (Serializer is MsgPack_v1_string) || (Serializer is MsgPack_v2_string) || (Serializer is MsgPack_v1_str_lz4) || (Serializer is MsgPack_v2_str_lz4);

            // primitives
            this.IntOutput = this.Serializer.Serialize(IntInput);

            // models
            if (isContractless)
            {
                this.AnswerOutput = this.Serializer.Serialize(Answer2Input);
            }
            else
            {
                this.AnswerOutput = this.Serializer.Serialize(AnswerInput);
            }
        }

        // Serialize
        /* [Benchmark] public object _PrimitiveIntSerialize() => this.Serializer.Serialize(IntInput); */

        [Benchmark]
        public object AnswerSerialize()
        {
            if (isContractless)
            {
                return this.Serializer.Serialize(Answer2Input);
            }
            else
            {
                return this.Serializer.Serialize(AnswerInput);
            }
        }

        // Deserialize
        /* [Benchmark] public Int32 _PrimitiveIntDeserialize() => this.Serializer.Deserialize<Int32>(this.IntOutput); */

        [Benchmark]
        public object AnswerDeserialize()
        {
            if (isContractless)
            {
                return this.Serializer.Deserialize<Answer2>(this.AnswerOutput);
            }
            else
            {
                return this.Serializer.Deserialize<Answer>(this.AnswerOutput);
            }
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class ShortRun_MsgPackV1_Vs_MsgPackV2_BytesInOut
    {
        [ParamsSource(nameof(Serializers))]
        public SerializerBase Serializer;

        // Currently BenchmarkdDotNet does not detect inherited ParamsSource so use copy and paste:)
        public IEnumerable<SerializerBase> Serializers => new SerializerBase[]
        {
            new MessagePack_v1(),
            new MessagePack_v2(),
        };

        protected static readonly ExpressionTreeFixture ExpressionTreeFixture = new ExpressionTreeFixture();

        // primitives
        protected static readonly int IntInput = ExpressionTreeFixture.Create<int>();

        // models
        protected static readonly Benchmark.Models.Answer AnswerInput = ExpressionTreeFixture.Create<Benchmark.Models.Answer>();

        private object IntOutput;
        private object AnswerOutput;

        [GlobalSetup]
        public void Setup()
        {
            // primitives
            this.IntOutput = this.Serializer.Serialize(IntInput);

            // models
            this.AnswerOutput = this.Serializer.Serialize(AnswerInput);
        }

        // Serialize
        [Benchmark] public object _PrimitiveIntSerialize() => this.Serializer.Serialize(IntInput);

        [Benchmark] public object AnswerSerialize() => this.Serializer.Serialize(AnswerInput);

        // Deserialize
        [Benchmark] public Int32 _PrimitiveIntDeserialize() => this.Serializer.Deserialize<Int32>(this.IntOutput);

        [Benchmark] public Answer AnswerDeserialize() => this.Serializer.Deserialize<Answer>(this.AnswerOutput);
    }
}

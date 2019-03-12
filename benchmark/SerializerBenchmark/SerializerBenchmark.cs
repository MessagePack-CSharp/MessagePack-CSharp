namespace Benchmark
{
    using Benchmark.Fixture;
    using Benchmark.Models;
    using Benchmark.Serializers;
    using BenchmarkDotNet.Attributes;
    using System;
    using System.Collections.Generic;

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
            new ProtobufNet(),
            new JsonNet(),
            new BinaryFormatter_(),
            new DataContract_(),
            new Hyperion_(),
            new Jil_(),
            new SpanJson_(),
            new Utf8Json_(),
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

        object SByteOutput;
        object ShortOutput;
        object IntOutput;
        object LongOutput;
        object ByteOutput;
        object UShortOutput;
        object UIntOutput;
        object ULongOutput;
        object BoolOutput;
        object StringOutput;
        object CharOutput;
        object DateTimeOutput;
        object GuidOutput;
        object BytesOutput;

        object AccessTokenOutput;
        object AccountMergeOutput;
        object AnswerOutput;
        object BadgeOutput;
        object CommentOutput;
        object ErrorOutput;
        object EventOutput;
        object MobileFeedOutput;
        object MobileQuestionOutput;
        object MobileRepChangeOutput;
        object MobileInboxItemOutput;
        object MobileBadgeAwardOutput;
        object MobilePrivilegeOutput;
        object MobileCommunityBulletinOutput;
        object MobileAssociationBonusOutput;
        object MobileCareersJobAdOutput;
        object MobileBannerAdOutput;
        object MobileUpdateNoticeOutput;
        object FlagOptionOutput;
        object InboxItemOutput;
        object InfoOutput;
        object NetworkUserOutput;
        object NotificationOutput;
        object PostOutput;
        object PrivilegeOutput;
        object QuestionOutput;
        object QuestionTimelineOutput;
        object ReputationOutput;
        object ReputationHistoryOutput;
        object RevisionOutput;
        object SearchExcerptOutput;
        object ShallowUserOutput;
        object SuggestedEditOutput;
        object TagOutput;
        object TagScoreOutput;
        object TagSynonymOutput;
        object TagWikiOutput;
        object TopTagOutput;
        object UserOutput;
        object UserTimelineOutput;
        object WritePermissionOutput;
        object MobileBannerAdImageOutput;
        object SiteOutput;
        object RelatedSiteOutput;
        object ClosedDetailsOutput;
        object NoticeOutput;
        object MigrationInfoOutput;
        object BadgeCountOutput;
        object StylingOutput;
        object OriginalQuestionOutput;

        [GlobalSetup]
        public void Setup()
        {
            // primitives
            SByteOutput = Serializer.Serialize(SByteInput);
            ShortOutput = Serializer.Serialize(ShortInput);
            IntOutput = Serializer.Serialize(IntInput);
            LongOutput = Serializer.Serialize(LongInput);
            ByteOutput = Serializer.Serialize(ByteInput);
            UShortOutput = Serializer.Serialize(UShortInput);
            UIntOutput = Serializer.Serialize(UIntInput);
            ULongOutput = Serializer.Serialize(ULongInput);
            BoolOutput = Serializer.Serialize(BoolInput);
            StringOutput = Serializer.Serialize(StringInput);
            CharOutput = Serializer.Serialize(CharInput);
            DateTimeOutput = Serializer.Serialize(DateTimeInput);
            GuidOutput = Serializer.Serialize(GuidInput);
            BytesOutput = Serializer.Serialize(BytesInput);

            // models
            AccessTokenOutput = Serializer.Serialize(AccessTokenInput);
            AccountMergeOutput = Serializer.Serialize(AccountMergeInput);
            AnswerOutput = Serializer.Serialize(AnswerInput);
            BadgeOutput = Serializer.Serialize(BadgeInput);
            CommentOutput = Serializer.Serialize(CommentInput);
            ErrorOutput = Serializer.Serialize(ErrorInput);
            EventOutput = Serializer.Serialize(EventInput);
            MobileFeedOutput = Serializer.Serialize(MobileFeedInput);
            MobileQuestionOutput = Serializer.Serialize(MobileQuestionInput);
            MobileRepChangeOutput = Serializer.Serialize(MobileRepChangeInput);
            MobileInboxItemOutput = Serializer.Serialize(MobileInboxItemInput);
            MobileBadgeAwardOutput = Serializer.Serialize(MobileBadgeAwardInput);
            MobilePrivilegeOutput = Serializer.Serialize(MobilePrivilegeInput);
            MobileCommunityBulletinOutput = Serializer.Serialize(MobileCommunityBulletinInput);
            MobileAssociationBonusOutput = Serializer.Serialize(MobileAssociationBonusInput);
            MobileCareersJobAdOutput = Serializer.Serialize(MobileCareersJobAdInput);
            MobileBannerAdOutput = Serializer.Serialize(MobileBannerAdInput);
            MobileUpdateNoticeOutput = Serializer.Serialize(MobileUpdateNoticeInput);
            FlagOptionOutput = Serializer.Serialize(FlagOptionInput);
            InboxItemOutput = Serializer.Serialize(InboxItemInput);
            InfoOutput = Serializer.Serialize(InfoInput);
            NetworkUserOutput = Serializer.Serialize(NetworkUserInput);
            NotificationOutput = Serializer.Serialize(NotificationInput);
            PostOutput = Serializer.Serialize(PostInput);
            PrivilegeOutput = Serializer.Serialize(PrivilegeInput);
            QuestionOutput = Serializer.Serialize(QuestionInput);
            QuestionTimelineOutput = Serializer.Serialize(QuestionTimelineInput);
            ReputationOutput = Serializer.Serialize(ReputationInput);
            ReputationHistoryOutput = Serializer.Serialize(ReputationHistoryInput);
            RevisionOutput = Serializer.Serialize(RevisionInput);
            SearchExcerptOutput = Serializer.Serialize(SearchExcerptInput);
            ShallowUserOutput = Serializer.Serialize(ShallowUserInput);
            SuggestedEditOutput = Serializer.Serialize(SuggestedEditInput);
            TagOutput = Serializer.Serialize(TagInput);
            TagScoreOutput = Serializer.Serialize(TagScoreInput);
            TagSynonymOutput = Serializer.Serialize(TagSynonymInput);
            TagWikiOutput = Serializer.Serialize(TagWikiInput);
            TopTagOutput = Serializer.Serialize(TopTagInput);
            UserOutput = Serializer.Serialize(UserInput);
            UserTimelineOutput = Serializer.Serialize(UserTimelineInput);
            WritePermissionOutput = Serializer.Serialize(WritePermissionInput);
            MobileBannerAdImageOutput = Serializer.Serialize(MobileBannerAdImageInput);
            SiteOutput = Serializer.Serialize(SiteInput);
            RelatedSiteOutput = Serializer.Serialize(RelatedSiteInput);
            ClosedDetailsOutput = Serializer.Serialize(ClosedDetailsInput);
            NoticeOutput = Serializer.Serialize(NoticeInput);
            MigrationInfoOutput = Serializer.Serialize(MigrationInfoInput);
            BadgeCountOutput = Serializer.Serialize(BadgeCountInput);
            StylingOutput = Serializer.Serialize(StylingInput);
            OriginalQuestionOutput = Serializer.Serialize(OriginalQuestionInput);
        }

        // Serialize

        [Benchmark] public object _PrimitiveSByteSerialize() => Serializer.Serialize(SByteInput);
        [Benchmark] public object _PrimitiveShortSerialize() => Serializer.Serialize(ShortInput);
        [Benchmark] public object _PrimitiveIntSerialize() => Serializer.Serialize(IntInput);
        [Benchmark] public object _PrimitiveLongSerialize() => Serializer.Serialize(LongInput);
        [Benchmark] public object _PrimitiveByteSerialize() => Serializer.Serialize(ByteInput);
        [Benchmark] public object _PrimitiveUShortSerialize() => Serializer.Serialize(UShortInput);
        [Benchmark] public object _PrimitiveUIntSerialize() => Serializer.Serialize(UIntInput);
        [Benchmark] public object _PrimitiveULongSerialize() => Serializer.Serialize(ULongInput);
        [Benchmark] public object _PrimitiveBoolSerialize() => Serializer.Serialize(BoolInput);
        [Benchmark] public object _PrimitiveStringSerialize() => Serializer.Serialize(StringInput);
        [Benchmark] public object _PrimitiveCharSerialize() => Serializer.Serialize(CharInput);
        [Benchmark] public object _PrimitiveDateTimeSerialize() => Serializer.Serialize(DateTimeInput);
        [Benchmark] public object _PrimitiveGuidSerialize() => Serializer.Serialize(GuidInput);
        [Benchmark] public object _PrimitiveBytesSerialize() => Serializer.Serialize(BytesInput);

        [Benchmark] public object AccessTokenSerialize() => Serializer.Serialize(AccessTokenInput);
        [Benchmark] public object AccountMergeSerialize() => Serializer.Serialize(AccountMergeInput);
        [Benchmark] public object AnswerSerialize() => Serializer.Serialize(AnswerInput);
        [Benchmark] public object BadgeSerialize() => Serializer.Serialize(BadgeInput);
        [Benchmark] public object CommentSerialize() => Serializer.Serialize(CommentInput);
        [Benchmark] public object ErrorSerialize() => Serializer.Serialize(ErrorInput);
        [Benchmark] public object EventSerialize() => Serializer.Serialize(EventInput);
        [Benchmark] public object MobileFeedSerialize() => Serializer.Serialize(MobileFeedInput);
        [Benchmark] public object MobileQuestionSerialize() => Serializer.Serialize(MobileQuestionInput);
        [Benchmark] public object MobileRepChangeSerialize() => Serializer.Serialize(MobileRepChangeInput);
        [Benchmark] public object MobileInboxItemSerialize() => Serializer.Serialize(MobileInboxItemInput);
        [Benchmark] public object MobileBadgeAwardSerialize() => Serializer.Serialize(MobileBadgeAwardInput);
        [Benchmark] public object MobilePrivilegeSerialize() => Serializer.Serialize(MobilePrivilegeInput);
        [Benchmark] public object MobileCommunityBulletinSerialize() => Serializer.Serialize(MobileCommunityBulletinInput);
        [Benchmark] public object MobileAssociationBonusSerialize() => Serializer.Serialize(MobileAssociationBonusInput);
        [Benchmark] public object MobileCareersJobAdSerialize() => Serializer.Serialize(MobileCareersJobAdInput);
        [Benchmark] public object MobileBannerAdSerialize() => Serializer.Serialize(MobileBannerAdInput);
        [Benchmark] public object MobileUpdateNoticeSerialize() => Serializer.Serialize(MobileUpdateNoticeInput);
        [Benchmark] public object FlagOptionSerialize() => Serializer.Serialize(FlagOptionInput);
        [Benchmark] public object InboxItemSerialize() => Serializer.Serialize(InboxItemInput);
        [Benchmark] public object InfoSerialize() => Serializer.Serialize(InfoInput);
        [Benchmark] public object NetworkUserSerialize() => Serializer.Serialize(NetworkUserInput);
        [Benchmark] public object NotificationSerialize() => Serializer.Serialize(NotificationInput);
        [Benchmark] public object PostSerialize() => Serializer.Serialize(PostInput);
        [Benchmark] public object PrivilegeSerialize() => Serializer.Serialize(PrivilegeInput);
        [Benchmark] public object QuestionSerialize() => Serializer.Serialize(QuestionInput);
        [Benchmark] public object QuestionTimelineSerialize() => Serializer.Serialize(QuestionTimelineInput);
        [Benchmark] public object ReputationSerialize() => Serializer.Serialize(ReputationInput);
        [Benchmark] public object ReputationHistorySerialize() => Serializer.Serialize(ReputationHistoryInput);
        [Benchmark] public object RevisionSerialize() => Serializer.Serialize(RevisionInput);
        [Benchmark] public object SearchExcerptSerialize() => Serializer.Serialize(SearchExcerptInput);
        [Benchmark] public object ShallowUserSerialize() => Serializer.Serialize(ShallowUserInput);
        [Benchmark] public object SuggestedEditSerialize() => Serializer.Serialize(SuggestedEditInput);
        [Benchmark] public object TagSerialize() => Serializer.Serialize(TagInput);
        [Benchmark] public object TagScoreSerialize() => Serializer.Serialize(TagScoreInput);
        [Benchmark] public object TagSynonymSerialize() => Serializer.Serialize(TagSynonymInput);
        [Benchmark] public object TagWikiSerialize() => Serializer.Serialize(TagWikiInput);
        [Benchmark] public object TopTagSerialize() => Serializer.Serialize(TopTagInput);
        [Benchmark] public object UserSerialize() => Serializer.Serialize(UserInput);
        [Benchmark] public object UserTimelineSerialize() => Serializer.Serialize(UserTimelineInput);
        [Benchmark] public object WritePermissionSerialize() => Serializer.Serialize(WritePermissionInput);
        [Benchmark] public object MobileBannerAdImageSerialize() => Serializer.Serialize(MobileBannerAdImageInput);
        [Benchmark] public object SiteSerialize() => Serializer.Serialize(SiteInput);
        [Benchmark] public object RelatedSiteSerialize() => Serializer.Serialize(RelatedSiteInput);
        [Benchmark] public object ClosedDetailsSerialize() => Serializer.Serialize(ClosedDetailsInput);
        [Benchmark] public object NoticeSerialize() => Serializer.Serialize(NoticeInput);
        [Benchmark] public object MigrationInfoSerialize() => Serializer.Serialize(MigrationInfoInput);
        [Benchmark] public object BadgeCountSerialize() => Serializer.Serialize(BadgeCountInput);
        [Benchmark] public object StylingSerialize() => Serializer.Serialize(StylingInput);
        [Benchmark] public object OriginalQuestionSerialize() => Serializer.Serialize(OriginalQuestionInput);

        // Deserialize

        [Benchmark] public SByte _PrimitiveSByteDeserialize() => Serializer.Deserialize<SByte>(SByteOutput);
        [Benchmark] public short _PrimitiveShortDeserialize() => Serializer.Deserialize<short>(ShortOutput);
        [Benchmark] public Int32 _PrimitiveIntDeserialize() => Serializer.Deserialize<Int32>(IntOutput);
        [Benchmark] public Int64 _PrimitiveLongDeserialize() => Serializer.Deserialize<Int64>(LongOutput);
        [Benchmark] public Byte _PrimitiveByteDeserialize() => Serializer.Deserialize<Byte>(ByteOutput);
        [Benchmark] public ushort _PrimitiveUShortDeserialize() => Serializer.Deserialize<ushort>(UShortOutput);
        [Benchmark] public uint _PrimitiveUIntDeserialize() => Serializer.Deserialize<uint>(UIntOutput);
        [Benchmark] public ulong _PrimitiveULongDeserialize() => Serializer.Deserialize<ulong>(ULongOutput);
        [Benchmark] public bool _PrimitiveBoolDeserialize() => Serializer.Deserialize<bool>(BoolOutput);
        [Benchmark] public String _PrimitiveStringDeserialize() => Serializer.Deserialize<String>(StringOutput);
        [Benchmark] public Char _PrimitiveCharDeserialize() => Serializer.Deserialize<Char>(CharOutput);
        [Benchmark] public DateTime _PrimitiveDateTimeDeserialize() => Serializer.Deserialize<DateTime>(DateTimeOutput);
        [Benchmark] public Guid _PrimitiveGuidDeserialize() => Serializer.Deserialize<Guid>(GuidOutput);
        [Benchmark] public byte[] _PrimitiveBytesDeserialize() => Serializer.Deserialize<byte[]>(BytesOutput);
        [Benchmark] public AccessToken AccessTokenDeserialize() => Serializer.Deserialize<AccessToken>(AccessTokenOutput);
        [Benchmark] public AccountMerge AccountMergeDeserialize() => Serializer.Deserialize<AccountMerge>(AccountMergeOutput);
        [Benchmark] public Answer AnswerDeserialize() => Serializer.Deserialize<Answer>(AnswerOutput);
        [Benchmark] public Badge BadgeDeserialize() => Serializer.Deserialize<Badge>(BadgeOutput);
        [Benchmark] public Comment CommentDeserialize() => Serializer.Deserialize<Comment>(CommentOutput);
        [Benchmark] public Error ErrorDeserialize() => Serializer.Deserialize<Error>(ErrorOutput);
        [Benchmark] public Event EventDeserialize() => Serializer.Deserialize<Event>(EventOutput);
        [Benchmark] public MobileFeed MobileFeedDeserialize() => Serializer.Deserialize<MobileFeed>(MobileFeedOutput);
        [Benchmark] public MobileQuestion MobileQuestionDeserialize() => Serializer.Deserialize<MobileQuestion>(MobileQuestionOutput);
        [Benchmark] public MobileRepChange MobileRepChangeDeserialize() => Serializer.Deserialize<MobileRepChange>(MobileRepChangeOutput);
        [Benchmark] public MobileInboxItem MobileInboxItemDeserialize() => Serializer.Deserialize<MobileInboxItem>(MobileInboxItemOutput);
        [Benchmark] public MobileBadgeAward MobileBadgeAwardDeserialize() => Serializer.Deserialize<MobileBadgeAward>(MobileBadgeAwardOutput);
        [Benchmark] public MobilePrivilege MobilePrivilegeDeserialize() => Serializer.Deserialize<MobilePrivilege>(MobilePrivilegeOutput);
        [Benchmark] public MobileCommunityBulletin MobileCommunityBulletinDeserialize() => Serializer.Deserialize<MobileCommunityBulletin>(MobileCommunityBulletinOutput);
        [Benchmark] public MobileAssociationBonus MobileAssociationBonusDeserialize() => Serializer.Deserialize<MobileAssociationBonus>(MobileAssociationBonusOutput);
        [Benchmark] public MobileCareersJobAd MobileCareersJobAdDeserialize() => Serializer.Deserialize<MobileCareersJobAd>(MobileCareersJobAdOutput);
        [Benchmark] public MobileBannerAd MobileBannerAdDeserialize() => Serializer.Deserialize<MobileBannerAd>(MobileBannerAdOutput);
        [Benchmark] public MobileUpdateNotice MobileUpdateNoticeDeserialize() => Serializer.Deserialize<MobileUpdateNotice>(MobileUpdateNoticeOutput);
        [Benchmark] public FlagOption FlagOptionDeserialize() => Serializer.Deserialize<FlagOption>(FlagOptionOutput);
        [Benchmark] public InboxItem InboxItemDeserialize() => Serializer.Deserialize<InboxItem>(InboxItemOutput);
        [Benchmark] public Info InfoDeserialize() => Serializer.Deserialize<Info>(InfoOutput);
        [Benchmark] public NetworkUser NetworkUserDeserialize() => Serializer.Deserialize<NetworkUser>(NetworkUserOutput);
        [Benchmark] public Notification NotificationDeserialize() => Serializer.Deserialize<Notification>(NotificationOutput);
        [Benchmark] public Post PostDeserialize() => Serializer.Deserialize<Post>(PostOutput);
        [Benchmark] public Privilege PrivilegeDeserialize() => Serializer.Deserialize<Privilege>(PrivilegeOutput);
        [Benchmark] public Question QuestionDeserialize() => Serializer.Deserialize<Question>(QuestionOutput);
        [Benchmark] public QuestionTimeline QuestionTimelineDeserialize() => Serializer.Deserialize<QuestionTimeline>(QuestionTimelineOutput);
        [Benchmark] public Reputation ReputationDeserialize() => Serializer.Deserialize<Reputation>(ReputationOutput);
        [Benchmark] public ReputationHistory ReputationHistoryDeserialize() => Serializer.Deserialize<ReputationHistory>(ReputationHistoryOutput);
        [Benchmark] public Revision RevisionDeserialize() => Serializer.Deserialize<Revision>(RevisionOutput);
        [Benchmark] public SearchExcerpt SearchExcerptDeserialize() => Serializer.Deserialize<SearchExcerpt>(SearchExcerptOutput);
        [Benchmark] public ShallowUser ShallowUserDeserialize() => Serializer.Deserialize<ShallowUser>(ShallowUserOutput);
        [Benchmark] public SuggestedEdit SuggestedEditDeserialize() => Serializer.Deserialize<SuggestedEdit>(SuggestedEditOutput);
        [Benchmark] public Tag TagDeserialize() => Serializer.Deserialize<Tag>(TagOutput);
        [Benchmark] public TagScore TagScoreDeserialize() => Serializer.Deserialize<TagScore>(TagScoreOutput);
        [Benchmark] public TagSynonym TagSynonymDeserialize() => Serializer.Deserialize<TagSynonym>(TagSynonymOutput);
        [Benchmark] public TagWiki TagWikiDeserialize() => Serializer.Deserialize<TagWiki>(TagWikiOutput);
        [Benchmark] public TopTag TopTagDeserialize() => Serializer.Deserialize<TopTag>(TopTagOutput);
        [Benchmark] public User UserDeserialize() => Serializer.Deserialize<User>(UserOutput);
        [Benchmark] public UserTimeline UserTimelineDeserialize() => Serializer.Deserialize<UserTimeline>(UserTimelineOutput);
        [Benchmark] public WritePermission WritePermissionDeserialize() => Serializer.Deserialize<WritePermission>(WritePermissionOutput);
        [Benchmark] public MobileBannerAd.MobileBannerAdImage MobileBannerAdImageDeserialize() => Serializer.Deserialize<MobileBannerAd.MobileBannerAdImage>(MobileBannerAdImageOutput);
        [Benchmark] public Info.Site SiteDeserialize() => Serializer.Deserialize<Info.Site>(SiteOutput);
        [Benchmark] public Info.RelatedSite RelatedSiteDeserialize() => Serializer.Deserialize<Info.RelatedSite>(RelatedSiteOutput);
        [Benchmark] public Question.ClosedDetails ClosedDetailsDeserialize() => Serializer.Deserialize<Question.ClosedDetails>(ClosedDetailsOutput);
        [Benchmark] public Question.Notice NoticeDeserialize() => Serializer.Deserialize<Question.Notice>(NoticeOutput);
        [Benchmark] public Question.MigrationInfo MigrationInfoDeserialize() => Serializer.Deserialize<Question.MigrationInfo>(MigrationInfoOutput);
        [Benchmark] public User.BadgeCount BadgeCountDeserialize() => Serializer.Deserialize<User.BadgeCount>(BadgeCountOutput);
        [Benchmark] public Info.Site.Styling StylingDeserialize() => Serializer.Deserialize<Info.Site.Styling>(StylingOutput);
        [Benchmark] public Question.ClosedDetails.OriginalQuestion OriginalQuestionDeserialize() => Serializer.Deserialize<Question.ClosedDetails.OriginalQuestion>(OriginalQuestionOutput);
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

        object SByteOutput;
        object ShortOutput;
        object IntOutput;
        object LongOutput;
        object ByteOutput;
        object UShortOutput;
        object UIntOutput;
        object ULongOutput;
        object BoolOutput;
        object StringOutput;
        object CharOutput;
        object DateTimeOutput;
        object GuidOutput;
        object BytesOutput;

        object AccessTokenOutput;
        object AccountMergeOutput;
        object AnswerOutput;
        object BadgeOutput;
        object CommentOutput;
        object ErrorOutput;
        object EventOutput;
        object MobileFeedOutput;
        object MobileQuestionOutput;
        object MobileRepChangeOutput;
        object MobileInboxItemOutput;
        object MobileBadgeAwardOutput;
        object MobilePrivilegeOutput;
        object MobileCommunityBulletinOutput;
        object MobileAssociationBonusOutput;
        object MobileCareersJobAdOutput;
        object MobileBannerAdOutput;
        object MobileUpdateNoticeOutput;
        object FlagOptionOutput;
        object InboxItemOutput;
        object InfoOutput;
        object NetworkUserOutput;
        object NotificationOutput;
        object PostOutput;
        object PrivilegeOutput;
        object QuestionOutput;
        object QuestionTimelineOutput;
        object ReputationOutput;
        object ReputationHistoryOutput;
        object RevisionOutput;
        object SearchExcerptOutput;
        object ShallowUserOutput;
        object SuggestedEditOutput;
        object TagOutput;
        object TagScoreOutput;
        object TagSynonymOutput;
        object TagWikiOutput;
        object TopTagOutput;
        object UserOutput;
        object UserTimelineOutput;
        object WritePermissionOutput;
        object MobileBannerAdImageOutput;
        object SiteOutput;
        object RelatedSiteOutput;
        object ClosedDetailsOutput;
        object NoticeOutput;
        object MigrationInfoOutput;
        object BadgeCountOutput;
        object StylingOutput;
        object OriginalQuestionOutput;

        [GlobalSetup]
        public void Setup()
        {
            // primitives
            SByteOutput = Serializer.Serialize(SByteInput);
            ShortOutput = Serializer.Serialize(ShortInput);
            IntOutput = Serializer.Serialize(IntInput);
            LongOutput = Serializer.Serialize(LongInput);
            ByteOutput = Serializer.Serialize(ByteInput);
            UShortOutput = Serializer.Serialize(UShortInput);
            UIntOutput = Serializer.Serialize(UIntInput);
            ULongOutput = Serializer.Serialize(ULongInput);
            BoolOutput = Serializer.Serialize(BoolInput);
            StringOutput = Serializer.Serialize(StringInput);
            CharOutput = Serializer.Serialize(CharInput);
            DateTimeOutput = Serializer.Serialize(DateTimeInput);
            GuidOutput = Serializer.Serialize(GuidInput);
            BytesOutput = Serializer.Serialize(BytesInput);

            // models
            AccessTokenOutput = Serializer.Serialize(AccessTokenInput);
            AccountMergeOutput = Serializer.Serialize(AccountMergeInput);
            AnswerOutput = Serializer.Serialize(AnswerInput);
            BadgeOutput = Serializer.Serialize(BadgeInput);
            CommentOutput = Serializer.Serialize(CommentInput);
            ErrorOutput = Serializer.Serialize(ErrorInput);
            EventOutput = Serializer.Serialize(EventInput);
            MobileFeedOutput = Serializer.Serialize(MobileFeedInput);
            MobileQuestionOutput = Serializer.Serialize(MobileQuestionInput);
            MobileRepChangeOutput = Serializer.Serialize(MobileRepChangeInput);
            MobileInboxItemOutput = Serializer.Serialize(MobileInboxItemInput);
            MobileBadgeAwardOutput = Serializer.Serialize(MobileBadgeAwardInput);
            MobilePrivilegeOutput = Serializer.Serialize(MobilePrivilegeInput);
            MobileCommunityBulletinOutput = Serializer.Serialize(MobileCommunityBulletinInput);
            MobileAssociationBonusOutput = Serializer.Serialize(MobileAssociationBonusInput);
            MobileCareersJobAdOutput = Serializer.Serialize(MobileCareersJobAdInput);
            MobileBannerAdOutput = Serializer.Serialize(MobileBannerAdInput);
            MobileUpdateNoticeOutput = Serializer.Serialize(MobileUpdateNoticeInput);
            FlagOptionOutput = Serializer.Serialize(FlagOptionInput);
            InboxItemOutput = Serializer.Serialize(InboxItemInput);
            InfoOutput = Serializer.Serialize(InfoInput);
            NetworkUserOutput = Serializer.Serialize(NetworkUserInput);
            NotificationOutput = Serializer.Serialize(NotificationInput);
            PostOutput = Serializer.Serialize(PostInput);
            PrivilegeOutput = Serializer.Serialize(PrivilegeInput);
            QuestionOutput = Serializer.Serialize(QuestionInput);
            QuestionTimelineOutput = Serializer.Serialize(QuestionTimelineInput);
            ReputationOutput = Serializer.Serialize(ReputationInput);
            ReputationHistoryOutput = Serializer.Serialize(ReputationHistoryInput);
            RevisionOutput = Serializer.Serialize(RevisionInput);
            SearchExcerptOutput = Serializer.Serialize(SearchExcerptInput);
            ShallowUserOutput = Serializer.Serialize(ShallowUserInput);
            SuggestedEditOutput = Serializer.Serialize(SuggestedEditInput);
            TagOutput = Serializer.Serialize(TagInput);
            TagScoreOutput = Serializer.Serialize(TagScoreInput);
            TagSynonymOutput = Serializer.Serialize(TagSynonymInput);
            TagWikiOutput = Serializer.Serialize(TagWikiInput);
            TopTagOutput = Serializer.Serialize(TopTagInput);
            UserOutput = Serializer.Serialize(UserInput);
            UserTimelineOutput = Serializer.Serialize(UserTimelineInput);
            WritePermissionOutput = Serializer.Serialize(WritePermissionInput);
            MobileBannerAdImageOutput = Serializer.Serialize(MobileBannerAdImageInput);
            SiteOutput = Serializer.Serialize(SiteInput);
            RelatedSiteOutput = Serializer.Serialize(RelatedSiteInput);
            ClosedDetailsOutput = Serializer.Serialize(ClosedDetailsInput);
            NoticeOutput = Serializer.Serialize(NoticeInput);
            MigrationInfoOutput = Serializer.Serialize(MigrationInfoInput);
            BadgeCountOutput = Serializer.Serialize(BadgeCountInput);
            StylingOutput = Serializer.Serialize(StylingInput);
            OriginalQuestionOutput = Serializer.Serialize(OriginalQuestionInput);
        }

        // Serialize

        [Benchmark] public object _PrimitiveSByteSerialize() => Serializer.Serialize(SByteInput);
        [Benchmark] public object _PrimitiveShortSerialize() => Serializer.Serialize(ShortInput);
        [Benchmark] public object _PrimitiveIntSerialize() => Serializer.Serialize(IntInput);
        [Benchmark] public object _PrimitiveLongSerialize() => Serializer.Serialize(LongInput);
        [Benchmark] public object _PrimitiveByteSerialize() => Serializer.Serialize(ByteInput);
        [Benchmark] public object _PrimitiveUShortSerialize() => Serializer.Serialize(UShortInput);
        [Benchmark] public object _PrimitiveUIntSerialize() => Serializer.Serialize(UIntInput);
        [Benchmark] public object _PrimitiveULongSerialize() => Serializer.Serialize(ULongInput);
        [Benchmark] public object _PrimitiveBoolSerialize() => Serializer.Serialize(BoolInput);
        [Benchmark] public object _PrimitiveStringSerialize() => Serializer.Serialize(StringInput);
        [Benchmark] public object _PrimitiveCharSerialize() => Serializer.Serialize(CharInput);
        [Benchmark] public object _PrimitiveDateTimeSerialize() => Serializer.Serialize(DateTimeInput);
        [Benchmark] public object _PrimitiveGuidSerialize() => Serializer.Serialize(GuidInput);
        [Benchmark] public object _PrimitiveBytesSerialize() => Serializer.Serialize(BytesInput);

        [Benchmark] public object AccessTokenSerialize() => Serializer.Serialize(AccessTokenInput);
        [Benchmark] public object AccountMergeSerialize() => Serializer.Serialize(AccountMergeInput);
        [Benchmark] public object AnswerSerialize() => Serializer.Serialize(AnswerInput);
        [Benchmark] public object BadgeSerialize() => Serializer.Serialize(BadgeInput);
        [Benchmark] public object CommentSerialize() => Serializer.Serialize(CommentInput);
        [Benchmark] public object ErrorSerialize() => Serializer.Serialize(ErrorInput);
        [Benchmark] public object EventSerialize() => Serializer.Serialize(EventInput);
        [Benchmark] public object MobileFeedSerialize() => Serializer.Serialize(MobileFeedInput);
        [Benchmark] public object MobileQuestionSerialize() => Serializer.Serialize(MobileQuestionInput);
        [Benchmark] public object MobileRepChangeSerialize() => Serializer.Serialize(MobileRepChangeInput);
        [Benchmark] public object MobileInboxItemSerialize() => Serializer.Serialize(MobileInboxItemInput);
        [Benchmark] public object MobileBadgeAwardSerialize() => Serializer.Serialize(MobileBadgeAwardInput);
        [Benchmark] public object MobilePrivilegeSerialize() => Serializer.Serialize(MobilePrivilegeInput);
        [Benchmark] public object MobileCommunityBulletinSerialize() => Serializer.Serialize(MobileCommunityBulletinInput);
        [Benchmark] public object MobileAssociationBonusSerialize() => Serializer.Serialize(MobileAssociationBonusInput);
        [Benchmark] public object MobileCareersJobAdSerialize() => Serializer.Serialize(MobileCareersJobAdInput);
        [Benchmark] public object MobileBannerAdSerialize() => Serializer.Serialize(MobileBannerAdInput);
        [Benchmark] public object MobileUpdateNoticeSerialize() => Serializer.Serialize(MobileUpdateNoticeInput);
        [Benchmark] public object FlagOptionSerialize() => Serializer.Serialize(FlagOptionInput);
        [Benchmark] public object InboxItemSerialize() => Serializer.Serialize(InboxItemInput);
        [Benchmark] public object InfoSerialize() => Serializer.Serialize(InfoInput);
        [Benchmark] public object NetworkUserSerialize() => Serializer.Serialize(NetworkUserInput);
        [Benchmark] public object NotificationSerialize() => Serializer.Serialize(NotificationInput);
        [Benchmark] public object PostSerialize() => Serializer.Serialize(PostInput);
        [Benchmark] public object PrivilegeSerialize() => Serializer.Serialize(PrivilegeInput);
        [Benchmark] public object QuestionSerialize() => Serializer.Serialize(QuestionInput);
        [Benchmark] public object QuestionTimelineSerialize() => Serializer.Serialize(QuestionTimelineInput);
        [Benchmark] public object ReputationSerialize() => Serializer.Serialize(ReputationInput);
        [Benchmark] public object ReputationHistorySerialize() => Serializer.Serialize(ReputationHistoryInput);
        [Benchmark] public object RevisionSerialize() => Serializer.Serialize(RevisionInput);
        [Benchmark] public object SearchExcerptSerialize() => Serializer.Serialize(SearchExcerptInput);
        [Benchmark] public object ShallowUserSerialize() => Serializer.Serialize(ShallowUserInput);
        [Benchmark] public object SuggestedEditSerialize() => Serializer.Serialize(SuggestedEditInput);
        [Benchmark] public object TagSerialize() => Serializer.Serialize(TagInput);
        [Benchmark] public object TagScoreSerialize() => Serializer.Serialize(TagScoreInput);
        [Benchmark] public object TagSynonymSerialize() => Serializer.Serialize(TagSynonymInput);
        [Benchmark] public object TagWikiSerialize() => Serializer.Serialize(TagWikiInput);
        [Benchmark] public object TopTagSerialize() => Serializer.Serialize(TopTagInput);
        [Benchmark] public object UserSerialize() => Serializer.Serialize(UserInput);
        [Benchmark] public object UserTimelineSerialize() => Serializer.Serialize(UserTimelineInput);
        [Benchmark] public object WritePermissionSerialize() => Serializer.Serialize(WritePermissionInput);
        [Benchmark] public object MobileBannerAdImageSerialize() => Serializer.Serialize(MobileBannerAdImageInput);
        [Benchmark] public object SiteSerialize() => Serializer.Serialize(SiteInput);
        [Benchmark] public object RelatedSiteSerialize() => Serializer.Serialize(RelatedSiteInput);
        [Benchmark] public object ClosedDetailsSerialize() => Serializer.Serialize(ClosedDetailsInput);
        [Benchmark] public object NoticeSerialize() => Serializer.Serialize(NoticeInput);
        [Benchmark] public object MigrationInfoSerialize() => Serializer.Serialize(MigrationInfoInput);
        [Benchmark] public object BadgeCountSerialize() => Serializer.Serialize(BadgeCountInput);
        [Benchmark] public object StylingSerialize() => Serializer.Serialize(StylingInput);
        [Benchmark] public object OriginalQuestionSerialize() => Serializer.Serialize(OriginalQuestionInput);

        // Deserialize

        [Benchmark] public SByte _PrimitiveSByteDeserialize() => Serializer.Deserialize<SByte>(SByteOutput);
        [Benchmark] public short _PrimitiveShortDeserialize() => Serializer.Deserialize<short>(ShortOutput);
        [Benchmark] public Int32 _PrimitiveIntDeserialize() => Serializer.Deserialize<Int32>(IntOutput);
        [Benchmark] public Int64 _PrimitiveLongDeserialize() => Serializer.Deserialize<Int64>(LongOutput);
        [Benchmark] public Byte _PrimitiveByteDeserialize() => Serializer.Deserialize<Byte>(ByteOutput);
        [Benchmark] public ushort _PrimitiveUShortDeserialize() => Serializer.Deserialize<ushort>(UShortOutput);
        [Benchmark] public uint _PrimitiveUIntDeserialize() => Serializer.Deserialize<uint>(UIntOutput);
        [Benchmark] public ulong _PrimitiveULongDeserialize() => Serializer.Deserialize<ulong>(ULongOutput);
        [Benchmark] public bool _PrimitiveBoolDeserialize() => Serializer.Deserialize<bool>(BoolOutput);
        [Benchmark] public String _PrimitiveStringDeserialize() => Serializer.Deserialize<String>(StringOutput);
        [Benchmark] public Char _PrimitiveCharDeserialize() => Serializer.Deserialize<Char>(CharOutput);
        [Benchmark] public DateTime _PrimitiveDateTimeDeserialize() => Serializer.Deserialize<DateTime>(DateTimeOutput);
        [Benchmark] public Guid _PrimitiveGuidDeserialize() => Serializer.Deserialize<Guid>(GuidOutput);
        [Benchmark] public byte[] _PrimitiveBytesDeserialize() => Serializer.Deserialize<byte[]>(BytesOutput);
        [Benchmark] public AccessToken AccessTokenDeserialize() => Serializer.Deserialize<AccessToken>(AccessTokenOutput);
        [Benchmark] public AccountMerge AccountMergeDeserialize() => Serializer.Deserialize<AccountMerge>(AccountMergeOutput);
        [Benchmark] public Answer AnswerDeserialize() => Serializer.Deserialize<Answer>(AnswerOutput);
        [Benchmark] public Badge BadgeDeserialize() => Serializer.Deserialize<Badge>(BadgeOutput);
        [Benchmark] public Comment CommentDeserialize() => Serializer.Deserialize<Comment>(CommentOutput);
        [Benchmark] public Error ErrorDeserialize() => Serializer.Deserialize<Error>(ErrorOutput);
        [Benchmark] public Event EventDeserialize() => Serializer.Deserialize<Event>(EventOutput);
        [Benchmark] public MobileFeed MobileFeedDeserialize() => Serializer.Deserialize<MobileFeed>(MobileFeedOutput);
        [Benchmark] public MobileQuestion MobileQuestionDeserialize() => Serializer.Deserialize<MobileQuestion>(MobileQuestionOutput);
        [Benchmark] public MobileRepChange MobileRepChangeDeserialize() => Serializer.Deserialize<MobileRepChange>(MobileRepChangeOutput);
        [Benchmark] public MobileInboxItem MobileInboxItemDeserialize() => Serializer.Deserialize<MobileInboxItem>(MobileInboxItemOutput);
        [Benchmark] public MobileBadgeAward MobileBadgeAwardDeserialize() => Serializer.Deserialize<MobileBadgeAward>(MobileBadgeAwardOutput);
        [Benchmark] public MobilePrivilege MobilePrivilegeDeserialize() => Serializer.Deserialize<MobilePrivilege>(MobilePrivilegeOutput);
        [Benchmark] public MobileCommunityBulletin MobileCommunityBulletinDeserialize() => Serializer.Deserialize<MobileCommunityBulletin>(MobileCommunityBulletinOutput);
        [Benchmark] public MobileAssociationBonus MobileAssociationBonusDeserialize() => Serializer.Deserialize<MobileAssociationBonus>(MobileAssociationBonusOutput);
        [Benchmark] public MobileCareersJobAd MobileCareersJobAdDeserialize() => Serializer.Deserialize<MobileCareersJobAd>(MobileCareersJobAdOutput);
        [Benchmark] public MobileBannerAd MobileBannerAdDeserialize() => Serializer.Deserialize<MobileBannerAd>(MobileBannerAdOutput);
        [Benchmark] public MobileUpdateNotice MobileUpdateNoticeDeserialize() => Serializer.Deserialize<MobileUpdateNotice>(MobileUpdateNoticeOutput);
        [Benchmark] public FlagOption FlagOptionDeserialize() => Serializer.Deserialize<FlagOption>(FlagOptionOutput);
        [Benchmark] public InboxItem InboxItemDeserialize() => Serializer.Deserialize<InboxItem>(InboxItemOutput);
        [Benchmark] public Info InfoDeserialize() => Serializer.Deserialize<Info>(InfoOutput);
        [Benchmark] public NetworkUser NetworkUserDeserialize() => Serializer.Deserialize<NetworkUser>(NetworkUserOutput);
        [Benchmark] public Notification NotificationDeserialize() => Serializer.Deserialize<Notification>(NotificationOutput);
        [Benchmark] public Post PostDeserialize() => Serializer.Deserialize<Post>(PostOutput);
        [Benchmark] public Privilege PrivilegeDeserialize() => Serializer.Deserialize<Privilege>(PrivilegeOutput);
        [Benchmark] public Question QuestionDeserialize() => Serializer.Deserialize<Question>(QuestionOutput);
        [Benchmark] public QuestionTimeline QuestionTimelineDeserialize() => Serializer.Deserialize<QuestionTimeline>(QuestionTimelineOutput);
        [Benchmark] public Reputation ReputationDeserialize() => Serializer.Deserialize<Reputation>(ReputationOutput);
        [Benchmark] public ReputationHistory ReputationHistoryDeserialize() => Serializer.Deserialize<ReputationHistory>(ReputationHistoryOutput);
        [Benchmark] public Revision RevisionDeserialize() => Serializer.Deserialize<Revision>(RevisionOutput);
        [Benchmark] public SearchExcerpt SearchExcerptDeserialize() => Serializer.Deserialize<SearchExcerpt>(SearchExcerptOutput);
        [Benchmark] public ShallowUser ShallowUserDeserialize() => Serializer.Deserialize<ShallowUser>(ShallowUserOutput);
        [Benchmark] public SuggestedEdit SuggestedEditDeserialize() => Serializer.Deserialize<SuggestedEdit>(SuggestedEditOutput);
        [Benchmark] public Tag TagDeserialize() => Serializer.Deserialize<Tag>(TagOutput);
        [Benchmark] public TagScore TagScoreDeserialize() => Serializer.Deserialize<TagScore>(TagScoreOutput);
        [Benchmark] public TagSynonym TagSynonymDeserialize() => Serializer.Deserialize<TagSynonym>(TagSynonymOutput);
        [Benchmark] public TagWiki TagWikiDeserialize() => Serializer.Deserialize<TagWiki>(TagWikiOutput);
        [Benchmark] public TopTag TopTagDeserialize() => Serializer.Deserialize<TopTag>(TopTagOutput);
        [Benchmark] public User UserDeserialize() => Serializer.Deserialize<User>(UserOutput);
        [Benchmark] public UserTimeline UserTimelineDeserialize() => Serializer.Deserialize<UserTimeline>(UserTimelineOutput);
        [Benchmark] public WritePermission WritePermissionDeserialize() => Serializer.Deserialize<WritePermission>(WritePermissionOutput);
        [Benchmark] public MobileBannerAd.MobileBannerAdImage MobileBannerAdImageDeserialize() => Serializer.Deserialize<MobileBannerAd.MobileBannerAdImage>(MobileBannerAdImageOutput);
        [Benchmark] public Info.Site SiteDeserialize() => Serializer.Deserialize<Info.Site>(SiteOutput);
        [Benchmark] public Info.RelatedSite RelatedSiteDeserialize() => Serializer.Deserialize<Info.RelatedSite>(RelatedSiteOutput);
        [Benchmark] public Question.ClosedDetails ClosedDetailsDeserialize() => Serializer.Deserialize<Question.ClosedDetails>(ClosedDetailsOutput);
        [Benchmark] public Question.Notice NoticeDeserialize() => Serializer.Deserialize<Question.Notice>(NoticeOutput);
        [Benchmark] public Question.MigrationInfo MigrationInfoDeserialize() => Serializer.Deserialize<Question.MigrationInfo>(MigrationInfoOutput);
        [Benchmark] public User.BadgeCount BadgeCountDeserialize() => Serializer.Deserialize<User.BadgeCount>(BadgeCountOutput);
        [Benchmark] public Info.Site.Styling StylingDeserialize() => Serializer.Deserialize<Info.Site.Styling>(StylingOutput);
        [Benchmark] public Question.ClosedDetails.OriginalQuestion OriginalQuestionDeserialize() => Serializer.Deserialize<Question.ClosedDetails.OriginalQuestion>(OriginalQuestionOutput);
    }


    [Config(typeof(BenchmarkConfig))]
    public class ShortRun_AllSerializerBenchmark_BytesInOut
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
            new ProtobufNet(),
            new JsonNet(),
            new BinaryFormatter_(),
            new DataContract_(),
            new Hyperion_(),
            new Jil_(),
            new SpanJson_(),
            new Utf8Json_(),
            new MsgPackCli(),
            new FsPickler_(),
            new Ceras_(),
        };

        protected static readonly ExpressionTreeFixture ExpressionTreeFixture = new ExpressionTreeFixture();

        // primitives

        protected static readonly int IntInput = ExpressionTreeFixture.Create<int>();

        // models

        protected static readonly Benchmark.Models.Answer AnswerInput = ExpressionTreeFixture.Create<Benchmark.Models.Answer>();

        object IntOutput;
        object AnswerOutput;


        [GlobalSetup]
        public void Setup()
        {
            // primitives
            IntOutput = Serializer.Serialize(IntInput);

            // models
            AnswerOutput = Serializer.Serialize(AnswerInput);
        }

        // Serialize

        [Benchmark] public object _PrimitiveIntSerialize() => Serializer.Serialize(IntInput);
        [Benchmark] public object AnswerSerialize() => Serializer.Serialize(AnswerInput);

        // Deserialize

        [Benchmark] public Int32 _PrimitiveIntDeserialize() => Serializer.Deserialize<Int32>(IntOutput);

        [Benchmark] public Answer AnswerDeserialize() => Serializer.Deserialize<Answer>(AnswerOutput);
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

        object IntOutput;
        object AnswerOutput;


        [GlobalSetup]
        public void Setup()
        {
            // primitives
            IntOutput = Serializer.Serialize(IntInput);

            // models
            AnswerOutput = Serializer.Serialize(AnswerInput);
        }

        // Serialize

        [Benchmark] public object _PrimitiveIntSerialize() => Serializer.Serialize(IntInput);
        [Benchmark] public object AnswerSerialize() => Serializer.Serialize(AnswerInput);

        // Deserialize

        [Benchmark] public Int32 _PrimitiveIntDeserialize() => Serializer.Deserialize<Int32>(IntOutput);

        [Benchmark] public Answer AnswerDeserialize() => Serializer.Deserialize<Answer>(AnswerOutput);
    }
}

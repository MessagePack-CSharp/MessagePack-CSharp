extern alias V3;

// The CAPCOM RE:Dox benchmark corpus (github.com/CAPCOM-TD-OSS/REDox, benchmarks/REDox.Benchmarks/Data),
// ported one-to-one: the twelve simdjson-data documents (apache_builds, canada, citm_catalog, github_events,
// gsoc-2018, instruments, marine_ik, mesh, numbers, random, twitter, update-center) and the POCO graphs RE:Dox
// binds them to - same class names, same members, same member order, declared the way RE:Dox declares them
// (object-typed members where the document only ever carries null, empty classes where it carries {}).
// What this port adds is the positional msgpack contract every measured library documents as its intended
// use: [MessagePackObject] + [Key(n)] in declaration order (the v3-aliased attributes are the ones this repo's
// generator keys on too, see AnswerBenchmark) and the matching Nerdbank.MessagePack.Key. The documents are
// loaded by System.Text.Json (the property names are the JSON names; gsoc-2018's "@context"/"@type" ride
// JsonPropertyName), so the data every library serializes is identical by construction.
//
// Generated from the RE:Dox sources by a one-shot awk pass (scratch, not kept); regenerate by hand if RE:Dox
// changes its models. The v3 source-generated twins live in V3GeneratedModels/RedoxModels.cs.

#pragma warning disable IDE1006 // naming matches the JSON documents

namespace RedoxData;

/// <summary>A RE:Dox dataset marker: names the simdjson-data document the models bind.</summary>
public interface IRedoxDataset
{
    static abstract string FileName { get; }
}

/// <summary>Nerdbank.MessagePack / PolyType shape witness for every dataset root.</summary>
[PolyType.GenerateShapeFor<ApacheBuilds.Root>]
[PolyType.GenerateShapeFor<Canada.Root>]
[PolyType.GenerateShapeFor<CitmCatalog.Root>]
[PolyType.GenerateShapeFor<GitHubEvents.GitHubEvent[]>]
[PolyType.GenerateShapeFor<Dictionary<int, Gsoc2018.GsocProject>>]
[PolyType.GenerateShapeFor<Instruments.Root>]
[PolyType.GenerateShapeFor<MarineIk.Root>]
[PolyType.GenerateShapeFor<Mesh.Root>]
[PolyType.GenerateShapeFor<double[]>]
[PolyType.GenerateShapeFor<Random.Root>]
[PolyType.GenerateShapeFor<Twitter.Root>]
[PolyType.GenerateShapeFor<UpdateCenter.Root>]
public partial class RedoxWitness;

public sealed partial class ApacheBuilds : IRedoxDataset
{
    public static string FileName => "apache_builds.json";

    [V3::MessagePack.MessagePackObject]
    public sealed partial class Root
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public AssignedLabel[]? assignedLabels { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? mode { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? nodeDescription { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? nodeName { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public int numExecutors { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? description { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public Job[]? jobs { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public LoadStatistics? overallLoad { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public View? primaryView { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public bool quietingDown { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public int slaveAgentPort { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public LoadStatistics? unlabeledLoad { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public bool useCrumbs { get; set; }

        [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
        public bool useSecurity { get; set; }

        [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
        public View[]? views { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Job
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? name { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? url { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? color { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class View
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? name { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? url { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class LoadStatistics
    {
    }

    [V3::MessagePack.MessagePackObject]
    public partial class AssignedLabel
    {
    }
}

public sealed partial class Canada : IRedoxDataset
{
    public static string FileName => "canada.json";

    [V3::MessagePack.MessagePackObject]
    public sealed partial class Root
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? type { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public Feature[]? features { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public sealed partial class Feature
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? type { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public FeatureProperties? properties { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public Geometry? geometry { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public sealed partial class FeatureProperties
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? name { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public sealed partial class Geometry
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? type { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public double[][][]? coordinates { get; set; }
    }
}

public sealed partial class CitmCatalog : IRedoxDataset
{
    public static string FileName => "citm_catalog.json";

    [V3::MessagePack.MessagePackObject]
    public sealed partial class Root
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Dictionary<int, string>? areaNames { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public Dictionary<int, string>? audienceSubCategoryNames { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public Dictionary<int, string>? blockNames { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public Dictionary<int, string>? seatCategoryNames { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public Dictionary<int, string>? subTopicNames { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public Dictionary<int, string>? subjectNames { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public Dictionary<int, string>? topicNames { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public Dictionary<int, int[]>? topicSubTopics { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public Dictionary<string, string>? venueNames { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public Dictionary<int, Event>? events { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public Performance[]? performances { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Event
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int id { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? name { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? description { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? subtitle { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? logo { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public int? subjectCode { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public int[]? topicIds { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public int[]? subTopicIds { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Performance
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int id { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int eventId { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? name { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? description { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? logo { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public Price[]? prices { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public SeatCategory[]? seatCategories { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public long start { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public string? seatMapImage { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public string? venueCode { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Price
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int amount { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int audienceSubCategoryId { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public int seatCategoryId { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class SeatCategory
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int seatCategoryId { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public Area[]? areas { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Area
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int areaId { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int[]? blockIds { get; set; }
    }
}

public sealed partial class GitHubEvents : IRedoxDataset
{
    public static string FileName => "github_events.json";

    [V3::MessagePack.MessagePackObject]
    public partial class GitHubEvent
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? type { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? created_at { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public Actor? actor { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public RepoShort? repo { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public bool @public { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public Payload? payload { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public string? id { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public Actor? org { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Actor
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? gravatar_id { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? login { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? avatar_url { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? url { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public long id { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class RepoShort
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? url { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public long id { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? name { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Payload
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Commit[]? commits { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int distinct_size { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public long push_id { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? head { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? before { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public int size { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public string? @ref { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public string? description { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public string? master_branch { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public string? ref_type { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public Repository? forkee { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public string? action { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public Issue? issue { get; set; }

        [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
        public Comment? comment { get; set; }

        [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
        public Page[]? pages { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Commit
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? url { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? message { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public bool distinct { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? sha { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public Author2? author { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Author2
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? email { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? name { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Repository
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? description { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public bool fork { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? url { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? language { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? stargazers_url { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? clone_url { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public string? tags_url { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public string? full_name { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public string? merges_url { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public int forks { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public bool @private { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public string? git_refs_url { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public string? archive_url { get; set; }

        [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
        public string? collaborators_url { get; set; }

        [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
        public User3? owner { get; set; }

        [V3::MessagePack.Key(15), Nerdbank.MessagePack.Key(15)]
        public string? languages_url { get; set; }

        [V3::MessagePack.Key(16), Nerdbank.MessagePack.Key(16)]
        public string? trees_url { get; set; }

        [V3::MessagePack.Key(17), Nerdbank.MessagePack.Key(17)]
        public string? labels_url { get; set; }

        [V3::MessagePack.Key(18), Nerdbank.MessagePack.Key(18)]
        public string? html_url { get; set; }

        [V3::MessagePack.Key(19), Nerdbank.MessagePack.Key(19)]
        public string? pushed_at { get; set; }

        [V3::MessagePack.Key(20), Nerdbank.MessagePack.Key(20)]
        public string? created_at { get; set; }

        [V3::MessagePack.Key(21), Nerdbank.MessagePack.Key(21)]
        public bool has_issues { get; set; }

        [V3::MessagePack.Key(22), Nerdbank.MessagePack.Key(22)]
        public string? forks_url { get; set; }

        [V3::MessagePack.Key(23), Nerdbank.MessagePack.Key(23)]
        public string? branches_url { get; set; }

        [V3::MessagePack.Key(24), Nerdbank.MessagePack.Key(24)]
        public string? commits_url { get; set; }

        [V3::MessagePack.Key(25), Nerdbank.MessagePack.Key(25)]
        public string? notifications_url { get; set; }

        [V3::MessagePack.Key(26), Nerdbank.MessagePack.Key(26)]
        public int open_issues { get; set; }

        [V3::MessagePack.Key(27), Nerdbank.MessagePack.Key(27)]
        public string? contents_url { get; set; }

        [V3::MessagePack.Key(28), Nerdbank.MessagePack.Key(28)]
        public string? blobs_url { get; set; }

        [V3::MessagePack.Key(29), Nerdbank.MessagePack.Key(29)]
        public string? issues_url { get; set; }

        [V3::MessagePack.Key(30), Nerdbank.MessagePack.Key(30)]
        public string? compare_url { get; set; }

        [V3::MessagePack.Key(31), Nerdbank.MessagePack.Key(31)]
        public string? issue_events_url { get; set; }

        [V3::MessagePack.Key(32), Nerdbank.MessagePack.Key(32)]
        public string? name { get; set; }

        [V3::MessagePack.Key(33), Nerdbank.MessagePack.Key(33)]
        public string? updated_at { get; set; }

        [V3::MessagePack.Key(34), Nerdbank.MessagePack.Key(34)]
        public string? statuses_url { get; set; }

        [V3::MessagePack.Key(35), Nerdbank.MessagePack.Key(35)]
        public int forks_count { get; set; }

        [V3::MessagePack.Key(36), Nerdbank.MessagePack.Key(36)]
        public string? assignees_url { get; set; }

        [V3::MessagePack.Key(37), Nerdbank.MessagePack.Key(37)]
        public string? ssh_url { get; set; }

        [V3::MessagePack.Key(38), Nerdbank.MessagePack.Key(38)]
        public bool @public { get; set; }

        [V3::MessagePack.Key(39), Nerdbank.MessagePack.Key(39)]
        public bool has_wiki { get; set; }

        [V3::MessagePack.Key(40), Nerdbank.MessagePack.Key(40)]
        public string? subscribers_url { get; set; }

        [V3::MessagePack.Key(41), Nerdbank.MessagePack.Key(41)]
        public string? mirror_url { get; set; }

        [V3::MessagePack.Key(42), Nerdbank.MessagePack.Key(42)]
        public int watchers_count { get; set; }

        [V3::MessagePack.Key(43), Nerdbank.MessagePack.Key(43)]
        public long id { get; set; }

        [V3::MessagePack.Key(44), Nerdbank.MessagePack.Key(44)]
        public bool has_downloads { get; set; }

        [V3::MessagePack.Key(45), Nerdbank.MessagePack.Key(45)]
        public string? git_commits_url { get; set; }

        [V3::MessagePack.Key(46), Nerdbank.MessagePack.Key(46)]
        public string? downloads_url { get; set; }

        [V3::MessagePack.Key(47), Nerdbank.MessagePack.Key(47)]
        public string? pulls_url { get; set; }

        [V3::MessagePack.Key(48), Nerdbank.MessagePack.Key(48)]
        public string? homepage { get; set; }

        [V3::MessagePack.Key(49), Nerdbank.MessagePack.Key(49)]
        public string? issue_comment_url { get; set; }

        [V3::MessagePack.Key(50), Nerdbank.MessagePack.Key(50)]
        public string? hooks_url { get; set; }

        [V3::MessagePack.Key(51), Nerdbank.MessagePack.Key(51)]
        public string? subscription_url { get; set; }

        [V3::MessagePack.Key(52), Nerdbank.MessagePack.Key(52)]
        public string? milestones_url { get; set; }

        [V3::MessagePack.Key(53), Nerdbank.MessagePack.Key(53)]
        public string? svn_url { get; set; }

        [V3::MessagePack.Key(54), Nerdbank.MessagePack.Key(54)]
        public string? events_url { get; set; }

        [V3::MessagePack.Key(55), Nerdbank.MessagePack.Key(55)]
        public string? git_tags_url { get; set; }

        [V3::MessagePack.Key(56), Nerdbank.MessagePack.Key(56)]
        public string? teams_url { get; set; }

        [V3::MessagePack.Key(57), Nerdbank.MessagePack.Key(57)]
        public string? comments_url { get; set; }

        [V3::MessagePack.Key(58), Nerdbank.MessagePack.Key(58)]
        public int open_issues_count { get; set; }

        [V3::MessagePack.Key(59), Nerdbank.MessagePack.Key(59)]
        public string? keys_url { get; set; }

        [V3::MessagePack.Key(60), Nerdbank.MessagePack.Key(60)]
        public string? git_url { get; set; }

        [V3::MessagePack.Key(61), Nerdbank.MessagePack.Key(61)]
        public string? contributors_url { get; set; }

        [V3::MessagePack.Key(62), Nerdbank.MessagePack.Key(62)]
        public int size { get; set; }

        [V3::MessagePack.Key(63), Nerdbank.MessagePack.Key(63)]
        public int watchers { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class User3
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? url { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? gists_url { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? gravatar_id { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? type { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? avatar_url { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? subscriptions_url { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public string? organizations_url { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public string? received_events_url { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public string? repos_url { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public string? login { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public long id { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public string? starred_url { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public string? events_url { get; set; }

        [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
        public string? followers_url { get; set; }

        [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
        public string? following_url { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Issue
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public User3? user { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? url { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public object[]? labels { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? html_url { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? labels_url { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public PullRequest? pull_request { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public string? created_at { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public string? closed_at { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public object? milestone { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public string? title { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public string? body { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public string? updated_at { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public int number { get; set; }

        [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
        public string? state { get; set; }

        [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
        public User3? assignee { get; set; }

        [V3::MessagePack.Key(15), Nerdbank.MessagePack.Key(15)]
        public long id { get; set; }

        [V3::MessagePack.Key(16), Nerdbank.MessagePack.Key(16)]
        public string? events_url { get; set; }

        [V3::MessagePack.Key(17), Nerdbank.MessagePack.Key(17)]
        public string? comments_url { get; set; }

        [V3::MessagePack.Key(18), Nerdbank.MessagePack.Key(18)]
        public int comments { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class PullRequest
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? html_url { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? patch_url { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? diff_url { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Comment
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public User3? user { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? url { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? issue_url { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? created_at { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? body { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? updated_at { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public long id { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Page
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? page_name { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? html_url { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? title { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? sha { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? summary { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? action { get; set; }
    }
}

public sealed partial class Gsoc2018 : IRedoxDataset
{
    public static string FileName => "gsoc-2018.json";

    [V3::MessagePack.MessagePackObject]
    public partial class GsocProject
    {
        [System.Text.Json.Serialization.JsonPropertyName("@context")]
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? Context { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("@type")]
        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? Type { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? Description { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("sponsor")]
        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public Sponsor? Sponsor { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("author")]
        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public Author? Author { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Sponsor
    {
        [System.Text.Json.Serialization.JsonPropertyName("@type")]
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? Type { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("disambiguatingDescription")]
        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? DisambiguatingDescription { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? Description { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("url")]
        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? Url { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("logo")]
        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? Logo { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Author
    {
        [System.Text.Json.Serialization.JsonPropertyName("@type")]
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? Type { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? Name { get; set; }
    }
}

public sealed partial class Instruments : IRedoxDataset
{
    public static string FileName => "instruments.json";

    [V3::MessagePack.MessagePackObject]
    public partial class Root
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public object? graphstate { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public Instrument[]? instruments { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? message { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? name { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public object? orderlist { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public Pattern[]? patterns { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public object? pluginstate { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public Sample[]? samples { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public int version { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Instrument
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int default_filter_cutoff { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public bool default_filter_cutoff_enabled { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public int default_filter_mode { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public int default_filter_resonance { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public bool default_filter_resonance_enabled { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public int default_pan { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public int duplicate_check_type { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public int duplicate_note_action { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public int fadeout { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public int global_volume { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public int graph_insert { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public string? legacy_filename { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public int midi_bank { get; set; }

        [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
        public int midi_channel { get; set; }

        [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
        public int midi_drum_set { get; set; }

        [V3::MessagePack.Key(15), Nerdbank.MessagePack.Key(15)]
        public int midi_program { get; set; }

        [V3::MessagePack.Key(16), Nerdbank.MessagePack.Key(16)]
        public string? name { get; set; }

        [V3::MessagePack.Key(17), Nerdbank.MessagePack.Key(17)]
        public int new_note_action { get; set; }

        [V3::MessagePack.Key(18), Nerdbank.MessagePack.Key(18)]
        public object? note_map { get; set; }

        [V3::MessagePack.Key(19), Nerdbank.MessagePack.Key(19)]
        public Envelope? panning_envelope { get; set; }

        [V3::MessagePack.Key(20), Nerdbank.MessagePack.Key(20)]
        public Envelope? pitch_envelope { get; set; }

        [V3::MessagePack.Key(21), Nerdbank.MessagePack.Key(21)]
        public int pitch_pan_center { get; set; }

        [V3::MessagePack.Key(22), Nerdbank.MessagePack.Key(22)]
        public int pitch_pan_separation { get; set; }

        [V3::MessagePack.Key(23), Nerdbank.MessagePack.Key(23)]
        public int pitch_to_tempo_lock { get; set; }

        [V3::MessagePack.Key(24), Nerdbank.MessagePack.Key(24)]
        public int random_cutoff_weight { get; set; }

        [V3::MessagePack.Key(25), Nerdbank.MessagePack.Key(25)]
        public int random_pan_weight { get; set; }

        [V3::MessagePack.Key(26), Nerdbank.MessagePack.Key(26)]
        public int random_resonance_weight { get; set; }

        [V3::MessagePack.Key(27), Nerdbank.MessagePack.Key(27)]
        public int random_volume_weight { get; set; }

        [V3::MessagePack.Key(28), Nerdbank.MessagePack.Key(28)]
        public object? sample_map { get; set; }

        [V3::MessagePack.Key(29), Nerdbank.MessagePack.Key(29)]
        public object? tuning { get; set; }

        [V3::MessagePack.Key(30), Nerdbank.MessagePack.Key(30)]
        public Envelope? volume_envelope { get; set; }

        [V3::MessagePack.Key(31), Nerdbank.MessagePack.Key(31)]
        public int volume_ramp_down { get; set; }

        [V3::MessagePack.Key(32), Nerdbank.MessagePack.Key(32)]
        public int volume_ramp_up { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Envelope
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int loop_end { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int loop_start { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public EnvelopeNode[]? nodes { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public int release_node { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public int sustain_end { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public int sustain_start { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class EnvelopeNode
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int tick { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int value { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Pattern
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public PatternRow[]? data { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? name { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public int rows { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public int rows_per_beat { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public int rows_per_measure { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class PatternRow
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int channel { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int fxcmd { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public int fxparam { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public int instr { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public int note { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public int row { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public int volcmd { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public int volval { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Sample
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int c5_samplerate { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int global_volume { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? legacy_filename { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public int length { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public int loop_end { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public int loop_start { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public string? name { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public int pan { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public int sustain_end { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public int sustain_start { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public int vibrato_depth { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public int vibrato_rate { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public int vibrato_sweep { get; set; }

        [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
        public int vibrato_type { get; set; }

        [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
        public int volume { get; set; }
    }
}

public sealed partial class MarineIk : IRedoxDataset
{
    public static string FileName => "marine_ik.json";

    [V3::MessagePack.MessagePackObject]
    public partial class Root
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public MarineImage[]? images { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public MarineGeometry[]? geometries { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public MarineTexture[]? textures { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public MarineMetadata? metadata { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public MarineMaterial[]? materials { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public MarineSceneObject? @object { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public MarineTopAnimation[]? animations { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineImage
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? url { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? uuid { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? name { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineTexture
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int[]? repeat { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int[]? wrap { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public int anisotropy { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? image { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? name { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public int mapping { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public int minFilter { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public string? uuid { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public int magFilter { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineMetadata
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? sourceFile { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? generator { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? type { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public double version { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineMaterial
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int vertexColors { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? name { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? type { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? uuid { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? blending { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? map { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public bool transparent { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public bool depthTest { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public int color { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public int shininess { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public int emissive { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public bool depthWrite { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public int specular { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineGeometry
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? type { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? uuid { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public MarineGeometryData? data { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineGeometryData
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public double[][]? uvs { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public MarineLegacyAnimationClip[]? animations { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public double[]? vertices { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public MarineGeometryDataMetadata? metadata { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? name { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public double[]? skinWeights { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public int[]? skinIndices { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public int influencesPerVertex { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public double[]? normals { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public MarineBone[]? bones { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public int[]? faces { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineGeometryDataMetadata
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int uvs { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int version { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public int faces { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? generator { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public int normals { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public int bones { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public int vertices { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineBone
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int parent { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public double[]? pos { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public double[]? rotq { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public double[]? scl { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? name { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineLegacyAnimationClip
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public MarineLegacyHierarchyNode[]? hierarchy { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public double length { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public int fps { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? name { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineLegacyHierarchyNode
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int parent { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public MarineLegacyKeyframe[]? keys { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineLegacyKeyframe
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public double[]? pos { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public double time { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public double[]? scl { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public double[]? rot { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineSceneObject
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public MarineSceneChild[]? children { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? type { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public double[]? matrix { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? uuid { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineSceneChild
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? name { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? uuid { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public double[]? matrix { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public bool visible { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? type { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? material { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public bool castShadow { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public bool receiveShadow { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public string? geometry { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class MarineTopAnimation
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int fps { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? name { get; set; }
    }
}

public sealed partial class Mesh : IRedoxDataset
{
    public static string FileName => "mesh.json";

    [V3::MessagePack.MessagePackObject]
    public sealed partial class Root
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public MeshBatch[]? batches { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public Dictionary<string, object>? morphTargets { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public double[]? positions { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public double[]? tex0 { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public uint[]? colors { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public double[][]? influences { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public double[]? normals { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public int[]? indices { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public sealed partial class MeshBatch
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int[]? indexRange { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int[]? vertexRange { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public int[]? usedBones { get; set; }
    }
}

public sealed partial class Numbers : IRedoxDataset
{
    public static string FileName => "numbers.json";
}

public sealed partial class Random : IRedoxDataset
{
    public static string FileName => "random.json";

    [V3::MessagePack.MessagePackObject]
    public sealed partial class Root
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int id { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? jsonrpc { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public int total { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public User2[]? result { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public sealed partial class User2
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int id { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? avatar { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public int age { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public bool admin { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? name { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? company { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public string? phone { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public string? email { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public string? birthDate { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public Friend[]? friends { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public string? field { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public sealed partial class Friend
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int id { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? name { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? phone { get; set; }
    }
}

public sealed partial class Twitter : IRedoxDataset
{
    public static string FileName => "twitter.json";

    [V3::MessagePack.MessagePackObject]
    public sealed partial class Root
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Status[]? statuses { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public Search_Metadata? search_metadata { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Search_Metadata
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public float completed_in { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public long max_id { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? max_id_str { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? next_results { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? query { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? refresh_url { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public int count { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public int since_id { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public string? since_id_str { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Status
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Metadata? metadata { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? created_at { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public long id { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? id_str { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? text { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? source { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public bool truncated { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public long? in_reply_to_status_id { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public string? in_reply_to_status_id_str { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public long? in_reply_to_user_id { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public string? in_reply_to_user_id_str { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public string? in_reply_to_screen_name { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public User? user { get; set; }

        [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
        public object? geo { get; set; }

        [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
        public object? coordinates { get; set; }

        [V3::MessagePack.Key(15), Nerdbank.MessagePack.Key(15)]
        public object? place { get; set; }

        [V3::MessagePack.Key(16), Nerdbank.MessagePack.Key(16)]
        public object? contributors { get; set; }

        [V3::MessagePack.Key(17), Nerdbank.MessagePack.Key(17)]
        public int retweet_count { get; set; }

        [V3::MessagePack.Key(18), Nerdbank.MessagePack.Key(18)]
        public int favorite_count { get; set; }

        [V3::MessagePack.Key(19), Nerdbank.MessagePack.Key(19)]
        public Entities1? entities { get; set; }

        [V3::MessagePack.Key(20), Nerdbank.MessagePack.Key(20)]
        public bool favorited { get; set; }

        [V3::MessagePack.Key(21), Nerdbank.MessagePack.Key(21)]
        public bool retweeted { get; set; }

        [V3::MessagePack.Key(22), Nerdbank.MessagePack.Key(22)]
        public string? lang { get; set; }

        [V3::MessagePack.Key(23), Nerdbank.MessagePack.Key(23)]
        public Retweeted_Status? retweeted_status { get; set; }

        [V3::MessagePack.Key(24), Nerdbank.MessagePack.Key(24)]
        public bool possibly_sensitive { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Metadata
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? result_type { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? iso_language_code { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class User
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public long id { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? id_str { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? name { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? screen_name { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? location { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? description { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public string? url { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public Entities? entities { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public bool @protected { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public int followers_count { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public int friends_count { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public int listed_count { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public string? created_at { get; set; }

        [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
        public int favourites_count { get; set; }

        [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
        public int? utc_offset { get; set; }

        [V3::MessagePack.Key(15), Nerdbank.MessagePack.Key(15)]
        public string? time_zone { get; set; }

        [V3::MessagePack.Key(16), Nerdbank.MessagePack.Key(16)]
        public bool geo_enabled { get; set; }

        [V3::MessagePack.Key(17), Nerdbank.MessagePack.Key(17)]
        public bool verified { get; set; }

        [V3::MessagePack.Key(18), Nerdbank.MessagePack.Key(18)]
        public int statuses_count { get; set; }

        [V3::MessagePack.Key(19), Nerdbank.MessagePack.Key(19)]
        public string? lang { get; set; }

        [V3::MessagePack.Key(20), Nerdbank.MessagePack.Key(20)]
        public bool contributors_enabled { get; set; }

        [V3::MessagePack.Key(21), Nerdbank.MessagePack.Key(21)]
        public bool is_translator { get; set; }

        [V3::MessagePack.Key(22), Nerdbank.MessagePack.Key(22)]
        public bool is_translation_enabled { get; set; }

        [V3::MessagePack.Key(23), Nerdbank.MessagePack.Key(23)]
        public string? profile_background_color { get; set; }

        [V3::MessagePack.Key(24), Nerdbank.MessagePack.Key(24)]
        public string? profile_background_image_url { get; set; }

        [V3::MessagePack.Key(25), Nerdbank.MessagePack.Key(25)]
        public string? profile_background_image_url_https { get; set; }

        [V3::MessagePack.Key(26), Nerdbank.MessagePack.Key(26)]
        public bool profile_background_tile { get; set; }

        [V3::MessagePack.Key(27), Nerdbank.MessagePack.Key(27)]
        public string? profile_image_url { get; set; }

        [V3::MessagePack.Key(28), Nerdbank.MessagePack.Key(28)]
        public string? profile_image_url_https { get; set; }

        [V3::MessagePack.Key(29), Nerdbank.MessagePack.Key(29)]
        public string? profile_banner_url { get; set; }

        [V3::MessagePack.Key(30), Nerdbank.MessagePack.Key(30)]
        public string? profile_link_color { get; set; }

        [V3::MessagePack.Key(31), Nerdbank.MessagePack.Key(31)]
        public string? profile_sidebar_border_color { get; set; }

        [V3::MessagePack.Key(32), Nerdbank.MessagePack.Key(32)]
        public string? profile_sidebar_fill_color { get; set; }

        [V3::MessagePack.Key(33), Nerdbank.MessagePack.Key(33)]
        public string? profile_text_color { get; set; }

        [V3::MessagePack.Key(34), Nerdbank.MessagePack.Key(34)]
        public bool profile_use_background_image { get; set; }

        [V3::MessagePack.Key(35), Nerdbank.MessagePack.Key(35)]
        public bool default_profile { get; set; }

        [V3::MessagePack.Key(36), Nerdbank.MessagePack.Key(36)]
        public bool default_profile_image { get; set; }

        [V3::MessagePack.Key(37), Nerdbank.MessagePack.Key(37)]
        public bool following { get; set; }

        [V3::MessagePack.Key(38), Nerdbank.MessagePack.Key(38)]
        public bool follow_request_sent { get; set; }

        [V3::MessagePack.Key(39), Nerdbank.MessagePack.Key(39)]
        public bool notifications { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Entities
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Description? description { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public Url1? url { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Description
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Url[]? urls { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Url
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? url { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? expanded_url { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? display_url { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public int[]? indices { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Url1
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Url2[]? urls { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Url2
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? url { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? expanded_url { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? display_url { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public int[]? indices { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Entities1
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Hashtag[]? hashtags { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public object[]? symbols { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public Url3[]? urls { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public User_Mentions[]? user_mentions { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public Medium[]? media { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Hashtag
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? text { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int[]? indices { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Url3
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? url { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? expanded_url { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? display_url { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public int[]? indices { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class User_Mentions
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? screen_name { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? name { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public long id { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? id_str { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public int[]? indices { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Medium
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public long id { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? id_str { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public int[]? indices { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? media_url { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? media_url_https { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? url { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public string? display_url { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public string? expanded_url { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public string? type { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public Sizes? sizes { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public long source_status_id { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public string? source_status_id_str { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Sizes
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Medium1? medium { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public Small? small { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public Thumb? thumb { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public Large? large { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Medium1
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int w { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int h { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? resize { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Small
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int w { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int h { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? resize { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Thumb
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int w { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int h { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? resize { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Large
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int w { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int h { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? resize { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Retweeted_Status
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Metadata1? metadata { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? created_at { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public long id { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? id_str { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? text { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? source { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public bool truncated { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public long? in_reply_to_status_id { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public string? in_reply_to_status_id_str { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public long? in_reply_to_user_id { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public string? in_reply_to_user_id_str { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public string? in_reply_to_screen_name { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public User1? user { get; set; }

        [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
        public object? geo { get; set; }

        [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
        public object? coordinates { get; set; }

        [V3::MessagePack.Key(15), Nerdbank.MessagePack.Key(15)]
        public object? place { get; set; }

        [V3::MessagePack.Key(16), Nerdbank.MessagePack.Key(16)]
        public object? contributors { get; set; }

        [V3::MessagePack.Key(17), Nerdbank.MessagePack.Key(17)]
        public int retweet_count { get; set; }

        [V3::MessagePack.Key(18), Nerdbank.MessagePack.Key(18)]
        public int favorite_count { get; set; }

        [V3::MessagePack.Key(19), Nerdbank.MessagePack.Key(19)]
        public Entities3? entities { get; set; }

        [V3::MessagePack.Key(20), Nerdbank.MessagePack.Key(20)]
        public bool favorited { get; set; }

        [V3::MessagePack.Key(21), Nerdbank.MessagePack.Key(21)]
        public bool retweeted { get; set; }

        [V3::MessagePack.Key(22), Nerdbank.MessagePack.Key(22)]
        public bool possibly_sensitive { get; set; }

        [V3::MessagePack.Key(23), Nerdbank.MessagePack.Key(23)]
        public string? lang { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Metadata1
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? result_type { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? iso_language_code { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class User1
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public long id { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? id_str { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? name { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? screen_name { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? location { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? description { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public string? url { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public Entities2? entities { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public bool @protected { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public int followers_count { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public int friends_count { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public int listed_count { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public string? created_at { get; set; }

        [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
        public int favourites_count { get; set; }

        [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
        public int? utc_offset { get; set; }

        [V3::MessagePack.Key(15), Nerdbank.MessagePack.Key(15)]
        public string? time_zone { get; set; }

        [V3::MessagePack.Key(16), Nerdbank.MessagePack.Key(16)]
        public bool geo_enabled { get; set; }

        [V3::MessagePack.Key(17), Nerdbank.MessagePack.Key(17)]
        public bool verified { get; set; }

        [V3::MessagePack.Key(18), Nerdbank.MessagePack.Key(18)]
        public int statuses_count { get; set; }

        [V3::MessagePack.Key(19), Nerdbank.MessagePack.Key(19)]
        public string? lang { get; set; }

        [V3::MessagePack.Key(20), Nerdbank.MessagePack.Key(20)]
        public bool contributors_enabled { get; set; }

        [V3::MessagePack.Key(21), Nerdbank.MessagePack.Key(21)]
        public bool is_translator { get; set; }

        [V3::MessagePack.Key(22), Nerdbank.MessagePack.Key(22)]
        public bool is_translation_enabled { get; set; }

        [V3::MessagePack.Key(23), Nerdbank.MessagePack.Key(23)]
        public string? profile_background_color { get; set; }

        [V3::MessagePack.Key(24), Nerdbank.MessagePack.Key(24)]
        public string? profile_background_image_url { get; set; }

        [V3::MessagePack.Key(25), Nerdbank.MessagePack.Key(25)]
        public string? profile_background_image_url_https { get; set; }

        [V3::MessagePack.Key(26), Nerdbank.MessagePack.Key(26)]
        public bool profile_background_tile { get; set; }

        [V3::MessagePack.Key(27), Nerdbank.MessagePack.Key(27)]
        public string? profile_image_url { get; set; }

        [V3::MessagePack.Key(28), Nerdbank.MessagePack.Key(28)]
        public string? profile_image_url_https { get; set; }

        [V3::MessagePack.Key(29), Nerdbank.MessagePack.Key(29)]
        public string? profile_banner_url { get; set; }

        [V3::MessagePack.Key(30), Nerdbank.MessagePack.Key(30)]
        public string? profile_link_color { get; set; }

        [V3::MessagePack.Key(31), Nerdbank.MessagePack.Key(31)]
        public string? profile_sidebar_border_color { get; set; }

        [V3::MessagePack.Key(32), Nerdbank.MessagePack.Key(32)]
        public string? profile_sidebar_fill_color { get; set; }

        [V3::MessagePack.Key(33), Nerdbank.MessagePack.Key(33)]
        public string? profile_text_color { get; set; }

        [V3::MessagePack.Key(34), Nerdbank.MessagePack.Key(34)]
        public bool profile_use_background_image { get; set; }

        [V3::MessagePack.Key(35), Nerdbank.MessagePack.Key(35)]
        public bool default_profile { get; set; }

        [V3::MessagePack.Key(36), Nerdbank.MessagePack.Key(36)]
        public bool default_profile_image { get; set; }

        [V3::MessagePack.Key(37), Nerdbank.MessagePack.Key(37)]
        public bool following { get; set; }

        [V3::MessagePack.Key(38), Nerdbank.MessagePack.Key(38)]
        public bool follow_request_sent { get; set; }

        [V3::MessagePack.Key(39), Nerdbank.MessagePack.Key(39)]
        public bool notifications { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Entities2
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Description1? description { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public Url5? url { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Description1
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Url4[]? urls { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Url4
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? url { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? expanded_url { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? display_url { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public int[]? indices { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Url5
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Url6[]? urls { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Url6
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? url { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? expanded_url { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? display_url { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public int[]? indices { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Entities3
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Hashtag1[]? hashtags { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public object[]? symbols { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public Url7[]? urls { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public User_Mentions1[]? user_mentions { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public Medium2[]? media { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Hashtag1
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? text { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int[]? indices { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Url7
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? url { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? expanded_url { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? display_url { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public int[]? indices { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class User_Mentions1
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? screen_name { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? name { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public long id { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? id_str { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public int[]? indices { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Medium2
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public long id { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? id_str { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public int[]? indices { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? media_url { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? media_url_https { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? url { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public string? display_url { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public string? expanded_url { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public string? type { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public Sizes1? sizes { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public long source_status_id { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public string? source_status_id_str { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Sizes1
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public Medium3? medium { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public Small1? small { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public Thumb1? thumb { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public Large1? large { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Medium3
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int w { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int h { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? resize { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Small1
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int w { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int h { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? resize { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Thumb1
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int w { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int h { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? resize { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Large1
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public int w { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public int h { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? resize { get; set; }
    }
}

public sealed partial class UpdateCenter : IRedoxDataset
{
    public static string FileName => "update-center.json";

    [V3::MessagePack.MessagePackObject]
    public partial class Root
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? connectionCheckUrl { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public Core? core { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? id { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public Dictionary<string, Plugin>? plugins { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public Signature? signature { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string? updateCenterVersion { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Core
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? buildDate { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? name { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? sha1 { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? url { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? version { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Plugin
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? buildDate { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public Dependency[]? dependencies { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public Developer[]? developers { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? excerpt { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? gav { get; set; }

        [V3::MessagePack.Key(5), Nerdbank.MessagePack.Key(5)]
        public string[]? labels { get; set; }

        [V3::MessagePack.Key(6), Nerdbank.MessagePack.Key(6)]
        public string? name { get; set; }

        [V3::MessagePack.Key(7), Nerdbank.MessagePack.Key(7)]
        public string? previousTimestamp { get; set; }

        [V3::MessagePack.Key(8), Nerdbank.MessagePack.Key(8)]
        public string? previousVersion { get; set; }

        [V3::MessagePack.Key(9), Nerdbank.MessagePack.Key(9)]
        public string? releaseTimestamp { get; set; }

        [V3::MessagePack.Key(10), Nerdbank.MessagePack.Key(10)]
        public string? requiredCore { get; set; }

        [V3::MessagePack.Key(11), Nerdbank.MessagePack.Key(11)]
        public string? scm { get; set; }

        [V3::MessagePack.Key(12), Nerdbank.MessagePack.Key(12)]
        public string? sha1 { get; set; }

        [V3::MessagePack.Key(13), Nerdbank.MessagePack.Key(13)]
        public string? title { get; set; }

        [V3::MessagePack.Key(14), Nerdbank.MessagePack.Key(14)]
        public string? url { get; set; }

        [V3::MessagePack.Key(15), Nerdbank.MessagePack.Key(15)]
        public string? version { get; set; }

        [V3::MessagePack.Key(16), Nerdbank.MessagePack.Key(16)]
        public string? wiki { get; set; }

        [V3::MessagePack.Key(17), Nerdbank.MessagePack.Key(17)]
        public string? compatibleSinceVersion { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Dependency
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? name { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public bool optional { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? version { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Developer
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string? developerId { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? email { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? name { get; set; }
    }

    [V3::MessagePack.MessagePackObject]
    public partial class Signature
    {
        [V3::MessagePack.Key(0), Nerdbank.MessagePack.Key(0)]
        public string[]? certificates { get; set; }

        [V3::MessagePack.Key(1), Nerdbank.MessagePack.Key(1)]
        public string? correct_digest { get; set; }

        [V3::MessagePack.Key(2), Nerdbank.MessagePack.Key(2)]
        public string? correct_signature { get; set; }

        [V3::MessagePack.Key(3), Nerdbank.MessagePack.Key(3)]
        public string? digest { get; set; }

        [V3::MessagePack.Key(4), Nerdbank.MessagePack.Key(4)]
        public string? signature { get; set; }
    }
}

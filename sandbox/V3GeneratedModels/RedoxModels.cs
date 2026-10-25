using MessagePack;

// v3 SOURCE-GENERATED twins of the RE:Dox corpus models (DisassembleBenchmark/RedoxModels.cs): same class
// names, same members, same [Key] layout, msgpack-attributed only, so the wire is byte-identical to the
// primary types and v3's real generator (MessagePackAnalyzer 3.1.8) runs over them in this v4-free satellite,
// exactly as the Answer twins above. RedoxBenchmark materializes them by deserializing the oracle payload.
// The five keyword-named members (@public, @private, @protected, @ref, @object) carry a trailing underscore
// here: v3's generator emits them unescaped (value.public), which does not parse; the wire is positional,
// so the names are irrelevant to it.

#pragma warning disable IDE1006 // naming matches the JSON documents

namespace V3GeneratedModels.Redox;

public sealed class ApacheBuilds
{
    [MessagePackObject]
    public sealed class Root
    {
        [Key(0)]
        public AssignedLabel[]? assignedLabels { get; set; }

        [Key(1)]
        public string? mode { get; set; }

        [Key(2)]
        public string? nodeDescription { get; set; }

        [Key(3)]
        public string? nodeName { get; set; }

        [Key(4)]
        public int numExecutors { get; set; }

        [Key(5)]
        public string? description { get; set; }

        [Key(6)]
        public Job[]? jobs { get; set; }

        [Key(7)]
        public LoadStatistics? overallLoad { get; set; }

        [Key(8)]
        public View? primaryView { get; set; }

        [Key(9)]
        public bool quietingDown { get; set; }

        [Key(10)]
        public int slaveAgentPort { get; set; }

        [Key(11)]
        public LoadStatistics? unlabeledLoad { get; set; }

        [Key(12)]
        public bool useCrumbs { get; set; }

        [Key(13)]
        public bool useSecurity { get; set; }

        [Key(14)]
        public View[]? views { get; set; }
    }

    [MessagePackObject]
    public class Job
    {
        [Key(0)]
        public string? name { get; set; }

        [Key(1)]
        public string? url { get; set; }

        [Key(2)]
        public string? color { get; set; }
    }

    [MessagePackObject]
    public class View
    {
        [Key(0)]
        public string? name { get; set; }

        [Key(1)]
        public string? url { get; set; }
    }

    [MessagePackObject]
    public class LoadStatistics
    {
    }

    [MessagePackObject]
    public class AssignedLabel
    {
    }
}

public sealed class Canada
{
    [MessagePackObject]
    public sealed class Root
    {
        [Key(0)]
        public string? type { get; set; }

        [Key(1)]
        public Feature[]? features { get; set; }
    }

    [MessagePackObject]
    public sealed class Feature
    {
        [Key(0)]
        public string? type { get; set; }

        [Key(1)]
        public FeatureProperties? properties { get; set; }

        [Key(2)]
        public Geometry? geometry { get; set; }
    }

    [MessagePackObject]
    public sealed class FeatureProperties
    {
        [Key(0)]
        public string? name { get; set; }
    }

    [MessagePackObject]
    public sealed class Geometry
    {
        [Key(0)]
        public string? type { get; set; }

        [Key(1)]
        public double[][][]? coordinates { get; set; }
    }
}

public sealed class CitmCatalog
{
    [MessagePackObject]
    public sealed class Root
    {
        [Key(0)]
        public Dictionary<int, string>? areaNames { get; set; }

        [Key(1)]
        public Dictionary<int, string>? audienceSubCategoryNames { get; set; }

        [Key(2)]
        public Dictionary<int, string>? blockNames { get; set; }

        [Key(3)]
        public Dictionary<int, string>? seatCategoryNames { get; set; }

        [Key(4)]
        public Dictionary<int, string>? subTopicNames { get; set; }

        [Key(5)]
        public Dictionary<int, string>? subjectNames { get; set; }

        [Key(6)]
        public Dictionary<int, string>? topicNames { get; set; }

        [Key(7)]
        public Dictionary<int, int[]>? topicSubTopics { get; set; }

        [Key(8)]
        public Dictionary<string, string>? venueNames { get; set; }

        [Key(9)]
        public Dictionary<int, Event>? events { get; set; }

        [Key(10)]
        public Performance[]? performances { get; set; }
    }

    [MessagePackObject]
    public class Event
    {
        [Key(0)]
        public int id { get; set; }

        [Key(1)]
        public string? name { get; set; }

        [Key(2)]
        public string? description { get; set; }

        [Key(3)]
        public string? subtitle { get; set; }

        [Key(4)]
        public string? logo { get; set; }

        [Key(5)]
        public int? subjectCode { get; set; }

        [Key(6)]
        public int[]? topicIds { get; set; }

        [Key(7)]
        public int[]? subTopicIds { get; set; }
    }

    [MessagePackObject]
    public class Performance
    {
        [Key(0)]
        public int id { get; set; }

        [Key(1)]
        public int eventId { get; set; }

        [Key(2)]
        public string? name { get; set; }

        [Key(3)]
        public string? description { get; set; }

        [Key(4)]
        public string? logo { get; set; }

        [Key(5)]
        public Price[]? prices { get; set; }

        [Key(6)]
        public SeatCategory[]? seatCategories { get; set; }

        [Key(7)]
        public long start { get; set; }

        [Key(8)]
        public string? seatMapImage { get; set; }

        [Key(9)]
        public string? venueCode { get; set; }
    }

    [MessagePackObject]
    public class Price
    {
        [Key(0)]
        public int amount { get; set; }

        [Key(1)]
        public int audienceSubCategoryId { get; set; }

        [Key(2)]
        public int seatCategoryId { get; set; }
    }

    [MessagePackObject]
    public class SeatCategory
    {
        [Key(0)]
        public int seatCategoryId { get; set; }

        [Key(1)]
        public Area[]? areas { get; set; }
    }

    [MessagePackObject]
    public class Area
    {
        [Key(0)]
        public int areaId { get; set; }

        [Key(1)]
        public int[]? blockIds { get; set; }
    }
}

public sealed class GitHubEvents
{
    [MessagePackObject]
    public class GitHubEvent
    {
        [Key(0)]
        public string? type { get; set; }

        [Key(1)]
        public string? created_at { get; set; }

        [Key(2)]
        public Actor? actor { get; set; }

        [Key(3)]
        public RepoShort? repo { get; set; }

        [Key(4)]
        public bool public_ { get; set; }

        [Key(5)]
        public Payload? payload { get; set; }

        [Key(6)]
        public string? id { get; set; }

        [Key(7)]
        public Actor? org { get; set; }
    }

    [MessagePackObject]
    public class Actor
    {
        [Key(0)]
        public string? gravatar_id { get; set; }

        [Key(1)]
        public string? login { get; set; }

        [Key(2)]
        public string? avatar_url { get; set; }

        [Key(3)]
        public string? url { get; set; }

        [Key(4)]
        public long id { get; set; }
    }

    [MessagePackObject]
    public class RepoShort
    {
        [Key(0)]
        public string? url { get; set; }

        [Key(1)]
        public long id { get; set; }

        [Key(2)]
        public string? name { get; set; }
    }

    [MessagePackObject]
    public class Payload
    {
        [Key(0)]
        public Commit[]? commits { get; set; }

        [Key(1)]
        public int distinct_size { get; set; }

        [Key(2)]
        public long push_id { get; set; }

        [Key(3)]
        public string? head { get; set; }

        [Key(4)]
        public string? before { get; set; }

        [Key(5)]
        public int size { get; set; }

        [Key(6)]
        public string? ref_ { get; set; }

        [Key(7)]
        public string? description { get; set; }

        [Key(8)]
        public string? master_branch { get; set; }

        [Key(9)]
        public string? ref_type { get; set; }

        [Key(10)]
        public Repository? forkee { get; set; }

        [Key(11)]
        public string? action { get; set; }

        [Key(12)]
        public Issue? issue { get; set; }

        [Key(13)]
        public Comment? comment { get; set; }

        [Key(14)]
        public Page[]? pages { get; set; }
    }

    [MessagePackObject]
    public class Commit
    {
        [Key(0)]
        public string? url { get; set; }

        [Key(1)]
        public string? message { get; set; }

        [Key(2)]
        public bool distinct { get; set; }

        [Key(3)]
        public string? sha { get; set; }

        [Key(4)]
        public Author2? author { get; set; }
    }

    [MessagePackObject]
    public class Author2
    {
        [Key(0)]
        public string? email { get; set; }

        [Key(1)]
        public string? name { get; set; }
    }

    [MessagePackObject]
    public class Repository
    {
        [Key(0)]
        public string? description { get; set; }

        [Key(1)]
        public bool fork { get; set; }

        [Key(2)]
        public string? url { get; set; }

        [Key(3)]
        public string? language { get; set; }

        [Key(4)]
        public string? stargazers_url { get; set; }

        [Key(5)]
        public string? clone_url { get; set; }

        [Key(6)]
        public string? tags_url { get; set; }

        [Key(7)]
        public string? full_name { get; set; }

        [Key(8)]
        public string? merges_url { get; set; }

        [Key(9)]
        public int forks { get; set; }

        [Key(10)]
        public bool private_ { get; set; }

        [Key(11)]
        public string? git_refs_url { get; set; }

        [Key(12)]
        public string? archive_url { get; set; }

        [Key(13)]
        public string? collaborators_url { get; set; }

        [Key(14)]
        public User3? owner { get; set; }

        [Key(15)]
        public string? languages_url { get; set; }

        [Key(16)]
        public string? trees_url { get; set; }

        [Key(17)]
        public string? labels_url { get; set; }

        [Key(18)]
        public string? html_url { get; set; }

        [Key(19)]
        public string? pushed_at { get; set; }

        [Key(20)]
        public string? created_at { get; set; }

        [Key(21)]
        public bool has_issues { get; set; }

        [Key(22)]
        public string? forks_url { get; set; }

        [Key(23)]
        public string? branches_url { get; set; }

        [Key(24)]
        public string? commits_url { get; set; }

        [Key(25)]
        public string? notifications_url { get; set; }

        [Key(26)]
        public int open_issues { get; set; }

        [Key(27)]
        public string? contents_url { get; set; }

        [Key(28)]
        public string? blobs_url { get; set; }

        [Key(29)]
        public string? issues_url { get; set; }

        [Key(30)]
        public string? compare_url { get; set; }

        [Key(31)]
        public string? issue_events_url { get; set; }

        [Key(32)]
        public string? name { get; set; }

        [Key(33)]
        public string? updated_at { get; set; }

        [Key(34)]
        public string? statuses_url { get; set; }

        [Key(35)]
        public int forks_count { get; set; }

        [Key(36)]
        public string? assignees_url { get; set; }

        [Key(37)]
        public string? ssh_url { get; set; }

        [Key(38)]
        public bool public_ { get; set; }

        [Key(39)]
        public bool has_wiki { get; set; }

        [Key(40)]
        public string? subscribers_url { get; set; }

        [Key(41)]
        public string? mirror_url { get; set; }

        [Key(42)]
        public int watchers_count { get; set; }

        [Key(43)]
        public long id { get; set; }

        [Key(44)]
        public bool has_downloads { get; set; }

        [Key(45)]
        public string? git_commits_url { get; set; }

        [Key(46)]
        public string? downloads_url { get; set; }

        [Key(47)]
        public string? pulls_url { get; set; }

        [Key(48)]
        public string? homepage { get; set; }

        [Key(49)]
        public string? issue_comment_url { get; set; }

        [Key(50)]
        public string? hooks_url { get; set; }

        [Key(51)]
        public string? subscription_url { get; set; }

        [Key(52)]
        public string? milestones_url { get; set; }

        [Key(53)]
        public string? svn_url { get; set; }

        [Key(54)]
        public string? events_url { get; set; }

        [Key(55)]
        public string? git_tags_url { get; set; }

        [Key(56)]
        public string? teams_url { get; set; }

        [Key(57)]
        public string? comments_url { get; set; }

        [Key(58)]
        public int open_issues_count { get; set; }

        [Key(59)]
        public string? keys_url { get; set; }

        [Key(60)]
        public string? git_url { get; set; }

        [Key(61)]
        public string? contributors_url { get; set; }

        [Key(62)]
        public int size { get; set; }

        [Key(63)]
        public int watchers { get; set; }
    }

    [MessagePackObject]
    public class User3
    {
        [Key(0)]
        public string? url { get; set; }

        [Key(1)]
        public string? gists_url { get; set; }

        [Key(2)]
        public string? gravatar_id { get; set; }

        [Key(3)]
        public string? type { get; set; }

        [Key(4)]
        public string? avatar_url { get; set; }

        [Key(5)]
        public string? subscriptions_url { get; set; }

        [Key(6)]
        public string? organizations_url { get; set; }

        [Key(7)]
        public string? received_events_url { get; set; }

        [Key(8)]
        public string? repos_url { get; set; }

        [Key(9)]
        public string? login { get; set; }

        [Key(10)]
        public long id { get; set; }

        [Key(11)]
        public string? starred_url { get; set; }

        [Key(12)]
        public string? events_url { get; set; }

        [Key(13)]
        public string? followers_url { get; set; }

        [Key(14)]
        public string? following_url { get; set; }
    }

    [MessagePackObject]
    public class Issue
    {
        [Key(0)]
        public User3? user { get; set; }

        [Key(1)]
        public string? url { get; set; }

        [Key(2)]
        public object[]? labels { get; set; }

        [Key(3)]
        public string? html_url { get; set; }

        [Key(4)]
        public string? labels_url { get; set; }

        [Key(5)]
        public PullRequest? pull_request { get; set; }

        [Key(6)]
        public string? created_at { get; set; }

        [Key(7)]
        public string? closed_at { get; set; }

        [Key(8)]
        public object? milestone { get; set; }

        [Key(9)]
        public string? title { get; set; }

        [Key(10)]
        public string? body { get; set; }

        [Key(11)]
        public string? updated_at { get; set; }

        [Key(12)]
        public int number { get; set; }

        [Key(13)]
        public string? state { get; set; }

        [Key(14)]
        public User3? assignee { get; set; }

        [Key(15)]
        public long id { get; set; }

        [Key(16)]
        public string? events_url { get; set; }

        [Key(17)]
        public string? comments_url { get; set; }

        [Key(18)]
        public int comments { get; set; }
    }

    [MessagePackObject]
    public class PullRequest
    {
        [Key(0)]
        public string? html_url { get; set; }

        [Key(1)]
        public string? patch_url { get; set; }

        [Key(2)]
        public string? diff_url { get; set; }
    }

    [MessagePackObject]
    public class Comment
    {
        [Key(0)]
        public User3? user { get; set; }

        [Key(1)]
        public string? url { get; set; }

        [Key(2)]
        public string? issue_url { get; set; }

        [Key(3)]
        public string? created_at { get; set; }

        [Key(4)]
        public string? body { get; set; }

        [Key(5)]
        public string? updated_at { get; set; }

        [Key(6)]
        public long id { get; set; }
    }

    [MessagePackObject]
    public class Page
    {
        [Key(0)]
        public string? page_name { get; set; }

        [Key(1)]
        public string? html_url { get; set; }

        [Key(2)]
        public string? title { get; set; }

        [Key(3)]
        public string? sha { get; set; }

        [Key(4)]
        public string? summary { get; set; }

        [Key(5)]
        public string? action { get; set; }
    }
}

public sealed class Gsoc2018
{
    [MessagePackObject]
    public class GsocProject
    {
        [System.Text.Json.Serialization.JsonPropertyName("@context")]
        [Key(0)]
        public string? Context { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("@type")]
        [Key(1)]
        public string? Type { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [Key(2)]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        [Key(3)]
        public string? Description { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("sponsor")]
        [Key(4)]
        public Sponsor? Sponsor { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("author")]
        [Key(5)]
        public Author? Author { get; set; }
    }

    [MessagePackObject]
    public class Sponsor
    {
        [System.Text.Json.Serialization.JsonPropertyName("@type")]
        [Key(0)]
        public string? Type { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [Key(1)]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("disambiguatingDescription")]
        [Key(2)]
        public string? DisambiguatingDescription { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        [Key(3)]
        public string? Description { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("url")]
        [Key(4)]
        public string? Url { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("logo")]
        [Key(5)]
        public string? Logo { get; set; }
    }

    [MessagePackObject]
    public class Author
    {
        [System.Text.Json.Serialization.JsonPropertyName("@type")]
        [Key(0)]
        public string? Type { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        [Key(1)]
        public string? Name { get; set; }
    }
}

public sealed class Instruments
{
    [MessagePackObject]
    public class Root
    {
        [Key(0)]
        public object? graphstate { get; set; }

        [Key(1)]
        public Instrument[]? instruments { get; set; }

        [Key(2)]
        public string? message { get; set; }

        [Key(3)]
        public string? name { get; set; }

        [Key(4)]
        public object? orderlist { get; set; }

        [Key(5)]
        public Pattern[]? patterns { get; set; }

        [Key(6)]
        public object? pluginstate { get; set; }

        [Key(7)]
        public Sample[]? samples { get; set; }

        [Key(8)]
        public int version { get; set; }
    }

    [MessagePackObject]
    public class Instrument
    {
        [Key(0)]
        public int default_filter_cutoff { get; set; }

        [Key(1)]
        public bool default_filter_cutoff_enabled { get; set; }

        [Key(2)]
        public int default_filter_mode { get; set; }

        [Key(3)]
        public int default_filter_resonance { get; set; }

        [Key(4)]
        public bool default_filter_resonance_enabled { get; set; }

        [Key(5)]
        public int default_pan { get; set; }

        [Key(6)]
        public int duplicate_check_type { get; set; }

        [Key(7)]
        public int duplicate_note_action { get; set; }

        [Key(8)]
        public int fadeout { get; set; }

        [Key(9)]
        public int global_volume { get; set; }

        [Key(10)]
        public int graph_insert { get; set; }

        [Key(11)]
        public string? legacy_filename { get; set; }

        [Key(12)]
        public int midi_bank { get; set; }

        [Key(13)]
        public int midi_channel { get; set; }

        [Key(14)]
        public int midi_drum_set { get; set; }

        [Key(15)]
        public int midi_program { get; set; }

        [Key(16)]
        public string? name { get; set; }

        [Key(17)]
        public int new_note_action { get; set; }

        [Key(18)]
        public object? note_map { get; set; }

        [Key(19)]
        public Envelope? panning_envelope { get; set; }

        [Key(20)]
        public Envelope? pitch_envelope { get; set; }

        [Key(21)]
        public int pitch_pan_center { get; set; }

        [Key(22)]
        public int pitch_pan_separation { get; set; }

        [Key(23)]
        public int pitch_to_tempo_lock { get; set; }

        [Key(24)]
        public int random_cutoff_weight { get; set; }

        [Key(25)]
        public int random_pan_weight { get; set; }

        [Key(26)]
        public int random_resonance_weight { get; set; }

        [Key(27)]
        public int random_volume_weight { get; set; }

        [Key(28)]
        public object? sample_map { get; set; }

        [Key(29)]
        public object? tuning { get; set; }

        [Key(30)]
        public Envelope? volume_envelope { get; set; }

        [Key(31)]
        public int volume_ramp_down { get; set; }

        [Key(32)]
        public int volume_ramp_up { get; set; }
    }

    [MessagePackObject]
    public class Envelope
    {
        [Key(0)]
        public int loop_end { get; set; }

        [Key(1)]
        public int loop_start { get; set; }

        [Key(2)]
        public EnvelopeNode[]? nodes { get; set; }

        [Key(3)]
        public int release_node { get; set; }

        [Key(4)]
        public int sustain_end { get; set; }

        [Key(5)]
        public int sustain_start { get; set; }
    }

    [MessagePackObject]
    public class EnvelopeNode
    {
        [Key(0)]
        public int tick { get; set; }

        [Key(1)]
        public int value { get; set; }
    }

    [MessagePackObject]
    public class Pattern
    {
        [Key(0)]
        public PatternRow[]? data { get; set; }

        [Key(1)]
        public string? name { get; set; }

        [Key(2)]
        public int rows { get; set; }

        [Key(3)]
        public int rows_per_beat { get; set; }

        [Key(4)]
        public int rows_per_measure { get; set; }
    }

    [MessagePackObject]
    public class PatternRow
    {
        [Key(0)]
        public int channel { get; set; }

        [Key(1)]
        public int fxcmd { get; set; }

        [Key(2)]
        public int fxparam { get; set; }

        [Key(3)]
        public int instr { get; set; }

        [Key(4)]
        public int note { get; set; }

        [Key(5)]
        public int row { get; set; }

        [Key(6)]
        public int volcmd { get; set; }

        [Key(7)]
        public int volval { get; set; }
    }

    [MessagePackObject]
    public class Sample
    {
        [Key(0)]
        public int c5_samplerate { get; set; }

        [Key(1)]
        public int global_volume { get; set; }

        [Key(2)]
        public string? legacy_filename { get; set; }

        [Key(3)]
        public int length { get; set; }

        [Key(4)]
        public int loop_end { get; set; }

        [Key(5)]
        public int loop_start { get; set; }

        [Key(6)]
        public string? name { get; set; }

        [Key(7)]
        public int pan { get; set; }

        [Key(8)]
        public int sustain_end { get; set; }

        [Key(9)]
        public int sustain_start { get; set; }

        [Key(10)]
        public int vibrato_depth { get; set; }

        [Key(11)]
        public int vibrato_rate { get; set; }

        [Key(12)]
        public int vibrato_sweep { get; set; }

        [Key(13)]
        public int vibrato_type { get; set; }

        [Key(14)]
        public int volume { get; set; }
    }
}

public sealed class MarineIk
{
    [MessagePackObject]
    public class Root
    {
        [Key(0)]
        public MarineImage[]? images { get; set; }

        [Key(1)]
        public MarineGeometry[]? geometries { get; set; }

        [Key(2)]
        public MarineTexture[]? textures { get; set; }

        [Key(3)]
        public MarineMetadata? metadata { get; set; }

        [Key(4)]
        public MarineMaterial[]? materials { get; set; }

        [Key(5)]
        public MarineSceneObject? object_ { get; set; }

        [Key(6)]
        public MarineTopAnimation[]? animations { get; set; }
    }

    [MessagePackObject]
    public class MarineImage
    {
        [Key(0)]
        public string? url { get; set; }

        [Key(1)]
        public string? uuid { get; set; }

        [Key(2)]
        public string? name { get; set; }
    }

    [MessagePackObject]
    public class MarineTexture
    {
        [Key(0)]
        public int[]? repeat { get; set; }

        [Key(1)]
        public int[]? wrap { get; set; }

        [Key(2)]
        public int anisotropy { get; set; }

        [Key(3)]
        public string? image { get; set; }

        [Key(4)]
        public string? name { get; set; }

        [Key(5)]
        public int mapping { get; set; }

        [Key(6)]
        public int minFilter { get; set; }

        [Key(7)]
        public string? uuid { get; set; }

        [Key(8)]
        public int magFilter { get; set; }
    }

    [MessagePackObject]
    public class MarineMetadata
    {
        [Key(0)]
        public string? sourceFile { get; set; }

        [Key(1)]
        public string? generator { get; set; }

        [Key(2)]
        public string? type { get; set; }

        [Key(3)]
        public double version { get; set; }
    }

    [MessagePackObject]
    public class MarineMaterial
    {
        [Key(0)]
        public int vertexColors { get; set; }

        [Key(1)]
        public string? name { get; set; }

        [Key(2)]
        public string? type { get; set; }

        [Key(3)]
        public string? uuid { get; set; }

        [Key(4)]
        public string? blending { get; set; }

        [Key(5)]
        public string? map { get; set; }

        [Key(6)]
        public bool transparent { get; set; }

        [Key(7)]
        public bool depthTest { get; set; }

        [Key(8)]
        public int color { get; set; }

        [Key(9)]
        public int shininess { get; set; }

        [Key(10)]
        public int emissive { get; set; }

        [Key(11)]
        public bool depthWrite { get; set; }

        [Key(12)]
        public int specular { get; set; }
    }

    [MessagePackObject]
    public class MarineGeometry
    {
        [Key(0)]
        public string? type { get; set; }

        [Key(1)]
        public string? uuid { get; set; }

        [Key(2)]
        public MarineGeometryData? data { get; set; }
    }

    [MessagePackObject]
    public class MarineGeometryData
    {
        [Key(0)]
        public double[][]? uvs { get; set; }

        [Key(1)]
        public MarineLegacyAnimationClip[]? animations { get; set; }

        [Key(2)]
        public double[]? vertices { get; set; }

        [Key(3)]
        public MarineGeometryDataMetadata? metadata { get; set; }

        [Key(4)]
        public string? name { get; set; }

        [Key(5)]
        public double[]? skinWeights { get; set; }

        [Key(6)]
        public int[]? skinIndices { get; set; }

        [Key(7)]
        public int influencesPerVertex { get; set; }

        [Key(8)]
        public double[]? normals { get; set; }

        [Key(9)]
        public MarineBone[]? bones { get; set; }

        [Key(10)]
        public int[]? faces { get; set; }
    }

    [MessagePackObject]
    public class MarineGeometryDataMetadata
    {
        [Key(0)]
        public int uvs { get; set; }

        [Key(1)]
        public int version { get; set; }

        [Key(2)]
        public int faces { get; set; }

        [Key(3)]
        public string? generator { get; set; }

        [Key(4)]
        public int normals { get; set; }

        [Key(5)]
        public int bones { get; set; }

        [Key(6)]
        public int vertices { get; set; }
    }

    [MessagePackObject]
    public class MarineBone
    {
        [Key(0)]
        public int parent { get; set; }

        [Key(1)]
        public double[]? pos { get; set; }

        [Key(2)]
        public double[]? rotq { get; set; }

        [Key(3)]
        public double[]? scl { get; set; }

        [Key(4)]
        public string? name { get; set; }
    }

    [MessagePackObject]
    public class MarineLegacyAnimationClip
    {
        [Key(0)]
        public MarineLegacyHierarchyNode[]? hierarchy { get; set; }

        [Key(1)]
        public double length { get; set; }

        [Key(2)]
        public int fps { get; set; }

        [Key(3)]
        public string? name { get; set; }
    }

    [MessagePackObject]
    public class MarineLegacyHierarchyNode
    {
        [Key(0)]
        public int parent { get; set; }

        [Key(1)]
        public MarineLegacyKeyframe[]? keys { get; set; }
    }

    [MessagePackObject]
    public class MarineLegacyKeyframe
    {
        [Key(0)]
        public double[]? pos { get; set; }

        [Key(1)]
        public double time { get; set; }

        [Key(2)]
        public double[]? scl { get; set; }

        [Key(3)]
        public double[]? rot { get; set; }
    }

    [MessagePackObject]
    public class MarineSceneObject
    {
        [Key(0)]
        public MarineSceneChild[]? children { get; set; }

        [Key(1)]
        public string? type { get; set; }

        [Key(2)]
        public double[]? matrix { get; set; }

        [Key(3)]
        public string? uuid { get; set; }
    }

    [MessagePackObject]
    public class MarineSceneChild
    {
        [Key(0)]
        public string? name { get; set; }

        [Key(1)]
        public string? uuid { get; set; }

        [Key(2)]
        public double[]? matrix { get; set; }

        [Key(3)]
        public bool visible { get; set; }

        [Key(4)]
        public string? type { get; set; }

        [Key(5)]
        public string? material { get; set; }

        [Key(6)]
        public bool castShadow { get; set; }

        [Key(7)]
        public bool receiveShadow { get; set; }

        [Key(8)]
        public string? geometry { get; set; }
    }

    [MessagePackObject]
    public class MarineTopAnimation
    {
        [Key(0)]
        public int fps { get; set; }

        [Key(1)]
        public string? name { get; set; }
    }
}

public sealed class Mesh
{
    [MessagePackObject]
    public sealed class Root
    {
        [Key(0)]
        public MeshBatch[]? batches { get; set; }

        [Key(1)]
        public Dictionary<string, object>? morphTargets { get; set; }

        [Key(2)]
        public double[]? positions { get; set; }

        [Key(3)]
        public double[]? tex0 { get; set; }

        [Key(4)]
        public uint[]? colors { get; set; }

        [Key(5)]
        public double[][]? influences { get; set; }

        [Key(6)]
        public double[]? normals { get; set; }

        [Key(7)]
        public int[]? indices { get; set; }
    }

    [MessagePackObject]
    public sealed class MeshBatch
    {
        [Key(0)]
        public int[]? indexRange { get; set; }

        [Key(1)]
        public int[]? vertexRange { get; set; }

        [Key(2)]
        public int[]? usedBones { get; set; }
    }
}

public sealed class Numbers
{
}

public sealed class Random
{
    [MessagePackObject]
    public sealed class Root
    {
        [Key(0)]
        public int id { get; set; }

        [Key(1)]
        public string? jsonrpc { get; set; }

        [Key(2)]
        public int total { get; set; }

        [Key(3)]
        public User2[]? result { get; set; }
    }

    [MessagePackObject]
    public sealed class User2
    {
        [Key(0)]
        public int id { get; set; }

        [Key(1)]
        public string? avatar { get; set; }

        [Key(2)]
        public int age { get; set; }

        [Key(3)]
        public bool admin { get; set; }

        [Key(4)]
        public string? name { get; set; }

        [Key(5)]
        public string? company { get; set; }

        [Key(6)]
        public string? phone { get; set; }

        [Key(7)]
        public string? email { get; set; }

        [Key(8)]
        public string? birthDate { get; set; }

        [Key(9)]
        public Friend[]? friends { get; set; }

        [Key(10)]
        public string? field { get; set; }
    }

    [MessagePackObject]
    public sealed class Friend
    {
        [Key(0)]
        public int id { get; set; }

        [Key(1)]
        public string? name { get; set; }

        [Key(2)]
        public string? phone { get; set; }
    }
}

public sealed class Twitter
{
    [MessagePackObject]
    public sealed class Root
    {
        [Key(0)]
        public Status[]? statuses { get; set; }

        [Key(1)]
        public Search_Metadata? search_metadata { get; set; }
    }

    [MessagePackObject]
    public class Search_Metadata
    {
        [Key(0)]
        public float completed_in { get; set; }

        [Key(1)]
        public long max_id { get; set; }

        [Key(2)]
        public string? max_id_str { get; set; }

        [Key(3)]
        public string? next_results { get; set; }

        [Key(4)]
        public string? query { get; set; }

        [Key(5)]
        public string? refresh_url { get; set; }

        [Key(6)]
        public int count { get; set; }

        [Key(7)]
        public int since_id { get; set; }

        [Key(8)]
        public string? since_id_str { get; set; }
    }

    [MessagePackObject]
    public class Status
    {
        [Key(0)]
        public Metadata? metadata { get; set; }

        [Key(1)]
        public string? created_at { get; set; }

        [Key(2)]
        public long id { get; set; }

        [Key(3)]
        public string? id_str { get; set; }

        [Key(4)]
        public string? text { get; set; }

        [Key(5)]
        public string? source { get; set; }

        [Key(6)]
        public bool truncated { get; set; }

        [Key(7)]
        public long? in_reply_to_status_id { get; set; }

        [Key(8)]
        public string? in_reply_to_status_id_str { get; set; }

        [Key(9)]
        public long? in_reply_to_user_id { get; set; }

        [Key(10)]
        public string? in_reply_to_user_id_str { get; set; }

        [Key(11)]
        public string? in_reply_to_screen_name { get; set; }

        [Key(12)]
        public User? user { get; set; }

        [Key(13)]
        public object? geo { get; set; }

        [Key(14)]
        public object? coordinates { get; set; }

        [Key(15)]
        public object? place { get; set; }

        [Key(16)]
        public object? contributors { get; set; }

        [Key(17)]
        public int retweet_count { get; set; }

        [Key(18)]
        public int favorite_count { get; set; }

        [Key(19)]
        public Entities1? entities { get; set; }

        [Key(20)]
        public bool favorited { get; set; }

        [Key(21)]
        public bool retweeted { get; set; }

        [Key(22)]
        public string? lang { get; set; }

        [Key(23)]
        public Retweeted_Status? retweeted_status { get; set; }

        [Key(24)]
        public bool possibly_sensitive { get; set; }
    }

    [MessagePackObject]
    public class Metadata
    {
        [Key(0)]
        public string? result_type { get; set; }

        [Key(1)]
        public string? iso_language_code { get; set; }
    }

    [MessagePackObject]
    public class User
    {
        [Key(0)]
        public long id { get; set; }

        [Key(1)]
        public string? id_str { get; set; }

        [Key(2)]
        public string? name { get; set; }

        [Key(3)]
        public string? screen_name { get; set; }

        [Key(4)]
        public string? location { get; set; }

        [Key(5)]
        public string? description { get; set; }

        [Key(6)]
        public string? url { get; set; }

        [Key(7)]
        public Entities? entities { get; set; }

        [Key(8)]
        public bool protected_ { get; set; }

        [Key(9)]
        public int followers_count { get; set; }

        [Key(10)]
        public int friends_count { get; set; }

        [Key(11)]
        public int listed_count { get; set; }

        [Key(12)]
        public string? created_at { get; set; }

        [Key(13)]
        public int favourites_count { get; set; }

        [Key(14)]
        public int? utc_offset { get; set; }

        [Key(15)]
        public string? time_zone { get; set; }

        [Key(16)]
        public bool geo_enabled { get; set; }

        [Key(17)]
        public bool verified { get; set; }

        [Key(18)]
        public int statuses_count { get; set; }

        [Key(19)]
        public string? lang { get; set; }

        [Key(20)]
        public bool contributors_enabled { get; set; }

        [Key(21)]
        public bool is_translator { get; set; }

        [Key(22)]
        public bool is_translation_enabled { get; set; }

        [Key(23)]
        public string? profile_background_color { get; set; }

        [Key(24)]
        public string? profile_background_image_url { get; set; }

        [Key(25)]
        public string? profile_background_image_url_https { get; set; }

        [Key(26)]
        public bool profile_background_tile { get; set; }

        [Key(27)]
        public string? profile_image_url { get; set; }

        [Key(28)]
        public string? profile_image_url_https { get; set; }

        [Key(29)]
        public string? profile_banner_url { get; set; }

        [Key(30)]
        public string? profile_link_color { get; set; }

        [Key(31)]
        public string? profile_sidebar_border_color { get; set; }

        [Key(32)]
        public string? profile_sidebar_fill_color { get; set; }

        [Key(33)]
        public string? profile_text_color { get; set; }

        [Key(34)]
        public bool profile_use_background_image { get; set; }

        [Key(35)]
        public bool default_profile { get; set; }

        [Key(36)]
        public bool default_profile_image { get; set; }

        [Key(37)]
        public bool following { get; set; }

        [Key(38)]
        public bool follow_request_sent { get; set; }

        [Key(39)]
        public bool notifications { get; set; }
    }

    [MessagePackObject]
    public class Entities
    {
        [Key(0)]
        public Description? description { get; set; }

        [Key(1)]
        public Url1? url { get; set; }
    }

    [MessagePackObject]
    public class Description
    {
        [Key(0)]
        public Url[]? urls { get; set; }
    }

    [MessagePackObject]
    public class Url
    {
        [Key(0)]
        public string? url { get; set; }

        [Key(1)]
        public string? expanded_url { get; set; }

        [Key(2)]
        public string? display_url { get; set; }

        [Key(3)]
        public int[]? indices { get; set; }
    }

    [MessagePackObject]
    public class Url1
    {
        [Key(0)]
        public Url2[]? urls { get; set; }
    }

    [MessagePackObject]
    public class Url2
    {
        [Key(0)]
        public string? url { get; set; }

        [Key(1)]
        public string? expanded_url { get; set; }

        [Key(2)]
        public string? display_url { get; set; }

        [Key(3)]
        public int[]? indices { get; set; }
    }

    [MessagePackObject]
    public class Entities1
    {
        [Key(0)]
        public Hashtag[]? hashtags { get; set; }

        [Key(1)]
        public object[]? symbols { get; set; }

        [Key(2)]
        public Url3[]? urls { get; set; }

        [Key(3)]
        public User_Mentions[]? user_mentions { get; set; }

        [Key(4)]
        public Medium[]? media { get; set; }
    }

    [MessagePackObject]
    public class Hashtag
    {
        [Key(0)]
        public string? text { get; set; }

        [Key(1)]
        public int[]? indices { get; set; }
    }

    [MessagePackObject]
    public class Url3
    {
        [Key(0)]
        public string? url { get; set; }

        [Key(1)]
        public string? expanded_url { get; set; }

        [Key(2)]
        public string? display_url { get; set; }

        [Key(3)]
        public int[]? indices { get; set; }
    }

    [MessagePackObject]
    public class User_Mentions
    {
        [Key(0)]
        public string? screen_name { get; set; }

        [Key(1)]
        public string? name { get; set; }

        [Key(2)]
        public long id { get; set; }

        [Key(3)]
        public string? id_str { get; set; }

        [Key(4)]
        public int[]? indices { get; set; }
    }

    [MessagePackObject]
    public class Medium
    {
        [Key(0)]
        public long id { get; set; }

        [Key(1)]
        public string? id_str { get; set; }

        [Key(2)]
        public int[]? indices { get; set; }

        [Key(3)]
        public string? media_url { get; set; }

        [Key(4)]
        public string? media_url_https { get; set; }

        [Key(5)]
        public string? url { get; set; }

        [Key(6)]
        public string? display_url { get; set; }

        [Key(7)]
        public string? expanded_url { get; set; }

        [Key(8)]
        public string? type { get; set; }

        [Key(9)]
        public Sizes? sizes { get; set; }

        [Key(10)]
        public long source_status_id { get; set; }

        [Key(11)]
        public string? source_status_id_str { get; set; }
    }

    [MessagePackObject]
    public class Sizes
    {
        [Key(0)]
        public Medium1? medium { get; set; }

        [Key(1)]
        public Small? small { get; set; }

        [Key(2)]
        public Thumb? thumb { get; set; }

        [Key(3)]
        public Large? large { get; set; }
    }

    [MessagePackObject]
    public class Medium1
    {
        [Key(0)]
        public int w { get; set; }

        [Key(1)]
        public int h { get; set; }

        [Key(2)]
        public string? resize { get; set; }
    }

    [MessagePackObject]
    public class Small
    {
        [Key(0)]
        public int w { get; set; }

        [Key(1)]
        public int h { get; set; }

        [Key(2)]
        public string? resize { get; set; }
    }

    [MessagePackObject]
    public class Thumb
    {
        [Key(0)]
        public int w { get; set; }

        [Key(1)]
        public int h { get; set; }

        [Key(2)]
        public string? resize { get; set; }
    }

    [MessagePackObject]
    public class Large
    {
        [Key(0)]
        public int w { get; set; }

        [Key(1)]
        public int h { get; set; }

        [Key(2)]
        public string? resize { get; set; }
    }

    [MessagePackObject]
    public class Retweeted_Status
    {
        [Key(0)]
        public Metadata1? metadata { get; set; }

        [Key(1)]
        public string? created_at { get; set; }

        [Key(2)]
        public long id { get; set; }

        [Key(3)]
        public string? id_str { get; set; }

        [Key(4)]
        public string? text { get; set; }

        [Key(5)]
        public string? source { get; set; }

        [Key(6)]
        public bool truncated { get; set; }

        [Key(7)]
        public long? in_reply_to_status_id { get; set; }

        [Key(8)]
        public string? in_reply_to_status_id_str { get; set; }

        [Key(9)]
        public long? in_reply_to_user_id { get; set; }

        [Key(10)]
        public string? in_reply_to_user_id_str { get; set; }

        [Key(11)]
        public string? in_reply_to_screen_name { get; set; }

        [Key(12)]
        public User1? user { get; set; }

        [Key(13)]
        public object? geo { get; set; }

        [Key(14)]
        public object? coordinates { get; set; }

        [Key(15)]
        public object? place { get; set; }

        [Key(16)]
        public object? contributors { get; set; }

        [Key(17)]
        public int retweet_count { get; set; }

        [Key(18)]
        public int favorite_count { get; set; }

        [Key(19)]
        public Entities3? entities { get; set; }

        [Key(20)]
        public bool favorited { get; set; }

        [Key(21)]
        public bool retweeted { get; set; }

        [Key(22)]
        public bool possibly_sensitive { get; set; }

        [Key(23)]
        public string? lang { get; set; }
    }

    [MessagePackObject]
    public class Metadata1
    {
        [Key(0)]
        public string? result_type { get; set; }

        [Key(1)]
        public string? iso_language_code { get; set; }
    }

    [MessagePackObject]
    public class User1
    {
        [Key(0)]
        public long id { get; set; }

        [Key(1)]
        public string? id_str { get; set; }

        [Key(2)]
        public string? name { get; set; }

        [Key(3)]
        public string? screen_name { get; set; }

        [Key(4)]
        public string? location { get; set; }

        [Key(5)]
        public string? description { get; set; }

        [Key(6)]
        public string? url { get; set; }

        [Key(7)]
        public Entities2? entities { get; set; }

        [Key(8)]
        public bool protected_ { get; set; }

        [Key(9)]
        public int followers_count { get; set; }

        [Key(10)]
        public int friends_count { get; set; }

        [Key(11)]
        public int listed_count { get; set; }

        [Key(12)]
        public string? created_at { get; set; }

        [Key(13)]
        public int favourites_count { get; set; }

        [Key(14)]
        public int? utc_offset { get; set; }

        [Key(15)]
        public string? time_zone { get; set; }

        [Key(16)]
        public bool geo_enabled { get; set; }

        [Key(17)]
        public bool verified { get; set; }

        [Key(18)]
        public int statuses_count { get; set; }

        [Key(19)]
        public string? lang { get; set; }

        [Key(20)]
        public bool contributors_enabled { get; set; }

        [Key(21)]
        public bool is_translator { get; set; }

        [Key(22)]
        public bool is_translation_enabled { get; set; }

        [Key(23)]
        public string? profile_background_color { get; set; }

        [Key(24)]
        public string? profile_background_image_url { get; set; }

        [Key(25)]
        public string? profile_background_image_url_https { get; set; }

        [Key(26)]
        public bool profile_background_tile { get; set; }

        [Key(27)]
        public string? profile_image_url { get; set; }

        [Key(28)]
        public string? profile_image_url_https { get; set; }

        [Key(29)]
        public string? profile_banner_url { get; set; }

        [Key(30)]
        public string? profile_link_color { get; set; }

        [Key(31)]
        public string? profile_sidebar_border_color { get; set; }

        [Key(32)]
        public string? profile_sidebar_fill_color { get; set; }

        [Key(33)]
        public string? profile_text_color { get; set; }

        [Key(34)]
        public bool profile_use_background_image { get; set; }

        [Key(35)]
        public bool default_profile { get; set; }

        [Key(36)]
        public bool default_profile_image { get; set; }

        [Key(37)]
        public bool following { get; set; }

        [Key(38)]
        public bool follow_request_sent { get; set; }

        [Key(39)]
        public bool notifications { get; set; }
    }

    [MessagePackObject]
    public class Entities2
    {
        [Key(0)]
        public Description1? description { get; set; }

        [Key(1)]
        public Url5? url { get; set; }
    }

    [MessagePackObject]
    public class Description1
    {
        [Key(0)]
        public Url4[]? urls { get; set; }
    }

    [MessagePackObject]
    public class Url4
    {
        [Key(0)]
        public string? url { get; set; }

        [Key(1)]
        public string? expanded_url { get; set; }

        [Key(2)]
        public string? display_url { get; set; }

        [Key(3)]
        public int[]? indices { get; set; }
    }

    [MessagePackObject]
    public class Url5
    {
        [Key(0)]
        public Url6[]? urls { get; set; }
    }

    [MessagePackObject]
    public class Url6
    {
        [Key(0)]
        public string? url { get; set; }

        [Key(1)]
        public string? expanded_url { get; set; }

        [Key(2)]
        public string? display_url { get; set; }

        [Key(3)]
        public int[]? indices { get; set; }
    }

    [MessagePackObject]
    public class Entities3
    {
        [Key(0)]
        public Hashtag1[]? hashtags { get; set; }

        [Key(1)]
        public object[]? symbols { get; set; }

        [Key(2)]
        public Url7[]? urls { get; set; }

        [Key(3)]
        public User_Mentions1[]? user_mentions { get; set; }

        [Key(4)]
        public Medium2[]? media { get; set; }
    }

    [MessagePackObject]
    public class Hashtag1
    {
        [Key(0)]
        public string? text { get; set; }

        [Key(1)]
        public int[]? indices { get; set; }
    }

    [MessagePackObject]
    public class Url7
    {
        [Key(0)]
        public string? url { get; set; }

        [Key(1)]
        public string? expanded_url { get; set; }

        [Key(2)]
        public string? display_url { get; set; }

        [Key(3)]
        public int[]? indices { get; set; }
    }

    [MessagePackObject]
    public class User_Mentions1
    {
        [Key(0)]
        public string? screen_name { get; set; }

        [Key(1)]
        public string? name { get; set; }

        [Key(2)]
        public long id { get; set; }

        [Key(3)]
        public string? id_str { get; set; }

        [Key(4)]
        public int[]? indices { get; set; }
    }

    [MessagePackObject]
    public class Medium2
    {
        [Key(0)]
        public long id { get; set; }

        [Key(1)]
        public string? id_str { get; set; }

        [Key(2)]
        public int[]? indices { get; set; }

        [Key(3)]
        public string? media_url { get; set; }

        [Key(4)]
        public string? media_url_https { get; set; }

        [Key(5)]
        public string? url { get; set; }

        [Key(6)]
        public string? display_url { get; set; }

        [Key(7)]
        public string? expanded_url { get; set; }

        [Key(8)]
        public string? type { get; set; }

        [Key(9)]
        public Sizes1? sizes { get; set; }

        [Key(10)]
        public long source_status_id { get; set; }

        [Key(11)]
        public string? source_status_id_str { get; set; }
    }

    [MessagePackObject]
    public class Sizes1
    {
        [Key(0)]
        public Medium3? medium { get; set; }

        [Key(1)]
        public Small1? small { get; set; }

        [Key(2)]
        public Thumb1? thumb { get; set; }

        [Key(3)]
        public Large1? large { get; set; }
    }

    [MessagePackObject]
    public class Medium3
    {
        [Key(0)]
        public int w { get; set; }

        [Key(1)]
        public int h { get; set; }

        [Key(2)]
        public string? resize { get; set; }
    }

    [MessagePackObject]
    public class Small1
    {
        [Key(0)]
        public int w { get; set; }

        [Key(1)]
        public int h { get; set; }

        [Key(2)]
        public string? resize { get; set; }
    }

    [MessagePackObject]
    public class Thumb1
    {
        [Key(0)]
        public int w { get; set; }

        [Key(1)]
        public int h { get; set; }

        [Key(2)]
        public string? resize { get; set; }
    }

    [MessagePackObject]
    public class Large1
    {
        [Key(0)]
        public int w { get; set; }

        [Key(1)]
        public int h { get; set; }

        [Key(2)]
        public string? resize { get; set; }
    }
}

public sealed class UpdateCenter
{
    [MessagePackObject]
    public class Root
    {
        [Key(0)]
        public string? connectionCheckUrl { get; set; }

        [Key(1)]
        public Core? core { get; set; }

        [Key(2)]
        public string? id { get; set; }

        [Key(3)]
        public Dictionary<string, Plugin>? plugins { get; set; }

        [Key(4)]
        public Signature? signature { get; set; }

        [Key(5)]
        public string? updateCenterVersion { get; set; }
    }

    [MessagePackObject]
    public class Core
    {
        [Key(0)]
        public string? buildDate { get; set; }

        [Key(1)]
        public string? name { get; set; }

        [Key(2)]
        public string? sha1 { get; set; }

        [Key(3)]
        public string? url { get; set; }

        [Key(4)]
        public string? version { get; set; }
    }

    [MessagePackObject]
    public class Plugin
    {
        [Key(0)]
        public string? buildDate { get; set; }

        [Key(1)]
        public Dependency[]? dependencies { get; set; }

        [Key(2)]
        public Developer[]? developers { get; set; }

        [Key(3)]
        public string? excerpt { get; set; }

        [Key(4)]
        public string? gav { get; set; }

        [Key(5)]
        public string[]? labels { get; set; }

        [Key(6)]
        public string? name { get; set; }

        [Key(7)]
        public string? previousTimestamp { get; set; }

        [Key(8)]
        public string? previousVersion { get; set; }

        [Key(9)]
        public string? releaseTimestamp { get; set; }

        [Key(10)]
        public string? requiredCore { get; set; }

        [Key(11)]
        public string? scm { get; set; }

        [Key(12)]
        public string? sha1 { get; set; }

        [Key(13)]
        public string? title { get; set; }

        [Key(14)]
        public string? url { get; set; }

        [Key(15)]
        public string? version { get; set; }

        [Key(16)]
        public string? wiki { get; set; }

        [Key(17)]
        public string? compatibleSinceVersion { get; set; }
    }

    [MessagePackObject]
    public class Dependency
    {
        [Key(0)]
        public string? name { get; set; }

        [Key(1)]
        public bool optional { get; set; }

        [Key(2)]
        public string? version { get; set; }
    }

    [MessagePackObject]
    public class Developer
    {
        [Key(0)]
        public string? developerId { get; set; }

        [Key(1)]
        public string? email { get; set; }

        [Key(2)]
        public string? name { get; set; }
    }

    [MessagePackObject]
    public class Signature
    {
        [Key(0)]
        public string[]? certificates { get; set; }

        [Key(1)]
        public string? correct_digest { get; set; }

        [Key(2)]
        public string? correct_signature { get; set; }

        [Key(3)]
        public string? digest { get; set; }

        [Key(4)]
        public string? signature { get; set; }
    }
}

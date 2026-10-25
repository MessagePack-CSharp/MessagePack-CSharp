extern alias V3;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using PolyType;
using PolyType.Abstractions;
using RedoxData;

// The CAPCOM RE:Dox serializer benchmark (github.com/CAPCOM-TD-OSS/REDox) on msgpack: its twelve
// simdjson-data documents and POCO graphs (RedoxModels.cs), serialized and deserialized whole, one
// row per library per direction per dataset. RE:Dox measures JSON libraries (and its own msgpack
// side in map form); this port measures the positional [Key] contract of
//   V4           this repo's source-generated formatters (MessagePack.MessagePackSerializer.Default)
//   V3           MessagePack-CSharp 3.1.8, StandardResolver (the dynamic Emit resolvers: v3's default on .NET)
//   V3SourceGen  MessagePack-CSharp 3.1.8, its real generated resolver over the V3GeneratedModels twins
//   Nerdbank     Nerdbank.MessagePack 1.4.82, PolyType source-generated shapes (RedoxWitness)
// on the same object graph: System.Text.Json loads each document once, V4/V3/Nerdbank serialize that
// instance, V3SourceGen serializes its twin materialized from the oracle payload. Every row
// deserializes its own serialized payload.
//
// Setup is the correctness gate, per dataset: V4 bytes == v3 dynamic bytes == v3 source-gen bytes
// (byte identity: same positional contract), and each library's deserialized graph re-serialized by
// V4 is byte-identical to the V4 payload (field-by-field equality via the oracle wire). Nerdbank's
// wire is allowed to differ (it is reported when it does).
//
// MEASURED (Ryzen AI 9 HX 470, .NET 11 rc1, ShortRun x 10 iterations, --no-disasm --join; µs/op,
// columns V4 | v3 dynamic | v3 source-gen | Nerdbank; allocations equal across libraries except where noted):
//                  ---------- Serialize ----------   --------- Deserialize ---------
//   apache_builds    20.6 |   42.0 |   46.6 |   33.6     72.7 |  101.5 |  107.6 |   90.1
//   canada          405.0 |  439.6 |  441.5 |  504.8     1904 |   2221 |   2296 |   2440
//   citm_catalog    118.1 |  304.6 |  223.2 |  259.5    186.4 |  316.3 |  323.7 |  396.7
//   github_events     8.6 |   18.8 |   26.0 |   15.7     27.1 |   33.3 |   42.4 |   35.6
//   gsoc-2018       631.4 |  870.3 |  976.4 |  822.2     1189 |   1304 |   1405 |   1341
//   instruments       8.0 |   48.6 |   27.2 |   41.6     22.8 |   45.4 |   42.4 |   66.5
//   marine_ik       475.2 |  818.5 |  790.8 |  752.4     1553 |   2364 |   2642 |   2970
//   mesh            105.6 |  112.0 |  110.7 |  110.7    186.6 |  251.8 |  266.6 |  425.8
//   numbers          30.4 |   31.6 |   32.4 |   30.6      2.5 |   33.7 |   33.4 |   34.4
//   random          220.9 |  371.3 |  398.6 |  345.4    358.0 |  533.2 |  587.1 |  495.3
//   twitter         125.5 |  269.8 |  289.7 |  266.4    271.9 |  321.8 |  401.0 |  370.8
//   update-center   218.9 |  383.3 |  454.8 |  347.5    448.2 |  588.9 |  692.7 |  626.5
//   geomean ratio    1.00 |   1.81 |   1.80 |   1.67     1.00 |   1.67 |   1.82 |   1.91
// V4 is fastest on all 24 cells. The spread is widest on object-heavy documents (instruments, twitter,
// citm_catalog, update-center: 2-6x serialize) and narrowest on the number-array documents (canada, mesh,
// numbers serialize: within 10%), where every library is bound by the same float64 stream; numbers
// deserialize (a bare double[]) is the weave formatter's 13x. Serialize allocations: V4 1,651 KB vs
// 2,316 KB (marine_ik) and 432 vs 595 KB (mesh) - the others grow an intermediate buffer past the final
// 1,690 KB / 442 KB payload, V4 sizes it from the walk. Nerdbank's wire matched the oracle byte-for-byte
// on 11 documents; apache_builds differs in content at equal length (its {} member classes).
//
// Data: sandbox/DisassembleBenchmark/redox-data/*.json (gitignored; fetch-redox-data.ps1 downloads the
// pinned simdjson-data revision RE:Dox uses), overridable with REDOX_DATA_DIR.
//
// Usage:
//   dotnet run -c Release -- --redox-check                                  (setup gate only, in-process)
//   dotnet run -c Release -- --filter *RedoxBenchmark* --no-disasm --join   (the table; --iterations=N widens ShortRun)
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[GenericTypeArguments(typeof(ApacheBuilds), typeof(ApacheBuilds.Root), typeof(V3GeneratedModels.Redox.ApacheBuilds.Root))]
[GenericTypeArguments(typeof(Canada), typeof(Canada.Root), typeof(V3GeneratedModels.Redox.Canada.Root))]
[GenericTypeArguments(typeof(CitmCatalog), typeof(CitmCatalog.Root), typeof(V3GeneratedModels.Redox.CitmCatalog.Root))]
[GenericTypeArguments(typeof(GitHubEvents), typeof(GitHubEvents.GitHubEvent[]), typeof(V3GeneratedModels.Redox.GitHubEvents.GitHubEvent[]))]
[GenericTypeArguments(typeof(Gsoc2018), typeof(Dictionary<int, Gsoc2018.GsocProject>), typeof(Dictionary<int, V3GeneratedModels.Redox.Gsoc2018.GsocProject>))]
[GenericTypeArguments(typeof(Instruments), typeof(Instruments.Root), typeof(V3GeneratedModels.Redox.Instruments.Root))]
[GenericTypeArguments(typeof(MarineIk), typeof(MarineIk.Root), typeof(V3GeneratedModels.Redox.MarineIk.Root))]
[GenericTypeArguments(typeof(Mesh), typeof(Mesh.Root), typeof(V3GeneratedModels.Redox.Mesh.Root))]
[GenericTypeArguments(typeof(Numbers), typeof(double[]), typeof(double[]))]
[GenericTypeArguments(typeof(RedoxData.Random), typeof(RedoxData.Random.Root), typeof(V3GeneratedModels.Redox.Random.Root))]
[GenericTypeArguments(typeof(Twitter), typeof(Twitter.Root), typeof(V3GeneratedModels.Redox.Twitter.Root))]
[GenericTypeArguments(typeof(UpdateCenter), typeof(UpdateCenter.Root), typeof(V3GeneratedModels.Redox.UpdateCenter.Root))]
public class RedoxBenchmark<TDataset, TRoot, TRootV3>
    where TDataset : IRedoxDataset
{
    static readonly ITypeShapeProvider nbProvider = TypeShapeResolver.Resolve<double[], RedoxWitness>().Provider;
    static readonly V3::MessagePack.MessagePackSerializerOptions v3SourceGen = V3GeneratedModels.V3SourceGen.Options;

    readonly Nerdbank.MessagePack.MessagePackSerializer nb = new();
    readonly ITypeShape<TRoot> nbShape = (ITypeShape<TRoot>)(nbProvider.GetTypeShape(typeof(TRoot))
        ?? throw new InvalidOperationException($"RedoxWitness has no shape for {typeof(TRoot)}"));

    TRoot root = default!;
    TRootV3 rootV3 = default!;
    byte[] v4Payload = default!;
    byte[] nbPayload = default!;

    public string Dataset => TDataset.FileName;
    public int V4PayloadLength => v4Payload.Length;
    public int NerdbankPayloadLength => nbPayload.Length;

    [GlobalSetup]
    public void Setup()
    {
        var json = File.ReadAllBytes(RedoxDataFiles.Locate(TDataset.FileName));
        root = JsonSerializer.Deserialize<TRoot>(json) ?? throw new InvalidOperationException($"{TDataset.FileName}: System.Text.Json produced null");

        v4Payload = MessagePack.MessagePackSerializer.Serialize(root);
        var v3Payload = V3::MessagePack.MessagePackSerializer.Serialize(root);
        if (!v3Payload.AsSpan().SequenceEqual(v4Payload)) throw new InvalidOperationException($"verify failed ({TDataset.FileName}): V4 bytes ({v4Payload.Length}) != MessagePack-CSharp oracle ({v3Payload.Length})");

        // the source-gen twin roundtrips the oracle payload through v3's GENERATED resolver
        rootV3 = V3::MessagePack.MessagePackSerializer.Deserialize<TRootV3>(v4Payload, v3SourceGen);
        if (!V3::MessagePack.MessagePackSerializer.Serialize(rootV3, v3SourceGen).AsSpan().SequenceEqual(v4Payload)) throw new InvalidOperationException($"verify failed ({TDataset.FileName}): MessagePack-CSharp source-gen twin roundtrip");

        nbPayload = nb.Serialize(root, nbShape);
        if (!nbPayload.AsSpan().SequenceEqual(v4Payload)) Console.WriteLine($"// note ({TDataset.FileName}): Nerdbank wire differs from the oracle ({nbPayload.Length} B vs {v4Payload.Length} B)");

        VerifyRoundtrip(MessagePack.MessagePackSerializer.Deserialize<TRoot>(v4Payload), "V4");
        VerifyRoundtrip(V3::MessagePack.MessagePackSerializer.Deserialize<TRoot>(v4Payload), "MessagePack-CSharp");
        VerifyRoundtrip(nb.Deserialize(nbPayload, nbShape), "Nerdbank");
    }

    void VerifyRoundtrip(TRoot? back, string label)
    {
        if (back is null) throw new InvalidOperationException($"verify failed ({TDataset.FileName}): {label} deserialized null");
        // field-by-field equality via the oracle wire: the roundtripped graph must re-serialize to the same bytes
        if (!MessagePack.MessagePackSerializer.Serialize(back).AsSpan().SequenceEqual(v4Payload)) throw new InvalidOperationException($"verify failed ({TDataset.FileName}): {label} roundtrip");
    }

    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public byte[] SerializeV4() => MessagePack.MessagePackSerializer.Serialize(root);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeV3() => V3::MessagePack.MessagePackSerializer.Serialize(root);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeV3SourceGen() => V3::MessagePack.MessagePackSerializer.Serialize(rootV3, v3SourceGen);

    [BenchmarkCategory("Serialize"), Benchmark]
    public byte[] SerializeNerdbank() => nb.Serialize(root, nbShape);

    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public TRoot DeserializeV4() => MessagePack.MessagePackSerializer.Deserialize<TRoot>(v4Payload)!;

    [BenchmarkCategory("Deserialize"), Benchmark]
    public TRoot DeserializeV3() => V3::MessagePack.MessagePackSerializer.Deserialize<TRoot>(v4Payload);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public TRootV3 DeserializeV3SourceGen() => V3::MessagePack.MessagePackSerializer.Deserialize<TRootV3>(v4Payload, v3SourceGen);

    [BenchmarkCategory("Deserialize"), Benchmark]
    public TRoot DeserializeNerdbank() => nb.Deserialize(nbPayload, nbShape)!;
}

/// <summary>Finds the RE:Dox documents: REDOX_DATA_DIR, else a redox-data folder in this directory or any parent.</summary>
public static class RedoxDataFiles
{
    public static string Locate(string fileName)
    {
        if (Environment.GetEnvironmentVariable("REDOX_DATA_DIR") is { Length: > 0 } dir)
        {
            var explicitPath = Path.Combine(dir, fileName);
            if (File.Exists(explicitPath)) return explicitPath;
            throw new FileNotFoundException($"REDOX_DATA_DIR is set but {explicitPath} does not exist");
        }

        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, "redox-data", fileName);
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException($"{fileName} not found in any redox-data folder above {AppContext.BaseDirectory}; run sandbox/DisassembleBenchmark/fetch-redox-data.ps1 or set REDOX_DATA_DIR");
    }
}

/// <summary>--redox-check: runs every dataset's Setup gate in-process and prints the wire sizes.</summary>
public static class RedoxCheck
{
    public static bool RunAll()
    {
        var ok = true;
        ok &= Run(new RedoxBenchmark<ApacheBuilds, ApacheBuilds.Root, V3GeneratedModels.Redox.ApacheBuilds.Root>());
        ok &= Run(new RedoxBenchmark<Canada, Canada.Root, V3GeneratedModels.Redox.Canada.Root>());
        ok &= Run(new RedoxBenchmark<CitmCatalog, CitmCatalog.Root, V3GeneratedModels.Redox.CitmCatalog.Root>());
        ok &= Run(new RedoxBenchmark<GitHubEvents, GitHubEvents.GitHubEvent[], V3GeneratedModels.Redox.GitHubEvents.GitHubEvent[]>());
        ok &= Run(new RedoxBenchmark<Gsoc2018, Dictionary<int, Gsoc2018.GsocProject>, Dictionary<int, V3GeneratedModels.Redox.Gsoc2018.GsocProject>>());
        ok &= Run(new RedoxBenchmark<Instruments, Instruments.Root, V3GeneratedModels.Redox.Instruments.Root>());
        ok &= Run(new RedoxBenchmark<MarineIk, MarineIk.Root, V3GeneratedModels.Redox.MarineIk.Root>());
        ok &= Run(new RedoxBenchmark<Mesh, Mesh.Root, V3GeneratedModels.Redox.Mesh.Root>());
        ok &= Run(new RedoxBenchmark<Numbers, double[], double[]>());
        ok &= Run(new RedoxBenchmark<RedoxData.Random, RedoxData.Random.Root, V3GeneratedModels.Redox.Random.Root>());
        ok &= Run(new RedoxBenchmark<Twitter, Twitter.Root, V3GeneratedModels.Redox.Twitter.Root>());
        ok &= Run(new RedoxBenchmark<UpdateCenter, UpdateCenter.Root, V3GeneratedModels.Redox.UpdateCenter.Root>());
        return ok;
    }

    static bool Run<TDataset, TRoot, TRootV3>(RedoxBenchmark<TDataset, TRoot, TRootV3> b)
        where TDataset : IRedoxDataset
    {
        try
        {
            b.Setup();
            Console.WriteLine($"OK {b.Dataset,-20} msgpack {b.V4PayloadLength,9:N0} B (Nerdbank {b.NerdbankPayloadLength,9:N0} B)");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NG {b.Dataset,-20} {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }
}

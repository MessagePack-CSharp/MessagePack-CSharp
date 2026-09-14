using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.RegularExpressions;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// generic harvesting: closed instantiations of the compilation's generic
// [MessagePackObject] types found inside serialized member types get static closed
// constructions in the generated factory — the Native AOT route (MakeGenericType cannot
// close over the ref struct buffer arguments there), and an Activator-free fast path on
// CoreCLR. MakeGenericType stays as the CoreCLR fallback for unharvested instantiations.
public class GenericHarvestTests
{
    static (GeneratorDriverRunResult Result, Compilation Updated) RunGenerator(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "HarvestProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        var updatedDriver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _, CancellationToken.None);
        return (updatedDriver.GetRunResult(), updated);
    }

    [Fact]
    public void ClosedMemberInstantiations_GetStaticFactoryBranches()
    {
        var (result, updated) = RunGenerator("""
            using MessagePack;
            using System.Collections.Generic;

            [MessagePackObject]
            public class Box<T>
            {
                [Key(0)] public T Item { get; set; }
            }

            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public Box<int> Direct { get; set; }
                [Key(1)] public List<Box<string>> Listed { get; set; }
                [Key(2)] public Box<Box<double>> Nested { get; set; }
            }
            """);
        var factory = Assert.Single(result.Results[0].GeneratedSources, s => s.HintName.Contains("Factory")).SourceText.ToString();

        // four closed instantiations: Box<int>, Box<string>, Box<double> (harvested from
        // inside the nested argument), and Box<Box<double>> itself
        Assert.Equal(4, Regex.Matches(factory, @"if \(type == typeof\(global::Box<(?!>)").Count);
        // the open MakeGenericType fallback stays for unharvested instantiations
        Assert.Contains("GetGenericTypeDefinition() == typeof(global::Box<>)", factory);
        Assert.Empty(updated.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void OpenMemberOfGenericContainer_IsNotHarvested()
    {
        // Box<T> referencing itself flows a type parameter: nothing closed to harvest
        var (result, _) = RunGenerator("""
            using MessagePack;

            [MessagePackObject]
            public class Box<T>
            {
                [Key(0)] public T Item { get; set; }
                [Key(1)] public Box<T> Next { get; set; }
            }
            """);
        var factory = Assert.Single(result.Results[0].GeneratedSources, s => s.HintName.Contains("Factory")).SourceText.ToString();
        Assert.Equal(0, Regex.Matches(factory, @"if \(type == typeof\(global::Box<(?!>)").Count);
    }
}

// the harvested closed branch serves both member resolution and root serialization
public class GenericHarvestRuntimeTests
{
    [Fact]
    public void HarvestedGenericMember_Roundtrips()
    {
        var holder = new HarvestBoxHolder
        {
            Numbers = new HarvestBox<int> { Item = 42 },
            Words = new HarvestBox<string> { Item = "v" },
        };
        var back = V4.Deserialize<HarvestBoxHolder>(V4.Serialize(holder))!;
        Assert.Equal(42, back.Numbers!.Item);
        Assert.Equal("v", back.Words!.Item);

        Assert.Equal(7, V4.Deserialize<HarvestBox<int>>(V4.Serialize(new HarvestBox<int> { Item = 7 }))!.Item);
    }
}

[MessagePackObject]
public class HarvestBox<T>
{
    [Key(0)] public T? Item { get; set; }
}

[MessagePackObject]
public class HarvestBoxHolder
{
    [Key(0)] public HarvestBox<int>? Numbers { get; set; }
    [Key(1)] public HarvestBox<string>? Words { get; set; }
}

using System.Reflection;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MessagePack.Tests;

// The generator's per-type outputs must re-run only when the model changes. The models are equatable records, so the
// way this breaks is a node input that drags a location or a symbol along: a diagnosed type then re-emits on every
// line-shifting keystroke, and a symbol pins the old compilation. Both stay invisible to every output-based test.
public class GeneratorIncrementalCachingTests
{
    // a keyed getter-only member: MsgPack007 (a warning) with a model still emitted
    const string Source = """
        using MessagePack;
        namespace Probe;
        [MessagePackObject]
        public class Warned
        {
            [Key(0)] public int A { get; }
            [Key(1)] public string? B { get; set; }
        }
        """;

    static (GeneratorDriver Driver, CSharpCompilation Compilation) FirstRun()
    {
        var compilation = AnalyzerTestHost.CreateCompilation(Source, "CachingProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new MessagePackGenerator().AsSourceGenerator()],
            parseOptions: parseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        Assert.Contains(driver.GetRunResult().Diagnostics, static d => d.Id == "MsgPack007");
        return (driver, compilation);
    }

    static GeneratorRunResult Rerun(GeneratorDriver driver, CSharpCompilation compilation, string newSource)
    {
        var oldTree = compilation.SyntaxTrees[0];
        var updated = compilation.ReplaceSyntaxTree(oldTree, CSharpSyntaxTree.ParseText(newSource, (CSharpParseOptions)oldTree.Options));
        return Assert.Single(driver.RunGenerators(updated).GetRunResult().Results);
    }

    static IncrementalStepRunReason[] Reasons(GeneratorRunResult result, string stepName) =>
        result.TrackedSteps[stepName].SelectMany(static step => step.Outputs).Select(static output => output.Reason).ToArray();

    [Fact]
    public void LineShiftAboveADiagnosedType_LeavesItsModelCached()
    {
        var (driver, compilation) = FirstRun();
        var result = Rerun(driver, compilation, "// a comment line shifts every location below it\n" + Source);

        // the parse result carries the moved MsgPack007 location, so it does change
        Assert.Contains(IncrementalStepRunReason.Modified, Reasons(result, "TypeParseResults"));
        // the model the formatter file is emitted from does not
        Assert.All(Reasons(result, "TypeModels"), static reason => Assert.True(reason is IncrementalStepRunReason.Unchanged or IncrementalStepRunReason.Cached, reason.ToString()));
    }

    [Fact]
    public void KeyChange_ModifiesTheModel()
    {
        var (driver, compilation) = FirstRun();
        var result = Rerun(driver, compilation, Source.Replace("[Key(1)]", "[Key(4)]"));

        Assert.Contains(IncrementalStepRunReason.Modified, Reasons(result, "TypeModels"));
    }

    [Fact]
    public void Models_HoldNoCompilationObjects()
    {
        // a symbol, syntax node or compilation inside an equatable model compares by reference: every run is
        // "Modified", and the model keeps the previous compilation alive
        Type[] forbidden = [typeof(ISymbol), typeof(SyntaxNode), typeof(SyntaxTree), typeof(Compilation), typeof(SemanticModel), typeof(Location)];
        var records = typeof(MessagePackGenerator).Assembly.GetTypes()
            .Where(static t => t.GetMethod("<Clone>$") is not null)
            .ToArray();
        Assert.NotEmpty(records);

        var offenders = new List<string>();
        foreach (var record in records)
        {
            foreach (var property in record.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.Name == "EqualityContract")
                {
                    continue;
                }
                foreach (var type in Flatten(property.PropertyType))
                {
                    if (forbidden.Any(f => f.IsAssignableFrom(type)))
                    {
                        offenders.Add($"{record.Name}.{property.Name}: {type.Name}");
                    }
                }
            }
        }
        Assert.Empty(offenders);

        static IEnumerable<Type> Flatten(Type type)
        {
            yield return type;
            if (type.HasElementType)
            {
                foreach (var inner in Flatten(type.GetElementType()!)) yield return inner;
            }
            foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : [])
            {
                foreach (var inner in Flatten(argument)) yield return inner;
            }
        }
    }
}

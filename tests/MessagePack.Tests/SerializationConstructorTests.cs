using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// [SerializationConstructor] is an explicit contract, so it must be unambiguous: the attribute is
// AllowMultiple=false per constructor (CS0579), and a second constructor carrying its own copy is
// rejected by both tiers instead of the declaration-order pick v4 used to make silently.
public class SerializationConstructorTests
{
    static readonly MessagePackSerializerOptions reflectionOptions = new(
        new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

    const string TwoAttributedConstructors = """
        using MessagePack;
        [MessagePackObject]
        public class TwoCtors
        {
            [Key(0)] public int X { get; }
            [Key(1)] public int Y { get; }

            [SerializationConstructor]
            public TwoCtors(int x) { X = x; }

            [SerializationConstructor]
            public TwoCtors(int x, int y) { X = x; Y = y; }
        }
        """;

    [Fact]
    public void Generator_RejectsASecondAttributedConstructor()
    {
        var compilation = AnalyzerTestHost.CreateCompilation(TwoAttributedConstructors, "SerializationCtorProbe");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, CancellationToken.None).GetRunResult();

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "MsgPack009");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("more than one constructor", diagnostic.GetMessage());
        // the diagnostic points at the second attributed constructor (generator locations carry a file
        // path and line span, not the tree)
        var reportedLine = diagnostic.Location.GetLineSpan().StartLinePosition.Line;
        var secondConstructorLine = compilation.SyntaxTrees[0].GetText().Lines.First(l => l.ToString().Contains("TwoCtors(int x, int y)")).LineNumber;
        Assert.Equal(secondConstructorLine, reportedLine);
        Assert.DoesNotContain(result.GeneratedTrees, t => t.FilePath.Contains("TwoCtors"));
    }

    [Fact]
    public void ReflectionTier_RejectsASecondAttributedConstructor()
    {
        var assembly = AnalyzerTestHost.CompileAndLoad(TwoAttributedConstructors, withGenerator: false);
        var type = assembly.GetType("TwoCtors")!;
        var instance = Activator.CreateInstance(type, 1, 2)!;

        var ex = Assert.Throws<MessagePackSerializationException>(() => V4.Serialize(type, instance, reflectionOptions));
        Assert.Contains("more than one constructor", ex.Message);
    }
}

using System.Collections.Immutable;
using MessagePack.CodeFixes;
using MessagePack.SourceGenerator.Analyzers;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace MessagePack.Tests;

// the fixers driven through a real AdhocWorkspace: run the analyzer (or the generator,
// for its diagnostics), hand the first matching diagnostic to the fixer, apply the first
// registered action, and assert on the rewritten probe document.
public class CodeFixTests
{
    static async Task<string> ApplyFixAsync(CodeFixProvider fixer, DiagnosticAnalyzer? analyzer, string source, string diagnosticId)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("FixProbe", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest))
            .AddMetadataReferences(AnalyzerTestHost.References);
        var document = project.AddDocument("Probe.cs", SourceText.From(source));
        var compilation = (await document.Project.GetCompilationAsync())!;

        ImmutableArray<Diagnostic> diagnostics;
        if (analyzer is not null)
        {
            diagnostics = await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync(CancellationToken.None);
        }
        else
        {
            var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: (CSharpParseOptions)document.Project.ParseOptions!);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out diagnostics, CancellationToken.None);
        }
        var diagnostic = diagnostics.First(d => d.Id == diagnosticId);

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await fixer.RegisterCodeFixesAsync(context);
        var action = Assert.Single(actions.GroupBy(a => a.EquivalenceKey).Select(g => g.First()));

        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        return (await solution.GetDocument(document.Id)!.GetTextAsync()).ToString();
    }

    [Fact]
    public async Task AddKeyAttributes_NumbersAfterTheHighestExisting()
    {
        var fixedSource = await ApplyFixAsync(new AddKeyAttributesCodeFixProvider(), analyzer: null, """
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public int Id { get; set; }
                public string? Name { get; set; }
                public double Score;
            }
            """, "MsgPack001");
        Assert.Contains("[Key(1)]", fixedSource);
        Assert.Contains("[Key(2)]", fixedSource);
        // properties number before fields, so Name gets 1 and Score gets 2
        Assert.True(fixedSource.IndexOf("[Key(1)]") < fixedSource.IndexOf("Name"));
        Assert.True(fixedSource.IndexOf("[Key(2)]") < fixedSource.IndexOf("Score"));
    }

    [Fact]
    public async Task AddMessagePackObject_ToUnionRoot()
    {
        var fixedSource = await ApplyFixAsync(new AddMessagePackObjectCodeFixProvider(), new UnionTagRequiresMessagePackObjectAnalyzer(), """
            using MessagePack;
            [UnionTag(typeof(Circle), 0)]
            public interface IShape { }
            [MessagePackObject]
            public class Circle : IShape
            {
                [Key(0)] public double R { get; set; }
            }
            """, "MsgPack103");
        Assert.Contains("[MessagePackObject]", fixedSource.Substring(0, fixedSource.IndexOf("interface IShape")));
    }

    [Fact]
    public async Task AnnotateOffender_AddsObjectAndKeys()
    {
        var fixedSource = await ApplyFixAsync(new AddMessagePackObjectCodeFixProvider(), new SerializableMemberTypeAnalyzer(), """
            using MessagePack;
            [MessagePackObject]
            public class Holder
            {
                [Key(0)] public Plain? Value { get; set; }
            }
            public class Plain
            {
                public int X { get; set; }
                public string? Y { get; set; }
            }
            """, "MsgPack101");
        Assert.Matches(@"\[MessagePackObject\]\s*public class Plain", fixedSource);
        var plainDeclaration = fixedSource.Substring(fixedSource.IndexOf("class Plain"));
        Assert.Contains("[Key(0)]", plainDeclaration);
        Assert.Contains("[Key(1)]", plainDeclaration);
    }

    [Fact]
    public async Task AddUnionTag_ForUntaggedPatternUnionCase()
    {
        var fixedSource = await ApplyFixAsync(new AddUnionTagCodeFixProvider(), new PatternUnionCoverageAnalyzer(), """
            using MessagePack;
            [System.Runtime.CompilerServices.Union]
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            public struct Shape
            {
                public Shape(Circle value) { }
                public Shape(Square value) { }
                public object? Value => null;
            }
            [MessagePackObject] public class Circle { [Key(0)] public double R { get; set; } }
            [MessagePackObject] public class Square { [Key(0)] public double S { get; set; } }
            namespace System.Runtime.CompilerServices
            {
                [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
                internal sealed class UnionAttribute : System.Attribute { }
            }
            """, "MsgPack107");
        Assert.Contains("UnionTag(typeof(Square), 1)", fixedSource);
    }

    [Fact]
    public async Task AddUnionTag_ForUntaggedClosedDerived()
    {
        var fixedSource = await ApplyFixAsync(new AddUnionTagCodeFixProvider(), new ClosedUnionCoverageAnalyzer(), """
            using MessagePack;
            using System.Runtime.CompilerServices;
            [MessagePackObject]
            [UnionTag(typeof(Circle), 0)]
            [IsClosedType(DerivedTypes = new[] { typeof(Circle), typeof(Square) })]
            public abstract class Shape { }
            [MessagePackObject] public class Circle : Shape { [Key(0)] public double R { get; set; } }
            [MessagePackObject] public class Square : Shape { [Key(0)] public double S { get; set; } }
            namespace System.Runtime.CompilerServices
            {
                internal sealed class IsClosedTypeAttribute : System.Attribute
                {
                    public System.Type[]? DerivedTypes { get; set; }
                }
            }
            """, "MsgPack106");
        Assert.Contains("UnionTag(typeof(Square), 1)", fixedSource);
    }
}

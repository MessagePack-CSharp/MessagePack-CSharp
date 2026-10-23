using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace MessagePack.Tests;

// v3 regression (MessagePack-CSharp#2283 / #2305): a [MessagePackObject] member typed as a closed generic VALUE type
// declared in a referenced assembly (record struct Item<T> from lib, Item<int> member in app) made TypeCollector index
// DeclaringSyntaxReferences[0] on a metadata symbol, so the generator faulted (CS8785) and emitted no resolver at all,
// and the analyzer faulted (AD0001). The build stayed green and only trimmed / Native AOT apps failed, at run time.
// The three shape conditions all had to hold (generic, value type, other assembly), so this keeps exactly that shape.
public class ReferencedAssemblyGenericValueTypeTests
{
    const string LibrarySource = """
        using MessagePack;
        namespace Lib{0};
        [MessagePackObject]
        public partial record struct Item<T>([property: Key(0)] string Key);
        """;

    const string ConsumerSource = """
        using MessagePack;
        using Lib{0};
        namespace App{0};
        [MessagePackObject]
        public sealed partial record Cmd([property: Key(0)] string Scope)
        {
            [Key(1)] public Item<int> Member { get; init; }
        }
        """;

    [Fact]
    public void Generator_DoesNotFault_AndEmitsTheConsumerFormatter()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var libraryBytes = Compile("Lib" + suffix, Source(LibrarySource, suffix), out _);

        Compile("App" + suffix, Source(ConsumerSource, suffix), out var result, MetadataReference.CreateFromImage(libraryBytes));

        Assert.All(result.Results, static r => Assert.Null(r.Exception));
        Assert.DoesNotContain(result.Diagnostics, static d => d.Id == "CS8785");
        Assert.DoesNotContain(result.Diagnostics, static d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(result.GeneratedTrees, static t => t.FilePath.Contains("CmdFormatter", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Analyzers_DoNotFault_OnTheConsumer()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var libraryBytes = Compile("Lib" + suffix, Source(LibrarySource, suffix), out _);
        var consumer = CreateCompilation("App" + suffix, Source(ConsumerSource, suffix), MetadataReference.CreateFromImage(libraryBytes));
        Assert.Empty(consumer.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));

        foreach (var analyzer in new DiagnosticAnalyzer[] { new MessagePack.SourceGenerator.Analyzers.IneffectiveAnnotationAnalyzer(), new MessagePack.SourceGenerator.Analyzers.SerializableMemberTypeAnalyzer() })
        {
            var diagnostics = await consumer.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync(CancellationToken.None);
            Assert.DoesNotContain(diagnostics, static d => d.Id == "AD0001");
        }
    }

    // end to end through both generated assemblies: the library's module initializer registers the open Item<>
    // formatter, the registry closes it over int for the consumer's member
    [Fact]
    public void Roundtrip_ThroughBothGeneratedAssemblies()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var libraryBytes = Compile("Lib" + suffix, Source(LibrarySource, suffix), out _);
        var consumerBytes = Compile("App" + suffix, Source(ConsumerSource, suffix), out _, MetadataReference.CreateFromImage(libraryBytes));

        var library = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(libraryBytes));
        RuntimeHelpers.RunModuleConstructor(library.ManifestModule.ModuleHandle);
        var consumer = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(consumerBytes));
        RuntimeHelpers.RunModuleConstructor(consumer.ManifestModule.ModuleHandle);

        var itemOfInt = library.GetType($"Lib{suffix}.Item`1")!.MakeGenericType(typeof(int));
        var item = Activator.CreateInstance(itemOfInt, ["k"])!;
        var cmd = consumer.GetType($"App{suffix}.Cmd")!;
        var instance = Activator.CreateInstance(cmd, ["scope"])!;
        cmd.GetProperty("Member")!.SetValue(instance, item);

        var options = new MessagePackSerializerOptions(new MessagePackFormatterResolver(MessagePackFormatterFactory.Default));
        var bytes = MessagePackSerializer.Serialize(cmd, instance, options);
        Assert.Equal("[\"scope\",[\"k\"]]", MessagePackSerializer.ConvertToJson(bytes));

        var back = MessagePackSerializer.Deserialize(cmd, bytes, options)!;
        Assert.Equal("scope", cmd.GetProperty("Scope")!.GetValue(back));
        Assert.Equal("k", itemOfInt.GetProperty("Key")!.GetValue(cmd.GetProperty("Member")!.GetValue(back)));
    }

    static string Source(string template, string suffix) => template.Replace("{0}", suffix);

    static CSharpCompilation CreateCompilation(string assemblyName, string source, params MetadataReference[] additionalReferences)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest).WithPreprocessorSymbols("NET9_0_OR_GREATER");
        return CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            AnalyzerTestHost.References.Concat(additionalReferences),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    // compiles through the real generator to IL, so a reference to the result is a genuine metadata symbol
    // (a CompilationReference would keep the syntax and could never reproduce the v3 fault)
    static byte[] Compile(string assemblyName, string source, out GeneratorDriverRunResult result, params MetadataReference[] additionalReferences)
    {
        var compilation = CreateCompilation(assemblyName, source, additionalReferences);
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _, CancellationToken.None).GetRunResult();
        var errors = result.Diagnostics.Concat(updated.GetDiagnostics()).Where(static d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, string.Join("\n", errors.Select(static d => d.ToString())));

        using var stream = new MemoryStream();
        var emitted = updated.Emit(stream);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        return stream.ToArray();
    }
}

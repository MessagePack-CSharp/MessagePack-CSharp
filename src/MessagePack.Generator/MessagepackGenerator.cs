// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.Generator;

[Generator(LanguageNames.CSharp)]
public partial class MessagepackGenerator : IIncrementalGenerator
{
    public const string MessagePackObjectAttributeFullName = "MessagePack.MessagePackObjectAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            MessagePackObjectAttributeFullName,
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (context, _) => (TypeDeclarationSyntax)context.TargetNode);

        var source = typeDeclarations
            .Combine(context.CompilationProvider)
            .WithComparer(Comparer.Instance);

        context.RegisterSourceOutput(source, static (context, source) =>
        {
            var (typeDeclaration, compilation) = source;
            Generate(typeDeclaration, compilation, new GeneratorContext(context));
        });
    }

    private class Comparer : IEqualityComparer<(TypeDeclarationSyntax, Compilation)>
    {
        public static readonly Comparer Instance = new Comparer();

        public bool Equals((TypeDeclarationSyntax, Compilation) x, (TypeDeclarationSyntax, Compilation) y)
        {
            return x.Item1.Equals(y.Item1);
        }

        public int GetHashCode((TypeDeclarationSyntax, Compilation) obj)
        {
            return obj.Item1.GetHashCode();
        }
    }

    private class GeneratorContext : IGeneratorContext
    {
        private SourceProductionContext context;

        public GeneratorContext(SourceProductionContext context)
        {
            this.context = context;
        }

        public CancellationToken CancellationToken => context.CancellationToken;

        public void AddSource(string hintName, string source) => context.AddSource(hintName, source);
    }

#if false
    public async Task RunAsync(
        [Option("i", "Input path to MSBuild project file or the directory containing Unity source files.")] string input,
        [Option("o", "Output file path(.cs) or directory (multiple generate file).")] string output,
        [Option("c", "Conditional compiler symbols, split with ','. Ignored if a project file is specified for input.")] string? conditionalSymbol = null,
        [Option("r", "Set resolver name.")] string resolverName = "GeneratedResolver",
        [Option("n", "Set namespace root name.")] string @namespace = "MessagePack",
        [Option("m", "Force use map mode serialization.")] bool useMapMode = false,
        [Option("ms", "Generate #if-- files by symbols, split with ','.")] string? multipleIfDirectiveOutputSymbols = null,
        [Option("ei", "Ignore type names.")] string[]? externalIgnoreTypeNames = null)
    {
        try
        {
            Compilation compilation;
            if (Directory.Exists(input))
            {
                string[]? conditionalSymbols = conditionalSymbol?.Split(',');
                compilation = await PseudoCompilation.CreateFromDirectoryAsync(input, conditionalSymbols, this.Context.CancellationToken);
            }
            else
            {
                (workspace, compilation) = await this.OpenMSBuildProjectAsync(input, this.Context.CancellationToken);
            }

            await new MessagePackCompiler.CodeGenerator(x => Console.WriteLine(x), this.Context.CancellationToken)
                .GenerateFileAsync(
                    compilation,
                    output,
                    resolverName,
                    @namespace,
                    useMapMode,
                    multipleIfDirectiveOutputSymbols,
                    externalIgnoreTypeNames).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await Console.Error.WriteLineAsync("Canceled");
            throw;
        }
        finally
        {
            workspace?.Dispose();
        }
    }

    private async Task<(Workspace Workspace, Compilation Compilation)> OpenMSBuildProjectAsync(string projectPath, CancellationToken cancellationToken)
    {
        var workspace = MSBuildWorkspace.Create();
        try
        {
            var logger = new ConsoleLogger(Microsoft.Build.Framework.LoggerVerbosity.Quiet);
            var project = await workspace.OpenProjectAsync(projectPath, logger, null, cancellationToken);
            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation is null)
            {
                throw new NotSupportedException("The project does not support creating Compilation.");
            }

            return (workspace, compilation);
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }
#endif
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Uncomment the following line to write expected files to disk
////#define WRITE_EXPECTED

#if WRITE_EXPECTED
#warning WRITE_EXPECTED is fine for local builds, but should not be merged to the main branch.
#endif

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using MessagePack.SourceGenerator.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using AnalyzerOptions = MessagePack.SourceGenerator.CodeAnalysis.AnalyzerOptions;

internal static partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
    where TSourceGenerator : new()
{
    internal class Test : CSharpSourceGeneratorTest<TSourceGenerator, DefaultVerifier>
    {
        private readonly string? testFile;
        private readonly string testMethod;

        public Test(ReferencesSet references = ReferencesSet.MessagePack, [CallerFilePath] string? testFile = null, [CallerMemberName] string testMethod = null!)
        {
            this.CompilerDiagnostics = CompilerDiagnostics.Warnings;
            this.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
            this.TestState.AdditionalReferences.AddRange(ReferencesHelper.GetReferences(references));

            this.testFile = testFile;
            this.testMethod = testMethod;

#if WRITE_EXPECTED
            TestBehaviors |= TestBehaviors.SkipGeneratedSourcesCheck;
#endif
        }

        public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp7_3;

        public static Task RunDefaultAsync(ITestOutputHelper logger, [StringSyntax("c#-test")] string testSource, AnalyzerOptions? options = null, LanguageVersion languageVersion = LanguageVersion.CSharp7_3, [CallerFilePath] string? testFile = null, [CallerMemberName] string testMethod = null!)
        {
            options ??= new();

            // TODO: throw if these attribute collections are non-empty. Tests should use the attributes themselves.
            string assumedFormattable = string.Join(string.Empty, options.AssumedFormattableTypes.Select(t => $"[assembly: MessagePackAssumedFormattable(typeof({t.Name.GetQualifiedName(Qualifiers.Namespace)}))]" + Environment.NewLine));
            string knownFormatters = string.Join(string.Empty, options.KnownFormatters.Select(t => $"[assembly: MessagePackKnownFormatter(typeof({t.Name.GetQualifiedName(Qualifiers.Namespace)}))]" + Environment.NewLine));

            string resolverPartialClassSource = $$"""
                using MessagePack;

                {{assumedFormattable}}
                {{knownFormatters}}

                namespace {{((NamespaceTypeContainer)options.Generator.Resolver.Name.Container!).Namespace}} {
                    [GeneratedMessagePackResolver(UseMapMode = {{(options.Generator.Formatters.UsesMapMode ? "true" : "false")}})]
                    partial class {{options.Generator.Resolver.Name.Name}} { }
                }
                """;
            return RunDefaultAsync(logger, testSource, resolverPartialClassSource, languageVersion, testFile, testMethod);
        }

        public async Task RunDefaultAsync(ITestOutputHelper logger)
        {
            try
            {
                await this.RunAsync();
            }
            finally
            {
                foreach (var generatedSource in this.TestState.GeneratedSources)
                {
                    logger.WriteLine("--------------------------------------------------------------");
                    logger.WriteLine(generatedSource.filename);
                    logger.WriteLine("--------------------------------------------------------------");
                    int lineNumber = 0;
                    foreach (TextLine line in generatedSource.content.Lines)
                    {
                        logger.WriteLine($"{++lineNumber,6}: {generatedSource.content.GetSubText(line.Span)}");
                    }

                    logger.WriteLine("--------------------------------------------------------------");
                }
            }
        }

        private static Task RunDefaultAsync(ITestOutputHelper logger, [StringSyntax("c#-test")] string testSource, string resolverPartialClassSource, LanguageVersion languageVersion, [CallerFilePath] string? testFile = null, [CallerMemberName] string testMethod = null!)
        {
            Test test = new(testFile: testFile, testMethod: testMethod)
            {
                TestState =
                {
                    Sources = { testSource, resolverPartialClassSource },
                },
                LanguageVersion = languageVersion,
            };

            return test.RunDefaultAsync(logger);
        }

        public Test AddGeneratedSources()
        {
            static void AddGeneratedSources(ProjectState project, string testMethod, bool withPrefix)
            {
                string prefix = withPrefix ? $"{project.Name}." : string.Empty;
                string expectedPrefix = $"{typeof(Test).Assembly.GetName().Name}.Resources.{testMethod}.{prefix}"
                    .Replace(' ', '_')
                    .Replace(',', '_')
                    .Replace('(', '_')
                    .Replace(')', '_');

                foreach (var resourceName in typeof(Test).Assembly.GetManifestResourceNames())
                {
                    if (!resourceName.StartsWith(expectedPrefix))
                    {
                        continue;
                    }

                    using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                    if (resourceStream is null)
                    {
                        throw new InvalidOperationException();
                    }

                    using var reader = new StreamReader(resourceStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
                    var name = resourceName.Substring(expectedPrefix.Length);
                    var code = reader.ReadToEnd();
                    var version = MessagePack.SourceGenerator.ThisAssembly.Version;
                    code = code.Replace(
                        "[assembly: MsgPack::Internal.GeneratedAssemblyMessagePackResolverAttribute(typeof(MessagePack.GeneratedMessagePackResolver), 3, 0)]",
                        $"[assembly: MsgPack::Internal.GeneratedAssemblyMessagePackResolverAttribute(typeof(MessagePack.GeneratedMessagePackResolver), {version.Major}, {version.Minor})]");
                    project.GeneratedSources.Add((typeof(TSourceGenerator), name, code));
                }
            }

            AddGeneratedSources(this.TestState, this.testMethod, this.TestState.AdditionalProjects.Count > 0);
            foreach (ProjectState addlProject in this.TestState.AdditionalProjects.Values)
            {
                AddGeneratedSources(addlProject, this.testMethod, true);
            }

            return this;
        }

        protected override Task RunImplAsync(CancellationToken cancellationToken)
        {
            this.AddGeneratedSources();

            foreach (ProjectState addlProject in this.TestState.AdditionalProjects.Values)
            {
                addlProject.AdditionalReferences.AddRange(this.TestState.AdditionalReferences);
                addlProject.DocumentationMode = DocumentationMode.Parse;
            }

            return base.RunImplAsync(cancellationToken);
        }

        protected override IEnumerable<Type> GetSourceGenerators()
        {
            yield return typeof(TSourceGenerator);
        }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            foreach (Type analyzer in typeof(MsgPack001SpecifyOptionsAnalyzer).Assembly.GetTypes().Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t)))
            {
                yield return (DiagnosticAnalyzer)Activator.CreateInstance(analyzer)!;
            }
        }

        protected override CompilationOptions CreateCompilationOptions()
        {
            var compilationOptions = (CSharpCompilationOptions)base.CreateCompilationOptions();
            return compilationOptions
                .WithAllowUnsafe(false)
                .WithWarningLevel(99)
                .WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItem("CS1591", ReportDiagnostic.Suppress));
        }

        protected override ParseOptions CreateParseOptions()
        {
            return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(this.LanguageVersion);
        }

        protected override async Task<(Compilation, ImmutableArray<Diagnostic>)> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
        {
            string fileNamePrefix = this.TestState.AdditionalProjects.Count > 0 ? $"{project.Name}." : string.Empty;
            var resourceDirectory = Path.Combine(Path.GetDirectoryName(this.testFile)!, "Resources", this.testMethod);

            var (compilation, diagnostics) = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
            var expectedNames = new HashSet<string>();
            foreach (var tree in compilation.SyntaxTrees.Skip(project.DocumentIds.Count))
            {
                WriteTreeToDiskIfNecessary(tree, resourceDirectory, fileNamePrefix);
                expectedNames.Add(Path.GetFileName(tree.FilePath));
            }

            var currentTestPrefix = $"{typeof(Test).Assembly.GetName().Name}.Resources.{this.testMethod}.{fileNamePrefix}";
            foreach (var name in this.GetType().Assembly.GetManifestResourceNames())
            {
                if (!name.StartsWith(currentTestPrefix))
                {
                    continue;
                }

                if (!expectedNames.Contains(name.Substring(currentTestPrefix.Length)))
                {
                    throw new InvalidOperationException($"Unexpected test resource: {name.Substring(currentTestPrefix.Length)}");
                }
            }

            return (compilation, diagnostics);
        }

        [Conditional("WRITE_EXPECTED")]
        private static void WriteTreeToDiskIfNecessary(SyntaxTree tree, string resourceDirectory, string fileNamePrefix)
        {
            if (tree.Encoding is null)
            {
                throw new ArgumentException("Syntax tree encoding was not specified");
            }

            string name = fileNamePrefix + Path.GetFileName(tree.FilePath);
            string filePath = Path.Combine(resourceDirectory, name);
            Directory.CreateDirectory(resourceDirectory);
            File.WriteAllText(filePath, tree.GetText().ToString(), tree.Encoding);
        }
    }
}

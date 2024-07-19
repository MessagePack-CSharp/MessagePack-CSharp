// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

internal static partial class VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix>
{
    internal class Test : VisualBasicCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    {
        internal Test()
        {
            this.ReferenceAssemblies = ReferencesHelper.DefaultTargetFrameworkReferences;
            this.TestBehaviors |= Microsoft.CodeAnalysis.Testing.TestBehaviors.SkipGeneratedCodeCheck;

            this.SolutionTransforms.Add((solution, projectId) =>
            {
                var parseOptions = (VisualBasicParseOptions?)solution.GetProject(projectId)?.ParseOptions;
                Assert.NotNull(parseOptions);
                solution = solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion.VisualBasic15_5));

                return solution;
            });

            this.TestState.AdditionalFilesFactories.Add(() =>
            {
                const string additionalFilePrefix = "AdditionalFiles.";
                return from resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames()
                       where resourceName.StartsWith(additionalFilePrefix, StringComparison.Ordinal)
                       let content = ReadManifestResource(Assembly.GetExecutingAssembly(), resourceName)
                       select (filename: resourceName.Substring(additionalFilePrefix.Length), SourceText.From(content));
            });
        }

        internal ReferencesSet MessagePackReferences { get; set; } = ReferencesSet.MessagePack;

        internal DiagnosticDescriptor? DefaultDiagnosticDescriptor { get; set; }

#pragma warning disable SA1316 // https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3781
        protected override async Task<(Compilation compilation, ImmutableArray<Diagnostic> generatorDiagnostics)> GetProjectCompilationAsync(Project project, IVerifier verifier, CancellationToken cancellationToken)
#pragma warning restore SA1316 // https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3781
        {
            var (compilation, generatorDiagnostics) = await base.GetProjectCompilationAsync(project, verifier, cancellationToken);
            compilation = ReferencesHelper.AddMessagePackReferences(compilation, this.MessagePackReferences);
            return (compilation, generatorDiagnostics);
        }

        protected override DiagnosticDescriptor? GetDefaultDiagnostic(DiagnosticAnalyzer[] analyzers) => this.DefaultDiagnosticDescriptor ?? base.GetDefaultDiagnostic(analyzers);

        private static string ReadManifestResource(Assembly assembly, string resourceName)
        {
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName) ?? throw new ArgumentException("No such resource stream", nameof(resourceName))))
            {
                return reader.ReadToEnd();
            }
        }
    }
}

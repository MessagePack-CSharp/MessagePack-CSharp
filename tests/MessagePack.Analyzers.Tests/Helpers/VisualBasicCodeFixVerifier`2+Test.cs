// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Testing;
using Xunit;

public static partial class VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix>
{
    public class Test : VisualBasicCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
    {
        public Test()
        {
            this.ReferenceAssemblies = ReferencesHelper.DefaultReferences;
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

        public DiagnosticDescriptor? DefaultDiagnosticDescriptor { get; set; }

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

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using Xunit;

public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
{
    public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
    {
        public Test()
        {
            this.ReferenceAssemblies = ReferencesHelper.DefaultReferences;
            this.CompilerDiagnostics = Microsoft.CodeAnalysis.Testing.CompilerDiagnostics.Warnings;

            this.TestState.AdditionalFilesFactories.Add(() =>
            {
                const string additionalFilePrefix = "AdditionalFiles.";
                return from resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames()
                       where resourceName.StartsWith(additionalFilePrefix, StringComparison.Ordinal)
                       let content = ReadManifestResource(Assembly.GetExecutingAssembly(), resourceName)
                       select (filename: resourceName.Substring(additionalFilePrefix.Length), SourceText.From(content));
            });
        }

        protected override ParseOptions CreateParseOptions()
        {
            return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion.CSharp10);
        }

        protected override CompilationOptions CreateCompilationOptions()
        {
            var compilationOptions = (CSharpCompilationOptions)base.CreateCompilationOptions();
            return compilationOptions
                .WithWarningLevel(99)
                .WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItem("CS1591", ReportDiagnostic.Suppress));
        }

        private static string ReadManifestResource(Assembly assembly, string resourceName)
        {
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName) ?? throw new ArgumentException("No such resource stream", nameof(resourceName))))
            {
                return reader.ReadToEnd();
            }
        }
    }
}

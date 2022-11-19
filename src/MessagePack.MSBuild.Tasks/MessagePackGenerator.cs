// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessagePackCompiler;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

// synchronous blocks aren't a problem in MSBuild tasks
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits

namespace MessagePack.MSBuild.Tasks
{
    public class MessagePackGenerator : Microsoft.Build.Utilities.Task, ICancelableTask
    {
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        [Required]
        public ITaskItem[] Compile { get; set; } = null!;

        [Required]
        public string GeneratedOutputPath { get; set; } = null!;

        [Required]
        public ITaskItem[] ReferencePath { get; set; } = null!;

        public string? DefineConstants { get; set; }

        [Required]
        public string ResolverName { get; set; } = null!;

        public string? Namespace { get; set; }

        public bool UseMapMode { get; set; }

        public string[]? ExternalIgnoreTypeNames { get; set; }

        internal CancellationToken CancellationToken => this.cts.Token;

        public void Cancel() => this.cts.Cancel();

        public override bool Execute()
        {
            if (string.IsNullOrWhiteSpace(this.ResolverName))
            {
                this.Log.LogError($"{nameof(ResolverName)} task parameter must not be set to an empty value.");
                return false;
            }

            try
            {
                var compilation = this.CreateCompilation();

                var generator = new CodeGenerator(x => this.Log.LogMessage(x), CancellationToken.None);
                generator.GenerateFileAsync(
                    compilation,
                    this.GeneratedOutputPath,
                    ResolverName,
                    Namespace,
                    UseMapMode,
                    null,
                    ExternalIgnoreTypeNames).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                this.Log.LogErrorFromException(ex, true);
                return false;
            }

            return true;
        }

        private Compilation CreateCompilation()
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Regular, this.DefineConstants?.Split(';', ','));
            var syntaxTrees = new List<SyntaxTree>(this.Compile.Length);
            foreach (var path in this.Compile)
            {
                string fullPath = path.GetMetadata("FullPath");

                if (string.Equals(fullPath, Path.GetFullPath(this.GeneratedOutputPath), StringComparison.OrdinalIgnoreCase))
                {
                    // Do not include a stale version of the file we are to generate in the compilation.
                    continue;
                }

                using var compile = File.OpenRead(path.ItemSpec);
                var sourceText = SourceText.From(compile);
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(sourceText, parseOptions, fullPath, cancellationToken: this.CancellationToken));
            }

            var references =
                from referencePath in this.ReferencePath
                select MetadataReference.CreateFromFile(referencePath.ItemSpec);

            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(
                "MsgPackTempProj",
                syntaxTrees,
                references,
                options);
            return compilation;
        }
    }
}

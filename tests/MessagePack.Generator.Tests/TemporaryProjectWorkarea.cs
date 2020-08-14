// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MessagePack.Formatters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MessagePack.Generator.Tests
{
    /// <summary>
    /// Provides a temporary work area for unit testing.
    /// </summary>
    public class TemporaryProjectWorkarea : IDisposable
    {
        private readonly string tempDirPath;
        private readonly string csprojFileName = "TempProject.csproj";
        private readonly bool cleanOnDisposing;

        public string CsProjectPath { get; }

        public string ProjectDirectory { get; }

        public string OutputDirectory { get; }

        public static TemporaryProjectWorkarea Create(bool cleanOnDisposing = true)
        {
            return new TemporaryProjectWorkarea(cleanOnDisposing);
        }

        private TemporaryProjectWorkarea(bool cleanOnDisposing)
        {
            this.cleanOnDisposing = cleanOnDisposing;
            this.tempDirPath = Path.Combine(Path.GetTempPath(), $"MessagePack.Generator.Tests-{Guid.NewGuid()}");

            ProjectDirectory = Path.Combine(tempDirPath, "Project");
            OutputDirectory = Path.Combine(tempDirPath, "Output");

            Directory.CreateDirectory(ProjectDirectory);
            Directory.CreateDirectory(OutputDirectory);

            var solutionRootDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../.."));
            var messagePackProjectDir = Path.Combine(solutionRootDir, "src/MessagePack/MessagePack.csproj");
            var annotationsProjectDir = Path.Combine(solutionRootDir, "src/MessagePack.Annotations/MessagePack.Annotations.csproj");

            CsProjectPath = Path.Combine(ProjectDirectory, csprojFileName);
            var csprojContents = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""" + messagePackProjectDir + @""" />
    <ProjectReference Include=""" + annotationsProjectDir + @""" />
  </ItemGroup>
</Project>
";
            AddFileToProject(csprojFileName, csprojContents);
        }

        public void AddFileToProject(string fileName, string contents)
        {
            File.WriteAllText(Path.Combine(ProjectDirectory, fileName), contents.Trim());
        }

        public CompilationContainer GetOutputCompilation()
        {
            var compilation = CSharpCompilation.Create(Guid.NewGuid().ToString())
                .AddSyntaxTrees(
                    Directory.EnumerateFiles(ProjectDirectory, "*.cs", SearchOption.AllDirectories)
                        .Concat(Directory.EnumerateFiles(OutputDirectory, "*.cs", SearchOption.AllDirectories))
                        .Select(x => CSharpSyntaxTree.ParseText(File.ReadAllText(x), CSharpParseOptions.Default, x)))
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(MessagePack.MessagePackObjectAttribute).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(IMessagePackFormatter<>).Assembly.Location));

            return new CompilationContainer(compilation);
        }

        public void Dispose()
        {
            if (cleanOnDisposing)
            {
                Directory.Delete(tempDirPath, true);
            }
        }
    }

    public class CompilationContainer
    {
        public Compilation Compilation { get; }

        public CompilationContainer(Compilation compilation)
        {
            this.Compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
        }

        public INamedTypeSymbol[] GetNamedTypeSymbolsFromGenerated()
        {
            return Compilation.SyntaxTrees
                .Select(x => Compilation.GetSemanticModel(x))
                .SelectMany(semanticModel =>
                {
                    return semanticModel.SyntaxTree.GetRoot()
                        .DescendantNodes()
                        .Select(x => semanticModel.GetDeclaredSymbol(x))
                        .OfType<INamedTypeSymbol>();
                })
                .ToArray();
        }
    }
}

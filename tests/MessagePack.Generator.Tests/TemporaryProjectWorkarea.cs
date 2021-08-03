// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using MessagePack.Formatters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nerdbank.Streams;

namespace MessagePack.Generator.Tests
{
    /// <summary>
    /// Provides a temporary work area for unit testing.
    /// </summary>
    public class TemporaryProjectWorkarea : IDisposable
    {
        private readonly string tempDirPath;
        private readonly string targetCsprojFileName = "TempTargetProject.csproj";
        private readonly string referencedCsprojFileName = "TempReferencedProject.csproj";
        private readonly bool cleanOnDisposing;

        /// <summary>
        /// Gets the identifier of the workarea.
        /// </summary>
        public Guid WorkareaId { get; }

        /// <summary>
        /// Gets Generator target csproj file path.
        /// </summary>
        public string TargetCsProjectPath { get; }

        /// <summary>
        /// Gets csproj file path Referenced from TargetProject.
        /// </summary>
        public string ReferencedCsProjectPath { get; }

        public string TargetProjectDirectory { get; }

        public string ReferencedProjectDirectory { get; }

        public string OutputDirectory { get; }

        public static TemporaryProjectWorkarea Create(bool cleanOnDisposing = true)
        {
            return new TemporaryProjectWorkarea(cleanOnDisposing);
        }

        private TemporaryProjectWorkarea(bool cleanOnDisposing)
        {
            WorkareaId = Guid.NewGuid();
            this.cleanOnDisposing = cleanOnDisposing;
            this.tempDirPath = Path.Combine(Path.GetTempPath(), $"MessagePack.Generator.Tests-{WorkareaId}");

            TargetProjectDirectory = Path.Combine(tempDirPath, "TargetProject");
            ReferencedProjectDirectory = Path.Combine(tempDirPath, "ReferencedProject");
            OutputDirectory = Path.Combine(tempDirPath, "Output");

            Directory.CreateDirectory(TargetProjectDirectory);
            Directory.CreateDirectory(ReferencedProjectDirectory);
            Directory.CreateDirectory(OutputDirectory);
            Directory.CreateDirectory(Path.Combine(OutputDirectory, "bin"));

            var solutionRootDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../.."));
            var messagePackProjectDir = Path.Combine(solutionRootDir, "src/MessagePack/MessagePack.csproj");
            var annotationsProjectDir = Path.Combine(solutionRootDir, "src/MessagePack.Annotations/MessagePack.Annotations.csproj");

            ReferencedCsProjectPath = Path.Combine(ReferencedProjectDirectory, referencedCsprojFileName);
            var referencedCsprojContents = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""" + annotationsProjectDir + @""" />
  </ItemGroup>
</Project>
";
            AddFileToReferencedProject(referencedCsprojFileName, referencedCsprojContents);

            TargetCsProjectPath = Path.Combine(TargetProjectDirectory, targetCsprojFileName);
            var csprojContents = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""" + messagePackProjectDir + @""" />
    <ProjectReference Include=""" + annotationsProjectDir + @""" />
    <ProjectReference Include=""" + ReferencedCsProjectPath + @""" />
  </ItemGroup>
</Project>
";
            AddFileToTargetProject(targetCsprojFileName, csprojContents);
        }

        /// <summary>
        /// Add file to Generator target project.
        /// </summary>
        public void AddFileToTargetProject(string fileName, string contents)
        {
            File.WriteAllText(Path.Combine(TargetProjectDirectory, fileName), contents.Trim());
        }

        /// <summary>
        /// Add file to project, referenced by Generator target project.
        /// </summary>
        public void AddFileToReferencedProject(string fileName, string contents)
        {
            File.WriteAllText(Path.Combine(ReferencedProjectDirectory, fileName), contents.Trim());
        }

        public OutputCompilation GetOutputCompilation()
        {
            var refAsmDir = Path.GetDirectoryName(typeof(object).Assembly.Location);

            var referenceCompilation = CSharpCompilation.Create(Guid.NewGuid().ToString())
                .AddSyntaxTrees(
                    Directory.EnumerateFiles(ReferencedProjectDirectory, "*.cs", SearchOption.AllDirectories)
                        .Select(x => CSharpSyntaxTree.ParseText(File.ReadAllText(x), CSharpParseOptions.Default, x)))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Private.CoreLib.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Runtime.Extensions.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Collections.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Linq.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Console.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Runtime.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Memory.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "netstandard.dll")))
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(MessagePack.MessagePackObjectAttribute).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(IMessagePackFormatter<>).Assembly.Location))
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var compilation = CSharpCompilation.Create(Guid.NewGuid().ToString())
                .AddSyntaxTrees(
                    Directory.EnumerateFiles(TargetProjectDirectory, "*.cs", SearchOption.AllDirectories)
                        .Concat(Directory.EnumerateFiles(OutputDirectory, "*.cs", SearchOption.AllDirectories))
                        .Select(x => CSharpSyntaxTree.ParseText(File.ReadAllText(x), CSharpParseOptions.Default, x)))
                .AddReferences(referenceCompilation.ToMetadataReference())
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Private.CoreLib.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Runtime.Extensions.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Collections.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Linq.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Console.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Runtime.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "System.Memory.dll")))
                .AddReferences(MetadataReference.CreateFromFile(Path.Combine(refAsmDir, "netstandard.dll")))
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(MessagePack.MessagePackObjectAttribute).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(IMessagePackFormatter<>).Assembly.Location))
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return new OutputCompilation(this, compilation);
        }

        public void Dispose()
        {
            if (cleanOnDisposing)
            {
                Directory.Delete(tempDirPath, true);
            }
        }
    }

    public class OutputCompilation
    {
        private readonly TemporaryProjectWorkarea _workarea;

        public Compilation Compilation { get; }

        public OutputCompilation(TemporaryProjectWorkarea workarea, Compilation compilation)
        {
            this._workarea = workarea;
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

        public IReadOnlyList<string> GetResolverKnownFormatterTypes()
        {
            return Compilation.SyntaxTrees
                .SelectMany(x => x.GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(x => x.Identifier.ToString().EndsWith("ResolverGetFormatterHelper"))
                    .SelectMany(x => x.DescendantNodes())
                    .OfType<MethodDeclarationSyntax>()
                    .Where(x => x.Identifier.ToString() == "GetFormatter")
                    .SelectMany(x => x.DescendantNodes())
                    .OfType<SwitchSectionSyntax>()
                    .SelectMany(x => x.DescendantNodes())
                    .OfType<ObjectCreationExpressionSyntax>()
                    .SelectMany(x => x.ChildNodes())
                    .Where(x => x is QualifiedNameSyntax || x is IdentifierNameSyntax || x is GenericNameSyntax || x is PredefinedTypeSyntax)
                    .Select(x => x.ToString()))
                .ToArray();
        }

        /// <summary>
        /// Load the generated assembly and execute the code in that context.
        /// </summary>
        public void ExecuteWithGeneratedAssembly(Action<AssemblyLoadContext, Assembly> action)
        {
            var memoryStream = new MemoryStream();
            Compilation.Emit(memoryStream);
            memoryStream.Position = 0;

            var assemblyLoadContext = new AssemblyLoadContext($"TempProject-{_workarea.WorkareaId}", isCollectible: true);
            try
            {
                assemblyLoadContext.LoadFromStream(memoryStream);
                var assembly = assemblyLoadContext.Assemblies.First();
                action(assemblyLoadContext, assembly);
            }
            finally
            {
                assemblyLoadContext.Unload();
            }
        }
    }
}

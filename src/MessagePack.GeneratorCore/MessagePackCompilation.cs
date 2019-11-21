// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.FileSystemGlobbing;

namespace MessagePackCompiler
{
    public class MessagePackCompilation
    {
        public static async Task<CSharpCompilation> CreateFromProjectAsync(string[] csprojs, string[] preprocessorSymbols, CancellationToken cancellationToken)
        {
            var parseOption = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Regular, preprocessorSymbols);
            var syntaxTrees = new List<SyntaxTree>();
            var metadata = new[]
                {
                    typeof(object),
                    typeof(Enumerable),
                    typeof(Task<>),
                    typeof(IgnoreDataMemberAttribute),
                    typeof(System.Collections.Generic.List<>),
                    typeof(System.Collections.Concurrent.ConcurrentDictionary<,>),
                }
               .Select(x => x.Assembly.Location)
               .Distinct()
               .Select(x => MetadataReference.CreateFromFile(x))
               .ToList();

            var sources = new HashSet<string>();
            var locations = new List<string>();
            foreach (var csproj in csprojs)
            {
                CollectDocument(csproj, sources, locations);
            }

            var hasAnnotations = false;
            foreach (var file in sources.Select(Path.GetFullPath).Distinct())
            {
                var text = File.ReadAllText(file.Replace('\\', Path.DirectorySeparatorChar), Encoding.UTF8);
                var syntax = CSharpSyntaxTree.ParseText(text, parseOption);
                syntaxTrees.Add(syntax);
                if (Path.GetFileNameWithoutExtension(file) == "Attributes")
                {
                    var root = await syntax.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    if (root.DescendantNodes().OfType<ClassDeclarationSyntax>().Any(x => x.Identifier.Text == "MessagePackObjectAttribute"))
                    {
                        hasAnnotations = true;
                    }
                }
            }

            if (!hasAnnotations)
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(DummyAnnotation, parseOption));
            }

            var nazoloc = locations.Distinct();
            foreach (var item in locations.Distinct().Where(x => !x.Contains("MonoBleedingEdge")))
            {
                metadata.Add(MetadataReference.CreateFromFile(item));
            }

            var compilation = CSharpCompilation.Create(
                "MessagepackCodeGenTemp",
                syntaxTrees,
                metadata,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

            return compilation;
        }

        private static void CollectDocument(string csproj, HashSet<string> source, List<string> metadataLocations)
        {
            XDocument document;
            using (var sr = new StreamReader(csproj, true))
            {
                var reader = new XmlTextReader(sr);
                reader.Namespaces = false;

                document = XDocument.Load(reader, LoadOptions.None);
            }

            var csProjRoot = Path.GetDirectoryName(csproj);
            var framworkRoot = Path.GetDirectoryName(typeof(object).Assembly.Location);

            // Legacy
            // <Project ToolsVersion=...>
            // New
            // <Project Sdk="Microsoft.NET.Sdk">
            var proj = document.Element("Project");
            var legacyFormat = proj.Attribute("Sdk")?.Value != "Microsoft.NET.Sdk";

            if (!legacyFormat)
            {
                // try to find EnableDefaultCompileItems
                if (document.Descendants("EnableDefaultCompileItems")?.FirstOrDefault()?.Value == "false"
                 || document.Descendants("EnableDefaultItems")?.FirstOrDefault()?.Value == "false")
                {
                    legacyFormat = true;
                }
            }

            {
                // compile files
                {
                    // default include
                    if (!legacyFormat)
                    {
                        foreach (var path in GetCompileFullPaths(null, "**/*.cs", csProjRoot))
                        {
                            source.Add(path);
                        }
                    }

                    // custom elements
                    foreach (var item in document.Descendants("Compile"))
                    {
                        var include = item.Attribute("Include")?.Value;
                        if (include != null)
                        {
                            foreach (var path in GetCompileFullPaths(item, include, csProjRoot))
                            {
                                source.Add(path);
                            }
                        }

                        var remove = item.Attribute("Remove")?.Value;
                        if (remove != null)
                        {
                            foreach (var path in GetCompileFullPaths(item, remove, csProjRoot))
                            {
                                source.Remove(path);
                            }
                        }
                    }

                    // default remove
                    if (!legacyFormat)
                    {
                        foreach (var path in GetCompileFullPaths(null, "./bin/**;./obj/**", csProjRoot))
                        {
                            source.Remove(path);
                        }
                    }
                }

                // shared
                foreach (var item in document.Descendants("Import"))
                {
                    if (item.Attribute("Label")?.Value == "Shared")
                    {
                        var sharedRoot = Path.GetDirectoryName(Path.Combine(csProjRoot, item.Attribute("Project").Value));
                        foreach (var file in IterateCsFileWithoutBinObj(Path.GetDirectoryName(sharedRoot)))
                        {
                            source.Add(file);
                        }
                    }
                }

                // proj-ref
                foreach (var item in document.Descendants("ProjectReference"))
                {
                    var refCsProjPath = item.Attribute("Include")?.Value;
                    if (refCsProjPath != null)
                    {
                        CollectDocument(Path.Combine(csProjRoot, refCsProjPath), source, metadataLocations);
                    }
                }

                // metadata
                foreach (var item in document.Descendants("Reference"))
                {
                    var hintPath = item.Element("HintPath")?.Value;
                    if (hintPath == null)
                    {
                        var path = Path.Combine(framworkRoot, item.Attribute("Include").Value + ".dll");
                        metadataLocations.Add(path);
                    }
                    else
                    {
                        metadataLocations.Add(Path.Combine(csProjRoot, hintPath));
                    }
                }

                // resolve NuGet reference
                foreach (var item in document.Descendants("PackageReference"))
                {
                    var nugetPackagesPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
                    if (nugetPackagesPath == null)
                    {
                        // Try default
                        // Windows: %userprofile%\.nuget\packages
                        // Mac/Linux: ~/.nuget/packages
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            nugetPackagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".nuget\packages");
                        }
                        else
                        {
                            nugetPackagesPath = "~/nuget/packages";
                        }
                    }

                    var targetFramework = document.Descendants("TargetFramework").FirstOrDefault()?.Value ?? document.Descendants("TargetFrameworks").First().Value.Split(';').First();
                    var includePath = item.Attribute("Include").Value.Trim().ToLower(); // maybe lower
                    var packageVersion = item.Attribute("Version").Value.Trim();

                    var pathpath = Path.Combine(nugetPackagesPath, includePath, packageVersion, "lib", targetFramework);
                    if (!Directory.Exists(pathpath))
                    {
                        pathpath = pathpath.ToLower(); // try all lower.
                    }

                    if (Directory.Exists(pathpath))
                    {
                        foreach (var dllPath in Directory.GetFiles(pathpath, "*.dll"))
                        {
                            metadataLocations.Add(Path.GetFullPath(dllPath));
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> GetCompileFullPaths(XElement compile, string includeOrRemovePattern, string csProjRoot)
        {
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddIncludePatterns(includeOrRemovePattern.Split(';'));
            var exclude = compile?.Attribute("Exclude")?.Value;
            if (exclude != null)
            {
                matcher.AddExcludePatterns(exclude.Split(';'));
            }

            foreach (var path in matcher.GetResultsInFullPath(csProjRoot))
            {
                yield return path;
            }
        }

        public static async Task<CSharpCompilation> CreateFromDirectoryAsync(string directoryRoot, string[] preprocessorSymbols, CancellationToken cancellationToken)
        {
            var parseOption = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Regular, preprocessorSymbols);

            var hasAnnotations = false;
            var syntaxTrees = new List<SyntaxTree>();
            foreach (var file in IterateCsFileWithoutBinObj(directoryRoot))
            {
                var text = File.ReadAllText(file.Replace('\\', Path.DirectorySeparatorChar), Encoding.UTF8);
                var syntax = CSharpSyntaxTree.ParseText(text, parseOption);
                syntaxTrees.Add(syntax);
                if (Path.GetFileNameWithoutExtension(file) == "Attributes")
                {
                    var root = await syntax.GetRootAsync(cancellationToken).ConfigureAwait(false);
                    if (root.DescendantNodes().OfType<ClassDeclarationSyntax>().Any(x => x.Identifier.Text == "MessagePackObjectAttribute"))
                    {
                        hasAnnotations = true;
                    }
                }
            }

            if (!hasAnnotations)
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(DummyAnnotation, parseOption));
            }

            var metadata = new[]
                {
                    typeof(object),
                    typeof(Enumerable),
                    typeof(Task<>),
                    typeof(IgnoreDataMemberAttribute),
                    typeof(System.Collections.Generic.List<>),
                    typeof(System.Collections.Concurrent.ConcurrentDictionary<,>),
                }
                .Select(x => x.Assembly.Location)
                .Distinct()
                .Select(x => MetadataReference.CreateFromFile(x))
                .ToList();

            var compilation = CSharpCompilation.Create(
                "MessagepackCodeGenTemp",
                syntaxTrees,
                metadata,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

            return compilation;
        }

        private static IEnumerable<string> IterateCsFileWithoutBinObj(string root)
        {
            foreach (var item in Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly))
            {
                yield return item;
            }

            foreach (var dir in Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly))
            {
                var dirName = new DirectoryInfo(dir).Name;
                if (dirName == "bin" || dirName == "obj")
                {
                    continue;
                }

                foreach (var item in IterateCsFileWithoutBinObj(dir))
                {
                    yield return item;
                }
            }
        }

        private const string DummyAnnotation = @"
using System;

namespace MessagePack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class MessagePackObjectAttribute : Attribute
    {
        public bool KeyAsPropertyName { get; private set; }

        public MessagePackObjectAttribute(bool keyAsPropertyName = false)
        {
            this.KeyAsPropertyName = keyAsPropertyName;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class KeyAttribute : Attribute
    {
        public int? IntKey { get; private set; }
        public string StringKey { get; private set; }

        public KeyAttribute(int x)
        {
            this.IntKey = x;
        }

        public KeyAttribute(string x)
        {
            this.StringKey = x;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class IgnoreMemberAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class UnionAttribute : Attribute
    {
        public int Key { get; private set; }
        public Type SubType { get; private set; }

        public UnionAttribute(int key, Type subType)
        {
            this.Key = key;
            this.SubType = subType;
        }
    }

    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = true)]
    public class SerializationConstructorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class MessagePackFormatterAttribute : Attribute
    {
        public Type FormatterType { get; private set; }
        public object[] Arguments { get; private set; }

        public MessagePackFormatterAttribute(Type formatterType)
        {
            this.FormatterType = formatterType;
        }

        public MessagePackFormatterAttribute(Type formatterType, params object[] arguments)
        {
            this.FormatterType = formatterType;
            this.Arguments = arguments;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack
{
    public interface IMessagePackSerializationCallbackReceiver
    {
        void OnBeforeSerialize();
        void OnAfterDeserialize();
    }
}
";
    }
}

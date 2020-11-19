// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.Generator
{
    internal static class PseudoCompilation
    {
        internal static async Task<CSharpCompilation> CreateFromDirectoryAsync(string directoryRoot, IEnumerable<string>? preprocessorSymbols, CancellationToken cancellationToken)
        {
            var parseOption = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Regular, CleanPreprocessorSymbols(preprocessorSymbols));

            var syntaxTrees = new List<SyntaxTree>();
            var hasAnnotations = false;
            foreach (var file in IterateCsFileWithoutBinObj(directoryRoot))
            {
                var text = File.ReadAllText(NormalizeDirectorySeparators(file), Encoding.UTF8);
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

            var metadata = GetStandardReferences().Select(x => MetadataReference.CreateFromFile(x)).ToArray();

            var compilation = CSharpCompilation.Create(
                "CodeGenTemp",
                syntaxTrees,
                DistinctReference(metadata),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

            return compilation;
        }

        private static IEnumerable<MetadataReference> DistinctReference(IEnumerable<MetadataReference> metadataReferences)
        {
            var set = new HashSet<string>();
            foreach (var item in metadataReferences)
            {
                if (item.Display is object && set.Add(Path.GetFileName(item.Display)))
                {
                    yield return item;
                }
            }
        }

        private static List<string> GetStandardReferences()
        {
            var standardMetadataType = new[]
            {
                typeof(object),
                typeof(Attribute),
                typeof(Enumerable),
                typeof(Task<>),
                typeof(IgnoreDataMemberAttribute),
                typeof(System.Collections.Hashtable),
                typeof(System.Collections.Generic.List<>),
                typeof(System.Collections.Generic.HashSet<>),
                typeof(System.Collections.Immutable.IImmutableList<>),
                typeof(System.Linq.ILookup<,>),
                typeof(System.Tuple<>),
                typeof(System.ValueTuple<>),
                typeof(System.Collections.Concurrent.ConcurrentDictionary<,>),
                typeof(System.Collections.ObjectModel.ObservableCollection<>),
            };

            var metadata = standardMetadataType
               .Select(x => x.Assembly.Location)
               .Distinct()
               .ToList();

            var dir = new FileInfo(typeof(object).Assembly.Location).Directory ?? throw new NullReferenceException("Assembly location directory not found!");
            {
                var path = Path.Combine(dir.FullName, "netstandard.dll");
                if (File.Exists(path))
                {
                    metadata.Add(path);
                }
            }

            {
                var path = Path.Combine(dir.FullName, "System.Runtime.dll");
                if (File.Exists(path))
                {
                    metadata.Add(path);
                }
            }

            return metadata;
        }

        private static IEnumerable<string>? CleanPreprocessorSymbols(IEnumerable<string>? preprocessorSymbols)
        {
            return preprocessorSymbols?.Where(x => !string.IsNullOrWhiteSpace(x));
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

        private static string NormalizeDirectorySeparators(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
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

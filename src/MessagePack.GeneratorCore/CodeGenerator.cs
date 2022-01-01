// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Text;
using MessagePackCompiler.CodeAnalysis;
using MessagePackCompiler.Generator;
using Microsoft.CodeAnalysis;
using StringLiteral;

namespace MessagePackCompiler
{
    public partial class CodeGenerator
    {
        private readonly Action<string> logger;

        public CodeGenerator(Action<string> logger, CancellationToken cancellationToken)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Generates the specialized resolver and formatters for the types that require serialization in a given compilation.
        /// </summary>
        /// <param name="compilation">The compilation to read types from as an input to code generation.</param>
        /// <param name="output">The name of the generated source file.</param>
        /// <param name="resolverName">The resolver name.</param>
        /// <param name="namespace">The namespace for the generated type to be created in. May be null.</param>
        /// <param name="useMapMode">A boolean value that indicates whether all formatters should use property maps instead of more compact arrays.</param>
        /// <param name="multipleIfDirectiveOutputSymbols">A comma-delimited list of symbols that should surround redundant generated files. May be null.</param>
        /// <param name="externalIgnoreTypeNames"> May be null.</param>
        /// <returns>A task that indicates when generation has completed.</returns>
        public async Task GenerateFileAsync(
           Compilation compilation,
           string output,
           string resolverName,
           string? @namespace,
           bool useMapMode,
           string? multipleIfDirectiveOutputSymbols,
           string[]? externalIgnoreTypeNames)
        {
            var namespaceDot = string.IsNullOrWhiteSpace(@namespace) ? string.Empty : @namespace + ".";
            var multipleOutputSymbols = multipleIfDirectiveOutputSymbols?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

            var sw = Stopwatch.StartNew();

            logger("Project Compilation Start:" + compilation.AssemblyName);

            var collector = new TypeCollector(compilation, true, useMapMode, externalIgnoreTypeNames, Console.WriteLine);

            logger("Project Compilation Complete:" + sw.Elapsed.ToString());

            sw.Restart();
            logger("Method Collect Start");

            var (objectInfo, enumInfo, genericInfo, unionInfo) = collector.Collect();

            logger("Method Collect Complete:" + sw.Elapsed.ToString());

            logger("Output Generation Start");
            sw.Restart();
            var isSingleFileOutput = Path.GetExtension(output) == ".cs";
            if (isSingleFileOutput)
            {
                var builder = ZString.CreateUtf8StringBuilder();
                try
                {
                    if (multipleOutputSymbols.Length == 0)
                    {
                        GenerateSingleFileSync(ref builder, resolverName, namespaceDot, objectInfo, enumInfo, unionInfo, genericInfo);
                        using var stream = new FileStream(PreparePath(output), FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                        await builder.WriteToAsync(stream).ConfigureAwait(false);
                    }
                    else
                    {
                        var innerBuilder = ZString.CreateUtf8StringBuilder();
                        try
                        {
                            GenerateSingleFileSync(ref builder, resolverName, namespaceDot, objectInfo, enumInfo, unionInfo, genericInfo);
                            foreach (var multiOutputSymbol in multipleOutputSymbols)
                            {
                                var fname = Path.GetFileNameWithoutExtension(output) + "." + MultiSymbolToSafeFilePath(multiOutputSymbol) + ".cs";
                                fname = PreparePath(Path.Combine(Path.GetDirectoryName(output) ?? string.Empty, fname));
                                using var stream = new FileStream(PreparePath(fname), FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                                innerBuilder.Clear();
                                static void Append(ref Utf8ValueStringBuilder builder, ReadOnlySpan<byte> span)
                                {
                                    span.CopyTo(builder.GetSpan(span.Length));
                                    builder.Advance(span.Length);
                                }

                                Append(ref builder, GetSharpIf());
                                innerBuilder.AppendLine(multiOutputSymbol);
                                await innerBuilder.WriteToAsync(stream).ConfigureAwait(false);
                                await builder.WriteToAsync(stream).ConfigureAwait(false);
                                innerBuilder.Clear();
                                innerBuilder.AppendLine();
                                Append(ref builder, GetSharpEndIf());
                                await innerBuilder.WriteToAsync(stream).ConfigureAwait(false);
                            }
                        }
                        finally
                        {
                            innerBuilder.Dispose();
                        }
                    }
                }
                finally
                {
                    builder.Dispose();
                }
            }
            else
            {
                if (multipleOutputSymbols.Length == 0)
                {
                    await GenerateMultipleFileAsync(output, resolverName, namespaceDot, objectInfo, enumInfo, unionInfo, genericInfo);
                }
                else
                {
                    await GenerateMultipleFileAsync(output, resolverName, namespaceDot, multipleOutputSymbols, objectInfo, enumInfo, unionInfo, genericInfo);
                }
            }

            if (objectInfo.Length == 0 && enumInfo.Length == 0 && genericInfo.Length == 0 && unionInfo.Length == 0)
            {
                logger("Generated result is empty, unexpected result?");
            }

            logger("Output Generation Complete:" + sw.Elapsed.ToString());
        }

        /// <summary>
        /// Generates the specialized resolver and formatters for the types that require serialization in a given compilation.
        /// </summary>
        /// <param name="resolverName">The resolver name.</param>
        /// <param name="namespaceDot">The namespace for the generated type to be created in.</param>
        /// <param name="objectInfo">The ObjectSerializationInfo array which TypeCollector.Collect returns.</param>
        /// <param name="enumInfo">The EnumSerializationInfo array which TypeCollector.Collect returns.</param>
        /// <param name="unionInfo">The UnionSerializationInfo array which TypeCollector.Collect returns.</param>
        /// <param name="genericInfo">The GenericSerializationInfo array which TypeCollector.Collect returns.</param>
        public static string GenerateSingleFileSync(string resolverName, string namespaceDot, ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, UnionSerializationInfo[] unionInfo, GenericSerializationInfo[] genericInfo)
        {
            var objectFormatterTemplates = objectInfo
                .GroupBy(x => (x.Namespace, x.IsStringKey))
                .Select(x =>
                {
                    var (nameSpace, isStringKey) = x.Key;
                    var objectSerializationInfos = x.ToArray();
                    var ns = namespaceDot + "Formatters" + (nameSpace is null ? string.Empty : "." + nameSpace);
                    var template = isStringKey ? new StringKeyFormatterTemplate(ns, objectSerializationInfos) : (IFormatterTemplate)new FormatterTemplate(ns, objectSerializationInfos);
                    return template;
                })
                .ToArray();

            string GetNamespace<T>(IGrouping<string?, T> x)
            {
                if (x.Key == null)
                {
                    return namespaceDot + "Formatters";
                }

                return namespaceDot + "Formatters." + x.Key;
            }

            var enumFormatterTemplates = enumInfo
                .GroupBy(x => x.Namespace)
                .Select(x => new EnumTemplate(GetNamespace(x), x.ToArray()))
                .ToArray();

            var unionFormatterTemplates = unionInfo
                .GroupBy(x => x.Namespace)
                .Select(x => new UnionTemplate(GetNamespace(x), x.ToArray()))
                .ToArray();

            var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray());

            var sb = ZString.CreateUtf8StringBuilder();
            resolverTemplate.TransformAppend(ref sb);
            sb.AppendLine();
            sb.AppendLine();
            foreach (var item in enumFormatterTemplates)
            {
                item.TransformAppend(ref sb);
                sb.AppendLine();
            }

            sb.AppendLine();
            foreach (var item in unionFormatterTemplates)
            {
                item.TransformAppend(ref sb);
                sb.AppendLine();
            }

            sb.AppendLine();
            foreach (var item in objectFormatterTemplates)
            {
                item.TransformAppend(ref sb);
                sb.AppendLine();
            }

            var answer = sb.ToString();
            sb.Dispose();
            return answer;
        }

        [Utf8("#if ")]
        private static partial ReadOnlySpan<byte> GetSharpIf();

        [Utf8("#endif")]
        private static partial ReadOnlySpan<byte> GetSharpEndIf();

        public static void GenerateSingleFileSync(ref Utf8ValueStringBuilder builder, string resolverName, string namespaceDot, ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, UnionSerializationInfo[] unionInfo, GenericSerializationInfo[] genericInfo)
        {
            var objectFormatterTemplates = objectInfo
                .GroupBy(x => (x.Namespace, x.IsStringKey))
                .Select(x =>
                {
                    var (nameSpace, isStringKey) = x.Key;
                    var objectSerializationInfos = x.ToArray();
                    var ns = namespaceDot + "Formatters" + (nameSpace is null ? string.Empty : "." + nameSpace);
                    var template = isStringKey ? new StringKeyFormatterTemplate(ns, objectSerializationInfos) : (IFormatterTemplate)new FormatterTemplate(ns, objectSerializationInfos);
                    return template;
                })
                .ToArray();

            string GetNamespace<T>(IGrouping<string?, T> x)
            {
                if (x.Key == null)
                {
                    return namespaceDot + "Formatters";
                }

                return namespaceDot + "Formatters." + x.Key;
            }

            var enumFormatterTemplates = enumInfo
                .GroupBy(x => x.Namespace)
                .Select(x => new EnumTemplate(GetNamespace(x), x.ToArray()))
                .ToArray();

            var unionFormatterTemplates = unionInfo
                .GroupBy(x => x.Namespace)
                .Select(x => new UnionTemplate(GetNamespace(x), x.ToArray()))
                .ToArray();

            var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray());

            resolverTemplate.TransformAppend(ref builder);
            builder.AppendLine();
            builder.AppendLine();
            foreach (var item in enumFormatterTemplates)
            {
                item.TransformAppend(ref builder);
                builder.AppendLine();
            }

            builder.AppendLine();
            foreach (var item in unionFormatterTemplates)
            {
                item.TransformAppend(ref builder);
                builder.AppendLine();
            }

            builder.AppendLine();
            foreach (var item in objectFormatterTemplates)
            {
                item.TransformAppend(ref builder);
                builder.AppendLine();
            }
        }

        private Task GenerateMultipleFileAsync(string output, string resolverName, string namespaceDot, string[] multioutSymbols, ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, UnionSerializationInfo[] unionInfo, GenericSerializationInfo[] genericInfo)
        {
            string GetNamespace(INamespaceInfo x)
            {
                if (x.Namespace == null)
                {
                    return namespaceDot + "Formatters";
                }

                return namespaceDot + "Formatters." + x.Namespace;
            }

            var waitingTasks = new Task[objectInfo.Length + enumInfo.Length + unionInfo.Length + 1];
            var waitingIndex = 0;
            foreach (var x in objectInfo)
            {
                var ns = namespaceDot + "Formatters" + (x.Namespace is null ? string.Empty : "." + x.Namespace);
                var template = x.IsStringKey ? new StringKeyFormatterTemplate(ns, new[] { x }) : (IFormatterTemplate)new FormatterTemplate(ns, new[] { x });
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, template.Namespace, x.Name + "Formatter", multioutSymbols, template);
            }

            foreach (var x in enumInfo)
            {
                var template = new EnumTemplate(GetNamespace(x), new[] { x });
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, template.Namespace, x.Name + "Formatter", multioutSymbols, template);
            }

            foreach (var x in unionInfo)
            {
                var template = new UnionTemplate(GetNamespace(x), new[] { x });
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, template.Namespace, x.Name + "Formatter", multioutSymbols, template);
            }

            var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray());
            waitingTasks[waitingIndex] = OutputToDirAsync(output, resolverTemplate.Namespace, resolverTemplate.ResolverName, multioutSymbols, resolverTemplate);
            return Task.WhenAll(waitingTasks);
        }

        private Task GenerateMultipleFileAsync(string output, string resolverName, string namespaceDot, ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, UnionSerializationInfo[] unionInfo, GenericSerializationInfo[] genericInfo)
        {
            string GetNamespace(INamespaceInfo x)
            {
                if (x.Namespace == null)
                {
                    return namespaceDot + "Formatters";
                }

                return namespaceDot + "Formatters." + x.Namespace;
            }

            var waitingTasks = new Task[objectInfo.Length + enumInfo.Length + unionInfo.Length + 1];
            var waitingIndex = 0;
            foreach (var x in objectInfo)
            {
                var ns = namespaceDot + "Formatters" + (x.Namespace is null ? string.Empty : "." + x.Namespace);
                var template = x.IsStringKey ? new StringKeyFormatterTemplate(ns, new[] { x }) : (IFormatterTemplate)new FormatterTemplate(ns, new[] { x });
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, template.Namespace, x.Name + "Formatter", template);
            }

            foreach (var x in enumInfo)
            {
                var template = new EnumTemplate(GetNamespace(x), new[] { x });
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, template.Namespace, x.Name + "Formatter", template);
            }

            foreach (var x in unionInfo)
            {
                var template = new UnionTemplate(GetNamespace(x), new[] { x });
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, template.Namespace, x.Name + "Formatter", template);
            }

            var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray());
            waitingTasks[waitingIndex] = OutputToDirAsync(output, resolverTemplate.Namespace, resolverTemplate.ResolverName, resolverTemplate);
            return Task.WhenAll(waitingTasks);
        }

        private async Task OutputToDirAsync(string dir, string ns, string name, ITemplate template)
        {
            var builder = ZString.CreateUtf8StringBuilder();
            try
            {
                template.TransformAppend(ref builder);
                var path = PreparePath(Path.Combine(dir, $"{ns}_{name}".Replace(".", "_").Replace("global::", string.Empty) + ".cs"));
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                await builder.WriteToAsync(stream).ConfigureAwait(false);
            }
            finally
            {
                builder.Dispose();
            }
        }

        private async Task OutputToDirAsync(string dir, string ns, string name, string[] multipleOutSymbols, ITemplate template)
        {
            var builder = ZString.CreateUtf8StringBuilder();
            var innerBuilder = ZString.CreateUtf8StringBuilder();
            try
            {
                template.TransformAppend(ref builder);
                foreach (var multipleOutSymbol in multipleOutSymbols)
                {
                    var path = PreparePath(Path.Combine(dir, MultiSymbolToSafeFilePath(multipleOutSymbol), $"{ns}_{name}".Replace(".", "_").Replace("global::", string.Empty) + ".cs"));
                    using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                    innerBuilder.Clear();
                    innerBuilder.Append("#if ");
                    innerBuilder.AppendLine(multipleOutSymbol);
                    await innerBuilder.WriteToAsync(stream).ConfigureAwait(false);
                    await builder.WriteToAsync(stream).ConfigureAwait(false);
                    innerBuilder.Clear();
                    innerBuilder.AppendLine();
                    innerBuilder.Append("#endif");
                    await innerBuilder.WriteToAsync(stream).ConfigureAwait(false);
                }
            }
            finally
            {
                innerBuilder.Dispose();
                builder.Dispose();
            }
        }

        private string PreparePath(string path)
        {
            path = path.Replace("global::", string.Empty);

            const string prefix = "[Out]";
            logger(prefix + path);

            var fi = new FileInfo(path);
            if (fi.Directory != null && !fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            return path;
        }

        private static string MultiSymbolToSafeFilePath(string symbol)
        {
            return symbol.Replace("!", "NOT_").Replace("(", string.Empty).Replace(")", string.Empty).Replace("||", "_OR_").Replace("&&", "_AND_");
        }

        private static string NormalizeNewLines(string content)
        {
            // The T4 generated code may be text with mixed line ending types. (CR + CRLF)
            // We need to normalize the line ending type in each Operating Systems. (e.g. Windows=CRLF, Linux/macOS=LF)
            return content.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }
    }
}

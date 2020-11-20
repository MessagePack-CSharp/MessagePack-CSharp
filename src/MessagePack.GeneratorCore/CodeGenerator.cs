// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessagePackCompiler.CodeAnalysis;
using MessagePackCompiler.Generator;
using Microsoft.CodeAnalysis;

namespace MessagePackCompiler
{
    public class CodeGenerator
    {
        private static readonly Encoding NoBomUtf8 = new UTF8Encoding(false);

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
            var multipleOutputSymbols = multipleIfDirectiveOutputSymbols?.Split(',') ?? Array.Empty<string>();

            var sw = Stopwatch.StartNew();

            foreach (var multiOutputSymbol in multipleOutputSymbols.Length == 0 ? new[] { string.Empty } : multipleOutputSymbols)
            {
                logger("Project Compilation Start:" + compilation.AssemblyName);

                var collector = new TypeCollector(compilation, true, useMapMode, externalIgnoreTypeNames, Console.WriteLine);

                logger("Project Compilation Complete:" + sw.Elapsed.ToString());

                sw.Restart();
                logger("Method Collect Start");

                var (objectInfo, enumInfo, genericInfo, unionInfo) = collector.Collect();

                logger("Method Collect Complete:" + sw.Elapsed.ToString());

                logger("Output Generation Start");
                sw.Restart();

                if (Path.GetExtension(output) == ".cs")
                {
                    // SingleFile Output
                    var fullGeneratedProgramText = GenerateSingleFileSync(resolverName, namespaceDot, objectInfo, enumInfo, unionInfo, genericInfo);
                    if (multiOutputSymbol == string.Empty)
                    {
                        await OutputAsync(output, fullGeneratedProgramText);
                    }
                    else
                    {
                        var fname = Path.GetFileNameWithoutExtension(output) + "." + MultiSymbolToSafeFilePath(multiOutputSymbol) + ".cs";
                        var text = $"#if {multiOutputSymbol}" + Environment.NewLine + fullGeneratedProgramText + Environment.NewLine + "#endif";
                        await OutputAsync(Path.Combine(Path.GetDirectoryName(output) ?? string.Empty, fname), text);
                    }
                }
                else
                {
                    // Multiple File output
                    await GenerateMultipleFileAsync(output, resolverName, objectInfo, enumInfo, unionInfo, namespaceDot, multiOutputSymbol, genericInfo);
                }

                if (objectInfo.Length == 0 && enumInfo.Length == 0 && genericInfo.Length == 0 && unionInfo.Length == 0)
                {
                    logger("Generated result is empty, unexpected result?");
                }
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

            var sb = new StringBuilder();
            sb.AppendLine(resolverTemplate.TransformText());
            sb.AppendLine();
            foreach (var item in enumFormatterTemplates)
            {
                var text = item.TransformText();
                sb.AppendLine(text);
            }

            sb.AppendLine();
            foreach (var item in unionFormatterTemplates)
            {
                var text = item.TransformText();
                sb.AppendLine(text);
            }

            sb.AppendLine();
            foreach (var item in objectFormatterTemplates)
            {
                var text = item.TransformText();
                sb.AppendLine(text);
            }

            return sb.ToString();
        }

        private Task GenerateMultipleFileAsync(string output, string resolverName, ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, UnionSerializationInfo[] unionInfo, string namespaceDot, string multioutSymbol, GenericSerializationInfo[] genericInfo)
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
                var text = template.TransformText();
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, template.Namespace, x.Name + "Formatter", multioutSymbol, text);
            }

            foreach (var x in enumInfo)
            {
                var template = new EnumTemplate(GetNamespace(x), new[] { x });
                var text = template.TransformText();
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, template.Namespace, x.Name + "Formatter", multioutSymbol, text);
            }

            foreach (var x in unionInfo)
            {
                var template = new UnionTemplate(GetNamespace(x), new[] { x });
                var text = template.TransformText();
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, template.Namespace, x.Name + "Formatter", multioutSymbol, text);
            }

            var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray());
            waitingTasks[waitingIndex] = OutputToDirAsync(output, resolverTemplate.Namespace, resolverTemplate.ResolverName, multioutSymbol, resolverTemplate.TransformText());
            return Task.WhenAll(waitingTasks);
        }

        private Task OutputToDirAsync(string dir, string ns, string name, string multipleOutSymbol, string text)
        {
            if (multipleOutSymbol == string.Empty)
            {
                return OutputAsync(Path.Combine(dir, $"{ns}_{name}".Replace(".", "_").Replace("global::", string.Empty) + ".cs"), text);
            }
            else
            {
                text = $"#if {multipleOutSymbol}" + Environment.NewLine + text + Environment.NewLine + "#endif";
                return OutputAsync(Path.Combine(dir, MultiSymbolToSafeFilePath(multipleOutSymbol), $"{ns}_{name}".Replace(".", "_").Replace("global::", string.Empty) + ".cs"), text);
            }
        }

        private Task OutputAsync(string path, string text)
        {
            path = path.Replace("global::", string.Empty);

            const string prefix = "[Out]";
            logger(prefix + path);

            var fi = new FileInfo(path);
            if (fi.Directory != null && !fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            File.WriteAllText(path, NormalizeNewLines(text), NoBomUtf8);
            return Task.CompletedTask;
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

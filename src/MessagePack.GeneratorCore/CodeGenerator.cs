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
    public partial class CodeGenerator
    {
        private static readonly Encoding NoBomUtf8 = new UTF8Encoding(false);

        private readonly Action<string> logger;
        private readonly CancellationToken cancellationToken;

        public CodeGenerator(Action<string> logger, CancellationToken cancellationToken)
        {
            this.logger = logger;
            this.cancellationToken = cancellationToken;
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
            var outputExtension = Path.GetExtension(output);
            var isSingleFileOutput = outputExtension == ".cs";
            if (isSingleFileOutput)
            {
                var builder = new StringBuilder();
                GenerateSingleFileSync(builder, resolverName, namespaceDot, objectInfo, enumInfo, unionInfo, genericInfo);
                var textBytes = NoBomUtf8.GetBytes(builder.ToString());
                if (multipleOutputSymbols.Length == 0)
                {
                    using var stream = new FileStream(PreparePath(output), FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                    await stream.WriteAsync(textBytes, 0, textBytes.Length, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var innerBuilder = new StringBuilder();
                    var pathBuilder = new StringBuilder();
                    foreach (var multiOutputSymbol in multipleOutputSymbols)
                    {
                        pathBuilder.Clear();
                        pathBuilder.Append(output, 0, output.Length - outputExtension.Length);
                        pathBuilder.Append('.');
                        AppendMultiSymbolToSafeFilePath(pathBuilder, multiOutputSymbol);
                        pathBuilder.Append(".cs");
                        var fname = PreparePath(pathBuilder.ToString());

                        using var stream = new FileStream(fname, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                        innerBuilder.Clear();
                        builder.Append("#if ");
                        innerBuilder.AppendLine(multiOutputSymbol);
                        var array = NoBomUtf8.GetBytes(innerBuilder.ToString());
                        await stream.WriteAsync(array, 0, array.Length, cancellationToken).ConfigureAwait(false);

                        await stream.WriteAsync(textBytes, 0, textBytes.Length, cancellationToken).ConfigureAwait(false);

                        innerBuilder.Clear();
                        innerBuilder.AppendLine();
                        builder.Append("#endif");
                        array = NoBomUtf8.GetBytes(innerBuilder.ToString());
                        await stream.WriteAsync(array, 0, array.Length, cancellationToken).ConfigureAwait(false);
                    }
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
                    await GenerateMultipleFileAsync(output, resolverName, namespaceDot, objectInfo, enumInfo, unionInfo, genericInfo, multipleOutputSymbols);
                }
            }

            if (objectInfo.Length == 0 && enumInfo.Length == 0 && genericInfo.Length == 0 && unionInfo.Length == 0)
            {
                logger("Generated result is empty, unexpected result?");
            }

            logger("Output Generation Complete:" + sw.Elapsed.ToString());
        }

        private void GenerateSingleFileSync(StringBuilder builder, string resolverName, string namespaceDot, ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, UnionSerializationInfo[] unionInfo, GenericSerializationInfo[] genericInfo)
        {
            string GetNamespace<T>(IGrouping<string?, T> x)
            {
                if (x.Key == null)
                {
                    return namespaceDot + "Formatters";
                }

                return namespaceDot + "Formatters." + x.Key;
            }

            var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray(), cancellationToken);

            resolverTemplate.TransformAppend(builder);
            builder.AppendLine();

            foreach (var group in enumInfo.GroupBy(x => x.Namespace))
            {
                builder.AppendLine();
                var template = new EnumTemplate(GetNamespace(group), group.ToArray(), cancellationToken);
                template.TransformAppend(builder);
            }

            foreach (var group in unionInfo.GroupBy(x => x.Namespace))
            {
                builder.AppendLine();
                var template = new UnionTemplate(GetNamespace(group), group.ToArray(), cancellationToken);
                template.TransformAppend(builder);
            }

            foreach (var x in objectInfo.GroupBy(info => (info.Namespace, info.IsStringKey)))
            {
                builder.AppendLine();
                var (nameSpace, isStringKey) = x.Key;
                var objectSerializationInfos = x.ToArray();
                var ns = namespaceDot + "Formatters" + (nameSpace is null ? string.Empty : "." + nameSpace);
                if (isStringKey)
                {
                    var template = new StringKeyFormatterTemplate(ns, objectSerializationInfos, cancellationToken);
                    template.TransformAppend(builder);
                }
                else
                {
                    var template = new FormatterTemplate(ns, objectSerializationInfos, cancellationToken);
                    template.TransformAppend(builder);
                }
            }
        }

        private async ValueTask GenerateMultipleFileAsync(string output, string resolverName, string namespaceDot, ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, UnionSerializationInfo[] unionInfo, GenericSerializationInfo[] genericInfo)
        {
            string GetNamespace(INamespaceInfo x)
            {
                if (x.Namespace == null)
                {
                    return namespaceDot + "Formatters";
                }

                return namespaceDot + "Formatters." + x.Namespace;
            }

#if NET6_0_OR_GREATER
            var objectTask = Parallel.ForEachAsync(objectInfo, cancellationToken, async (x, token) =>
            {
                var ns = namespaceDot + "Formatters" + (x.Namespace is null ? string.Empty : "." + x.Namespace);
                ITemplate template;
                if (x.IsStringKey)
                {
                    template = new StringKeyFormatterTemplate(ns, new[] { x }, cancellationToken);
                }
                else
                {
                    template = new FormatterTemplate(ns, new[] { x }, cancellationToken);
                }

                await OutputToDirAsync(output, x.Name + "Formatter", template).ConfigureAwait(false);
            });
            var enumTask = Parallel.ForEachAsync(enumInfo, cancellationToken, async (x, token) =>
            {
                var template = new EnumTemplate(GetNamespace(x), new[] { x }, cancellationToken);
                await OutputToDirAsync(output, x.Name + "Formatter", template).ConfigureAwait(false);
            });
            var unionTask = Parallel.ForEachAsync(unionInfo, cancellationToken, async (x, token) =>
            {
                var template = new UnionTemplate(GetNamespace(x), new[] { x }, cancellationToken);
                await OutputToDirAsync(output, x.Name + "Formatter", template).ConfigureAwait(false);
            });

            var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray(), cancellationToken);
            await OutputToDirAsync(output, resolverTemplate.ResolverName, resolverTemplate).ConfigureAwait(false);
            var waitingTasks = new Task[3]
            {
                objectTask,
                enumTask,
                unionTask,
            };
#else
            var waitingTasks = new Task[objectInfo.Length + enumInfo.Length + unionInfo.Length + 1];
            var waitingIndex = 0;
            foreach (var x in objectInfo)
            {
                var ns = namespaceDot + "Formatters" + (x.Namespace is null ? string.Empty : "." + x.Namespace);
                if (x.IsStringKey)
                {
                    var template = new StringKeyFormatterTemplate(ns, new[] { x }, cancellationToken);
                    waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template).AsTask();
                }
                else
                {
                    var template = new FormatterTemplate(ns, new[] { x }, cancellationToken);
                    waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template).AsTask();
                }
            }

            foreach (var x in enumInfo)
            {
                var template = new EnumTemplate(GetNamespace(x), new[] { x }, cancellationToken);
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template).AsTask();
            }

            foreach (var x in unionInfo)
            {
                var template = new UnionTemplate(GetNamespace(x), new[] { x }, cancellationToken);
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template).AsTask();
            }

            var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray(), cancellationToken);
            waitingTasks[waitingIndex] = OutputToDirAsync(output, resolverTemplate.ResolverName, resolverTemplate).AsTask();
#endif
            await Task.WhenAll(waitingTasks).ConfigureAwait(false);
        }

        private async ValueTask GenerateMultipleFileAsync(string output, string resolverName, string namespaceDot, ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, UnionSerializationInfo[] unionInfo, GenericSerializationInfo[] genericInfo, string[] multioutSymbols)
        {
            string GetNamespace(INamespaceInfo x)
            {
                if (x.Namespace == null)
                {
                    return namespaceDot + "Formatters";
                }

                return namespaceDot + "Formatters." + x.Namespace;
            }

#if NET6_0_OR_GREATER
            var objectTask = Parallel.ForEachAsync(objectInfo, cancellationToken, async (x, token) =>
            {
                var ns = namespaceDot + "Formatters" + (x.Namespace is null ? string.Empty : "." + x.Namespace);
                ITemplate template;
                if (x.IsStringKey)
                {
                    template = new StringKeyFormatterTemplate(ns, new[] { x }, cancellationToken);
                }
                else
                {
                    template = new FormatterTemplate(ns, new[] { x }, cancellationToken);
                }

                await OutputToDirAsync(output, x.Name + "Formatter", template, multioutSymbols).ConfigureAwait(false);
            });
            var enumTask = Parallel.ForEachAsync(enumInfo, cancellationToken, async (x, token) =>
            {
                var template = new EnumTemplate(GetNamespace(x), new[] { x }, cancellationToken);
                await OutputToDirAsync(output, x.Name + "Formatter", template, multioutSymbols).ConfigureAwait(false);
            });
            var unionTask = Parallel.ForEachAsync(unionInfo, cancellationToken, async (x, token) =>
            {
                var template = new UnionTemplate(GetNamespace(x), new[] { x }, cancellationToken);
                await OutputToDirAsync(output, x.Name + "Formatter", template, multioutSymbols).ConfigureAwait(false);
            });

            var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray(), cancellationToken);
            await OutputToDirAsync(output, resolverTemplate.ResolverName, resolverTemplate, multioutSymbols).ConfigureAwait(false);
            var waitingTasks = new Task[3]
            {
                objectTask,
                enumTask,
                unionTask,
            };
#else
            var waitingTasks = new Task[objectInfo.Length + enumInfo.Length + unionInfo.Length + 1];
            var waitingIndex = 0;
            foreach (var x in objectInfo)
            {
                var ns = namespaceDot + "Formatters" + (x.Namespace is null ? string.Empty : "." + x.Namespace);
                ITemplate template;
                if (x.IsStringKey)
                {
                    template = new StringKeyFormatterTemplate(ns, new[] { x }, cancellationToken);
                }
                else
                {
                    template = new FormatterTemplate(ns, new[] { x }, cancellationToken);
                }

                waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template, multioutSymbols).AsTask();
            }

            foreach (var x in enumInfo)
            {
                var template = new EnumTemplate(GetNamespace(x), new[] { x }, cancellationToken);
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template, multioutSymbols).AsTask();
            }

            foreach (var x in unionInfo)
            {
                var template = new UnionTemplate(GetNamespace(x), new[] { x }, cancellationToken);
                waitingTasks[waitingIndex++] = OutputToDirAsync(output, x.Name + "Formatter", template, multioutSymbols).AsTask();
            }

            var resolverTemplate = new ResolverTemplate(namespaceDot + "Resolvers", namespaceDot + "Formatters", resolverName, genericInfo.Where(x => !x.IsOpenGenericType).Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo.Where(x => !x.IsOpenGenericType)).ToArray(), cancellationToken);
            waitingTasks[waitingIndex] = OutputToDirAsync(output, resolverTemplate.ResolverName, resolverTemplate, multioutSymbols).AsTask();
#endif
            await Task.WhenAll(waitingTasks).ConfigureAwait(false);
        }

        private async ValueTask OutputToDirAsync<T>(string dir, string name, T template)
            where T : ITemplate
        {
            var builder = new StringBuilder();
            template.TransformAppend(builder);
            var path = PreparePath(Path.Combine(dir, $"{template.Namespace}_{name}".Replace(".", "_").Replace("global::", string.Empty) + ".cs"));
            var array = NoBomUtf8.GetBytes(builder.ToString());
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
            await stream.WriteAsync(array, 0, array.Length, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask OutputToDirAsync<T>(string dir, string name, T template, string[] multipleOutSymbols)
            where T : ITemplate
        {
            var builder = new StringBuilder();
            var innerBuilder = new StringBuilder();
            innerBuilder.AppendLine();
            innerBuilder.Append("#endif");
            var endifArray = NoBomUtf8.GetBytes(innerBuilder.ToString());

            var pathBuilder = new StringBuilder();
            template.TransformAppend(builder);
            var textArray = NoBomUtf8.GetBytes(builder.ToString());
            foreach (var multipleOutSymbol in multipleOutSymbols)
            {
                pathBuilder.Clear();
                AppendMultiSymbolToSafeFilePath(pathBuilder, multipleOutSymbol);
                pathBuilder.Append(Path.PathSeparator);
                pathBuilder.Append(template.Namespace);
                pathBuilder.Append('_');
                pathBuilder.Append(name);
                pathBuilder.Replace('.', '_');
                pathBuilder.Replace("global::", string.Empty);
                pathBuilder.Append(".cs");
                var path = PreparePath(Path.Combine(dir, pathBuilder.ToString()));

                innerBuilder.Clear();
                innerBuilder.Append("#if ");
                innerBuilder.AppendLine(multipleOutSymbol);
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                var array = NoBomUtf8.GetBytes(innerBuilder.ToString());
                await stream.WriteAsync(array, 0, array.Length, cancellationToken).ConfigureAwait(false);
                await stream.WriteAsync(textArray, 0, textArray.Length, cancellationToken).ConfigureAwait(false);
                await stream.WriteAsync(endifArray, 0, endifArray.Length, cancellationToken).ConfigureAwait(false);
            }
        }

        private string PreparePath(string path)
        {
            path = path.Replace("global::", string.Empty);

            const string Prefix = "[Out]";
            logger(Prefix + path);

            var fi = new FileInfo(path);
            if (fi.Directory != null && !fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            return path;
        }

        private static void AppendMultiSymbolToSafeFilePath(StringBuilder builder, string symbol)
        {
            for (var i = 0; i < symbol.Length; i++)
            {
                switch (symbol[i])
                {
                    case '!':
                        builder.Append("NOT_");
                        break;
                    case '(':
                    case ')':
                        break;
                    case '|' when i + 1 < symbol.Length && symbol[i + 1] == '|':
                        builder.Append("_OR_");
                        ++i;
                        break;
                    case '&' when i + 1 < symbol.Length && symbol[i + 1] == '&':
                        builder.Append("_AND_");
                        ++i;
                        break;
                    default:
                        builder.Append(symbol[i]);
                        break;
                }
            }
        }
    }
}

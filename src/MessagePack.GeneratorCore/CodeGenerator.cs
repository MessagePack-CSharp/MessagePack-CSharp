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
                var builder = ZString.CreateUtf8StringBuilder();
                try
                {
                    GenerateSingleFileSync(ref builder, resolverName, namespaceDot, objectInfo, enumInfo, unionInfo, genericInfo);
                    if (multipleOutputSymbols.Length == 0)
                    {
#if NET6_0_OR_GREATER
                        using var handle = File.OpenHandle(PreparePath(output), FileMode.Create, FileAccess.Write, FileShare.ReadWrite, FileOptions.Asynchronous, 0);
                        await RandomAccess.WriteAsync(handle, builder.AsMemory(), 0, cancellationToken).ConfigureAwait(false);
#else
                        using var stream = new FileStream(PreparePath(output), FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                        await builder.WriteToAsync(stream).ConfigureAwait(false);
#endif
                    }
                    else
                    {
                        var innerBuilder = ZString.CreateUtf8StringBuilder();
                        var pathBuilder = ZString.CreateStringBuilder();
                        try
                        {
                            foreach (var multiOutputSymbol in multipleOutputSymbols)
                            {
                                pathBuilder.Clear();
                                pathBuilder.Append(output.AsSpan(0, output.Length - outputExtension.Length));
                                pathBuilder.Append('.');
                                AppendMultiSymbolToSafeFilePath(ref pathBuilder, multiOutputSymbol);
                                pathBuilder.Append(".cs");
                                var fname = PreparePath(pathBuilder.ToString());
#if NET6_0_OR_GREATER
                                using var handle = File.OpenHandle(fname, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, FileOptions.Asynchronous, 0);
#else
                                using var stream = new FileStream(fname, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
#endif
                                innerBuilder.Clear();
                                Append(ref builder, GetSharpIf());
                                innerBuilder.AppendLine(multiOutputSymbol);
#if NET6_0_OR_GREATER
                                await RandomAccess.WriteAsync(handle, innerBuilder.AsMemory(), 0, cancellationToken).ConfigureAwait(false);
                                long offset = innerBuilder.Length;
                                await RandomAccess.WriteAsync(handle, builder.AsMemory(), offset, cancellationToken).ConfigureAwait(false);
                                offset += builder.Length;
#else
                                await innerBuilder.WriteToAsync(stream).ConfigureAwait(false);
                                await builder.WriteToAsync(stream).ConfigureAwait(false);
#endif
                                innerBuilder.Clear();
                                innerBuilder.AppendLine();
                                Append(ref builder, GetSharpEndIf());
#if NET6_0_OR_GREATER
                                await RandomAccess.WriteAsync(handle, innerBuilder.AsMemory(), offset, cancellationToken).ConfigureAwait(false);
#else
                                await innerBuilder.WriteToAsync(stream).ConfigureAwait(false);
#endif
                            }
                        }
                        finally
                        {
                            pathBuilder.Dispose();
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
                    await GenerateMultipleFileAsync(output, resolverName, namespaceDot, objectInfo, enumInfo, unionInfo, genericInfo, multipleOutputSymbols);
                }
            }

            if (objectInfo.Length == 0 && enumInfo.Length == 0 && genericInfo.Length == 0 && unionInfo.Length == 0)
            {
                logger("Generated result is empty, unexpected result?");
            }

            logger("Output Generation Complete:" + sw.Elapsed.ToString());
        }

        [Utf8("#if ")]
        private static partial ReadOnlySpan<byte> GetSharpIf();

        [Utf8("#endif")]
        private static partial ReadOnlySpan<byte> GetSharpEndIf();

        private void GenerateSingleFileSync(ref Utf8ValueStringBuilder builder, string resolverName, string namespaceDot, ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, UnionSerializationInfo[] unionInfo, GenericSerializationInfo[] genericInfo)
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

            resolverTemplate.TransformAppend(ref builder);
            builder.AppendLine();

            foreach (var group in enumInfo.GroupBy(x => x.Namespace))
            {
                builder.AppendLine();
                var template = new EnumTemplate(GetNamespace(group), group.ToArray(), cancellationToken);
                template.TransformAppend(ref builder);
            }

            foreach (var group in unionInfo.GroupBy(x => x.Namespace))
            {
                builder.AppendLine();
                var template = new UnionTemplate(GetNamespace(group), group.ToArray(), cancellationToken);
                template.TransformAppend(ref builder);
            }

            foreach (var x in objectInfo.GroupBy(info => (info.Namespace, info.IsStringKey)))
            {
                builder.AppendLine();
                var (nameSpace, isStringKey) = x.Key;
                var objectSerializationInfos = x.ToArray();
                var ns = namespaceDot + "Formatters" + (nameSpace is null ? string.Empty : "." + nameSpace);
                IFormatterTemplate template;
                if (isStringKey)
                {
                    template = new StringKeyFormatterTemplate(ns, objectSerializationInfos, cancellationToken);
                }
                else
                {
                    template = new FormatterTemplate(ns, objectSerializationInfos, cancellationToken);
                }

                template.TransformAppend(ref builder);
            }
        }

        private async ValueTask OutputToDirAsync(string dir, string name, ITemplate template)
        {
            var builder = ZString.CreateUtf8StringBuilder();
            try
            {
                template.TransformAppend(ref builder);
                var path = PreparePath(Path.Combine(dir, $"{template.Namespace}_{name}".Replace(".", "_").Replace("global::", string.Empty) + ".cs"));
#if NET6_0_OR_GREATER
                using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, FileOptions.Asynchronous, 0);
                await RandomAccess.WriteAsync(handle, builder.AsMemory(), 0, cancellationToken).ConfigureAwait(false);
#else
                using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                await builder.WriteToAsync(stream).ConfigureAwait(false);
#endif
            }
            finally
            {
                builder.Dispose();
            }
        }

        private async ValueTask OutputToDirAsync(string dir, string name, ITemplate template, string[] multipleOutSymbols)
        {
            var builder = ZString.CreateUtf8StringBuilder();
            var innerBuilder = ZString.CreateUtf8StringBuilder();
            var pathBuilder = ZString.CreateStringBuilder();
            try
            {
                template.TransformAppend(ref builder);
                foreach (var multipleOutSymbol in multipleOutSymbols)
                {
                    pathBuilder.Clear();
                    AppendMultiSymbolToSafeFilePath(ref pathBuilder, multipleOutSymbol);
                    pathBuilder.Append(Path.PathSeparator);
                    pathBuilder.Append(template.Namespace);
                    pathBuilder.Append('_');
                    pathBuilder.Append(name);
                    pathBuilder.Replace('.', '_');
                    pathBuilder.Replace("global::", string.Empty);
                    pathBuilder.Append(".cs");
                    var path = PreparePath(Path.Combine(dir, pathBuilder.ToString()));
                    innerBuilder.Clear();
                    Append(ref innerBuilder, GetSharpIf());
                    innerBuilder.AppendLine(multipleOutSymbol);
#if NET6_0_OR_GREATER
                    using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, FileOptions.Asynchronous, 0);
                    await RandomAccess.WriteAsync(handle, innerBuilder.AsMemory(), 0, cancellationToken).ConfigureAwait(false);
                    long offset = innerBuilder.Length;
                    await RandomAccess.WriteAsync(handle, builder.AsMemory(), offset, cancellationToken).ConfigureAwait(false);
                    offset += builder.Length;
#else
                    using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                    await innerBuilder.WriteToAsync(stream).ConfigureAwait(false);
                    await builder.WriteToAsync(stream).ConfigureAwait(false);
#endif
                    innerBuilder.Clear();
                    innerBuilder.AppendLine();
                    Append(ref innerBuilder, GetSharpEndIf());
#if NET6_0_OR_GREATER
                    await RandomAccess.WriteAsync(handle, innerBuilder.AsMemory(), offset, cancellationToken).ConfigureAwait(false);
#else
                    await innerBuilder.WriteToAsync(stream).ConfigureAwait(false);
#endif
                }
            }
            finally
            {
                pathBuilder.Dispose();
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

        private static void Append(ref Utf8ValueStringBuilder builder, ReadOnlySpan<byte> span)
        {
            span.CopyTo(builder.GetSpan(span.Length));
            builder.Advance(span.Length);
        }

        private static void AppendMultiSymbolToSafeFilePath(ref Utf16ValueStringBuilder builder, string symbol)
        {
            for (int i = 0; i < symbol.Length; i++)
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

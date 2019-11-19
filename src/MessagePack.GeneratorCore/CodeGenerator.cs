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

namespace MessagePackCompiler
{
    public class CodeGenerator
    {
        private static readonly Encoding NoBomUtf8 = new UTF8Encoding(false);

        private Action<string> logger;
        private CancellationToken cancellationToken;

        public CodeGenerator(Action<string> logger, CancellationToken cancellationToken)
        {
            this.logger = logger;
            this.cancellationToken = cancellationToken;
        }

        public async Task GenerateFileAsync(
           string input,
           string output,
           string conditionalSymbol,
           string resolverName,
           string @namespace,
           bool useMapMode,
           string multipleIfDirectiveOutputSymbols)
        {
            var namespaceDot = string.IsNullOrWhiteSpace(@namespace) ? string.Empty : @namespace + ".";
            var conditionalSymbols = conditionalSymbol?.Split(',') ?? Array.Empty<string>();
            var multipleOutputSymbols = multipleIfDirectiveOutputSymbols?.Split(',') ?? Array.Empty<string>();

            var sw = Stopwatch.StartNew();

            foreach (var multioutSymbol in multipleOutputSymbols.Length == 0 ? new[] { string.Empty } : multipleOutputSymbols)
            {
                logger("Project Compilation Start:" + input);

                var compilation = (Path.GetExtension(input) == ".csproj")
                    ? await MessagePackCompilation.CreateFromProjectAsync(input.Split(','), conditionalSymbols.Concat(new[] { multioutSymbol }).ToArray(), cancellationToken)
.ConfigureAwait(false) : await MessagePackCompilation.CreateFromDirectoryAsync(input, conditionalSymbols.Concat(new[] { multioutSymbol }).ToArray(), cancellationToken).ConfigureAwait(false);
                var collector = new TypeCollector(compilation, true, useMapMode, x => Console.WriteLine(x));

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
                    var objectFormatterTemplates = objectInfo
                        .GroupBy(x => x.Namespace)
                        .Select(x => new FormatterTemplate()
                        {
                            Namespace = namespaceDot + "Formatters" + ((x.Key == null) ? string.Empty : "." + x.Key),
                            ObjectSerializationInfos = x.ToArray(),
                        })
                        .ToArray();

                    var enumFormatterTemplates = enumInfo
                        .GroupBy(x => x.Namespace)
                        .Select(x => new EnumTemplate()
                        {
                            Namespace = namespaceDot + "Formatters" + ((x.Key == null) ? string.Empty : "." + x.Key),
                            EnumSerializationInfos = x.ToArray(),
                        })
                        .ToArray();

                    var unionFormatterTemplates = unionInfo
                        .GroupBy(x => x.Namespace)
                        .Select(x => new UnionTemplate()
                        {
                            Namespace = namespaceDot + "Formatters" + ((x.Key == null) ? string.Empty : "." + x.Key),
                            UnionSerializationInfos = x.ToArray(),
                        })
                        .ToArray();

                    var resolverTemplate = new ResolverTemplate()
                    {
                        Namespace = namespaceDot + "Resolvers",
                        FormatterNamespace = namespaceDot + "Formatters",
                        ResolverName = resolverName,
                        RegisterInfos = genericInfo.Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo).ToArray(),
                    };

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

                    if (multioutSymbol == string.Empty)
                    {
                        await OutputAsync(output, sb.ToString(), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var fname = Path.GetFileNameWithoutExtension(output) + "." + MultiSymbolToSafeFilePath(multioutSymbol) + ".cs";
                        var text = $"#if {multioutSymbol}" + Environment.NewLine + sb.ToString() + Environment.NewLine + "#endif";
                        await OutputAsync(Path.Combine(Path.GetDirectoryName(output), fname), text, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Multiple File output
                    foreach (var x in objectInfo)
                    {
                        var template = new FormatterTemplate()
                        {
                            Namespace = namespaceDot + "Formatters" + ((x.Namespace == null) ? string.Empty : "." + x.Namespace),
                            ObjectSerializationInfos = new[] { x },
                        };

                        var text = template.TransformText();
                        await OutputToDirAsync(output, template.Namespace, x.Name + "Formatter", multioutSymbol, text, cancellationToken).ConfigureAwait(false);
                    }

                    foreach (var x in enumInfo)
                    {
                        var template = new EnumTemplate()
                        {
                            Namespace = namespaceDot + "Formatters" + ((x.Namespace == null) ? string.Empty : "." + x.Namespace),
                            EnumSerializationInfos = new[] { x },
                        };

                        var text = template.TransformText();
                        await OutputToDirAsync(output, template.Namespace, x.Name + "Formatter", multioutSymbol, text, cancellationToken).ConfigureAwait(false);
                    }

                    foreach (var x in unionInfo)
                    {
                        var template = new UnionTemplate()
                        {
                            Namespace = namespaceDot + "Formatters" + ((x.Namespace == null) ? string.Empty : "." + x.Namespace),
                            UnionSerializationInfos = new[] { x },
                        };

                        var text = template.TransformText();
                        await OutputToDirAsync(output, template.Namespace, x.Name + "Formatter", multioutSymbol, text, cancellationToken).ConfigureAwait(false);
                    }

                    var resolverTemplate = new ResolverTemplate()
                    {
                        Namespace = namespaceDot + "Resolvers",
                        FormatterNamespace = namespaceDot + "Formatters",
                        ResolverName = resolverName,
                        RegisterInfos = genericInfo.Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo).ToArray(),
                    };

                    await OutputToDirAsync(output, resolverTemplate.Namespace, resolverTemplate.ResolverName, multioutSymbol, resolverTemplate.TransformText(), cancellationToken).ConfigureAwait(false);
                }
            }

            logger("Output Generation Complete:" + sw.Elapsed.ToString());
        }

        private Task OutputToDirAsync(string dir, string ns, string name, string multipleOutSymbol, string text, CancellationToken cancellationToken)
        {
            if (multipleOutSymbol == string.Empty)
            {
                return OutputAsync(Path.Combine(dir, $"{ns}_{name}".Replace(".", "_").Replace("global::", string.Empty) + ".cs"), text, cancellationToken);
            }
            else
            {
                text = $"#if {multipleOutSymbol}" + Environment.NewLine + text + Environment.NewLine + "#endif";
                return OutputAsync(Path.Combine(dir, MultiSymbolToSafeFilePath(multipleOutSymbol), $"{ns}_{name}".Replace(".", "_").Replace("global::", string.Empty) + ".cs"), text, cancellationToken);
            }
        }

        private Task OutputAsync(string path, string text, CancellationToken cancellationToken)
        {
            path = path.Replace("global::", string.Empty);

            const string prefix = "[Out]";
            logger(prefix + path);

            var fi = new FileInfo(path);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            System.IO.File.WriteAllText(path, text, NoBomUtf8);
            return Task.CompletedTask;
        }

        private static string MultiSymbolToSafeFilePath(string symbol)
        {
            return symbol.Replace("!", "NOT_").Replace("(", string.Empty).Replace(")", string.Empty).Replace("||", "_OR_").Replace("&&", "_AND_");
        }
    }
}

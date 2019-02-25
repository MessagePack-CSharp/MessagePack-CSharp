using MessagePackCompiler.Generator;
using MicroBatchFramework;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePackCompiler
{
    /// <summary>
    /// Code generator of MessagePack-CSharp.
    /// </summary>
    public class MessagepackCompiler : BatchBase
    {
        static async Task Main(string[] args)
        {
            await new HostBuilder().RunBatchEngineAsync<MessagepackCompiler>(args);
        }

        public async Task RunAsync(
            [Option("i", "Input path of analyze csproj or directory, if input multiple csproj split with ','.")]string input,
            [Option("o", "Output file path(.cs) or directory.")]string output,
            [Option("c", "Conditional compiler symbols, split with ','.")]string conditionalSymbol = null,
            [Option("r", "Set resolver name.")]string resolverName = "GeneratedResolver",
            [Option("n", "Set namespace root name.")]string @namespace = "MessagePack",
            [Option("m", "Force use map mode serialization.")]bool useMapMode = false,
            [Option("ms", "Generate #if-- files by symbols, split with ','.")]string multipleIfDiretiveOutputSymbols = null
            )
        {
            var namespaceDot = string.IsNullOrWhiteSpace(@namespace) ? "" : @namespace + ".";
            var conditionalSymbols = conditionalSymbol?.Split(',') ?? Array.Empty<string>();
            var multipleOutputSymbols = multipleIfDiretiveOutputSymbols?.Split(',') ?? Array.Empty<string>();

            var sw = Stopwatch.StartNew();

            foreach (var multioutSymbol in multipleOutputSymbols.Length == 0 ? new[] { "" } : multipleOutputSymbols)
            {
                Console.WriteLine("Project Compilation Start:" + input);

                var compilation = (Path.GetExtension(input) == ".csproj")
                    ? await MessagePackCompilation.CreateFromProjectAsync(input.Split(','), conditionalSymbols.Concat(new[] { multioutSymbol }).ToArray(), this.Context.CancellationToken)
                    : await MessagePackCompilation.CreateFromDirectoryAsync(input, conditionalSymbols.Concat(new[] { multioutSymbol }).ToArray(), this.Context.CancellationToken);
                var collector = new TypeCollector(compilation, true, useMapMode);

                Console.WriteLine("Project Compilation Complete:" + sw.Elapsed.ToString());
                Console.WriteLine();

                sw.Restart();
                Console.WriteLine("Method Collect Start");

                var (objectInfo, enumInfo, genericInfo, unionInfo) = collector.Collect();

                Console.WriteLine("Method Collect Complete:" + sw.Elapsed.ToString());

                Console.WriteLine("Output Generation Start");
                sw.Restart();

                if (Path.GetExtension(output) == ".cs")
                {
                    // SingleFile Output
                    var objectFormatterTemplates = objectInfo
                        .GroupBy(x => x.Namespace)
                        .Select(x => new FormatterTemplate()
                        {
                            Namespace = namespaceDot + "Formatters" + ((x.Key == null) ? "" : "." + x.Key),
                            objectSerializationInfos = x.ToArray(),
                        })
                        .ToArray();

                    var enumFormatterTemplates = enumInfo
                        .GroupBy(x => x.Namespace)
                        .Select(x => new EnumTemplate()
                        {
                            Namespace = namespaceDot + "Formatters" + ((x.Key == null) ? "" : "." + x.Key),
                            enumSerializationInfos = x.ToArray()
                        })
                        .ToArray();

                    var unionFormatterTemplates = unionInfo
                        .GroupBy(x => x.Namespace)
                        .Select(x => new UnionTemplate()
                        {
                            Namespace = namespaceDot + "Formatters" + ((x.Key == null) ? "" : "." + x.Key),
                            unionSerializationInfos = x.ToArray()
                        })
                        .ToArray();

                    var resolverTemplate = new ResolverTemplate()
                    {
                        Namespace = namespaceDot + "Resolvers",
                        FormatterNamespace = namespaceDot + "Formatters",
                        ResolverName = resolverName,
                        registerInfos = genericInfo.Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo).ToArray()
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

                    if (multioutSymbol == "")
                    {
                        await OutputAsync(output, sb.ToString(), Context.CancellationToken);
                    }
                    else
                    {
                        var fname = Path.GetFileNameWithoutExtension(output) + "." + multioutSymbol + ".cs";
                        var text = $"#if {multioutSymbol}" + Environment.NewLine + sb.ToString() + Environment.NewLine + "#endif";
                        await OutputAsync(Path.Combine(Path.GetDirectoryName(output), fname), text, Context.CancellationToken);
                    }
                }
                else
                {
                    // Multiple File output
                    foreach (var x in objectInfo)
                    {
                        var template = new FormatterTemplate()
                        {
                            Namespace = namespaceDot + "Formatters" + ((x.Namespace == null) ? "" : "." + x.Namespace),
                            objectSerializationInfos = new[] { x },
                        };

                        var text = template.TransformText();
                        await OutputToDirAsync(output, template.Namespace, x.Name, multioutSymbol, text, Context.CancellationToken);
                    }

                    foreach (var x in enumInfo)
                    {
                        var template = new EnumTemplate()
                        {
                            Namespace = namespaceDot + "Formatters" + ((x.Namespace == null) ? "" : "." + x.Namespace),
                            enumSerializationInfos = new[] { x },
                        };

                        var text = template.TransformText();
                        await OutputToDirAsync(output, template.Namespace, x.Name, multioutSymbol, text, Context.CancellationToken);
                    }

                    foreach (var x in unionInfo)
                    {
                        var template = new UnionTemplate()
                        {
                            Namespace = namespaceDot + "Formatters" + ((x.Namespace == null) ? "" : "." + x.Namespace),
                            unionSerializationInfos = new[] { x },
                        };

                        var text = template.TransformText();
                        await OutputToDirAsync(output, template.Namespace, x.Name, multioutSymbol, text, Context.CancellationToken);
                    }

                    var resolverTemplate = new ResolverTemplate()
                    {
                        Namespace = namespaceDot + "Resolvers",
                        FormatterNamespace = namespaceDot + "Formatters",
                        ResolverName = resolverName,
                        registerInfos = genericInfo.Cast<IResolverRegisterInfo>().Concat(enumInfo).Concat(unionInfo).Concat(objectInfo).ToArray()
                    };

                    await OutputToDirAsync(output, resolverTemplate.Namespace, resolverTemplate.ResolverName, multioutSymbol, resolverTemplate.TransformText(), Context.CancellationToken);
                }
            }

            Console.WriteLine("Output Generation Complete:" + sw.Elapsed.ToString());
        }

        static Task OutputToDirAsync(string dir, string ns, string name, string multipleOutSymbol, string text, CancellationToken cancellationToken)
        {
            if (multipleOutSymbol == "")
            {
                return OutputAsync(Path.Combine(dir, $"{ns}_{name}".Replace(".", "_").Replace("global::", "") + ".cs"), text, cancellationToken);
            }
            else
            {
                text = $"#if {multipleOutSymbol}" + Environment.NewLine + text + Environment.NewLine + "#endif";
                return OutputAsync(Path.Combine(dir, multipleOutSymbol, $"{ns}_{name}".Replace(".", "_").Replace("global::", "") + ".cs"), text, cancellationToken);
            }
        }

        static Task OutputAsync(string path, string text, CancellationToken cancellationToken)
        {
            path = path.Replace("global::", "");

            const string prefix = "[Out]";
            Console.WriteLine(prefix + path);

            var fi = new FileInfo(path);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            return System.IO.File.WriteAllTextAsync(path, text, Encoding.UTF8, cancellationToken);
        }
    }
}
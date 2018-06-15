﻿using MessagePack.CodeGenerator.Generator;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePack.CodeGenerator
{
    class CommandlineArguments
    {
        public string InputPath { get; private set; }
        public string OutputPath { get; private set; }
        public List<string> ConditionalSymbols { get; private set; }
        public string ResolverName { get; private set; }
        public string NamespaceRoot { get; private set; }
        public bool IsUseMap { get; private set; }
        public string TargetFramework { get; private set; }
        public bool Quiet { get; private set; } = true;

        public bool IsParsed { get; set; }

        public CommandlineArguments(string[] args)
        {
            ConditionalSymbols = new List<string>();
            NamespaceRoot = "MessagePack";
            ResolverName = "GeneratedResolver";
            IsUseMap = false;

            var option = new OptionSet()
            {
                { "i|input=", "[required]Input path of analyze csproj", x => { InputPath = x; } },
                { "o|output=", "[required]Output file path", x => { OutputPath = x; } },
                { "c|conditionalsymbol=", "[optional, default=empty]conditional compiler symbol", x => { ConditionalSymbols.AddRange(x.Split(',')); } },
                { "r|resolvername=", "[optional, default=GeneratedResolver]Set resolver name", x => { ResolverName = x; } },
                { "n|namespace=", "[optional, default=MessagePack]Set namespace root name", x => { NamespaceRoot = x; } },
                { "m|usemapmode", "[optional, default=false]Force use map mode serialization", x => { IsUseMap = true; } },
                { "f|framework=", "[optional, default=first target framework]TargetFramework in the multi-target project", x => { TargetFramework = x; } },
                { "q|quiet=", "[optional, default=true]Quiet display option for workspace failures", (bool x) => { Quiet = x; } },
            };
            if (args.Length == 0)
            {
                goto SHOW_HELP;
            }
            else
            {
                option.Parse(args);

                if (InputPath == null || OutputPath == null)
                {
                    Console.WriteLine("Invalid Argument:" + string.Join(" ", args));
                    Console.WriteLine();
                    goto SHOW_HELP;
                }

                IsParsed = true;
                return;
            }

            SHOW_HELP:
            Console.WriteLine("mpc arguments help:");
            option.WriteOptionDescriptions(Console.Out);
            IsParsed = false;
        }

        public string GetNamespaceDot()
        {
            return string.IsNullOrWhiteSpace(NamespaceRoot) ? "" : NamespaceRoot + ".";
        }
    }


    class Program
    {
        static async Task Main(string[] args)
        {
            var cmdArgs = new CommandlineArguments(args);
            if (!cmdArgs.IsParsed)
            {
                return;
            }

            // Generator Start...

            var sw = Stopwatch.StartNew();
            Console.WriteLine("Project Compilation Start:" + cmdArgs.InputPath);

            var collector = await TypeCollector.CreateTypeCollector(cmdArgs.InputPath, cmdArgs.ConditionalSymbols, true,
                cmdArgs.IsUseMap, cmdArgs.TargetFramework, cmdArgs.Quiet);

            Console.WriteLine("Project Compilation Complete: " + sw.Elapsed.ToString());
            Console.WriteLine();

            sw.Restart();
            Console.WriteLine("Method Collect Start");

            var (objectInfo, enumInfo, genericInfo, unionInfo) = collector.Collect();

            Console.WriteLine("Method Collect Complete: " + sw.Elapsed.ToString());

            Console.WriteLine("Output Generation Start");
            sw.Restart();

            var objectFormatterTemplates = objectInfo
                .GroupBy(x => x.Namespace)
                .Select(x => new FormatterTemplate()
                {
                    Namespace = cmdArgs.GetNamespaceDot() + "Formatters" + ((x.Key == null) ? "" : "." + x.Key),
                    objectSerializationInfos = x.ToArray(),
                })
                .ToArray();

            var enumFormatterTemplates = enumInfo
                .GroupBy(x => x.Namespace)
                .Select(x => new EnumTemplate()
                {
                    Namespace = cmdArgs.GetNamespaceDot() + "Formatters" + ((x.Key == null) ? "" : "." + x.Key),
                    enumSerializationInfos = x.ToArray()
                })
                .ToArray();

            var unionFormatterTemplates = unionInfo
                .GroupBy(x => x.Namespace)
                .Select(x => new UnionTemplate()
                {
                    Namespace = cmdArgs.GetNamespaceDot() + "Formatters" + ((x.Key == null) ? "" : "." + x.Key),
                    unionSerializationInfos = x.ToArray()
                })
                .ToArray();

            var resolverTemplate = new ResolverTemplate()
            {
                Namespace = cmdArgs.GetNamespaceDot() + "Resolvers",
                FormatterNamespace = cmdArgs.GetNamespaceDot() + "Formatters",
                ResolverName = cmdArgs.ResolverName,
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

            Output(cmdArgs.OutputPath, sb.ToString());

            Console.WriteLine("String Generation Complete: " + sw.Elapsed.ToString());
        }

        static void Output(string path, string text)
        {
            path = path.Replace("global::", "");

            const string prefix = "[Out]";
            Console.WriteLine(prefix + path);

            var fi = new FileInfo(path);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            System.IO.File.WriteAllText(path, text, Encoding.UTF8);
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;

namespace MessagePack.Generator
{
    public class MessagepackCompiler : ConsoleAppBase
    {
        private static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder()
                .ConfigureLogging(logging => logging.ReplaceToSimpleConsole())
                .RunConsoleAppFrameworkAsync<MessagepackCompiler>(args)
                .ConfigureAwait(false);
        }

        public async Task RunAsync(
            [Option("i", "Input path of analyze csproj or directory, if input multiple csproj split with ','.")]string input,
            [Option("o", "Output file path(.cs) or directory(multiple generate file).")]string output,
            [Option("c", "Conditional compiler symbols, split with ','.")]string conditionalSymbol = null,
            [Option("r", "Set resolver name.")]string resolverName = "GeneratedResolver",
            [Option("n", "Set namespace root name.")]string @namespace = "MessagePack",
            [Option("m", "Force use map mode serialization.")]bool useMapMode = false,
            [Option("ms", "Generate #if-- files by symbols, split with ','.")]string multipleIfDirectiveOutputSymbols = null)
        {
            await new MessagePackCompiler.CodeGenerator(x => Console.WriteLine(x), this.Context.CancellationToken)
                .GenerateFileAsync(
                    input,
                    output,
                    conditionalSymbol,
                    resolverName,
                    @namespace,
                    useMapMode,
                    multipleIfDirectiveOutputSymbols).ConfigureAwait(false);
        }
    }
}

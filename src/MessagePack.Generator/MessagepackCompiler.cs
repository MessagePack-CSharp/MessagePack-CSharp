// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using ConsoleAppFramework;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Hosting;

namespace MessagePack.Generator
{
    public class MessagepackCompiler : ConsoleAppBase
    {
        private static async Task Main(string[] args)
        {
            var instance = MSBuildLocator.RegisterDefaults();
            AssemblyLoadContext.Default.Resolving += (assemblyLoadContext, assemblyName) =>
            {
                var path = Path.Combine(instance.MSBuildPath, assemblyName.Name + ".dll");
                if (File.Exists(path))
                {
                    return assemblyLoadContext.LoadFromAssemblyPath(path);
                }

                return null;
            };

            await Host.CreateDefaultBuilder()
                .ConfigureLogging(logging => logging.ReplaceToSimpleConsole())
                .RunConsoleAppFrameworkAsync<MessagepackCompiler>(args);
        }

        public async Task RunAsync(
            [Option("i", "Input path of analyze MSBuild project file or directory, if input multiple project files split with ','.")] string input,
            [Option("o", "Output file path(.cs) or directory (multiple generate file).")] string output,
            [Option("c", "Conditional compiler symbols, split with ','. Ignored if a project file is specified for input.")] string? conditionalSymbol = null,
            [Option("r", "Set resolver name.")] string resolverName = "GeneratedResolver",
            [Option("n", "Set namespace root name.")] string @namespace = "MessagePack",
            [Option("m", "Force use map mode serialization.")] bool useMapMode = false,
            [Option("ms", "Generate #if-- files by symbols, split with ','.")] string? multipleIfDirectiveOutputSymbols = null,
            [Option("ei", "Ignore type names.")] string[]? externalIgnoreTypeNames = null)
        {
            Workspace? workspace = null;
            try
            {
                Compilation compilation;
                if (Directory.Exists(input))
                {
                    string[]? conditionalSymbols = conditionalSymbol?.Split(',');
                    compilation = await PseudoCompilation.CreateFromDirectoryAsync(input, conditionalSymbols, this.Context.CancellationToken);
                }
                else
                {
                    (workspace, compilation) = await this.OpenMSBuildProjectAsync(input, this.Context.CancellationToken);
                }

                await new MessagePackCompiler.CodeGenerator(x => Console.WriteLine(x), this.Context.CancellationToken)
                    .GenerateFileAsync(
                        compilation,
                        output,
                        resolverName,
                        @namespace,
                        useMapMode,
                        multipleIfDirectiveOutputSymbols,
                        externalIgnoreTypeNames).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await Console.Error.WriteLineAsync("Canceled");
                throw;
            }
            finally
            {
                workspace?.Dispose();
            }
        }

        private async Task<(Workspace Workspace, Compilation Compilation)> OpenMSBuildProjectAsync(string projectPath, CancellationToken cancellationToken)
        {
            var workspace = MSBuildWorkspace.Create();
            try
            {
                var logger = new ConsoleLogger(Microsoft.Build.Framework.LoggerVerbosity.Quiet);
                var project = await workspace.OpenProjectAsync(projectPath, logger, null, cancellationToken);
                var compilation = await project.GetCompilationAsync(cancellationToken);
                if (compilation is null)
                {
                    throw new NotSupportedException("The project does not support creating Compilation.");
                }

                return (workspace, compilation);
            }
            catch
            {
                workspace.Dispose();
                throw;
            }
        }
    }
}

using MessagePack.CodeGenerator;
using MicroBatchFramework;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace MessagePackCompiler
{
    /// <summary>
    /// Code generator of MessagePack-CSharp.
    /// </summary>
    public class Program : BatchBase
    {
        static async Task Main(string[] args)
        {
            args = "-i foo -o tako".Split(' ');

            await new HostBuilder().RunBatchEngineAsync<Program>(args);
        }

        public async Task RunAsync(
            [Option("i", "Input path of analyze csproj or directory.")]string input,
            [Option("o", "Output file path(.cs) or directory.")]string output,
            [Option("c", "Conditional compiler symbols, split with ','.")]string conditionalSymbol = null,
            [Option("r", "Set resolver name.")]string resolverName = "GeneratedResolver",
            [Option("n", "Set namespace root name.")]string @namespace = "MessagePack",
            [Option("m", "Force use map mode serialization.")]bool useMapMode = false,
            [Option("multiout", "Generate #if-- files by symbols.")]string multipleIfDiretiveOutputBySymbolsDirectory = null
            )
        {

            input = @"C:\Users\neuecc\Documents\neuecc\MessagePack\sandbox\SharedData\SharedData.csproj";
            //input = @"C:\Users\neuecc\Documents\UnityProjects\New Unity Project\Assembly-CSharp.csproj";

            //var foo = await RoslynExtensions.GetCompilationFromProject(input);


            await CodeGenerationWorkspace.CreateAsync(input, this.Context.CancellationToken);

            // throw new System.Exception();


            //System.Console.WriteLine("Yeah!");







        }
    }

    public class ErrorInterceptor : MicroBatchFramework.IBatchInterceptor
    {
        public ValueTask OnBatchEngineBeginAsync()
        {
            return default(ValueTask);
        }

        public ValueTask OnBatchEngineEndAsync()
        {
            return default(ValueTask);
        }

        public ValueTask OnBatchRunBeginAsync(BatchContext context)
        {
            return default(ValueTask);
        }

        public ValueTask OnBatchRunCompleteAsync(BatchContext context, string errorMessageIfFailed, Exception exceptionIfExists)
        {
            if (exceptionIfExists != null)
            {
                Console.WriteLine(exceptionIfExists);
            }
            return default(ValueTask);
        }
    }
}

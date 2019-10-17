using System;
using System.Threading;
using MessagePackCompiler;
using Microsoft.Build.Framework;

namespace MessagePack.MSBuild.Tasks
{
    public class MessagePackGenerator : Microsoft.Build.Utilities.Task
    {
        [Required]
        public string Input { get; set; }
        [Required]
        public string Output { get; set; }

        public string ConditionalSymbol { get; set; }

        public string ResolverName { get; set; }

        public string Namespace { get; set; }

        public bool UseMapMode { get; set; }

        public string MultipleIfDirectiveOutputSymbols { get; set; }

        public override bool Execute()
        {
            try
            {
                new CodeGenerator(x => this.Log.LogMessage(x), CancellationToken.None)
                    .GenerateFileAsync(
                        Input,
                        Output,
                        ConditionalSymbol,
                        ResolverName ?? "GeneratedResolver",
                        Namespace ?? "MessagePack",
                        UseMapMode,
                        MultipleIfDirectiveOutputSymbols
                    )
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                this.Log.LogErrorFromException(ex, true);
                return false;
            }
            return true;
        }
    }
}

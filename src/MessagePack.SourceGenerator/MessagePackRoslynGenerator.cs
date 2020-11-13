// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using MessagePackCompiler.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MessagePackCompiler.SourceGenerator
{
    [Generator]
    public class MessagePackRoslynGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // If you want to debug, remove this comment out.
            //System.Diagnostics.Debugger.Launch();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var options = context.AnalyzerConfigOptions.GlobalOptions;
            options.TryGetValue("build_property.DisallowInternal", out var disallowInternalText);
            options.TryGetValue("build_property.UseMapMode", out var forceUseMapText);
            options.TryGetValue("build_property.ResolverTypeName", out var resolverTypeNameText);
            options.TryGetValue("build_property.ResolverNameSpace", out var resolverNameSpaceText);

            var disallowInternal = disallowInternalText == "true";
            var isForceUseMap = forceUseMapText == "true";
            if (string.IsNullOrWhiteSpace(resolverTypeNameText))
            {
                resolverTypeNameText = "GeneratedResolver";
            }

            if (string.IsNullOrWhiteSpace(resolverNameSpaceText))
            {
                resolverNameSpaceText = "MessagePack.";
            }
            else if (resolverNameSpaceText.Length > 0 && resolverNameSpaceText[resolverNameSpaceText.Length - 1] != '.')
            {
                resolverNameSpaceText += '.';
            }

            var encoding = new UTF8Encoding(false);
            var collector = new TypeCollector(context.Compilation, disallowInternal, isForceUseMap, Array.Empty<string>(), _ => { });
            var (objectInfo, enumInfo, genericInfo, unionInfo) = collector.Collect();
            var generatedSource = CodeGenerator.GenerateSingleFileSync(resolverTypeNameText, resolverNameSpaceText, objectInfo, enumInfo, unionInfo, genericInfo);
            var sourceText = SourceText.From(generatedSource, encoding);
            context.AddSource("MessagePackCSharp.SourceGenerator.GeneratedSource.cs", sourceText);
        }
    }
}

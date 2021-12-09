// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MessagePackAnalyzer
{
    /// <summary>
    /// An analyzer that guards against calling APIs that rely on static, mutable fields defining "default" options.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class MsgPack001SpecifyOptionsAnalyzer : DiagnosticAnalyzer
    {
        public const string MissingOptionsId = "MsgPack001";

        public static readonly DiagnosticDescriptor MissingOptionsDescriptor = new DiagnosticDescriptor(
            id: MissingOptionsId,
            title: new LocalizableResourceString(nameof(Strings.MsgPack001_Title), Strings.ResourceManager, typeof(Strings)),
            messageFormat: new LocalizableResourceString(nameof(Strings.MsgPack001_MessageFormat), Strings.ResourceManager, typeof(Strings)),
            category: "Reliability",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: new LocalizableResourceString(nameof(Strings.MsgPack001_Description), Strings.ResourceManager, typeof(Strings)),
            helpLinkUri: AnalyzerUtilities.GetHelpLink(MissingOptionsId));

        private static readonly ImmutableArray<DiagnosticDescriptor> ReusableSupportedDiagnostics = ImmutableArray.Create(MissingOptionsDescriptor);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ReusableSupportedDiagnostics;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                ITypeSymbol messagePackSerializationOptionsTypeSymbol = compilationStartContext.Compilation.GetTypeByMetadataName("MessagePack.MessagePackSerializerOptions");
                if (messagePackSerializationOptionsTypeSymbol is object)
                {
                    compilationStartContext.RegisterOperationAction(c => this.AnalyzeInvocation(c, messagePackSerializationOptionsTypeSymbol), OperationKind.Invocation);
                }
            });
        }

        private void AnalyzeInvocation(OperationAnalysisContext ctxt, ITypeSymbol messagePackSerializationOptionsTypeSymbol)
        {
            var operation = (IInvocationOperation)ctxt.Operation;

            // Is this an invocation on a method defined in the MessagePack assembly?
            if (Equals(operation.TargetMethod.ContainingAssembly, messagePackSerializationOptionsTypeSymbol.ContainingAssembly))
            {
                var optionsArg = operation.Arguments.FirstOrDefault(arg => Equals(arg.Value?.Type, messagePackSerializationOptionsTypeSymbol));
                if (optionsArg is object && optionsArg.Value.IsImplicit)
                {
                    // The caller is omitting a MessagePackSerializerOptions argument or setting it to null.
                    ctxt.ReportDiagnostic(Diagnostic.Create(MissingOptionsDescriptor, optionsArg.Value.Syntax.GetLocation()));
                }
            }
        }
    }
}

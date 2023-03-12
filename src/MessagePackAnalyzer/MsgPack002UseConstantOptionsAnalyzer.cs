// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MessagePackAnalyzer
{
    /// <summary>
    /// An analyzer to guide callers to avoid use of mutable static fields for MessagePackSerializerOptions.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class MsgPack002UseConstantOptionsAnalyzer : DiagnosticAnalyzer
    {
        public const string MutableSharedOptionsId = "MsgPack002";

        public static readonly DiagnosticDescriptor MutableSharedOptionsDescriptor = new DiagnosticDescriptor(
          id: MutableSharedOptionsId,
          title: new LocalizableResourceString(nameof(Strings.MsgPack002_Title), Strings.ResourceManager, typeof(Strings)),
          messageFormat: new LocalizableResourceString(nameof(Strings.MsgPack002_MessageFormat), Strings.ResourceManager, typeof(Strings)),
          category: "Reliability",
          defaultSeverity: DiagnosticSeverity.Warning,
          isEnabledByDefault: false,
          description: new LocalizableResourceString(nameof(Strings.MsgPack002_Description), Strings.ResourceManager, typeof(Strings)),
          helpLinkUri: AnalyzerUtilities.GetHelpLink(MutableSharedOptionsId));

        private static readonly ImmutableArray<DiagnosticDescriptor> ReusableSupportedDiagnostics = ImmutableArray.Create(MutableSharedOptionsDescriptor);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ReusableSupportedDiagnostics;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                ITypeSymbol? messagePackSerializationOptionsTypeSymbol = compilationStartContext.Compilation.GetTypeByMetadataName("MessagePack.MessagePackSerializerOptions");
                if (messagePackSerializationOptionsTypeSymbol is object)
                {
                    compilationStartContext.RegisterOperationAction(c => this.AnalyzeMemberReference(c, messagePackSerializationOptionsTypeSymbol), OperationKind.PropertyReference, OperationKind.FieldReference);
                }
            });
        }

        private static bool IsLessWritableThanReadable(ISymbol symbol)
        {
            if (symbol is IPropertySymbol property)
            {
                if (property.GetMethod is null)
                {
                    // The property has no getter, so the calling code has other problems.
                    // Don't report a problem.
                    return true;
                }

                if (property.SetMethod is null)
                {
                    // If the property has no setter, we're totally good.
                    return true;
                }

                return property.SetMethod.DeclaredAccessibility < property.GetMethod.DeclaredAccessibility;
            }

            if (symbol is IFieldSymbol field)
            {
                return field.IsReadOnly;
            }

            return true;
        }

        private void AnalyzeMemberReference(OperationAnalysisContext ctxt, ITypeSymbol messagePackSerializationOptionsTypeSymbol)
        {
            var memberReferenceOperation = (IMemberReferenceOperation)ctxt.Operation;
            var referencedMember = memberReferenceOperation.Member;
            if (SymbolEqualityComparer.Default.Equals(memberReferenceOperation.Type, messagePackSerializationOptionsTypeSymbol) && referencedMember.IsStatic && referencedMember.DeclaredAccessibility > Accessibility.Private && !IsLessWritableThanReadable(referencedMember))
            {
                // The caller is passing in a value from a mutable static that is as writable as it is readable (a dangerous habit).
                // TODO: fix ID, message, etc. to describe the problem.
                ctxt.ReportDiagnostic(Diagnostic.Create(MutableSharedOptionsDescriptor, memberReferenceOperation.Syntax.GetLocation()));
            }
        }
    }
}

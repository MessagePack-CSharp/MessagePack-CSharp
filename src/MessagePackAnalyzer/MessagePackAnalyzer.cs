// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePackAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MessagePackAnalyzer : DiagnosticAnalyzer
    {
        public const string UseMessagePackObjectAttributeId = "MsgPack003";
        public const string AttributeMessagePackObjectMembersId = "MsgPack004";
        public const string InvalidMessagePackObjectId = "MsgPack005";
        public const string MessagePackFormatterMustBeMessagePackFormatterId = "MsgPack006";

        internal const string Category = "Usage";

        internal const string MessagePackObjectAttributeShortName = "MessagePackObjectAttribute";
        internal const string KeyAttributeShortName = "KeyAttribute";
        internal const string IgnoreShortName = "IgnoreMemberAttribute";
        internal const string IgnoreDataMemberShortName = "IgnoreDataMemberAttribute";
        internal const string UnionAttributeShortName = "UnionAttribute";

        internal static readonly DiagnosticDescriptor TypeMustBeMessagePackObject = new DiagnosticDescriptor(
            id: UseMessagePackObjectAttributeId,
            title: "Use MessagePackObjectAttribute",
            category: Category,
            messageFormat: "Type must be marked with MessagePackObjectAttribute. {0}.", // type.Name
            description: "Type must be marked with MessagePackObjectAttribute.",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: AnalyzerUtilities.GetHelpLink(UseMessagePackObjectAttributeId));

        internal static readonly DiagnosticDescriptor MessageFormatterMustBeMessagePackFormatter = new DiagnosticDescriptor(
            id: MessagePackFormatterMustBeMessagePackFormatterId,
            title: "Must be IMessageFormatter",
            category: Category,
            messageFormat: "Type must be of IMessagePackFormatter. {0}.", // type.Name
            description: "Type must be of IMessagePackFormatter.",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: AnalyzerUtilities.GetHelpLink(UseMessagePackObjectAttributeId));

        internal static readonly DiagnosticDescriptor PublicMemberNeedsKey = new DiagnosticDescriptor(
            id: AttributeMessagePackObjectMembersId,
            title: "Attribute public members of MessagePack objects",
            category: Category,
            messageFormat: "Public members of MessagePackObject-attributed types require either KeyAttribute or IgnoreMemberAttribute. {0}.{1}.", // type.Name + "." + item.Name
            description: "Public member must be marked with KeyAttribute or IgnoreMemberAttribute.",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: AnalyzerUtilities.GetHelpLink(AttributeMessagePackObjectMembersId));

        internal static readonly DiagnosticDescriptor InvalidMessagePackObject = new DiagnosticDescriptor(
            id: InvalidMessagePackObjectId,
            title: "MessagePackObject validation",
            category: Category,
            messageFormat: "Invalid MessagePackObject definition. {0}", // details
            description: "Invalid MessagePackObject definition.",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMessagePackObjectId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            TypeMustBeMessagePackObject,
            PublicMemberNeedsKey,
            InvalidMessagePackObject,
            MessageFormatterMustBeMessagePackFormatter);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterCompilationStartAction(ctxt =>
            {
                if (ReferenceSymbols.TryCreate(ctxt.Compilation, out ReferenceSymbols? typeReferences))
                {
                    ctxt.RegisterSyntaxNodeAction(c => Analyze(c, typeReferences), SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration);
                }
            });
        }

        private static void Analyze(SyntaxNodeAnalysisContext context, ReferenceSymbols typeReferences)
        {
            TypeDeclarationSyntax typeDeclaration = (TypeDeclarationSyntax)context.Node;
            INamedTypeSymbol? declaredSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration);
            if (declaredSymbol is null)
            {
                return;
            }

            if (
               ((declaredSymbol.TypeKind == TypeKind.Interface) && declaredSymbol.GetAttributes().Any(x2 => Equals(x2.AttributeClass, typeReferences.UnionAttribute)))
            || ((declaredSymbol.TypeKind == TypeKind.Class) && declaredSymbol.GetAttributes().Any(x2 => Equals(x2.AttributeClass, typeReferences.MessagePackObjectAttribute)))
            || ((declaredSymbol.TypeKind == TypeKind.Struct) && declaredSymbol.GetAttributes().Any(x2 => Equals(x2.AttributeClass, typeReferences.MessagePackObjectAttribute))))
            {
                var reportContext = new DiagnosticsReportContext(context);
                var collector = new TypeCollector(reportContext, typeReferences);
                collector.CollectCore(declaredSymbol);
                reportContext.ReportAll();
            }
        }
    }
}

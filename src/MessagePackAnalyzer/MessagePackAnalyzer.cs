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
        private const string DiagnosticIdBase = "MessagePackAnalyzer";

        internal const string Title = "Lint of MessagePack Type.";
        internal const string Category = "Usage";

        internal const string MessagePackObjectAttributeShortName = "MessagePackObjectAttribute";
        internal const string KeyAttributeShortName = "KeyAttribute";
        internal const string IgnoreShortName = "IgnoreMemberAttribute";
        internal const string IgnoreDataMemberShortName = "IgnoreDataMemberAttribute";
        internal const string UnionAttributeShortName = "UnionAttribute";

        internal static readonly DiagnosticDescriptor TypeMustBeMessagePackObject = new DiagnosticDescriptor(
            id: DiagnosticIdBase + "_" + nameof(TypeMustBeMessagePackObject),
            title: Title,
            category: Category,
            messageFormat: "Type must be marked with MessagePackObjectAttribute. {0}.", // type.Name
            description: "Type must be marked with MessagePackObjectAttribute.",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor PublicMemberNeedsKey = new DiagnosticDescriptor(
            id: DiagnosticIdBase + "_" + nameof(PublicMemberNeedsKey),
            title: Title,
            category: Category,
            messageFormat: "Public member requires KeyAttribute or IgnoreMemberAttribute. {0}.{1}.", // type.Name + "." + item.Name
            description: "Public member must be marked with KeyAttribute or IgnoreMemberAttribute.",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidMessagePackObject = new DiagnosticDescriptor(
            id: DiagnosticIdBase + "_" + nameof(InvalidMessagePackObject),
            title: Title,
            category: Category,
            messageFormat: "Invalid MessagePackObject definition. {0}", // details
            description: "Invalid MessagePackObject definition.",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            TypeMustBeMessagePackObject,
            PublicMemberNeedsKey,
            InvalidMessagePackObject);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            SemanticModel model = context.SemanticModel;

            var typeDeclaration = context.Node as TypeDeclarationSyntax;
            if (typeDeclaration == null)
            {
                return;
            }

            INamedTypeSymbol declaredSymbol = model.GetDeclaredSymbol(typeDeclaration);
            if (declaredSymbol == null)
            {
                return;
            }

            var typeReferences = new ReferenceSymbols(model.Compilation);

            if (
               ((declaredSymbol.TypeKind == TypeKind.Interface) && declaredSymbol.GetAttributes().Any(x2 => x2.AttributeClass == typeReferences.UnionAttribute))
            || ((declaredSymbol.TypeKind == TypeKind.Class) && declaredSymbol.GetAttributes().Any(x2 => x2.AttributeClass == typeReferences.MessagePackObjectAttribute))
            || ((declaredSymbol.TypeKind == TypeKind.Struct) && declaredSymbol.GetAttributes().Any(x2 => x2.AttributeClass == typeReferences.MessagePackObjectAttribute)))
            {
                var reportContext = new DiagnosticsReportContext(context);
                var collector = new TypeCollector(reportContext, model.Compilation);
                collector.CollectCore(declaredSymbol);
                reportContext.ReportAll();
            }
        }
    }
}

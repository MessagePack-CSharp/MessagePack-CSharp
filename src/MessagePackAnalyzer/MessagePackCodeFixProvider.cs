// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace MessagePackAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MessagePackCodeFixProvider)), Shared]
    public class MessagePackCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    MessagePackAnalyzer.PublicMemberNeedsKey.Id,
                    MessagePackAnalyzer.TypeMustBeMessagePackObject.Id);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            if (root is null)
            {
                return;
            }

            SemanticModel? model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            var typeInfo = context.Diagnostics[0]?.Properties.GetValueOrDefault("type", null);
            INamedTypeSymbol? namedSymbol = typeInfo is not null
                ? model?.Compilation.GetTypeByMetadataName(typeInfo.Replace("global::", string.Empty))
                : null;

            if (namedSymbol is null)
            {
                SyntaxNode targetNode = root.FindNode(context.Span);
                var property = targetNode as PropertyDeclarationSyntax;
                var field = targetNode as FieldDeclarationSyntax;
                var dec = targetNode as VariableDeclaratorSyntax;

                ITypeSymbol? targetType = null;
                if (property == null && field == null)
                {
                    var typeDeclare = targetNode as TypeDeclarationSyntax;
                    if (typeDeclare != null)
                    {
                        targetType = model.GetDeclaredSymbol(typeDeclare);
                    }
                    else if (dec != null)
                    {
                        var fieldOrProperty = model.GetDeclaredSymbol(dec) as ISymbol;
                        if (context.Diagnostics[0].Id == MessagePackAnalyzer.TypeMustBeMessagePackObject.Id)
                        {
                            targetType = (fieldOrProperty as IPropertySymbol)?.Type;
                            if (targetType == null)
                            {
                                targetType = (fieldOrProperty as IFieldSymbol)?.Type;
                            }
                        }
                        else
                        {
                            targetType = (fieldOrProperty as IPropertySymbol)?.ContainingType;
                            if (targetType == null)
                            {
                                targetType = (fieldOrProperty as IFieldSymbol)?.ContainingType;
                            }
                        }
                    }
                }
                else
                {
                    if (context.Diagnostics[0].Id == MessagePackAnalyzer.TypeMustBeMessagePackObject.Id)
                    {
                        targetType = (property != null)
                            ? (model.GetDeclaredSymbol(property) as IPropertySymbol)?.Type
                            : (model.GetDeclaredSymbol(field!) as IFieldSymbol)?.Type;
                    }
                    else
                    {
                        targetType = (property != null)
                            ? (model.GetDeclaredSymbol(property) as IPropertySymbol)?.ContainingType
                            : (model.GetDeclaredSymbol(field!) as IFieldSymbol)?.ContainingType;
                    }
                }

                if (targetType == null)
                {
                    return;
                }

                if (targetType.TypeKind == TypeKind.Array)
                {
                    targetType = ((IArrayTypeSymbol)targetType).ElementType;
                }

                namedSymbol = targetType as INamedTypeSymbol;
                if (namedSymbol == null)
                {
                    return;
                }
            }

            if (namedSymbol.IsRecord && namedSymbol.Constructors.Any(c => IsPrimaryConstructor(c, namedSymbol)))
            {
                // We can`t specify the target of the attribute ([property:Key(X)] with the current roslyn version of this code fix.
                return;
            }

            var action = CodeAction.Create("Add MessagePack KeyAttribute", c => AddKeyAttributeAsync(context.Document, namedSymbol, c), "MessagePackAnalyzer.AddKeyAttribute");

            context.RegisterCodeFix(action, context.Diagnostics.First()); // use single.
        }

        private static async Task<Solution> AddKeyAttributeAsync(Document document, INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            var solutionEditor = new SolutionEditor(document.Project.Solution);

            ISymbol[] targets = type.GetAllMembers()
                .Where(x => x.Kind == SymbolKind.Property || x.Kind == SymbolKind.Field)
                .Where(x => x.GetAttributes().FindAttributeShortName(MessagePackAnalyzer.IgnoreShortName) == null && x.GetAttributes().FindAttributeShortName(MessagePackAnalyzer.IgnoreDataMemberShortName) == null)
                .Where(x => !x.IsStatic)
                .Where(x =>
                {
                    return x switch
                    {
                        IPropertySymbol p => p.ExplicitInterfaceImplementations.Length == 0,
                        IFieldSymbol f => !f.IsImplicitlyDeclared,
                        _ => throw new NotSupportedException("Unsupported member type."),
                    };
                })
                .ToArray();

            var startOrder = targets
                .Select(x => x.GetAttributes().FindAttributeShortName(MessagePackAnalyzer.KeyAttributeShortName))
                .Where(x => x != null)
                .Select(x => x.ConstructorArguments[0])
                .Where(x => !x.IsNull)
                .Where(x => x.Value is int)
                .Select(x => (int)x.Value!)
                .DefaultIfEmpty(-1) // if empty, start from zero.
                .Max() + 1;

            foreach (ISymbol member in targets)
            {
                if (!member.IsImplicitlyDeclared &&
                    member.GetAttributes().FindAttributeShortName(MessagePackAnalyzer.KeyAttributeShortName) is null)
                {
                    SyntaxNode node = await member.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                    var documentEditor = await solutionEditor.GetDocumentEditorAsync(document.Project.Solution.GetDocumentId(node.SyntaxTree), cancellationToken).ConfigureAwait(false);
                    var syntaxGenerator = SyntaxGenerator.GetGenerator(documentEditor.OriginalDocument);
                    documentEditor.AddAttribute(node, syntaxGenerator.Attribute("MessagePack.KeyAttribute", syntaxGenerator.LiteralExpression(startOrder++)));
                }
            }

            if (type.GetAttributes().FindAttributeShortName(MessagePackAnalyzer.MessagePackObjectAttributeShortName) == null)
            {
                SyntaxNode node = await type.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                var documentEditor = await solutionEditor.GetDocumentEditorAsync(document.Project.Solution.GetDocumentId(node.SyntaxTree), cancellationToken).ConfigureAwait(false);
                var syntaxGenerator = SyntaxGenerator.GetGenerator(documentEditor.OriginalDocument);
                documentEditor.AddAttribute(node, syntaxGenerator.Attribute("MessagePack.MessagePackObject"));
            }

            return solutionEditor.GetChangedSolution();
        }

        private static bool IsPrimaryConstructor(IMethodSymbol constructor, INamedTypeSymbol type)
        {
            var constructorParameters = constructor.Parameters;
            var typeProperties = type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(prop => !prop.IsImplicitlyDeclared)
                .ToList();

            return constructorParameters.Length == typeProperties.Count &&
                constructorParameters.All(param =>
                    typeProperties.Any(prop =>
                        prop.Name == param.Name &&
                        prop.Type.ToDisplayString() == param.Type.ToDisplayString()));
        }
    }
}

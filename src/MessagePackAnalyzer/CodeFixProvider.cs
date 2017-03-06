using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
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
                    MessagePackAnalyzer.TypeMustBeMessagePackObject.Id
                );
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            var targetNode = root.FindNode(context.Span);
            var property = targetNode as PropertyDeclarationSyntax;
            var field = targetNode as FieldDeclarationSyntax;
            var dec = targetNode as VariableDeclaratorSyntax;

            INamedTypeSymbol targetType = null;
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
                        targetType = (INamedTypeSymbol)(fieldOrProperty as IPropertySymbol)?.Type;
                        if (targetType == null)
                        {
                            targetType = (INamedTypeSymbol)(fieldOrProperty as IFieldSymbol)?.Type;
                        }
                    }
                    else
                    {
                        targetType = (INamedTypeSymbol)(fieldOrProperty as IPropertySymbol)?.ContainingType;
                        if (targetType == null)
                        {
                            targetType = (INamedTypeSymbol)(fieldOrProperty as IFieldSymbol)?.ContainingType;
                        }
                    }
                }
            }
            else
            {
                if (context.Diagnostics[0].Id == MessagePackAnalyzer.TypeMustBeMessagePackObject.Id)
                {
                    targetType = (property != null)
                        ? (INamedTypeSymbol)(model.GetDeclaredSymbol(property) as IPropertySymbol)?.Type
                        : (INamedTypeSymbol)(model.GetDeclaredSymbol(field) as IFieldSymbol)?.Type;
                }
                else
                {
                    targetType = (property != null)
                        ? (INamedTypeSymbol)(model.GetDeclaredSymbol(property) as IPropertySymbol)?.ContainingType
                        : (INamedTypeSymbol)(model.GetDeclaredSymbol(field) as IFieldSymbol)?.ContainingType;
                }
            }
            if (targetType == null) return;

            var action = CodeAction.Create("Add MessagePack KeyAttribute", c => AddKeyAttribute(context.Document, targetType, c), "MessagePackAnalyzer.AddKeyAttribute");

            context.RegisterCodeFix(action, context.Diagnostics.First()); // use single.
        }

        static async Task<Document> AddKeyAttribute(Document document, INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            if (type.DeclaringSyntaxReferences.Length != 0)
            {
                document = document.Project.GetDocument(type.DeclaringSyntaxReferences[0].SyntaxTree);
            }

            var editor = await DocumentEditor.CreateAsync(document);

            var targets = type.GetAllMembers()
                .Where(x => x.Kind == SymbolKind.Property || x.Kind == SymbolKind.Field)
                .Where(x => x.GetAttributes().FindAttributeShortName(MessagePackAnalyzer.IgnoreShortName) == null)
                .Where(x => !x.IsStatic)
                .Where(x =>
                {
                    var p = x as IPropertySymbol;
                    if (p == null)
                    {
                        var f = (x as IFieldSymbol);
                        if (f.IsImplicitlyDeclared) return false;
                        return true;
                    }

                    return p.ExplicitInterfaceImplementations.Length == 0;
                })
                .ToArray();

            var startOrder = targets
                .Select(x => x.GetAttributes().FindAttributeShortName(MessagePackAnalyzer.KeyAttributeShortName))
                .Where(x => x != null)
                .Select(x => x.ConstructorArguments[0])
                .Where(x => !x.IsNull)
                .Where(x => x.Value.GetType() == typeof(int))
                .Select(x => (int)x.Value)
                .DefaultIfEmpty(-1) // if empty, start from zero.
                .Max() + 1;

            foreach (var item in targets)
            {
                var node = await item.DeclaringSyntaxReferences[0].GetSyntaxAsync();

                var attr = item.GetAttributes().FindAttributeShortName(MessagePackAnalyzer.KeyAttributeShortName);
                if (attr != null) continue; // already tagged Index.

                var attribute = RoslynExtensions.ParseAttributeList($"[Key({startOrder++})]")
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

                editor.AddAttribute(node, attribute);
            }

            if (type.GetAttributes().FindAttributeShortName(MessagePackAnalyzer.MessagePackObjectAttributeShortName) == null)
            {
                var rootNode = await type.DeclaringSyntaxReferences[0].GetSyntaxAsync();
                editor.AddAttribute(rootNode, RoslynExtensions.ParseAttributeList("[MessagePackObject]"));
            }

            var newDocument = editor.GetChangedDocument();
            var newRoot = editor.GetChangedRoot() as CompilationUnitSyntax;
            newDocument = newDocument.WithSyntaxRoot(newRoot.WithUsing("MessagePack"));

            return newDocument;
        }
    }
}

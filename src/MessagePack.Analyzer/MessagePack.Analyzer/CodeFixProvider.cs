using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePack.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MessagePackCodeFixProvider)), Shared]
    public class MessagePackCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    MessagePackAnalyzer.PublicMemberNeedsKey.Id
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
            if (property == null || field == null) return;

            var targetType = (property != null)
                ? model.GetDeclaredSymbol(property)?.ContainingType
                : model.GetDeclaredSymbol(field)?.ContainingType;
            if (targetType == null) return;

            var action = CodeAction.Create("Add KeyAttribute", c => AddKeyAttribute(context.Document, targetType, c), "MessagePackAnalyzer.PublicMemberNeedsKey");

            context.RegisterCodeFix(action, context.Diagnostics.First()); // use single.
        }

        static async Task<Document> AddKeyAttribute(Document document, INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document);

            var targets = type.GetAllMembers()
                .Where(x => x.Kind == SymbolKind.Property || x.Kind == SymbolKind.Field)
                .Where(x => x.GetAttributes().FindAttributeShortName(MessagePackAnalyzer.IgnoreShortName) == null)
                .Where(x => !x.IsStatic)
                .Where(x =>
                {
                    var p = x as IPropertySymbol;
                    if (p == null) return true;

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

                var attribute = RoslynExtensions.ParseAttributeList($"[Key({startOrder++})]");

                editor.AddAttribute(node, attribute);
            }

            var newDocument = editor.GetChangedDocument();
            return newDocument;
        }
    }
}
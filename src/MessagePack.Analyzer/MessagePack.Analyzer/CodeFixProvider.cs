//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CodeActions;
//using Microsoft.CodeAnalysis.CodeFixes;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Editing;
//using System.Collections.Immutable;
//using System.Composition;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace ZeroFormatter.Analyzer
//{
//    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ZeroFormatterCodeFixProvider)), Shared]
//    public class ZeroFormatterCodeFixProvider : CodeFixProvider
//    {
//        public sealed override ImmutableArray<string> FixableDiagnosticIds
//        {
//            get
//            {
//                return ImmutableArray.Create(
//                    ZeroFormatterAnalyzer.PublicPropertyNeedsIndex.Id,
//                    ZeroFormatterAnalyzer.PublicPropertyMustBeVirtual.Id
//                );
//            }
//        }

//        public sealed override FixAllProvider GetFixAllProvider()
//        {
//            return WellKnownFixAllProviders.BatchFixer;
//        }

//        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
//        {
//            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
//            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

//            var targetNode = root.FindNode(context.Span);
//            var property = targetNode as PropertyDeclarationSyntax;
//            if (property == null) return;

//            var targetType = model.GetDeclaredSymbol(property)?.ContainingType;
//            if (targetType == null) return;
//            if (!targetType.GetAllMembers().OfType<IPropertySymbol>().Any()) return;

//            var action = CodeAction.Create("Add IndexAttribute and Set 'virtual'", c => AddIndexAttributeAndSetVirtual(context.Document, targetType, c));

//            context.RegisterCodeFix(action, context.Diagnostics.First()); // use single.
//        }

//        static async Task<Document> AddIndexAttributeAndSetVirtual(Document document, INamedTypeSymbol type, CancellationToken cancellationToken)
//        {
//            var editor = await DocumentEditor.CreateAsync(document);

//            var targets = type.GetAllMembers().OfType<IPropertySymbol>()
//                .Where(x => x.GetAttributes().FindAttributeShortName(ZeroFormatterAnalyzer.IgnoreShortName) == null)
//                .Where(x => !x.IsStatic)
//                .Where(x => x.ExplicitInterfaceImplementations.Length == 0)
//                .ToArray();

//            var startOrder = targets
//                .Select(x => x.GetAttributes().FindAttributeShortName(ZeroFormatterAnalyzer.IndexAttributeShortName))
//                .Where(x => x != null)
//                .Select(x => x.ConstructorArguments[0])
//                .Where(x => !x.IsNull)
//                .Select(x => (int)x.Value)
//                .DefaultIfEmpty(-1) // if empty, start from zero.
//                .Max() + 1;

//            foreach (var item in targets)
//            {
//                var node = await item.DeclaringSyntaxReferences[0].GetSyntaxAsync();
//                editor.SetModifiers(node, DeclarationModifiers.Virtual); // force virtual

//                var attr = item.GetAttributes().FindAttributeShortName(ZeroFormatterAnalyzer.IndexAttributeShortName);
//                if (attr != null) continue; // already tagged Index.

//                var attribute = RoslynExtensions.ParseAttributeList($"[Index({startOrder++})]");

//                editor.AddAttribute(node, attribute);
//            }

//            var newDocument = editor.GetChangedDocument();
//            return newDocument;
//        }
//    }
//}
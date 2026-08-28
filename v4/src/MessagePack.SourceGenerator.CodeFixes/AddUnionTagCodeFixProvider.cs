using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace MessagePack.CodeFixes;

/// <summary>
/// MsgPack106 (closed hierarchy) and MsgPack107 (pattern union): an untagged case gets its
/// [UnionTag(typeof(Case), nextTag)] appended on the root, whose identity rides in the
/// diagnostic's RootDocId/CaseDocId properties (the tag continues after the highest
/// already declared — the tag rides last in every attribute shape).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddUnionTagCodeFixProvider))]
[Shared]
public sealed class AddUnionTagCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("MsgPack106", "MsgPack107");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (!diagnostic.Properties.TryGetValue("RootDocId", out var rootId) || rootId is null
                || !diagnostic.Properties.TryGetValue("CaseDocId", out var caseId) || caseId is null)
            {
                continue;
            }
            var compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken).ConfigureAwait(false);
            if (compilation is null
                || DocumentationCommentId.GetFirstSymbolForDeclarationId(rootId, compilation) is not INamedTypeSymbol root
                || DocumentationCommentId.GetFirstSymbolForDeclarationId(caseId, compilation) is not INamedTypeSymbol caseType)
            {
                continue;
            }
            Document? rootDocument = null;
            TypeDeclarationSyntax? rootDeclaration = null;
            foreach (var syntaxReference in root.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is TypeDeclarationSyntax declaration
                    && context.Document.Project.Solution.GetDocument(syntaxReference.SyntaxTree) is { } document)
                {
                    (rootDocument, rootDeclaration) = (document, declaration);
                    break;
                }
            }
            if (rootDocument is null || rootDeclaration is null)
            {
                continue;
            }
            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Add [UnionTag] for '{caseType.Name}' to '{root.Name}'",
                    cancellationToken => AddTagAsync(rootDocument, rootDeclaration, root, caseType, cancellationToken),
                    equivalenceKey: $"AddUnionTag:{rootId}:{caseId}"),
                diagnostic);
        }
    }

    static async Task<Solution> AddTagAsync(Document document, TypeDeclarationSyntax rootDeclaration, INamedTypeSymbol root, INamedTypeSymbol caseType, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        AnnotationSyntax.AddUnionTag(editor, rootDeclaration, root, caseType);
        return editor.GetChangedDocument().Project.Solution;
    }
}

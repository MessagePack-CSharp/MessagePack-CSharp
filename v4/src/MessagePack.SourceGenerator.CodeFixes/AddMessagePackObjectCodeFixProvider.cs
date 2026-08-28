using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace MessagePack.CodeFixes;

/// <summary>
/// MsgPack103 (a [UnionTag] root without [MessagePackObject]): annotate the root.
/// MsgPack101 (a serialized member's type has no resolvable formatter): annotate the
/// OFFENDING type — handed over in the diagnostic's OffenderDocId property, since the
/// diagnostic sits on the member and the offender may live in another document — with
/// [MessagePackObject], and number its members with [Key] in the same edit.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddMessagePackObjectCodeFixProvider))]
[Shared]
public sealed class AddMessagePackObjectCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("MsgPack101", "MsgPack103", "MsgPack108");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id == "MsgPack103")
            {
                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                if (root?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<TypeDeclarationSyntax>() is { } declaration)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            $"Add [MessagePackObject] to '{declaration.Identifier.ValueText}'",
                            cancellationToken => AnnotateRootAsync(context.Document, declaration, cancellationToken),
                            equivalenceKey: $"AddMessagePackObject:{declaration.Identifier.ValueText}"),
                        diagnostic);
                }
                continue;
            }

            if (!diagnostic.Properties.TryGetValue("OffenderDocId", out var declarationId) || declarationId is null)
            {
                continue;
            }
            var compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken).ConfigureAwait(false);
            if (compilation is null
                || DocumentationCommentId.GetFirstSymbolForDeclarationId(declarationId, compilation) is not INamedTypeSymbol offender)
            {
                continue;
            }
            var (offenderDocument, offenderDeclaration) = FindDeclaration(context.Document.Project.Solution, offender);
            if (offenderDocument is null || offenderDeclaration is null)
            {
                continue;
            }
            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Annotate '{offender.Name}' with [MessagePackObject] and [Key]",
                    cancellationToken => AnnotateOffenderAsync(offenderDocument, offenderDeclaration, cancellationToken),
                    equivalenceKey: $"AnnotateOffender:{declarationId}"),
                diagnostic);
        }
    }

    static (Document?, TypeDeclarationSyntax?) FindDeclaration(Solution solution, INamedTypeSymbol type)
    {
        foreach (var syntaxReference in type.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is TypeDeclarationSyntax declaration
                && solution.GetDocument(syntaxReference.SyntaxTree) is { } document)
            {
                return (document, declaration);
            }
        }
        return (null, null);
    }

    static async Task<Document> AnnotateRootAsync(Document document, TypeDeclarationSyntax declaration, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        AnnotationSyntax.AddMessagePackObject(editor, declaration);
        return editor.GetChangedDocument();
    }

    static async Task<Solution> AnnotateOffenderAsync(Document document, TypeDeclarationSyntax declaration, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        AnnotationSyntax.AddMessagePackObject(editor, declaration);
        AnnotationSyntax.AddKeyAttributes(editor, editor.SemanticModel, declaration);
        return editor.GetChangedDocument().Project.Solution;
    }
}

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace MessagePack.CodeFixes;

/// <summary>
/// MsgPack001 (a public member of a [MessagePackObject] type needs [Key] or [IgnoreMember]): one whole-type action
/// numbers every unannotated member, properties then fields, continuing after the highest key already present,
/// v3's flagship fix.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddKeyAttributesCodeFixProvider))]
[Shared]
public sealed class AddKeyAttributesCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("MsgPack001");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var declaration = root?.FindNode(context.Span).FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (declaration is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                $"Add [Key] attributes to '{declaration.Identifier.ValueText}'",
                cancellationToken => AddKeysAsync(context.Document, declaration, cancellationToken),
                equivalenceKey: $"AddKeys:{declaration.Identifier.ValueText}"),
            context.Diagnostics);
    }

    static async Task<Document> AddKeysAsync(Document document, TypeDeclarationSyntax declaration, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        AnnotationSyntax.AddKeyAttributes(editor, editor.SemanticModel, declaration);
        return editor.GetChangedDocument();
    }
}

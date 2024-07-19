// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MessagePackCodeFixProvider)), Shared]
public class MsgPack011CodeFixProvider : CodeFixProvider
{
    private static readonly ImmutableArray<string> FixableIds = ImmutableArray.Create(MsgPack00xMessagePackAnalyzer.PartialTypeRequired.Id);

    public sealed override ImmutableArray<string> FixableDiagnosticIds => FixableIds;

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case MsgPack00xMessagePackAnalyzer.PartialTypeRequiredId:
                    SyntaxNode? syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
                    BaseTypeDeclarationSyntax? typeDecl = syntaxRoot?.FindNode(diagnostic.Location.SourceSpan) as BaseTypeDeclarationSyntax;

                    if (syntaxRoot is not null && typeDecl is not null)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Add partial modifier",
                                ct => AddPartialModifierAsync(context.Document, syntaxRoot, typeDecl, diagnostic, ct),
                                "AddPartialModifier"),
                            diagnostic);
                    }

                    break;
            }
        }
    }

    private static Task<Document> AddPartialModifierAsync(Document document, SyntaxNode syntaxRoot, BaseTypeDeclarationSyntax typeDecl, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        SyntaxNode? modifiedSyntax = syntaxRoot.ReplaceNode(
            typeDecl,
            typeDecl.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword)));
        document = document.WithSyntaxRoot(modifiedSyntax);
        return Task.FromResult(document);
    }
}

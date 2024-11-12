// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MsgPack015CodeFixProvider)), Shared]
public class MsgPack015CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MsgPack00xMessagePackAnalyzer.MessagePackObjectAllowPrivateRequired.Id);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            if (root is null)
            {
                return;
            }

            SyntaxNode targetNode = root.FindNode(diagnostic.Location.SourceSpan);
            if (targetNode.FirstAncestorOrSelf<AttributeSyntax>() is AttributeSyntax attSyntax)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Set AllowPrivate = true",
                        cancellationToken => SetAllowPrivateAsync(context.Document, attSyntax, cancellationToken),
                        nameof(MessagePackCodeFixProvider)),
                    diagnostic);
            }
        }
    }

    private async Task<Document> SetAllowPrivateAsync(Document document, AttributeSyntax attSyntax, CancellationToken cancellationToken)
    {
        SyntaxNode? modifiedRoot = await document.GetSyntaxRootAsync(cancellationToken);
        if (modifiedRoot is null)
        {
            return document;
        }

        AttributeArgumentSyntax? existingArgument = attSyntax.ArgumentList?.Arguments.FirstOrDefault(x => x.NameEquals?.Name.Identifier.Text == MsgPack00xMessagePackAnalyzer.AllowPrivatePropertyName);
        if (existingArgument is null)
        {
            AttributeArgumentListSyntax? argumentList = attSyntax.ArgumentList ?? SyntaxFactory.AttributeArgumentList();
            argumentList = argumentList.AddArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals(MsgPack00xMessagePackAnalyzer.AllowPrivatePropertyName), null, SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)));
            modifiedRoot = modifiedRoot.ReplaceNode(attSyntax, attSyntax.WithArgumentList(argumentList));
        }
        else
        {
            modifiedRoot = modifiedRoot.ReplaceNode(existingArgument.Expression, SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));
        }

        return document.WithSyntaxRoot(modifiedRoot);
    }
}

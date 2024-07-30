// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace MessagePack.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SimpleBaseTypeSyntax)), Shared]
public class FormatterCodeFixProvider : CodeFixProvider
{
    private static readonly ImmutableArray<string> FixableIds = ImmutableArray.Create(
        MsgPack00xMessagePackAnalyzer.InaccessibleFormatterTypeId,
        MsgPack00xMessagePackAnalyzer.InaccessibleFormatterInstanceId,
        MsgPack00xMessagePackAnalyzer.PartialTypeRequired.Id);

    public sealed override ImmutableArray<string> FixableDiagnosticIds => FixableIds;

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            SyntaxNode? syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            BaseTypeDeclarationSyntax? typeDecl = syntaxRoot?.FindNode(diagnostic.Location.SourceSpan) as BaseTypeDeclarationSyntax;
            if (syntaxRoot is not null && typeDecl is not null)
            {
                switch (diagnostic.Id)
                {
                    case MsgPack00xMessagePackAnalyzer.PartialTypeRequiredId:
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Add partial modifier",
                                ct => AddPartialModifierAsync(context.Document, syntaxRoot, typeDecl, diagnostic, ct),
                                "AddPartialModifier"),
                            diagnostic);

                        break;
                    case MsgPack00xMessagePackAnalyzer.InaccessibleFormatterTypeId:
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Make type internal",
                                ct => ExposeMemberInternally(context.Document, syntaxRoot, typeDecl, diagnostic, ct),
                                "MakeTypeInternal"),
                            diagnostic);

                        break;
                    case MsgPack00xMessagePackAnalyzer.InaccessibleFormatterInstanceId:
                        SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
                        if (semanticModel?.GetDeclaredSymbol(typeDecl, context.CancellationToken) is INamedTypeSymbol typeSymbol)
                        {
                            IMethodSymbol? defaultCtor = typeSymbol.InstanceConstructors.FirstOrDefault(c => c.Parameters.Length == 0);
                            if (defaultCtor is { DeclaringSyntaxReferences: { Length: > 0 } })
                            {
                                MemberDeclarationSyntax? ctorSyntax = defaultCtor.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken) as MemberDeclarationSyntax;
                                if (ctorSyntax is not null)
                                {
                                    syntaxRoot = await defaultCtor.DeclaringSyntaxReferences[0].SyntaxTree.GetRootAsync(context.CancellationToken);
                                    context.RegisterCodeFix(
                                        CodeAction.Create(
                                            "Make constructor internal",
                                            ct => ExposeMemberInternally(context.Document, syntaxRoot, ctorSyntax, diagnostic, context.CancellationToken),
                                            "MakeCtorInternal"),
                                        diagnostic);
                                }
                            }
                        }

                        break;
                }
            }
        }
    }

    private static Task<Document> AddPartialModifierAsync(Document document, SyntaxNode syntaxRoot, BaseTypeDeclarationSyntax typeDecl, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        SyntaxNode? modifiedSyntax = syntaxRoot;
        if (TryAddPartialModifier(typeDecl, out BaseTypeDeclarationSyntax? modified))
        {
            modifiedSyntax = modifiedSyntax.ReplaceNode(typeDecl, modified);
        }

        foreach (Location addlLocation in diagnostic.AdditionalLocations)
        {
            BaseTypeDeclarationSyntax? addlType = modifiedSyntax.FindNode(addlLocation.SourceSpan) as BaseTypeDeclarationSyntax;
            if (addlType is not null && TryAddPartialModifier(addlType, out modified))
            {
                modifiedSyntax = modifiedSyntax.ReplaceNode(addlType, modified);
            }
        }

        document = document.WithSyntaxRoot(modifiedSyntax);
        return Task.FromResult(document);
    }

    private static Task<Document> ExposeMemberInternally(Document document, SyntaxNode syntaxRoot, MemberDeclarationSyntax memberDecl, Diagnostic diagnostic, CancellationToken ct)
    {
        SyntaxNode? modifiedSyntax = syntaxRoot.ReplaceNode(memberDecl, memberDecl.WithModifiers(AddInternalVisibility(memberDecl.Modifiers)));
        document = document.WithSyntaxRoot(modifiedSyntax);
        return Task.FromResult(document);
    }

    private static SyntaxTokenList AddInternalVisibility(SyntaxTokenList modifiers)
    {
        SyntaxToken internalKeywordToken = SyntaxFactory.Token(SyntaxKind.InternalKeyword);

        int privateIndex = -1, protectedIndex = -1, internalIndex = -1, publicIndex = -1;
        for (int i = 0; i < modifiers.Count; i++)
        {
            switch (modifiers[i].Kind())
            {
                case SyntaxKind.PrivateKeyword:
                    privateIndex = i;
                    break;
                case SyntaxKind.ProtectedKeyword:
                    protectedIndex = i;
                    break;
                case SyntaxKind.InternalKeyword:
                    internalIndex = i;
                    break;
                case SyntaxKind.PublicKeyword:
                    publicIndex = i;
                    break;
            }
        }

        if (internalIndex != -1)
        {
            // Nothing to do.
            return modifiers;
        }

        if (privateIndex != -1 && protectedIndex != -1)
        {
            // Upgrade private protected to internal.
            SyntaxTokenList newModifiers = privateIndex < protectedIndex
                ? modifiers.RemoveAt(protectedIndex).RemoveAt(privateIndex)
                : modifiers.RemoveAt(privateIndex).RemoveAt(protectedIndex);
            return newModifiers.Insert(0, internalKeywordToken);
        }

        if (protectedIndex != -1)
        {
            // upgrade to protected internal
            return modifiers.Insert(protectedIndex + 1, internalKeywordToken);
        }

        if (privateIndex != -1)
        {
            return ReplaceModifierInList(privateIndex);
        }

        // No visibility keywords exist. Add "internal".
        return modifiers.Insert(0, internalKeywordToken);

        SyntaxToken ReplaceModifier(SyntaxToken original) => internalKeywordToken.WithLeadingTrivia(original.LeadingTrivia).WithTrailingTrivia(original.TrailingTrivia);
        SyntaxTokenList ReplaceModifierInList(int index) => modifiers.Replace(modifiers[index], ReplaceModifier(modifiers[index]));
    }

    private static bool TryAddPartialModifier(BaseTypeDeclarationSyntax typeDeclaration, [NotNullWhen(true)] out BaseTypeDeclarationSyntax? modified)
    {
        if (typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            modified = null;
            return false;
        }

        modified = typeDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));
        return true;
    }
}

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
        MsgPack00xMessagePackAnalyzer.PartialTypeRequiredId,
        MsgPack00xMessagePackAnalyzer.NullableReferenceTypeFormatterId);

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
            CSharpSyntaxNode? diagnosticTargetNode = syntaxRoot?.FindNode(diagnostic.Location.SourceSpan) as CSharpSyntaxNode;
            BaseTypeDeclarationSyntax? typeDecl = diagnosticTargetNode as BaseTypeDeclarationSyntax;
            if (syntaxRoot is not null)
            {
                switch (diagnostic.Id)
                {
                    case MsgPack00xMessagePackAnalyzer.PartialTypeRequiredId when typeDecl is not null:
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Add partial modifier",
                                ct => AddPartialModifierAsync(context.Document, syntaxRoot, typeDecl, diagnostic, ct),
                                "AddPartialModifier"),
                            diagnostic);

                        break;
                    case MsgPack00xMessagePackAnalyzer.InaccessibleFormatterTypeId when typeDecl is not null:
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Make type internal",
                                ct => ExposeMemberInternally(context.Document, syntaxRoot, typeDecl, diagnostic, ct),
                                "MakeTypeInternal"),
                            diagnostic);

                        break;
                    case MsgPack00xMessagePackAnalyzer.InaccessibleFormatterInstanceId when typeDecl is not null:
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
                    case MsgPack00xMessagePackAnalyzer.NullableReferenceTypeFormatterId:
                        if (diagnosticTargetNode is TypeSyntax typeArg)
                        {
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Add nullable annotation",
                                    ct => AddNullableAnnotationAsync(context, typeArg, ct),
                                    "NullableRefAddition"),
                                diagnostic);
                        }

                        break;
                }
            }
        }
    }

    private static async Task<Document> AddNullableAnnotationAsync(CodeFixContext context, TypeSyntax typeArg, CancellationToken ct)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(ct);
        if (root is null)
        {
            return context.Document;
        }

        // Attempt to update any and all references to this type.
        BaseTypeDeclarationSyntax? typeDecl = typeArg.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>();
        if (typeDecl is not null)
        {
            SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(ct);
            if (semanticModel is not null)
            {
                ITypeSymbol? nonAnnotated = semanticModel.GetTypeInfo(typeArg, ct).Type;
                if (nonAnnotated is not null)
                {
                    BaseTypeDeclarationSyntax updatedTypeDecl = typeDecl.ReplaceNodes(
                        typeDecl.DescendantNodes().OfType<TypeSyntax>(),
                        (old, current) =>
                        {
                            ITypeSymbol? actual = semanticModel.GetTypeInfo(old, ct).Type;
                            return SymbolEqualityComparer.IncludeNullability.Equals(actual, nonAnnotated)
                                ? SyntaxFactory.NullableType(current.WithoutTrailingTrivia()).WithTrailingTrivia(current.GetTrailingTrivia())
                                : current;
                        });
                    root = root.ReplaceNode(typeDecl, updatedTypeDecl);
                }
            }
        }

        Document document = context.Document;
        document = document.WithSyntaxRoot(root);
        return await LineEndingPreserver.NormalizeDocumentAsync(context.Document, document, ct).ConfigureAwait(false);
    }

    private static async Task<Document> AddPartialModifierAsync(Document document, SyntaxNode syntaxRoot, BaseTypeDeclarationSyntax typeDecl, Diagnostic diagnostic, CancellationToken cancellationToken)
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

        Document changedDocument = document.WithSyntaxRoot(modifiedSyntax);
        return await LineEndingPreserver.NormalizeDocumentAsync(document, changedDocument, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> ExposeMemberInternally(Document document, SyntaxNode syntaxRoot, MemberDeclarationSyntax memberDecl, Diagnostic diagnostic, CancellationToken ct)
    {
        SyntaxNode? modifiedSyntax = syntaxRoot.ReplaceNode(memberDecl, AddInternalVisibility(memberDecl));
        Document changedDocument = document.WithSyntaxRoot(modifiedSyntax);
        return await LineEndingPreserver.NormalizeDocumentAsync(document, changedDocument, ct).ConfigureAwait(false);
    }

    private static MemberDeclarationSyntax AddInternalVisibility(MemberDeclarationSyntax memberDecl)
    {
        if (memberDecl.Modifiers.Count == 0)
        {
            SyntaxToken firstToken = memberDecl.GetFirstToken();
            SyntaxToken internalToken = SyntaxFactory.Token(SyntaxKind.InternalKeyword)
                .WithLeadingTrivia(NormalizeLineEndingTrivia(firstToken.LeadingTrivia))
                .WithTrailingTrivia(SyntaxFactory.Space);

            MemberDeclarationSyntax withModifier = memberDecl.WithModifiers(memberDecl.Modifiers.Insert(0, internalToken));
            SyntaxToken secondToken = withModifier.GetFirstToken().GetNextToken();
            return withModifier.ReplaceToken(secondToken, secondToken.WithLeadingTrivia(default(SyntaxTriviaList)));
        }

        return memberDecl.WithModifiers(AddInternalVisibility(memberDecl.Modifiers));
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
            int firstIndex = Math.Min(privateIndex, protectedIndex);
            int secondIndex = Math.Max(privateIndex, protectedIndex);
            SyntaxToken firstToken = modifiers[firstIndex];
            SyntaxToken secondToken = modifiers[secondIndex];
            SyntaxToken replacementToken = internalKeywordToken
                .WithLeadingTrivia(NormalizeLineEndingTrivia(firstToken.LeadingTrivia))
                .WithTrailingTrivia(NormalizeLineEndingTrivia(secondToken.TrailingTrivia));
            SyntaxTokenList newModifiers = modifiers.RemoveAt(secondIndex).RemoveAt(firstIndex);
            return newModifiers.Insert(firstIndex, replacementToken);
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
        return modifiers.Insert(0, internalKeywordToken.WithTrailingTrivia(SyntaxFactory.Space));

        SyntaxToken ReplaceModifier(SyntaxToken original) => internalKeywordToken.WithLeadingTrivia(original.LeadingTrivia).WithTrailingTrivia(original.TrailingTrivia);
        SyntaxTokenList ReplaceModifierInList(int index) => modifiers.Replace(modifiers[index], ReplaceModifier(modifiers[index]));
    }

    private static SyntaxTriviaList NormalizeLineEndingTrivia(SyntaxTriviaList trivia)
    {
        if (!trivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia) && t.ToFullString().Contains('\r')))
        {
            return trivia;
        }

        return SyntaxFactory.TriviaList(trivia.Select(t =>
            t.IsKind(SyntaxKind.EndOfLineTrivia) && t.ToFullString().Contains('\r')
                ? SyntaxFactory.EndOfLine("\n")
                : t));
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

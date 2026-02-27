// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MessagePackCodeFixProvider)), Shared]
public class MessagePackCodeFixProvider : CodeFixProvider
{
    public const string AddKeyAttributeEquivanceKey = "MessagePackAnalyzer.AddKeyAttribute";
    public const string AddIgnoreMemberAttributeEquivalenceKey = "MessagePackAnalyzer.AddIgnoreMemberAttribute";

    public override ImmutableArray<string> FixableDiagnosticIds
    {
        get
        {
            return ImmutableArray.Create(
                MsgPack00xMessagePackAnalyzer.MemberNeedsKey.Id,
                MsgPack00xMessagePackAnalyzer.TypeMustBeMessagePackObject.Id);
        }
    }

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
        if (root is null)
        {
            return;
        }

        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (model is null)
        {
            return;
        }

        var targetNode = root.FindNode(context.Span);
        var myTypeInfo = model.GetTypeInfo(targetNode, context.CancellationToken);

        var typeName = context.Diagnostics[0]?.Properties.GetValueOrDefault("type", null);
        var namedSymbol =
            myTypeInfo.Type as INamedTypeSymbol ??
            (typeName is not null ? model.Compilation.GetTypeByMetadataName(typeName.Replace("global::", string.Empty)) : null);

        if (namedSymbol is null)
        {
            var property = targetNode as PropertyDeclarationSyntax;
            var field = targetNode as FieldDeclarationSyntax;
            var dec = targetNode as VariableDeclaratorSyntax;
            var identifierName = targetNode as IdentifierNameSyntax;

            ITypeSymbol? targetType = null;
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
                    if (context.Diagnostics[0].Id == MsgPack00xMessagePackAnalyzer.TypeMustBeMessagePackObject.Id)
                    {
                        targetType = (fieldOrProperty as IPropertySymbol)?.Type;
                        if (targetType == null)
                        {
                            targetType = (fieldOrProperty as IFieldSymbol)?.Type;
                        }
                    }
                    else
                    {
                        targetType = (fieldOrProperty as IPropertySymbol)?.ContainingType;
                        if (targetType == null)
                        {
                            targetType = (fieldOrProperty as IFieldSymbol)?.ContainingType;
                        }
                    }
                }
                else if (targetNode.Parent?.Parent is RecordDeclarationSyntax recordDeclarationSyntax)
                {
                    // Support primary constructors for records (but not for classes, since their semantics differ!).
                    targetType = model.GetDeclaredSymbol(recordDeclarationSyntax) as ITypeSymbol;
                }
            }
            else
            {
                if (context.Diagnostics[0].Id == MsgPack00xMessagePackAnalyzer.TypeMustBeMessagePackObject.Id)
                {
                    targetType = property != null
                        ? (model.GetDeclaredSymbol(property) as IPropertySymbol)?.Type
                        : (model.GetDeclaredSymbol(field!) as IFieldSymbol)?.Type;
                }
                else
                {
                    targetType = property != null
                        ? (model.GetDeclaredSymbol(property) as IPropertySymbol)?.ContainingType
                        : (model.GetDeclaredSymbol(field!) as IFieldSymbol)?.ContainingType;
                }
            }

            if (targetType == null)
            {
                return;
            }

            if (targetType.TypeKind == TypeKind.Array)
            {
                targetType = ((IArrayTypeSymbol)targetType).ElementType;
            }

            namedSymbol = targetType as INamedTypeSymbol;
            if (namedSymbol == null)
            {
                return;
            }
        }

        CodeAction addKeyAction = CodeAction.Create("Add MessagePack KeyAttribute", c => AddKeyAttributeAsync(context.Document, namedSymbol, c), AddKeyAttributeEquivanceKey);
        context.RegisterCodeFix(addKeyAction, context.Diagnostics.First());

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            SyntaxNode? diagnosticTargetNode = root.FindNode(diagnostic.Location.SourceSpan);
            if (diagnosticTargetNode?.AncestorsAndSelf().FirstOrDefault(n => n is FieldDeclarationSyntax or PropertyDeclarationSyntax) is MemberDeclarationSyntax fieldOrProperty)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Add MessagePack [IgnoreMember]",
                        ct => AddIgnoreMemberAttributeAsync(context.Document, fieldOrProperty, ct),
                        AddIgnoreMemberAttributeEquivalenceKey),
                    diagnostic);
            }
        }
    }

    private static IEnumerable<ISymbol> FindMembersMissingAttributes(INamedTypeSymbol type)
    {
        bool nonPublicMembersNeedAttributes = false;
        ISymbol[] candidateMembers = type.GetAllMembers()
            .Where(x => x.Kind == SymbolKind.Property || x.Kind == SymbolKind.Field)
            .Where(x => !x.IsStatic)
            .Where(x =>
            {
                return x switch
                {
                    IPropertySymbol p => p.ExplicitInterfaceImplementations.Length == 0,
                    IFieldSymbol f => !f.IsImplicitlyDeclared,
                    _ => throw new NotSupportedException("Unsupported member type."),
                };
            }).ToArray();

        // Do we need to attribute non-public members? We do if the user opted in or if any non-public members carry attributes.
        bool? allowPrivateAttribute = (bool?)type.GetAttributes().FindAttributeShortName(MsgPack00xMessagePackAnalyzer.MessagePackObjectAttributeShortName)?.NamedArguments.FirstOrDefault(a => a.Key == MsgPack00xMessagePackAnalyzer.AllowPrivatePropertyName).Value.Value;
        nonPublicMembersNeedAttributes |= allowPrivateAttribute is true || candidateMembers.Any(x => HasAnyIgnoreAttribute(x) || HasAnyKeyAttribute(x));

        return candidateMembers
            .Where(x => !HasAnyIgnoreAttribute(x) && (nonPublicMembersNeedAttributes || (x.DeclaredAccessibility & Accessibility.Public) == Accessibility.Public));

        static bool HasAnyIgnoreAttribute(ISymbol symbol) => symbol.GetAttributes().FindAttributeShortName(MsgPack00xMessagePackAnalyzer.IgnoreShortName) is not null || symbol.GetAttributes().FindAttributeShortName(MsgPack00xMessagePackAnalyzer.IgnoreDataMemberShortName) is not null;
        static bool HasAnyKeyAttribute(ISymbol symbol) => symbol.GetAttributes().FindAttributeShortName(MsgPack00xMessagePackAnalyzer.KeyAttributeShortName) is not null || symbol.GetAttributes().FindAttributeShortName(MsgPack00xMessagePackAnalyzer.DataMemberShortName) is not null;
    }

    private static async Task<Solution> AddKeyAttributeAsync(Document document, INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        var solutionEditor = new SolutionEditor(document.Project.Solution);

        var targets = FindMembersMissingAttributes(type).ToArray();

        var startOrder = targets
            .Select(x => x.GetAttributes().FindAttributeShortName(MsgPack00xMessagePackAnalyzer.KeyAttributeShortName))
            .Where(x => x != null)
            .Select(x => x!.ConstructorArguments[0])
            .Where(x => !x.IsNull)
            .Where(x => x.Value is int)
            .Select(x => (int)x.Value!)
            .DefaultIfEmpty(-1) // if empty, start from zero.
            .Max() + 1;

        foreach (var member in targets)
        {
            if (!member.IsImplicitlyDeclared && member.GetAttributes().FindAttributeShortName(MsgPack00xMessagePackAnalyzer.KeyAttributeShortName) is null)
            {
                var node = await member.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                var documentEditor = await solutionEditor.GetDocumentEditorAsync(document.Project.Solution.GetDocumentId(node.SyntaxTree), cancellationToken).ConfigureAwait(false);
                var syntaxGenerator = SyntaxGenerator.GetGenerator(documentEditor.OriginalDocument);

                // Preserve comments on fields.
                if (node is VariableDeclaratorSyntax)
                {
                    node = node.Parent;
                }

                AttributeListSyntax attributeList = (AttributeListSyntax)syntaxGenerator.Attribute("MessagePack.KeyAttribute", syntaxGenerator.LiteralExpression(startOrder++));
                if (node is ParameterSyntax parameter)
                {
                    // The primary constructor requires special target on the attribute list.
                    attributeList = attributeList.WithTarget(
                        SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.PropertyKeyword)))
                        .WithLeadingTrivia(parameter.GetLeadingTrivia());
                    ParameterSyntax attributedParameter = parameter.AddAttributeLists(attributeList);
                    documentEditor.ReplaceNode(parameter, attributedParameter);
                }
                else
                {
                    documentEditor.AddAttribute(node, attributeList);
                }
            }
        }

        if (type.GetAttributes().FindAttributeShortName(MsgPack00xMessagePackAnalyzer.MessagePackObjectAttributeShortName) == null)
        {
            var node = await type.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            var documentEditor = await solutionEditor.GetDocumentEditorAsync(document.Project.Solution.GetDocumentId(node.SyntaxTree), cancellationToken).ConfigureAwait(false);
            var syntaxGenerator = SyntaxGenerator.GetGenerator(documentEditor.OriginalDocument);
            documentEditor.AddAttribute(node, syntaxGenerator.Attribute("MessagePack.MessagePackObject"));
        }

        Solution changedSolution = solutionEditor.GetChangedSolution();
        return await LineEndingPreserver.NormalizeChangedDocumentsAsync(document.Project.Solution, changedSolution, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Solution> AddIgnoreMemberAttributeAsync(Document document, MemberDeclarationSyntax memberDecl, CancellationToken cancellationToken)
    {
        SolutionEditor solutionEditor = new(document.Project.Solution);
        DocumentEditor documentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken);
        SyntaxGenerator syntaxGenerator = SyntaxGenerator.GetGenerator(documentEditor.OriginalDocument);

        documentEditor.AddAttribute(memberDecl, syntaxGenerator.Attribute("MessagePack.IgnoreMemberAttribute"));

        Solution changedSolution = solutionEditor.GetChangedSolution();
        return await LineEndingPreserver.NormalizeChangedDocumentsAsync(document.Project.Solution, changedSolution, cancellationToken).ConfigureAwait(false);
    }
}

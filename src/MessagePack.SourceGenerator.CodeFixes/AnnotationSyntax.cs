using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace MessagePack.CodeFixes;

/// <summary>
/// The shared annotation edits behind the fixers: adding [MessagePackObject],
/// numbering [Key(n)] over a declaration's unannotated members, appending [UnionTag].
/// All symbol matching goes by full attribute name (base-chain walk), mirroring the parsers.
/// </summary>
static class AnnotationSyntax
{
    public const string MessagePackObjectAttributeName = "MessagePack.MessagePackObjectAttribute";
    public const string KeyAttributeName = "MessagePack.KeyAttribute";
    public const string IgnoreMemberAttributeName = "MessagePack.IgnoreMemberAttribute";
    public const string UnionTagAttributeName = "MessagePack.UnionTagAttribute";

    public static bool HasAttribute(ISymbol symbol, string fullName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            for (var attributeType = attribute.AttributeClass; attributeType is not null; attributeType = attributeType.BaseType)
            {
                if (attributeType.ToDisplayString() == fullName)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static void AddMessagePackObject(DocumentEditor editor, TypeDeclarationSyntax declaration)
    {
        editor.AddAttribute(declaration, editor.Generator.Attribute("MessagePack.MessagePackObject").WithAdditionalAnnotations(Simplifier.Annotation));
    }

    // annotates every unannotated serialized member of this declaration with [Key(n)],
    // numbering properties first then fields (the generator's wire order)
    // and continuing after the highest key already present anywhere in the type's hierarchy
    public static void AddKeyAttributes(DocumentEditor editor, SemanticModel semanticModel, TypeDeclarationSyntax declaration)
    {
        var type = semanticModel.GetDeclaredSymbol(declaration);
        var nextKey = type is null ? 0 : MaxExistingKey(type) + 1;
        // a type already keyed by strings stays a map: an int key next to a string key is MsgPack002
        var stringKeys = type is not null && HasStringKey(type);

        foreach (var member in SerializedMembersInKeyOrder(declaration))
        {
            var symbol = member is FieldDeclarationSyntax field
                ? semanticModel.GetDeclaredSymbol(field.Declaration.Variables[0])
                : semanticModel.GetDeclaredSymbol(member);
            if (symbol is null
                || HasAttributeInOverrideChain(symbol, KeyAttributeName)
                || HasAttributeInOverrideChain(symbol, IgnoreMemberAttributeName)
                || IsUnknownMembersPacket(symbol))
            {
                continue; // (an override shares its base declaration's key: adding another would duplicate it; the
                          // MessagePackUnknownMembers packet carries no key, MsgPack020)
            }
            if (member is FieldDeclarationSyntax { Declaration.Variables.Count: > 1 } multiple)
            {
                // `public int A, B;` is one declaration, and an attribute on it applies to every declarator: split it,
                // so that each field gets its own key instead of all of them sharing (and duplicating) one
                var replacements = new List<SyntaxNode>();
                foreach (var variable in multiple.Declaration.Variables)
                {
                    var single = multiple
                        .WithDeclaration(multiple.Declaration.WithVariables(SyntaxFactory.SingletonSeparatedList(variable.WithoutTrivia())))
                        .WithLeadingTrivia(multiple.GetLeadingTrivia())
                        .WithTrailingTrivia(multiple.GetTrailingTrivia());
                    var key = stringKeys ? editor.Generator.LiteralExpression(variable.Identifier.ValueText) : editor.Generator.LiteralExpression(nextKey);
                    replacements.Add(editor.Generator.AddAttributes(single, editor.Generator.Attribute("MessagePack.Key", key).WithAdditionalAnnotations(Simplifier.Annotation)));
                    nextKey++;
                }
                editor.InsertAfter(multiple, replacements.Skip(1));
                editor.ReplaceNode(multiple, replacements[0]);
                continue;
            }
            editor.AddAttribute(member, editor.Generator.Attribute("MessagePack.Key", stringKeys ? editor.Generator.LiteralExpression(symbol.Name) : editor.Generator.LiteralExpression(nextKey)).WithAdditionalAnnotations(Simplifier.Annotation));
            nextKey++;
        }
    }

    public static void AddUnionTag(DocumentEditor editor, TypeDeclarationSyntax rootDeclaration, INamedTypeSymbol root, INamedTypeSymbol caseType)
    {
        var nextTag = MaxExistingTag(root) + 1;
        var attribute = editor.Generator.Attribute(
            "MessagePack.UnionTag",
            editor.Generator.TypeOfExpression(editor.Generator.TypeExpression(caseType)),
            editor.Generator.LiteralExpression(nextTag));
        editor.AddAttribute(rootDeclaration, attribute.WithAdditionalAnnotations(Simplifier.Annotation));
    }

    static IEnumerable<MemberDeclarationSyntax> SerializedMembersInKeyOrder(TypeDeclarationSyntax declaration)
    {
        foreach (var member in declaration.Members)
        {
            if (member is PropertyDeclarationSyntax property
                && property.Modifiers.Any(SyntaxKind.PublicKeyword)
                && !property.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                yield return property;
            }
        }
        foreach (var member in declaration.Members)
        {
            if (member is FieldDeclarationSyntax field
                && field.Modifiers.Any(SyntaxKind.PublicKeyword)
                && !field.Modifiers.Any(SyntaxKind.StaticKeyword)
                && !field.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                yield return field;
            }
        }
    }

    static bool IsUnknownMembersPacket(ISymbol symbol) =>
        (symbol is IPropertySymbol property ? property.Type : symbol is IFieldSymbol field ? field.Type : null)
            ?.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString() == "MessagePack.MessagePackUnknownMembers";

    static bool HasStringKey(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                foreach (var attribute in member.GetAttributes())
                {
                    if (IsKeyAttribute(attribute.AttributeClass)
                        && attribute.ConstructorArguments.Length >= 1 // a derived attribute may take more (the generator reads the first)
                        && attribute.ConstructorArguments[0].Value is string)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // [Key] or an attribute derived from it ([MyKey(3)] keys a member the same way, and numbers after it)
    static bool IsKeyAttribute(INamedTypeSymbol? attributeClass)
    {
        for (var attributeType = attributeClass; attributeType is not null; attributeType = attributeType.BaseType)
        {
            if (attributeType.ToDisplayString() == KeyAttributeName)
            {
                return true;
            }
        }
        return false;
    }

    static bool HasAttributeInOverrideChain(ISymbol symbol, string attributeName)
    {
        for (var current = symbol; current is not null; current = current is IPropertySymbol { OverriddenProperty: { } overridden } ? overridden : null)
        {
            if (HasAttribute(current, attributeName))
            {
                return true;
            }
        }
        return false;
    }

    static int MaxExistingKey(INamedTypeSymbol type)
    {
        var max = -1;
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                foreach (var attribute in member.GetAttributes())
                {
                    if (IsKeyAttribute(attribute.AttributeClass)
                        && attribute.ConstructorArguments.Length >= 1
                        && attribute.ConstructorArguments[0].Value is int key
                        && key > max)
                    {
                        max = key;
                    }
                }
            }
        }
        return max;
    }

    static int MaxExistingTag(INamedTypeSymbol root)
    {
        var max = -1;
        foreach (var attribute in root.GetAttributes())
        {
            var isUnionTag = false;
            for (var attributeType = attribute.AttributeClass; attributeType is not null; attributeType = attributeType.BaseType)
            {
                if (attributeType.ToDisplayString() == UnionTagAttributeName)
                {
                    isUnionTag = true;
                    break;
                }
            }
            // the tag rides last in every shape
            if (isUnionTag
                && attribute.ConstructorArguments.Length > 0
                && attribute.ConstructorArguments[attribute.ConstructorArguments.Length - 1].Value is int tag
                && tag > max)
            {
                max = tag;
            }
        }
        return max;
    }
}

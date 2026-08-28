using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace MessagePack.CodeFixes;

/// <summary>
/// The shared annotation edits behind the fixers: adding [MessagePackObject], numbering
/// [Key(n)] over a declaration's unannotated members, appending [UnionTag]. All symbol
/// matching goes by full attribute name (base-chain walk), mirroring the parsers.
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

    // annotates every unannotated serialized member of THIS declaration with [Key(n)],
    // numbering properties first then fields (the generator's wire order) and continuing
    // after the highest key already present anywhere in the type's hierarchy
    public static void AddKeyAttributes(DocumentEditor editor, SemanticModel semanticModel, TypeDeclarationSyntax declaration)
    {
        var type = semanticModel.GetDeclaredSymbol(declaration);
        var nextKey = type is null ? 0 : MaxExistingKey(type) + 1;

        foreach (var member in SerializedMembersInWireOrder(declaration))
        {
            var symbol = member is FieldDeclarationSyntax field
                ? semanticModel.GetDeclaredSymbol(field.Declaration.Variables[0])
                : semanticModel.GetDeclaredSymbol(member);
            if (symbol is null
                || HasAttribute(symbol, KeyAttributeName)
                || HasAttribute(symbol, IgnoreMemberAttributeName))
            {
                continue;
            }
            editor.AddAttribute(member, editor.Generator.Attribute("MessagePack.Key", editor.Generator.LiteralExpression(nextKey)).WithAdditionalAnnotations(Simplifier.Annotation));
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

    static IEnumerable<MemberDeclarationSyntax> SerializedMembersInWireOrder(TypeDeclarationSyntax declaration)
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

    static int MaxExistingKey(INamedTypeSymbol type)
    {
        var max = -1;
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                foreach (var attribute in member.GetAttributes())
                {
                    if (attribute.AttributeClass?.ToDisplayString() == KeyAttributeName
                        && attribute.ConstructorArguments.Length == 1
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

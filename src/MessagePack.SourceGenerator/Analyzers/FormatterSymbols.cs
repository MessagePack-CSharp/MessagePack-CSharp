using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>Shared symbol questions for the analyzers that look inside IMessagePackFormatter implementations.</summary>
static class FormatterSymbols
{
    const string MessagePackObjectAttributeName = "MessagePack.MessagePackObjectAttribute";

    /// <summary>
    /// The [MessagePackObject] on <paramref name="type"/> or the nearest base type that carries one. The attribute is
    /// Inherited = true, and the reflection tier serves an unannotated derived type through the base's annotation (the
    /// generator does not, so the AOT chains miss such a type); the analyzers that model the Default chain look the same way.
    /// </summary>
    public static AttributeData? FindInheritedMessagePackObject(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var attribute in current.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == MessagePackObjectAttributeName)
                {
                    return attribute;
                }
            }
        }
        return null;
    }

    public static bool ImplementsFormatterInterface(INamedTypeSymbol type)
    {
        foreach (var implemented in type.AllInterfaces)
        {
            if (IsFormatterInterface(implemented))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>True when <paramref name="method"/> is the type's implementation of IMessagePackFormatter.Serialize or .Deserialize.</summary>
    public static bool IsFormatterEntry(IMethodSymbol method)
    {
        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation))
        {
            return false;
        }
        var containingType = method.ContainingType;
        foreach (var implemented in containingType.AllInterfaces)
        {
            if (!IsFormatterInterface(implemented))
            {
                continue;
            }
            foreach (var member in implemented.GetMembers())
            {
                if (member is IMethodSymbol { Name: "Serialize" or "Deserialize" } interfaceMethod
                    && SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(interfaceMethod), method))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>True for the type's implementation of IMessagePackFormatter.Serialize, implicit or explicit (whose Name carries the interface).</summary>
    public static bool IsSerializeEntry(IMethodSymbol method)
    {
        foreach (var implemented in method.ExplicitInterfaceImplementations)
        {
            return implemented.Name == "Serialize";
        }
        return method.Name == "Serialize";
    }

    /// <summary>
    /// The model to read the body of a helper through. A helper usually sits in the formatter's own file, whose model the
    /// analysis already built. One in another file of a partial type needs a model of its own: RS1030 asks analyzers not
    /// to create one (it is not cached), so that happens only in this case. The caller has checked that the tree belongs
    /// to this compilation (a helper in another project is a source symbol of that compilation in the IDE).
    /// </summary>
    public static SemanticModel SemanticModelOf(SyntaxTree tree, Microsoft.CodeAnalysis.IOperation operation, Compilation compilation)
    {
        if (operation.SemanticModel is { } model && model.SyntaxTree == tree)
        {
            return model;
        }
#pragma warning disable RS1030 // the helper's body is in another file, which no analysis context hands a model for
        return compilation.GetSemanticModel(tree);
#pragma warning restore RS1030
    }

    static bool IsFormatterInterface(INamedTypeSymbol type)
    {
        return type.OriginalDefinition is { MetadataName: "IMessagePackFormatter`3" } original
            && original.ContainingNamespace.ToDisplayString() == "MessagePack";
    }
}

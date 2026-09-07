using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>Shared symbol questions for the analyzers that look inside IMessagePackFormatter implementations.</summary>
static class FormatterSymbols
{
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

    static bool IsFormatterInterface(INamedTypeSymbol type)
    {
        return type.OriginalDefinition is { MetadataName: "IMessagePackFormatter`3" } original
            && original.ContainingNamespace.ToDisplayString() == "MessagePack";
    }
}

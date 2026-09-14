using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MessagePack.SourceGenerator;

/// <summary>Location captured as equatable values (Location itself is not cache-safe).</summary>
public readonly record struct LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? From(ISymbol symbol)
    {
        foreach (var location in symbol.Locations)
        {
            if (location.SourceTree is { } tree)
            {
                return new LocationInfo(tree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
            }
        }
        return null;
    }

    /// <summary>The attribute application itself, so a per-attribute diagnostic lands on the offending declaration rather than the type.</summary>
    public static LocationInfo? From(AttributeData attribute)
    {
        if (attribute.ApplicationSyntaxReference is { } reference)
        {
            var location = reference.GetSyntax().GetLocation();
            return new LocationInfo(reference.SyntaxTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
        }
        return null;
    }
}

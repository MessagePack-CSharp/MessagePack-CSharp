using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections;

namespace UltraMessagePack.SourceGenerator;

/// <summary>
/// Value-equatable array wrapper: incremental pipeline models must compare by content,
/// or every edit invalidates the cache and re-runs emission for every type.
/// </summary>
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    readonly T[]? array;

    public EquatableArray(T[] array)
    {
        this.array = array;
    }

    public T[] AsArray() => array ?? [];

    public int Length => array?.Length ?? 0;

    public T this[int index] => AsArray()[index];

    public bool Equals(EquatableArray<T> other)
    {
        var a = AsArray();
        var b = other.AsArray();
        if (a.Length != b.Length)
        {
            return false;
        }
        for (int i = 0; i < a.Length; i++)
        {
            if (!a[i].Equals(b[i]))
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var item in AsArray())
        {
            hash = (hash * 31) + (item?.GetHashCode() ?? 0);
        }
        return hash;
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)AsArray()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

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
}

/// <summary>Pre-formatted diagnostic carried through the pipeline as pure values.</summary>
public sealed record DiagnosticInfo(string Id, string Message, LocationInfo? Location)
{
    public Diagnostic ToDiagnostic()
    {
        var descriptor = Diagnostics.ById(Id);
        return Diagnostic.Create(descriptor, Location?.ToLocation() ?? Microsoft.CodeAnalysis.Location.None, Message);
    }
}

public static class Diagnostics
{
    // {0} carries the whole pre-formatted message: keeps DiagnosticInfo equatable without
    // dragging object[] args through the pipeline
    static DiagnosticDescriptor Make(string id, string title, DiagnosticSeverity severity) =>
        new(id, title, "{0}", "UltraMessagePack.SourceGenerator", severity, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MemberNeedsKey = Make("UMP001", "Public member requires Key or IgnoreMember", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor MixedKeys = Make("UMP002", "Int and string keys must not be mixed", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor DuplicateKey = Make("UMP003", "Keys must be unique", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor NoParameterlessConstructor = Make("UMP004", "A parameterless constructor is required", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor UnsupportedType = Make("UMP005", "Type shape is not supported yet", DiagnosticSeverity.Warning);
    public static readonly DiagnosticDescriptor TypeNotAccessible = Make("UMP006", "Type must be public or internal", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor MemberNotSettable = Make("UMP007", "Keyed member requires an accessible non-init setter", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor InvalidKey = Make("UMP008", "Key is invalid", DiagnosticSeverity.Error);

    public static DiagnosticDescriptor ById(string id) => id switch
    {
        "UMP001" => MemberNeedsKey,
        "UMP002" => MixedKeys,
        "UMP003" => DuplicateKey,
        "UMP004" => NoParameterlessConstructor,
        "UMP005" => UnsupportedType,
        "UMP006" => TypeNotAccessible,
        "UMP007" => MemberNotSettable,
        _ => InvalidKey,
    };
}

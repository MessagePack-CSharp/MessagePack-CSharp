using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// [Key(int)] numbering with large gaps: the array wire form writes MaxKey+1 slots, so
/// every hole costs one nil byte on every serialized instance of the type. More than 8
/// holes warns, uniformly, tens-style numbering (0, 10, 20, ...), stray huge keys, and
/// reserved leading regions ([Key(10)] starts) alike. Small gaps stay under the bar
/// because retiring a deleted member's key is normal versioning; a type that has retired
/// More than 8 keys (where renumbering would break its wire) suppresses per type with
/// #pragma warning disable MsgPack111. v3 had no counterpart (checked MsgPack003-018);
/// the runtime silently pads, which is exactly why the mistake deserves a compile-time eye.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SparseIntKeyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MsgPack111";

    const int HoleCountThreshold = 8;

    const string MessagePackObjectAttributeName = "MessagePack.MessagePackObjectAttribute";
    const string KeyAttributeName = "MessagePack.KeyAttribute";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Sparse int keys pad the wire with nil slots",
        "{0}",
        "MessagePack.SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The array wire form writes MaxKey+1 slots, so int keys with large gaps make every serialized instance carry nil padding. Renumber the keys contiguously; a long-lived type whose retired keys cannot be renumbered without breaking its wire suppresses this per type.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
    }

    static void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            return;
        }

        var isMessagePackObject = false;
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != MessagePackObjectAttributeName)
            {
                continue;
            }
            isMessagePackObject = true;
            // keyAsPropertyName / KeyNamingPolicy overload = map wire form, no positional slots
            if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is true or int)
            {
                return;
            }
        }
        if (!isMessagePackObject)
        {
            return;
        }

        // distinct key values across the hierarchy: shadowed/duplicate keys are MsgPack003's business,
        // negative keys MsgPack008's, this analyzer only sizes the array the surviving keys imply
        var keys = new HashSet<int>();
        var maxKey = -1;
        for (var current = type; current is not null && current.SpecialType is not (SpecialType.System_Object or SpecialType.System_ValueType); current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is not (IPropertySymbol or IFieldSymbol))
                {
                    continue;
                }
                foreach (var attribute in member.GetAttributes())
                {
                    if (attribute.AttributeClass?.ToDisplayString() == KeyAttributeName
                        && attribute.ConstructorArguments.Length == 1
                        && attribute.ConstructorArguments[0].Value is int key
                        && key >= 0)
                    {
                        keys.Add(key);
                        if (key > maxKey)
                        {
                            maxKey = key;
                        }
                    }
                }
            }
        }

        var holes = maxKey + 1 - keys.Count;
        if (holes > HoleCountThreshold)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                type.Locations[0],
                $"'{type.ToDisplayString()}' serializes as a {maxKey + 1}-slot array for its {keys.Count} keyed members: every instance writes {holes} nil slots on the wire. Renumber the int keys contiguously."));
        }
    }
}

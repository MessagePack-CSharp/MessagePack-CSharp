using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// Annotations the serializer silently ignores: a member carrying both [Key] and [IgnoreMember] (ignore wins,
/// the key is dead), [Key]/[IgnoreMember] on a static member (statics never participate),
/// and [Key]/[IgnoreMember] on a member of a struct or sealed class that carries neither [MessagePackObject] nor
/// [DataContract] (nothing can inherit it into an annotated hierarchy,
/// so the annotation can never take effect, unsealed classes are exempt: a base member's [Key] is honored through an
/// annotated derived type). MsgPack110 turns each silent no-op into a warning.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IneffectiveAnnotationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MsgPack110";

    const string MessagePackObjectAttributeName = "MessagePack.MessagePackObjectAttribute";
    const string KeyAttributeName = "MessagePack.KeyAttribute";
    const string IgnoreMemberAttributeName = "MessagePack.IgnoreMemberAttribute";
    const string IgnoreDataMemberAttributeName = "System.Runtime.Serialization.IgnoreDataMemberAttribute";
    const string DataContractAttributeName = "System.Runtime.Serialization.DataContractAttribute";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Annotation has no effect",
        "{0}",
        "MessagePack.SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An annotation the serializer never reads is a latent bug: the member serializes (or not) contrary to what the code says. The analyzer flags the combinations both tiers silently ignore.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMember, SymbolKind.Property, SymbolKind.Field);
    }

    static void AnalyzeMember(SymbolAnalysisContext context)
    {
        var member = context.Symbol;
        var hasKey = false;
        var hasIgnore = false;
        foreach (var attribute in member.GetAttributes())
        {
            var name = attribute.AttributeClass?.ToDisplayString();
            hasKey |= name == KeyAttributeName;
            hasIgnore |= name == IgnoreMemberAttributeName || name == IgnoreDataMemberAttributeName;
        }
        if (!hasKey && !hasIgnore)
        {
            return;
        }
        var annotation = hasKey && hasIgnore ? "[Key] and [IgnoreMember]" : hasKey ? "[Key]" : "[IgnoreMember]";

        if (hasKey && hasIgnore)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                PickLocation(member),
                $"'{member.ContainingType.ToDisplayString()}.{member.Name}' carries both [Key] and [IgnoreMember]; the member is ignored and the key is dead, so remove one of them"));
            return;
        }

        if (member.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                PickLocation(member),
                $"'{member.ContainingType.ToDisplayString()}.{member.Name}' is static; static members never serialize, so its {annotation} has no effect"));
            return;
        }

        // a struct or sealed class cannot be inherited into an annotated hierarchy,
        // so a [Key] without [MessagePackObject]/[DataContract] on the type is provably dead,
        // [Key] means nothing to contractless either. [IgnoreMember] is exempt (the contractless tier honors it),
        // and so is an unsealed class (a derived [MessagePackObject] type honors a base member's [Key]).
        var containingType = member.ContainingType;
        if (hasKey
            && (containingType.TypeKind == TypeKind.Struct || (containingType.TypeKind == TypeKind.Class && containingType.IsSealed))
            && !HasTypeAnnotation(containingType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                PickLocation(member),
                $"'{member.ContainingType.ToDisplayString()}' carries neither [MessagePackObject] nor [DataContract] and cannot serve as a base type, so the [Key] on '{member.Name}' has no effect"));
        }
    }

    static bool HasTypeAnnotation(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            for (var attributeType = attribute.AttributeClass; attributeType is not null; attributeType = attributeType.BaseType)
            {
                var name = attributeType.ToDisplayString();
                if (name is MessagePackObjectAttributeName or DataContractAttributeName)
                {
                    return true;
                }
            }
        }
        return false;
    }

    static Location PickLocation(ISymbol symbol)
    {
        foreach (var location in symbol.Locations)
        {
            if (location.SourceTree is not null)
            {
                return location;
            }
        }
        return Location.None;
    }
}

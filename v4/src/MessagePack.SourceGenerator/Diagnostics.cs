using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator;

public static class Diagnostics
{
    // UMP0xx = generator pipeline diagnostics (this class); UMP1xx = standalone DiagnosticAnalyzers under Analyzers/
    // (same dll, MessagePack-CSharp v3 layout)

    // {0} carries the whole pre-formatted message: keeps DiagnosticInfo equatable without dragging object[] args
    // through the pipeline
    static DiagnosticDescriptor Make(string id, string title, DiagnosticSeverity severity) =>
        new(id, title, "{0}", "MessagePack.SourceGenerator", severity, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MemberNeedsKey = Make("MsgPack001", "Public member requires Key or IgnoreMember", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor MixedKeys = Make("MsgPack002", "Int and string keys must not be mixed", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor DuplicateKey = Make("MsgPack003", "Keys must be unique", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor NoParameterlessConstructor = Make("MsgPack004", "A parameterless constructor is required", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor UnsupportedType = Make("MsgPack005", "Type shape is not supported yet", DiagnosticSeverity.Warning);
    public static readonly DiagnosticDescriptor TypeNotAccessible = Make("MsgPack006", "Type must be public or internal", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor MemberNotSettable = Make("MsgPack007", "Keyed member cannot be deserialized", DiagnosticSeverity.Warning);
    public static readonly DiagnosticDescriptor InvalidKey = Make("MsgPack008", "Key is invalid", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor InvalidSerializationConstructor = Make("MsgPack009", "SerializationConstructor does not match the serialized members", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor AllowPrivateRequiresPartial = Make("MsgPack010", "AllowPrivate requires the type to be partial", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor InvalidUnion = Make("MsgPack011", "Union declaration is invalid", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor InvalidFormatterAttribute = Make("MsgPack012", "MessagePackFormatter attribute names an unusable type", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor FormatterAttributeArguments = Make("MsgPack013", "MessagePackFormatter arguments do not bind", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor TypeLevelFormatterNotGenerated = Make("MsgPack014", "Type-level MessagePackFormatter registration cannot be generated for this type", DiagnosticSeverity.Warning);
    public static readonly DiagnosticDescriptor InvalidCircularReference = Make("MsgPack015", "AllowCircularReferences constraints are not met", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor InvalidSerializableFactory = Make("MsgPack016", "MessagePackSerializable factory class shape is invalid", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor InvalidSerializableRoot = Make("MsgPack017", "MessagePackSerializable root type is invalid", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor DuplicateSurrogate = Make("MsgPack018", "Multiple surrogates declared for one target type", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor SurrogateNotRegistered = Make("MsgPack019", "Surrogate declaration is not auto-registered", DiagnosticSeverity.Warning);
    public static readonly DiagnosticDescriptor InvalidUnknownMembersMember = Make("MsgPack020", "MessagePackUnknownMembers member is invalid", DiagnosticSeverity.Error);
    public static readonly DiagnosticDescriptor OverrideChangesKey = Make("MsgPack021", "Property override declares a different key than the declaration it overrides", DiagnosticSeverity.Error);

    public static DiagnosticDescriptor ById(string id) => id switch
    {
        "MsgPack001" => MemberNeedsKey,
        "MsgPack002" => MixedKeys,
        "MsgPack003" => DuplicateKey,
        "MsgPack004" => NoParameterlessConstructor,
        "MsgPack005" => UnsupportedType,
        "MsgPack006" => TypeNotAccessible,
        "MsgPack007" => MemberNotSettable,
        "MsgPack008" => InvalidKey,
        "MsgPack009" => InvalidSerializationConstructor,
        "MsgPack010" => AllowPrivateRequiresPartial,
        "MsgPack011" => InvalidUnion,
        "MsgPack012" => InvalidFormatterAttribute,
        "MsgPack013" => FormatterAttributeArguments,
        "MsgPack014" => TypeLevelFormatterNotGenerated,
        "MsgPack015" => InvalidCircularReference,
        "MsgPack016" => InvalidSerializableFactory,
        "MsgPack017" => InvalidSerializableRoot,
        "MsgPack018" => DuplicateSurrogate,
        "MsgPack019" => SurrogateNotRegistered,
        "MsgPack020" => InvalidUnknownMembersMember,
        "MsgPack021" => OverrideChangesKey,
        _ => InvalidKey,
    };
}

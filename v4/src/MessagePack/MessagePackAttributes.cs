namespace MessagePack;

// Drop-in equivalents of the MessagePack.Annotations attribute types.
// Full names, constructor signatures, and property shapes are kept identical so that
// models annotated for v3 compile against v4 unchanged, and both the source generator
// and ReflectionObjectFormatter (which match attributes by full name) see no difference.
//
// Deliberate deviation #1: v3's UnionAttribute is renamed to UnionTagAttribute, and a
// union base additionally requires [MessagePackObject]. C# 15 introduces a union feature
// whose attribute vocabulary claims the "Union" name, and this library intends to
// serialize those unions too — sharing the name would make both unusable side by side.
// The rename also frees the constructor to take (caseType, tag) — JsonDerivedType's order,
// and the reading order of the UnionTag<TCaseType>(tag) form. The discriminator is named
// Tag (matching the attribute), not v3's Key: member keys ([Key]) and union discriminators
// are different concepts, and different words keep them apart.
// Migration is mechanical and compiler-guided: [Union] fails to compile, rename it to
// [UnionTag], swap the two arguments (the int/Type mismatch makes the old order an error,
// never a silent reinterpretation), and add [MessagePackObject] to the base (MsgPack103 errors
// when it is missing).
//
// Deliberate deviation #2: v3's ExcludeFormatterFromSourceGeneratedResolverAttribute and
// MessagePackKnownFormatterAttribute are DELETED. The generator never auto-collects
// hand-written formatter classes into a resolver, so there is nothing to exclude from;
// and in the factory architecture a formatter CLASS existing contributes nothing to
// resolution by itself (coverage comes from registration or [MessagePackFormatter]),
// so "a formatter type exists" is not a meaningful declaration any more. The surviving
// declaration, v3's MessagePackAssumedFormattable, is renamed MessagePackKnownType —
// "known" is the established serialization vocabulary ([KnownType] lineage) naming the
// declarer's claim rather than the analyzer's leap of faith, and "formattable" leaked
// plumbing vocabulary into an attribute users meet through the high-level API.
// MIGRATION NOTE: KnownType takes the SERVED TYPE. A v3 [MessagePackKnownFormatter] is
// not mechanically renameable — it took the formatter type; declare the types that
// formatter serves instead.
//
// Deliberate deviation #3: the v3 string-name UnionTag constructor (assembly-qualified
// name, resolved via Type.GetType) is deleted — v4 unions are source-generator-only, the
// generator requires a symbol it can resolve, and the AQN string was both unusable there
// and trimmer-hostile; migrate to typeof(...) or UnionTag<TCaseType>. A (string, int)
// constructor EXISTS again with DIFFERENT semantics: the string names a TYPE PARAMETER of
// a generic union root (typeof cannot express one), resolved at compile time by the
// generator — a leftover v3 AQN string fails loudly there instead of misbehaving.

/// <summary>Marks a type as serializable by the object formatters and the source generator. On an interface (or abstract class) it pairs with <see cref="UnionTagAttribute"/> to declare a polymorphic base.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public class MessagePackObjectAttribute : Attribute
{
    /// <summary>Gets a value indicating whether members serialize as a map keyed by their names instead of a <see cref="KeyAttribute"/>-indexed array.</summary>
    public bool KeyAsPropertyName { get; }

    /// <summary>Gets the conversion applied to member names when <see cref="KeyAsPropertyName"/> is in effect.</summary>
    public KeyNamingPolicy KeyNamingPolicy { get; }

    public MessagePackObjectAttribute(bool keyAsPropertyName = false)
    {
        this.KeyAsPropertyName = keyAsPropertyName;
    }

    /// <summary>Serializes as a map keyed by member names converted through <paramref name="keyNamingPolicy"/>; an explicit <see cref="KeyAttribute"/> string key on a member wins over the policy.</summary>
    public MessagePackObjectAttribute(KeyNamingPolicy keyNamingPolicy)
    {
        this.KeyAsPropertyName = true;
        this.KeyNamingPolicy = keyNamingPolicy;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the source generator skips this type, leaving it to the runtime reflection formatters.
    /// The serialization contract declared by this attribute stays in force and the wire format is identical between the two tiers; only the implementation moves from compile time to run time, trading generated-code performance and Native AOT support for the type.
    /// This is the escape hatch for shapes the generated formatter cannot serve.
    /// Not valid on union roots, where no runtime tier handles unions; the generator reports MsgPack011 there instead of leaving the root without any formatter.
    /// </summary>
    public bool SuppressSourceGeneration { get; set; }

    /// <summary>Gets or sets a value indicating whether non-public members participate in serialization.</summary>
    public bool AllowPrivate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether object identity is preserved for this type within one
    /// (de)serialization: repeated references become back-references on the wire, which is what makes
    /// circular object graphs serializable. Requires the source generator, a class with a parameterless
    /// constructor, and members settable after construction; the type's wire format gains a
    /// library-specific envelope that plain MessagePack readers do not understand.
    /// </summary>
    public bool AllowCircularReferences { get; set; }
}

/// <summary>Assigns the array index or map key of a member within a <see cref="MessagePackObjectAttribute"/> type.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class KeyAttribute : Attribute
{
    public int? IntKey { get; }

    public string? StringKey { get; }

    public KeyAttribute(int x)
    {
        this.IntKey = x;
    }

    public KeyAttribute(string x)
    {
        this.StringKey = x ?? throw new ArgumentNullException(nameof(x));
    }
}

/// <summary>Excludes a member from serialization.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class IgnoreMemberAttribute : Attribute
{
}

/// <summary>
/// Assigns the wire-embedded integer tag (discriminator) to a case type of a polymorphic
/// base or of a union. The annotated root must also carry <see cref="MessagePackObjectAttribute"/> —
/// discovery is driven by that single attribute (analyzer MsgPack103 errors when it is missing).
/// </summary>
/// <remarks>
/// v3's UnionAttribute, renamed: C# 15's union feature claims the "Union" attribute name (see the file header).
/// v3's SubType is likewise renamed to CaseType — still accurate for interface/abstract roots (a derived
/// type is a case), and correct for unions, whose cases need not be subtypes of anything. The Struct target
/// serves C# union declarations and hand-written <c>IUnion</c> structs — the source generator rejects
/// [UnionTag] on any other struct.
/// </remarks>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public class UnionTagAttribute : Attribute
{
    /// <summary>Gets the discriminator that identifies the case type on the wire.</summary>
    public int Tag { get; }

    /// <summary>Gets the case type: a derived or implementing type of a polymorphic base, or a union case. Null for the type-parameter form.</summary>
    public Type? CaseType { get; }

    /// <summary>Gets the name of the union root's type parameter serving as the case (generic unions, e.g. the T of <c>union Result&lt;T&gt;(T, Error)</c>). Null for the Type forms.</summary>
    public string? CaseTypeParameter { get; }

    public UnionTagAttribute(Type caseType, int tag)
    {
        this.Tag = tag;
        this.CaseType = caseType ?? throw new ArgumentNullException(nameof(caseType));
    }

    /// <summary>
    /// Tags a case that IS a type parameter of a generic union, named by that parameter
    /// (<c>[UnionTag("T", 0)]</c>) — typeof cannot express one. Resolved by the source
    /// generator at compile time; a name that is not a type parameter of the root is an error.
    /// </summary>
    public UnionTagAttribute(string caseTypeParameter, int tag)
    {
        this.Tag = tag;
        this.CaseTypeParameter = caseTypeParameter ?? throw new ArgumentNullException(nameof(caseTypeParameter));
    }
}

#if NET
/// <summary>
/// The typed form of <see cref="UnionTagAttribute"/>. Generic attributes need a modern
/// runtime, so this variant exists on the net8.0+ builds only.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class UnionTagAttribute<TCaseType> : UnionTagAttribute
{
    public UnionTagAttribute(int tag)
        : base(typeof(TCaseType), tag)
    {
    }
}
#endif

/// <summary>Selects the constructor that deserialization uses when several are available.</summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = true)]
public class SerializationConstructorAttribute : Attribute
{
}

/// <summary>
/// Declares a serialization ROOT type on a partial factory class, the JsonSerializerContext
/// pattern: the source generator fills in the class's other half (deriving
/// <see cref="MessagePackFormatterFactory"/>) with static closed formatter constructions for
/// the declared type and everything reachable inside it, and auto-registers them through a
/// module initializer. This serves types that appear only as serialization roots and never as
/// a member of a [MessagePackObject] type (e.g. <c>Person[]</c> or <c>List&lt;Person&gt;</c>
/// passed straight to Serialize), which the member-graph harvest cannot see; without it the
/// Native AOT chains fail to resolve such roots. The generated Instance can also be composed
/// into an explicit factory chain for registry-free resolution.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class MessagePackSerializableAttribute : Attribute
{
    public Type Type { get; }

    public MessagePackSerializableAttribute(Type type)
    {
        this.Type = type ?? throw new ArgumentNullException(nameof(type));
    }
}

#if NET
/// <summary>
/// The typed form of <see cref="MessagePackSerializableAttribute"/>. Generic attributes need
/// a modern runtime, so this variant exists on the net8.0+ builds only.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MessagePackSerializableAttribute<T> : MessagePackSerializableAttribute
{
    public MessagePackSerializableAttribute()
        : base(typeof(T))
    {
    }
}
#endif

/// <summary>
/// Declares to the analyzer that a type is serializable through coverage it cannot see (a
/// factory, runtime registration, or another assembly). Harvested from every referenced
/// assembly, so a library declares its coverage once for all consumers.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class MessagePackKnownTypeAttribute : Attribute
{
    public Type Type { get; }

    public MessagePackKnownTypeAttribute(Type knownType)
    {
        this.Type = knownType ?? throw new ArgumentNullException(nameof(knownType));
    }
}

/// <summary>
/// Overrides the formatter used for the annotated type or member.
/// Point it at a concrete <see cref="MessagePackFormatterFactory"/> (arguments go to the
/// factory's constructor), or at a formatter as an unbound generic over the buffer pair
/// (<c>typeof(MyFormatter&lt;,&gt;)</c>). MsgPack105 validates the type at compile time; the
/// <c>MessagePackFormatterAttribute&lt;TFactory&gt;</c> variant enforces the factory shape
/// through its constraint instead.
/// A type-level annotation is compiled by the source generator into the generated factory's
/// registration (there is no runtime attribute tier — the argument shape changed from v3's
/// instance type to a factory type, and an assembly must be compiled with the generator for
/// the annotation to take effect). Closed, accessible types only; MsgPack014 flags the rest.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class MessagePackFormatterAttribute : Attribute
{
    public Type FactoryType { get; }

    public object?[]? Arguments { get; }

    public MessagePackFormatterAttribute(Type factoryType)
    {
        this.FactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
    }

    public MessagePackFormatterAttribute(Type factoryType, params object?[]? arguments)
    {
        this.FactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
        this.Arguments = arguments;
    }
}

#if NET
/// <summary>
/// The typed form of <see cref="MessagePackFormatterAttribute"/>: the constraint makes
/// "is this a factory" a compile error instead of a diagnostic. Generic attributes need a
/// modern runtime, so this variant exists on the net8.0+ builds only.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class MessagePackFormatterAttribute<TFactory> : MessagePackFormatterAttribute
    where TFactory : MessagePackFormatterFactory
{
    public MessagePackFormatterAttribute()
        : base(typeof(TFactory))
    {
    }

    public MessagePackFormatterAttribute(params object?[]? arguments)
        : base(typeof(TFactory), arguments)
    {
    }
}
#endif

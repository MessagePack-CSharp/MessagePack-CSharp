namespace MessagePack;

/// <summary>
/// Marks a type as serializable by the object formatters and the source generator.
/// On an interface or abstract class, combine it with <see cref="UnionTagAttribute"/> to declare a polymorphic base.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public class MessagePackObjectAttribute : Attribute
{
    /// <summary>Whether members are written as a map keyed by their names instead of an array indexed by <see cref="KeyAttribute"/>.</summary>
    public bool KeyAsPropertyName { get; }

    /// <summary>Conversion applied to member names when <see cref="KeyAsPropertyName"/> is true.</summary>
    public KeyNamingPolicy KeyNamingPolicy { get; }

    /// <summary>Serializes members as an array indexed by <see cref="KeyAttribute"/>, or as a map keyed by member name when <paramref name="keyAsPropertyName"/> is true.</summary>
    public MessagePackObjectAttribute(bool keyAsPropertyName = false)
    {
        this.KeyAsPropertyName = keyAsPropertyName;
    }

    /// <summary>
    /// Serializes members as a map keyed by member names converted through <paramref name="keyNamingPolicy"/>.
    /// An explicit string <see cref="KeyAttribute"/> on a member takes precedence over the policy.
    /// </summary>
    public MessagePackObjectAttribute(KeyNamingPolicy keyNamingPolicy)
    {
        this.KeyAsPropertyName = true;
        this.KeyNamingPolicy = keyNamingPolicy;
    }

    /// <summary>
    /// Whether the source generator skips this type and leaves it to the runtime reflection formatters.
    /// The serialized format is the same, but the type loses generated-code performance and Native AOT support.
    /// Not allowed on union roots, which have no runtime formatter (MsgPack011).
    /// </summary>
    public bool SuppressSourceGeneration { get; set; }

    /// <summary>Whether non-public members participate in serialization.</summary>
    public bool AllowPrivate { get; set; }

    /// <summary>
    /// Whether object identity is preserved within one serialization, so that repeated and circular references are written as back-references.
    /// Requires the source generator, a class with a parameterless constructor, and members that can be set after construction.
    /// The serialized format gains a library-specific envelope that other MessagePack readers do not understand.
    /// </summary>
    public bool AllowCircularReferences { get; set; }
}

/// <summary>Assigns the array index or map key of a member in a <see cref="MessagePackObjectAttribute"/> type.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class KeyAttribute : Attribute
{
    /// <summary>Array index of the member, or null when a string key was given.</summary>
    public int? IntKey { get; }

    /// <summary>Map key of the member, or null when an integer key was given.</summary>
    public string? StringKey { get; }

    /// <summary>Assigns the array index <paramref name="x"/>.</summary>
    public KeyAttribute(int x)
    {
        this.IntKey = x;
    }

    /// <summary>Assigns the map key <paramref name="x"/>.</summary>
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
/// Assigns the integer tag that identifies one case type of a polymorphic base or of a union in the serialized data.
/// The annotated root must also carry <see cref="MessagePackObjectAttribute"/>.
/// On a struct it is accepted only for C# union declarations and <c>IUnion</c> implementations.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public class UnionTagAttribute : Attribute
{
    /// <summary>Tag that identifies the case type in the serialized data.</summary>
    public int Tag { get; }

    /// <summary>Case type, which is a derived or implementing type of the base or a union case. Null for the type-parameter form.</summary>
    public Type? CaseType { get; }

    /// <summary>Name of the root's type parameter that serves as the case, for generic unions. Null for the <see cref="Type"/> forms.</summary>
    public string? CaseTypeParameter { get; }

    /// <summary>Assigns <paramref name="tag"/> to <paramref name="caseType"/>.</summary>
    public UnionTagAttribute(Type caseType, int tag)
    {
        this.Tag = tag;
        this.CaseType = caseType ?? throw new ArgumentNullException(nameof(caseType));
    }

    /// <summary>
    /// Assigns <paramref name="tag"/> to a case that is a type parameter of a generic union, named by that parameter, as in <c>[UnionTag("T", 0)]</c>.
    /// The source generator resolves the name at compile time and reports a name that is not a type parameter of the root.
    /// </summary>
    public UnionTagAttribute(string caseTypeParameter, int tag)
    {
        this.Tag = tag;
        this.CaseTypeParameter = caseTypeParameter ?? throw new ArgumentNullException(nameof(caseTypeParameter));
    }
}

#if NET9_0_OR_GREATER

/// <summary>
/// Assigns the integer tag that identifies one case type of a polymorphic base or of a union in the serialized data.
/// The annotated root must also carry <see cref="MessagePackObjectAttribute"/>.
/// On a struct it is accepted only for C# union declarations and <c>IUnion</c> implementations.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class UnionTagAttribute<TCaseType> : UnionTagAttribute
{
    /// <summary>Assigns <paramref name="tag"/> to <typeparamref name="TCaseType"/>.</summary>
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
/// Declares a serialization root type on a partial class.
/// The source generator completes the class as a <see cref="MessagePackFormatterFactory"/> covering the type and everything reachable from it, and registers it automatically.
/// Use it for types passed directly to Serialize that never appear as a member of a <see cref="MessagePackObjectAttribute"/> type, such as <c>Person[]</c> or <c>List&lt;Person&gt;</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class MessagePackSerializableAttribute : Attribute
{
    /// <summary>Root type to generate formatters for.</summary>
    public Type Type { get; }

    /// <summary>Declares <paramref name="type"/> as a serialization root.</summary>
    public MessagePackSerializableAttribute(Type type)
    {
        this.Type = type ?? throw new ArgumentNullException(nameof(type));
    }
}

#if NET9_0_OR_GREATER

/// <summary>
/// Declares a serialization root type on a partial class.
/// The source generator completes the class as a <see cref="MessagePackFormatterFactory"/> covering the type and everything reachable from it, and registers it automatically.
/// Use it for types passed directly to Serialize that never appear as a member of a <see cref="MessagePackObjectAttribute"/> type, such as <c>Person[]</c> or <c>List&lt;Person&gt;</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class MessagePackSerializableAttribute<T> : MessagePackSerializableAttribute
{
    /// <summary>Declares <typeparamref name="T"/> as a serialization root.</summary>
    public MessagePackSerializableAttribute()
        : base(typeof(T))
    {
    }
}

#endif

/// <summary>
/// Tells the analyzer that a type is serializable through means it cannot see, such as a factory, runtime registration, or another assembly.
/// Declarations in referenced assemblies are honored too, so a library declares its coverage once for every consumer.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class MessagePackKnownTypeAttribute : Attribute
{
    /// <summary>Type declared as serializable.</summary>
    public Type Type { get; }

    /// <summary>Declares <paramref name="knownType"/> as serializable.</summary>
    public MessagePackKnownTypeAttribute(Type knownType)
    {
        this.Type = knownType ?? throw new ArgumentNullException(nameof(knownType));
    }
}

// A type-level annotation is compiled by the source generator into the generated factory's registration and has no
// runtime tier (a v3-compiled formatter is bound to v3's writer/reader and could not run here anyway), so an annotated
// type whose assembly was built without the generator fails with a targeted message at the reflection tier.
// MsgPack105 validates the type at compile time, MsgPack014 rejects open or inaccessible types.

/// <summary>
/// Overrides the formatter used for the annotated type or member.
/// Point it at a <see cref="MessagePackFormatterFactory"/>, whose constructor receives <see cref="Arguments"/>,
/// or at a formatter type left open over the buffer pair, such as <c>typeof(MyFormatter&lt;,&gt;)</c>.
/// A type-level annotation requires the source generator. A member-level annotation is honored by the reflection formatters as well.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class MessagePackFormatterAttribute : Attribute
{
    /// <summary>Factory type, or open formatter type, that provides the formatter.</summary>
    public Type FactoryType { get; }

    /// <summary>Constructor arguments passed to <see cref="FactoryType"/>, or null.</summary>
    public object?[]? Arguments { get; }

    /// <summary>Uses <paramref name="factoryType"/> with its parameterless constructor.</summary>
    public MessagePackFormatterAttribute(Type factoryType)
    {
        this.FactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
    }

    /// <summary>Uses <paramref name="factoryType"/>, constructed with <paramref name="arguments"/>.</summary>
    public MessagePackFormatterAttribute(Type factoryType, params object?[]? arguments)
    {
        this.FactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
        this.Arguments = arguments;
    }
}

#if NET9_0_OR_GREATER

/// <summary>
/// Typed form of <see cref="MessagePackFormatterAttribute"/>, whose constraint checks the factory type at compile time.
/// Not available on the netstandard builds.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class MessagePackFormatterAttribute<TFactory> : MessagePackFormatterAttribute
    where TFactory : MessagePackFormatterFactory
{
    /// <summary>Uses <typeparamref name="TFactory"/> with its parameterless constructor.</summary>
    public MessagePackFormatterAttribute()
        : base(typeof(TFactory))
    {
    }

    /// <summary>Uses <typeparamref name="TFactory"/>, constructed with <paramref name="arguments"/>.</summary>
    public MessagePackFormatterAttribute(params object?[]? arguments)
        : base(typeof(TFactory), arguments)
    {
    }
}
#endif

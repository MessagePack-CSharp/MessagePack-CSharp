// v3 attribute types that v4 renamed or deleted, kept in their v3 shape (full name, usage, constructors, properties).
// Source that still references them gets a compiler error naming the replacement ([Obsolete] with error: true,
// CS0619 at every usage site) instead of a bare CS0246. Assemblies compiled against v3 still load, because the attribute type
// resolves here and enumerating custom attributes does not throw. The reflection tier matches attributes by full name
// and reads [Union] through its Key/SubType properties, so it serves those types unchanged.
// [Obsolete] has no runtime effect, so the two roles do not interfere.
// Only [Union] actually has the runtime role. v3 declared the three analyzer-facing attributes below with
// [Conditional("NEVERDEFINED")], so a v3-built assembly never carries them and their stubs matter at compile time alone
// (verified against 3.1.8's metadata).

namespace MessagePack;

/// <summary>v3's union case declaration. v4 renamed it to <see cref="UnionTagAttribute"/>.</summary>
[Obsolete("v4 renamed [Union] to [UnionTag] and swapped the arguments: [Union(key, typeof(T))] becomes [UnionTag(typeof(T), key)] (or [UnionTag<T>(key)]), the root additionally needs [MessagePackObject], and the assembly-qualified-name string form is gone (use typeof). Reference the MessagePack package directly; this package is only a migration shim.", error: true)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public class UnionAttribute : Attribute
{
    public int Key { get; }

    public Type SubType { get; }

    public UnionAttribute(int key, Type subType)
    {
        this.Key = key;
        this.SubType = subType ?? throw new ArgumentNullException(nameof(subType));
    }

    public UnionAttribute(int key, string subType)
    {
        this.Key = key;
        this.SubType = Type.GetType(subType, throwOnError: true)!;
    }
}

/// <summary>v3's declaration that a formatter class exists. v4 deleted it, since a formatter type by itself contributes nothing to resolution; declare the served type with <see cref="MessagePackKnownTypeAttribute"/> instead.</summary>
[Obsolete("v4 deleted [MessagePackKnownFormatter]. Declare the served type with [assembly: MessagePackKnownType(typeof(T))]; it takes the serialized type, not the formatter type. Reference the MessagePack package directly; this package is only a migration shim.", error: true)]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module, AllowMultiple = true, Inherited = true)]
public class MessagePackKnownFormatterAttribute : Attribute
{
    public Type FormatterType { get; }

    public MessagePackKnownFormatterAttribute(Type formatterType)
    {
        this.FormatterType = formatterType ?? throw new ArgumentNullException(nameof(formatterType));
    }
}

/// <summary>v3's declaration that a type is formattable at runtime. v4 renamed it to <see cref="MessagePackKnownTypeAttribute"/>.</summary>
[Obsolete("v4 renamed [MessagePackAssumedFormattable] to [MessagePackKnownType]; the argument is unchanged. Reference the MessagePack package directly; this package is only a migration shim.", error: true)]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module, AllowMultiple = true, Inherited = true)]
public class MessagePackAssumedFormattableAttribute : Attribute
{
    public Type FormattableType { get; }

    public MessagePackAssumedFormattableAttribute(Type formattableType)
    {
        this.FormattableType = formattableType ?? throw new ArgumentNullException(nameof(formattableType));
    }
}

/// <summary>v3's opt-out from the generated resolver. v4 deleted it, since the generator never collects hand-written formatter classes, so there is nothing to exclude from.</summary>
[Obsolete("v4 deleted [ExcludeFormatterFromSourceGeneratedResolver]. The source generator never collects formatter classes into a resolver, so remove the attribute. Reference the MessagePack package directly; this package is only a migration shim.", error: true)]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ExcludeFormatterFromSourceGeneratedResolverAttribute : Attribute
{
}

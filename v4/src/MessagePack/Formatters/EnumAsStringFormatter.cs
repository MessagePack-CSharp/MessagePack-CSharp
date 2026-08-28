// NOTE: Keeping this complex implementation is not my preference, but we keep the v3 implementation as-is to ensure compatibility.
// The performance is not necessarily optimized either, but since this is an optional Formatter, I will leave it as is for now.

using SerializerFoundation;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace MessagePack.Formatters;

// MsgPack104: the enum-name str is this formatter's wire form for T, not a string member of the object model.
#pragma warning disable MsgPack104

public sealed class EnumAsStringFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where T : struct, Enum
{
    private readonly bool ignoreCase;
    private readonly Dictionary<string, T> nameValueMapping;
    private readonly Dictionary<T, string> valueNameMapping;
    private readonly IReadOnlyDictionary<string, string>? clrToSerializationName;
    private readonly IReadOnlyDictionary<string, string>? serializationToClrName;
    private readonly bool isFlags;

    /// <summary>
    /// Initializes a new instance of the EnumAsStringFormatter class
    /// that is case sensitive.
    /// </summary>
    public EnumAsStringFormatter()
        : this(ignoreCase: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the EnumAsStringFormatter class.
    /// </summary>
    /// <param name="ignoreCase">A value indicating whether to allow enum value names to mismatch on deserialization.</param>
    public EnumAsStringFormatter(bool ignoreCase)
    {
        this.ignoreCase = ignoreCase;
        StringComparer stringComparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        var type = typeof(T);

        this.isFlags = type.GetCustomAttribute<FlagsAttribute>() is object;
        var fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
        var nameValueMapping = new Dictionary<string, T>(fields.Length, stringComparer);
        var valueNameMapping = new Dictionary<T, string>();
        Dictionary<string, string>? clrToSerializationName = null;
        Dictionary<string, string>? serializationToClrName = null;

        foreach (FieldInfo enumValueMember in fields)
        {
            string name = enumValueMember.Name;
            T value = (T)enumValueMember.GetValue(null)!;

            // Consider the case where the serialized form of the enum value is overridden via an attribute.
            var attribute = enumValueMember.GetCustomAttribute<EnumMemberAttribute>();
            if (attribute is { IsValueSetExplicitly: true, Value: not null })
            {
                clrToSerializationName ??= new(stringComparer);
                serializationToClrName ??= new(stringComparer);

                clrToSerializationName.Add(name, attribute.Value);
                serializationToClrName.Add(attribute.Value, name);

                name = attribute.Value;
            }

            nameValueMapping[name] = value;
            valueNameMapping[value] = name;
        }

        this.nameValueMapping = nameValueMapping;
        this.valueNameMapping = valueNameMapping;
        this.clrToSerializationName = clrToSerializationName;
        this.serializationToClrName = serializationToClrName;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T value)
    {
        // Enum.ToString() is slow, so avoid it when we can.
        if (!this.valueNameMapping.TryGetValue(value, out string? valueString))
        {
            // fallback for flags, values with no name, etc
            valueString = this.GetSerializedNames(value.ToString());
        }

        buffer.WriteString(valueString);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        string? name = buffer.ReadString();
        if (name is null)
        {
            MessagePackSerializationException.ThrowUnexpectedNilWhileDeserializing<T>();
        }

        // Avoid Enum.TryParse when we can because it is too slow. An unrecognized name is
        // malformed DATA, so it throws MessagePackSerializationException at the point of
        // detection (codebase convention) rather than surfacing Enum.Parse's ArgumentException.
        if (!this.nameValueMapping.TryGetValue(name!, out T v2) &&
            !Enum.TryParse(this.GetClrNames(name!), this.ignoreCase, out v2))
        {
            MessagePackSerializationException.ThrowInvalidEnumName<T>(name!);
        }

        value = v2;
    }

    private string GetClrNames(string serializedNames)
    {
        if (this.serializationToClrName is not null && this.isFlags && serializedNames.Contains(", ", StringComparison.Ordinal))
        {
            return Translate(serializedNames, this.serializationToClrName);
        }

        // We don't need to consider the trivial case of no commas because our caller would have found that in the lookup table and not called us.
        return serializedNames;
    }

    private string GetSerializedNames(string clrNames)
    {
        if (this.clrToSerializationName is not null && this.isFlags && clrNames.Contains(", ", StringComparison.Ordinal))
        {
            return Translate(clrNames, this.clrToSerializationName);
        }

        // We don't need to consider the trivial case of no commas because our caller would have found that in the lookup table and not called us.
        return clrNames;
    }

    private static string Translate(string items, IReadOnlyDictionary<string, string> mapping)
    {
        string[] elements = items.Split(',');

        for (int i = 0; i < elements.Length; i++)
        {
            // Trim the leading space if there is one (due to the delimiter being ", ").
            if (i > 0 && elements[i].Length > 0 && elements[i][0] == ' ')
            {
                elements[i] = elements[i].Substring(1);
            }

            if (mapping.TryGetValue(elements[i], out string? substituteValue))
            {
                elements[i] = substituteValue;
            }
        }

        return string.Join(", ", elements);
    }
}

public sealed partial class EnumAsStringFormatterFactory<T> : MessagePackFormatterFactory
    where T : struct, Enum
{
    readonly bool ignoreCase;

    public EnumAsStringFormatterFactory(bool ignoreCase = false)
    {
        this.ignoreCase = ignoreCase;
    }

    // one method, two signatures: net9+ overrides the base virtual (constraints
    // inherited); downlevel has no base member, so the constraints are spelled out
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(T))
        {
            return new EnumAsStringFormatter<TWriteBuffer, TReadBuffer, T>(ignoreCase);
        }

        return null;
    }
}

public sealed class GenericEnumAsStringFormatterFactory : GenericFormatterFactoryBase
{
    readonly bool ignoreCase;

    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    public GenericEnumAsStringFormatterFactory(bool ignoreCase = false)
    {
        this.ignoreCase = ignoreCase;
    }

    protected override Type? GetOpenFactoryType(Type type, out Type[] typeArguments, out object?[]? constructorArguments)
    {
        typeArguments = [type];
        constructorArguments = [ignoreCase];
        return type.IsEnum ? typeof(EnumAsStringFormatterFactory<>) : null;
    }
}

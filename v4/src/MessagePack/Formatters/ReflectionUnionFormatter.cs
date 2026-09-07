using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MessagePack.Formatters;

// v3 DynamicUnionResolver's role, serving assemblies the source generator never saw. That includes pre-built v3 DLLs.
// The v3 attribute name (MessagePack.UnionAttribute, Key/SubType shape) is matched alongside the v4 name
// (MessagePack.UnionTagAttribute, Tag/CaseType), both duck-read by full name like the rest of this tier, and v4 ships
// no UnionAttribute type, so nothing collides with the C# union feature's attribute namespace.
// One deliberate difference from the source-generated union formatter: an untagged runtime type writes nil, v3's exact
// behavior. This tier's whole purpose is running v3 payload producers and consumers unchanged, version skew included,
// while the generator keeps its throw (there the writer knows its own type set, so an untagged value is a declaration bug).

/// <summary>
/// Serializes a polymorphic base or union through reflection over its <see cref="UnionTagAttribute"/> cases, as a two-element array of tag and value.
/// An untagged runtime type writes nil and an unknown tag reads as null, as in v3.
/// New code should declare unions for the source generator; this formatter exists for compatibility.
/// </summary>
public sealed partial class ReflectionUnionFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T?>
{
    CaseSlot[] cases = null!;
    Dictionary<Type, CaseSlot> caseByType = null!;
    Dictionary<int, CaseSlot> caseByTag = null!;

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "instances only exist behind ReflectionFormatterFactory's RequiresDynamicCode gate; the caller has already opted into dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "CreateCaseSlot is a private member of this formatter, rooted by the typeof/nameof reference below; its type argument is a declared union case type, rooted by the attribute that names it")]
    public void Initialize(MessagePackFormatterResolver resolver)
    {
        var declared = ReadUnionCases(typeof(T));
        var slots = new CaseSlot[declared.Count];
        var byType = new Dictionary<Type, CaseSlot>(declared.Count);
        var byTag = new Dictionary<int, CaseSlot>(declared.Count);
        var createCaseSlot = typeof(ReflectionUnionFormatter<TWriteBuffer, TReadBuffer, T>).GetMethod(nameof(CreateCaseSlot), BindingFlags.NonPublic | BindingFlags.Static)!;
        for (int i = 0; i < declared.Count; i++)
        {
            var (tag, caseType) = declared[i];
            var slot = (CaseSlot)createCaseSlot.MakeGenericMethod(caseType).Invoke(null, [resolver, tag])!;
            slots[i] = slot;
            if (!byTag.TryAdd(tag, slot))
            {
                throw new MessagePackSerializationException($"'{typeof(T).FullName}' declares union tag {tag} more than once.");
            }
            if (!byType.TryAdd(caseType, slot))
            {
                throw new MessagePackSerializationException($"'{typeof(T).FullName}' declares union case '{caseType.FullName}' more than once.");
            }
        }
        cases = slots;
        caseByType = byType;
        caseByTag = byTag;
    }

    /// <inheritdoc/>
    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T? value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }
        var runtimeType = value.GetType();
        if (!caseByType.TryGetValue(runtimeType, out var slot))
        {
            buffer.WriteNil(); // v3 rule, an untagged runtime type is version skew rather than an error
            return;
        }
        state.Enter();
        buffer.WriteArrayHeader(2);
        buffer.WriteInt32(slot.Tag);
        slot.Serialize(ref buffer, ref state, value);
        state.Exit();
    }

    /// <inheritdoc/>
    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T? value)
    {
        if (buffer.TryReadNil())
        {
            value = default;
            return;
        }
        state.Enter();
        if (buffer.ReadArrayHeader() != 2)
        {
            throw new MessagePackSerializationException("Invalid Union data was detected.");
        }
        if (caseByTag.TryGetValue(buffer.ReadInt32(), out var slot))
        {
            value = (T?)slot.Deserialize(ref buffer, ref state);
        }
        else
        {
            buffer.Skip(); // version tolerance for an unknown case tag
            value = default;
        }
        state.Exit();
    }

    // Both attribute generations are matched by full name (the attribute's own base chain is walked so derived
    // attribute types count), v4 [UnionTag(caseType, tag)] with Tag/CaseType/CaseTypeParameter and v3 [Union(key, subType)]
    // with Key/SubType. The type-parameter form ([UnionTag("T", 0)] on a generic root) resolves against the closed
    // root's type arguments.
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "attribute property reads are gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation; attributes applied in user code keep their properties rooted")]
    static List<(int Tag, Type CaseType)> ReadUnionCases(Type type)
    {
        var result = new List<(int, Type)>();
        foreach (var attribute in type.GetCustomAttributes(inherit: false))
        {
            for (var current = attribute.GetType(); current is not null; current = current.BaseType)
            {
                if (current.FullName == MessagePackAttributeNames.UnionTag)
                {
                    var attributeType = attribute.GetType();
                    var tag = attributeType.GetProperty("Tag")?.GetValue(attribute) as int?
                        ?? throw new MessagePackSerializationException($"'{typeof(T).FullName}' carries a union attribute without a readable Tag.");
                    var caseType = attributeType.GetProperty("CaseType")?.GetValue(attribute) as Type;
                    if (caseType is null)
                    {
                        var parameterName = attributeType.GetProperty("CaseTypeParameter")?.GetValue(attribute) as string;
                        caseType = ResolveTypeParameter(type, parameterName)
                            ?? throw new MessagePackSerializationException($"'{typeof(T).FullName}' union tag {tag} names type parameter '{parameterName}', which the closed root does not declare.");
                    }
                    result.Add((tag, caseType));
                    break;
                }
                if (current.FullName == MessagePackAttributeNames.V3Union)
                {
                    var attributeType = attribute.GetType();
                    if (attributeType.GetProperty("Key")?.GetValue(attribute) is not int tag)
                    {
                        // A migration alias, an attribute named like v3's [Union] but without its Key property, deriving
                        // from [UnionTag] to dodge CS0104 in dual-using code. Keep walking, the base chain reads as v4.
                        continue;
                    }
                    var caseType = attributeType.GetProperty("SubType")?.GetValue(attribute) as Type
                        ?? throw new MessagePackSerializationException($"'{typeof(T).FullName}' carries a v3 [Union] attribute whose SubType did not resolve.");
                    result.Add((tag, caseType));
                    break;
                }
            }
        }
        result.Sort(static (x, y) => x.Item1.CompareTo(y.Item1));
        return result;
    }

    static Type? ResolveTypeParameter(Type closedRoot, string? parameterName)
    {
        if (parameterName is null || !closedRoot.IsConstructedGenericType)
        {
            return null;
        }
        var parameters = closedRoot.GetGenericTypeDefinition().GetGenericArguments();
        var arguments = closedRoot.GetGenericArguments();
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].Name == parameterName)
            {
                return arguments[i];
            }
        }
        return null;
    }

    static CaseSlot CreateCaseSlot<TCase>(MessagePackFormatterResolver resolver, int tag)
    {
        return new CaseSlot<TCase>(resolver, tag);
    }

    abstract class CaseSlot
    {
        public int Tag;

        public abstract void Serialize(ref TWriteBuffer buffer, ref SerializeState state, object value);

        public abstract object? Deserialize(ref TReadBuffer buffer, ref DeserializeState state);
    }

    sealed class CaseSlot<TCase> : CaseSlot
    {
        readonly IMessagePackFormatter<TWriteBuffer, TReadBuffer, TCase> formatter;

        public CaseSlot(MessagePackFormatterResolver resolver, int tag)
        {
            formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TCase>();
            Tag = tag;
        }

        public override void Serialize(ref TWriteBuffer buffer, ref SerializeState state, object value)
        {
            formatter.Serialize(ref buffer, ref state, (TCase)value);
        }

        public override object? Deserialize(ref TReadBuffer buffer, ref DeserializeState state)
        {
            TCase value = default!;
            formatter.Deserialize(ref buffer, ref state, ref value);
            return value;
        }
    }
}

/// <summary>Creates <see cref="ReflectionUnionFormatter{TWriteBuffer, TReadBuffer, T}"/> instances for <typeparamref name="T"/>.</summary>
public sealed partial class ReflectionUnionFormatterFactory<T> : MessagePackFormatterFactory
{
    /// <summary>Creates the formatter for <paramref name="type"/>, which must be <typeparamref name="T"/>.</summary>
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, SerializerFoundation.IWriteBuffer
        where TReadBuffer : struct, SerializerFoundation.IReadBuffer
#endif
    {
        return type == typeof(T) ? new ReflectionUnionFormatter<TWriteBuffer, TReadBuffer, T>() : null;
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif
using System.Text;

namespace MessagePack.Formatters;

// MsgPack104: the direct member slots only swap in after the resolver returned the canonical StringFormatter,
// so their direct primitive calls are resolver-respecting by construction (see the Direct-slot comment).
#pragma warning disable MsgPack104

// The attribute contract is MessagePack-CSharp's, matched by full name exactly like the source generator does,
// so an annotations assembly a v3 codebase already references keeps working unchanged.
// The System.Runtime.Serialization alternates ride the same name matching. The v3 dynamic resolvers honored them,
// and this tier keeps that (the generator does not).
static class MessagePackAttributeNames
{
    public const string MessagePackObject = "MessagePack.MessagePackObjectAttribute";
    public const string Key = "MessagePack.KeyAttribute";
    public const string UnionTag = "MessagePack.UnionTagAttribute";
    public const string V3Union = "MessagePack.UnionAttribute"; // v3's name for UnionTag, from v3-built assemblies
    public const string IgnoreMember = "MessagePack.IgnoreMemberAttribute";
    public const string SerializationConstructor = "MessagePack.SerializationConstructorAttribute";
    public const string RequiredMember = "System.Runtime.CompilerServices.RequiredMemberAttribute";
    public const string Formatter = "MessagePack.MessagePackFormatterAttribute";
    public const string DataContract = "System.Runtime.Serialization.DataContractAttribute";
    public const string DataMember = "System.Runtime.Serialization.DataMemberAttribute";
    public const string IgnoreDataMember = "System.Runtime.Serialization.IgnoreDataMemberAttribute";
}

// A plain generic class driven by a member table built once in Initialize. Reflection is distilled into typed
// delegates at construction time, so the steady-state serialize path is one virtual call plus one delegate call per
// member, boxing-free. Formatters are never emitted. The only IL generation in here is the one-instruction field
// accessors in FieldSlot (fields have no MethodInfo to bind a delegate to). It replaces v3's whole
// DynamicObject/DynamicContractlessObject resolver family with one class whose format follows the type:
//   [MessagePackObject] with [Key(int)]    - array of MaxKey+1 slots, nil holes
//                                            (byte-compatible with the source-generated
//                                            formatter and v3's DynamicObjectResolver)
//   [MessagePackObject] with [Key(string)]
//   or [MessagePackObject(true)]           - map of declared key to value
//   [DataContract]                         - [DataMember]-opted members only; Order is the
//                                            int key, Name the string key, neither means
//                                            the member name (v3 DynamicObjectResolver)
//   no attribute                           - map of member name to value (v3 contractless)
// Annotated shapes are validated at Initialize with the source generator's rules (MsgPack001 missing key, MsgPack002
// mixed keys, MsgPack003 duplicates, MsgPack008 negative), so a type cannot silently serialize differently depending
// on whether the generator covered it. One deliberate relaxation: a keyed member without a setter (MsgPack007, a
// generator error) is serialized and skipped on read here, because the constructor-argument path below often fills
// it, which the runtime can verify and the generator cannot yet.
//
// Contractless members are public instance properties (public getter required, setter optional)
// and public instance fields. Construction follows v3's rule. The public constructor with the most parameters where
// every parameter name-matches a member (ordinal-ignore-case, assignable type) wins,
// and parameterless is the natural last resort. A parameterized winner switches deserialization to the argument-state
// path (values buffered boxed, constructor invoked, leftovers applied through setters),
// which is what makes records, anonymous types and getter-only members round-trip.
// The zero-boxing populate path still serves parameterless types and explicit populate calls.

/// <summary>
/// Serializes objects through reflection, following the same attribute rules as the source generator and producing the same bytes.
/// Types without attributes are serialized as maps of member name to value.
/// </summary>
public sealed partial class ReflectionObjectFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
{
    enum ConstructionMode
    {
        Populate,        // parameterless (or struct default): create then set members
        Arguments,       // parameterized constructor: buffer values, invoke, then setters
        Unconstructable, // no usable constructor: only populate-into-existing works
    }

    // not readonly: [MessagePackObject(AllowPrivate = true)] widens it in BuildMemberTable
    bool allowPrivate;

    static readonly bool IsCallbackReceiver = typeof(IMessagePackSerializationCallbackReceiver).IsAssignableFrom(typeof(T));

    MemberSlot[] slots = null!;
    byte[][] encodedKeys = null!; // UTF-8 wire keys, parallel to slots (map modes only)
    bool arrayFormat;             // [Key(int)] (or an empty annotated type): array wire form
    int[] arraySlotByKey = [];    // wire index -> slot index, -1 = key hole (array mode only)
    ConstructionMode mode;
    ConstructorInfo? argumentConstructor; // Arguments mode only
    int argumentCount;
    bool validateRequired;        // resolver.ValidateRequiredMembers, snapshot at Initialize
    bool hasRequiredSlots;
    int requiredArrayCount;       // array mode: smallest count that still carries every required slot

    /// <summary>Creates a formatter over the public members of <typeparamref name="T"/>.</summary>
    public ReflectionObjectFormatter()
        : this(allowPrivate: false)
    {
    }

    // No extra machinery is needed for allowPrivate. Delegate binding to a private accessor is just reflection,
    // which is what made v3 grow a separate DynamicMethod-based resolver family.
    /// <summary>Creates a formatter that, with <paramref name="allowPrivate"/>, also includes non-public members, accessors and constructors.</summary>
    public ReflectionObjectFormatter(bool allowPrivate)
    {
        this.allowPrivate = allowPrivate;
    }

    /// <inheritdoc/>
    public void Initialize(MessagePackFormatterResolver resolver)
    {
        BuildMemberTable();
        validateRequired = resolver.ValidateRequiredMembers;
        for (int i = 0; i < slots.Length; i++)
        {
            // ValidateNull was pre-set to the member's non-nullable annotation;
            // the resolver flag gates whether that annotation is actually enforced
            slots[i].ValidateNull &= resolver.ValidateNullableAnnotations;
            hasRequiredSlots |= slots[i].IsRequired;
            slots[i].Initialize(resolver);
            var specialized = slots[i].Specialize();
            if (!ReferenceEquals(specialized, slots[i]))
            {
                specialized.ConstructorParameterIndex = slots[i].ConstructorParameterIndex;
                specialized.MemberName = slots[i].MemberName;
                specialized.IsRequired = slots[i].IsRequired;
                specialized.ValidateNull = slots[i].ValidateNull;
                slots[i] = specialized;
            }
        }
        if (arrayFormat)
        {
            for (int key = 0; key < arraySlotByKey.Length; key++)
            {
                if (arraySlotByKey[key] >= 0 && slots[arraySlotByKey[key]].IsRequired)
                {
                    requiredArrayCount = key + 1;
                }
            }
        }
    }

    readonly record struct MemberCandidate(MemberInfo Member, Type MemberType, bool IsPublic, string WireName);

    sealed class BaseTypesFirstComparer : IComparer<Type?>
    {
        public static readonly BaseTypesFirstComparer Instance = new();

        public int Compare(Type? x, Type? y)
        {
            if (x == y)
            {
                return 0;
            }
            if (x is null)
            {
                return -1;
            }
            if (y is null)
            {
                return 1;
            }
            if (x.IsAssignableFrom(y))
            {
                return -1; // x is a base of y
            }
            if (y.IsAssignableFrom(x))
            {
                return 1;
            }
            return 0;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2090", Justification = "reflection member discovery is gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation; trimmed members are the documented risk the caller accepted")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "the allowPrivate hierarchy walk enumerates base types of T; same RequiresUnreferencedCode gate and documented trimming risk as the direct typeof(T) discovery")]
    void BuildMemberTable()
    {
        // inherit: true, honoring each attribute's own AttributeUsage: [MessagePackObject] declares Inherited = true
        // (v3 and v4 alike), so an unannotated derived type serializes by its base's contract;
        // [DataContract] declares Inherited = false
        var typeAttributes = typeof(T).GetCustomAttributes(inherit: true);
        var objectAttribute = FindAttribute(typeAttributes, MessagePackAttributeNames.MessagePackObject);
        allowPrivate |= ReadBoolProperty(objectAttribute, "AllowPrivate");
        if (ReadBoolProperty(objectAttribute, "AllowCircularReferences"))
        {
            // the envelope/back-reference wire and register-before-populate discipline live only in the
            // source-generated formatter; serving the type here would silently produce a different (untracked)
            // wire format
            throw new NotSupportedException($"[MessagePackObject(AllowCircularReferences = true)] on '{typeof(T).FullName}' requires the source-generated formatter; the reflection tier does not serve circular-reference types.");
        }

        // Discovery enumerates v3's attributed-table order: every property across the hierarchy base-types-first,
        // then every field the same way (DynamicObjectResolver's GetAllProperties/GetAllFields recursion),
        // each level DeclaredOnly, a flattened GetProperties call would hide a `new`-shadowed base property,
        // and every declaration of a shadowed name serializes (v3's contract, oracle-probed).
        // Overrides are enumerated at both declarations, exactly like v3: each is independently eligible (a
        // [DataMember]/[Key] may sit on either end of the chain), and the keyed tables below collapse the pair when
        // their keys collide.
        var levels = new List<Type>();
        for (var current = typeof(T); current is not null && current != typeof(object); current = current.BaseType)
        {
            levels.Add(current);
        }
        levels.Reverse();

        var candidates = new List<MemberCandidate>();
        var memberFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
            | (allowPrivate ? BindingFlags.NonPublic : 0);
        foreach (var level in levels)
        {
            foreach (var property in level.GetProperties(memberFlags))
            {
                var getMethod = property.GetMethod;
                if (property.GetIndexParameters().Length != 0 ||
                    getMethod is null ||
                    (!allowPrivate && !getMethod.IsPublic))
                {
                    continue;
                }
                candidates.Add(new MemberCandidate(property, property.PropertyType, getMethod.IsPublic, property.Name));
            }
        }
        foreach (var level in levels)
        {
            foreach (var field in level.GetFields(memberFlags))
            {
                // compiler-generated fields are auto-property backing fields (and friends): the property slot already
                // owns that data. [NonSerialized] opts a field out in every mode (v3's shared rule)
                if (field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) ||
                    field.IsDefined(typeof(NonSerializedAttribute), inherit: true))
                {
                    continue;
                }
                candidates.Add(new MemberCandidate(field, field.FieldType, field.IsPublic, field.Name));
            }
        }

        // v3's contractless/map-by-name order is different: base level first,
        // properties then fields within each level (a stable declaring-type sort over the kind-major list produces
        // exactly that). Overrides collapse to the most derived declaration here (no keys to collide on,
        // so the collapse happens in the view itself). The shadowed-name qualification is defined on this view: the
        // base-most declaration keeps the plain name, every later one gets the "{DeclaringType.FullName}.{Name}" map
        // key. deliberate deviation from v3's dynamic resolver: v3 quietly dropped a property shadowed by another
        // property (a GetRuntimeProperties flattening artifact, shadowed fields it qualified and kept);
        // v4 treats every declaration as the separate storage it is, uniformly (v3's own mpc kept both, too).
        var seenRootGetters = new HashSet<MethodInfo>();
        var keepForPerLevel = new bool[candidates.Count];
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            keepForPerLevel[i] = candidates[i].Member is not PropertyInfo property
                || property.GetMethod is not { } getMethod
                || seenRootGetters.Add(getMethod.GetBaseDefinition());
        }
        var perLevelCandidates = candidates
            .Where((c, i) => keepForPerLevel[i])
            .OrderBy(static c => c.Member.DeclaringType, BaseTypesFirstComparer.Instance)
            .ToList();
        var namesInBaseOrder = new HashSet<string>();
        for (int i = 0; i < perLevelCandidates.Count; i++)
        {
            if (!namesInBaseOrder.Add(perLevelCandidates[i].Member.Name))
            {
                perLevelCandidates[i] = perLevelCandidates[i] with { WireName = $"{perLevelCandidates[i].Member.DeclaringType!.FullName}.{perLevelCandidates[i].Member.Name}" };
            }
        }

        // v3 sorts string-keyed members by [DataMember(Order)] last of all (absent members sort as int.MaxValue,
        // present-without-Order as -1, ties stay stable); the int-keyed wire is positioned by key,
        // so applying it uniformly is harmless there
        candidates = [.. candidates.OrderBy(static c => GetDataMemberOrder(c.Member))];
        perLevelCandidates = [.. perLevelCandidates.OrderBy(static c => GetDataMemberOrder(c.Member))];

        foreach (var candidate in candidates)
        {
            if (candidate.MemberType == typeof(MessagePackUnknownMembers))
            {
                // capture/replay lives only in the source-generated formatter;
                // serving the type here would silently drop the retention the member exists to provide
                throw new NotSupportedException($"'{typeof(T).FullName}.{candidate.Member.Name}' declares a MessagePackUnknownMembers member, which requires the source-generated formatter; the reflection tier does not capture unknown members.");
            }
        }

        if (objectAttribute is not null)
        {
            // map-by-name mode follows the contractless enumeration (v3 routed KeyAsPropertyName through the same
            // branch); explicit [Key] the attributed one
            BuildAttributedTable(candidates, perLevelCandidates, objectAttribute);
        }
        else if (FindAttribute(typeAttributes, MessagePackAttributeNames.DataContract) is not null)
        {
            BuildDataContractTable(candidates);
        }
        else
        {
            BuildContractlessTable(perLevelCandidates);
        }
    }

    void BuildContractlessTable(List<MemberCandidate> candidates)
    {
        // [IgnoreMember]/[IgnoreDataMember] exclude in contractless too (v3's DynamicContractlessObjectResolver honored
        // both); [Key] stays meaningless here
        var included = new List<MemberCandidate>(candidates.Count);
        foreach (var candidate in candidates)
        {
            // includes contract attributes declared on a base virtual (see GetMemberAttributes)
            var attributes = GetMemberAttributes(candidate.Member);
            if (FindAttribute(attributes, MessagePackAttributeNames.IgnoreMember) is null
                && FindAttribute(attributes, MessagePackAttributeNames.IgnoreDataMember) is null)
            {
                included.Add(candidate);
            }
        }

        var memberSlots = new MemberSlot[included.Count];
        var keys = new byte[included.Count][];
        var names = new string[included.Count];
        var keepWithoutConstructor = new List<bool>(included.Count);
        for (int i = 0; i < included.Count; i++)
        {
            memberSlots[i] = CreateSlot(included[i]);
            ApplyMemberMetadata(memberSlots[i], included[i]);
            keys[i] = Encoding.UTF8.GetBytes(included[i].WireName); // shadowed members carry their qualified key
            names[i] = included[i].Member.Name;                     // constructor parameters match the plain name
            keepWithoutConstructor.Add(IsWritableMember(included[i], allowPrivate));
        }

        slots = memberSlots;
        encodedKeys = keys;
        SelectConstructor(names);
        FilterNonContractSlots(keepWithoutConstructor);
    }

    void BuildAttributedTable(List<MemberCandidate> candidates, List<MemberCandidate> perLevelCandidates, object objectAttribute)
    {
        var keyAsPropertyName = ReadBoolProperty(objectAttribute, "KeyAsPropertyName");
        var namingPolicy = ReadKeyNamingPolicyProperty(objectAttribute);
        if (keyAsPropertyName)
        {
            // v3 wire order per branch: map-by-name follows the contractless per-level enumeration (which also carries
            // the shadowed-name qualification); explicit [Key] keeps the kind-major list.
            // Int keys position by key either way.
            candidates = perLevelCandidates;
        }

        var memberSlots = new List<MemberSlot>();
        var names = new List<string>();
        var memberIntKeys = new List<int>();
        var memberStringKeys = new List<string?>();
        var seenIntKeys = new Dictionary<int, MemberCandidate>();
        var seenStringKeys = new Dictionary<string, MemberCandidate>();
        var keepWithoutConstructor = new List<bool>();
        var hasIntKey = false;
        var hasStringKey = keyAsPropertyName;

        foreach (var candidate in candidates)
        {
            var name = candidate.Member.Name;
            // includes contract attributes declared on a base virtual (see GetMemberAttributes)
            var attributes = GetMemberAttributes(candidate.Member);
            if (FindAttribute(attributes, MessagePackAttributeNames.IgnoreMember) is not null
                || FindAttribute(attributes, MessagePackAttributeNames.IgnoreDataMember) is not null)
            {
                continue;
            }

            int? intKey = null;
            string? stringKey = null;
            if (FindAttribute(attributes, MessagePackAttributeNames.Key) is { } keyAttribute)
            {
                (intKey, stringKey) = ReadKeyArguments(keyAttribute);
                ThrowIfOverrideChangesKey(attributes, intKey, stringKey, name);
            }

            if (keyAsPropertyName && intKey is not null)
            {
                // v3 rule: KeyAsPropertyName wins; a stray int [Key] on a map-mode type is ignored and the member keeps
                // its name-derived string key
                intKey = null;
            }

            if (intKey is null && stringKey is null)
            {
                if (keyAsPropertyName)
                {
                    // an explicit [Key("...")] took the branch above, so the policy only shapes defaults;
                    // a shadowed member's qualified key passes through raw
                    stringKey = candidate.WireName != name ? candidate.WireName : KeyNamingPolicyConverter.ConvertName(namingPolicy, name);
                }
                else if (candidate.IsPublic)
                {
                    // the source generator's MsgPack001, enforced at runtime so the reflection and generated
                    // interpretations of a type can never diverge
                    throw new MessagePackSerializationException($"'{typeof(T).FullName}.{name}' is a public member of a [MessagePackObject] type and needs [Key] or [IgnoreMember] (or use [MessagePackObject(true)]).");
                }
                else
                {
                    continue; // non-public members participate only by explicit [Key]
                }
            }

            if (intKey is { } key)
            {
                if (key < 0)
                {
                    throw new MessagePackSerializationException($"'{typeof(T).FullName}.{name}' has a negative key ({key}).");
                }
                if (seenIntKeys.TryGetValue(key, out var conflicting))
                {
                    if (IsQuietOverrideCollapse(conflicting, candidate))
                    {
                        continue;
                    }
                    throw new MessagePackSerializationException($"'{typeof(T).FullName}' declares key {key} more than once.");
                }
                seenIntKeys.Add(key, candidate);
                hasIntKey = true;
            }
            else
            {
                if (seenStringKeys.TryGetValue(stringKey!, out var conflicting))
                {
                    if (IsQuietOverrideCollapse(conflicting, candidate))
                    {
                        continue;
                    }
                    throw new MessagePackSerializationException($"'{typeof(T).FullName}' declares key \"{stringKey}\" more than once.");
                }
                seenStringKeys.Add(stringKey!, candidate);
                hasStringKey = true;
            }

            var slot = CreateSlot(candidate);
            ApplyMemberMetadata(slot, candidate);
            memberSlots.Add(slot);
            names.Add(name);
            memberIntKeys.Add(intKey ?? -1);
            memberStringKeys.Add(stringKey);
            // in map mode, a name-defaulted member follows the contractless inclusion rule (v3 treated it as
            // non-explicit); an explicit [Key] always serializes
            keepWithoutConstructor.Add(FindAttribute(attributes, MessagePackAttributeNames.Key) is not null || IsWritableMember(candidate, allowPrivate));
        }

        if (hasIntKey && hasStringKey)
        {
            throw new MessagePackSerializationException($"'{typeof(T).FullName}' mixes int keys and string keys; a type serializes as either an array (int keys) or a map (string keys), not both.");
        }

        FinishKeyedTable(memberSlots, names, memberIntKeys, memberStringKeys, hasStringKey);
        if (keyAsPropertyName)
        {
            FilterNonContractSlots(keepWithoutConstructor);
        }
    }

    // [DataContract] (v3 DynamicObjectResolver parity): members are opt-in via [DataMember],
    // Order becomes the int key, Name the string key, neither means the member name,
    // and the array/map split plus duplicate/mixed validation follow the [Key] rules exactly
    void BuildDataContractTable(List<MemberCandidate> candidates)
    {
        var memberSlots = new List<MemberSlot>();
        var names = new List<string>();
        var memberIntKeys = new List<int>();
        var memberStringKeys = new List<string?>();
        var seenIntKeys = new Dictionary<int, MemberCandidate>();
        var seenStringKeys = new Dictionary<string, MemberCandidate>();
        var hasIntKey = false;
        var hasStringKey = false;

        foreach (var candidate in candidates)
        {
            var name = candidate.Member.Name;
            // includes contract attributes declared on a base virtual (see GetMemberAttributes)
            var attributes = GetMemberAttributes(candidate.Member);
            if (FindAttribute(attributes, MessagePackAttributeNames.DataMember) is not { } dataMember
                || FindAttribute(attributes, MessagePackAttributeNames.IgnoreMember) is not null
                || FindAttribute(attributes, MessagePackAttributeNames.IgnoreDataMember) is not null)
            {
                continue;
            }

            var (order, explicitName) = ReadDataMemberArguments(dataMember);
            int? intKey = order >= 0 ? order : null;
            var stringKey = intKey is null ? explicitName ?? name : null;

            if (intKey is { } key)
            {
                if (seenIntKeys.TryGetValue(key, out var conflicting))
                {
                    if (IsQuietOverrideCollapse(conflicting, candidate))
                    {
                        continue;
                    }
                    throw new MessagePackSerializationException($"'{typeof(T).FullName}' declares [DataMember] Order {key} more than once.");
                }
                seenIntKeys.Add(key, candidate);
                hasIntKey = true;
            }
            else
            {
                if (seenStringKeys.TryGetValue(stringKey!, out var conflicting))
                {
                    if (IsQuietOverrideCollapse(conflicting, candidate))
                    {
                        continue;
                    }
                    throw new MessagePackSerializationException($"'{typeof(T).FullName}' declares [DataMember] name \"{stringKey}\" more than once.");
                }
                seenStringKeys.Add(stringKey!, candidate);
                hasStringKey = true;
            }

            var slot = CreateSlot(candidate);
            ApplyMemberMetadata(slot, candidate);
            memberSlots.Add(slot);
            names.Add(name);
            memberIntKeys.Add(intKey ?? -1);
            memberStringKeys.Add(stringKey);
        }

        if (hasIntKey && hasStringKey)
        {
            throw new MessagePackSerializationException($"'{typeof(T).FullName}' mixes [DataMember] Order and Name/name-less members; a type serializes as either an array (Order) or a map (names), not both.");
        }

        FinishKeyedTable(memberSlots, names, memberIntKeys, memberStringKeys, hasStringKey);
    }

    void FinishKeyedTable(List<MemberSlot> memberSlots, List<string> names, List<int> memberIntKeys, List<string?> memberStringKeys, bool hasStringKey)
    {
        slots = [.. memberSlots];
        if (!hasStringKey)
        {
            // int keys (or a memberless annotated type, which the generator also treats as an empty array)
            // serialize as the v3 array format with nil key holes
            arrayFormat = true;
            var length = 0;
            foreach (var key in memberIntKeys)
            {
                length = Math.Max(length, key + 1);
            }
            var slotByKey = new int[length];
            for (int i = 0; i < slotByKey.Length; i++)
            {
                slotByKey[i] = -1;
            }
            for (int i = 0; i < memberIntKeys.Count; i++)
            {
                slotByKey[memberIntKeys[i]] = i;
            }
            arraySlotByKey = slotByKey;
            encodedKeys = [];
        }
        else
        {
            var keys = new byte[memberStringKeys.Count][];
            for (int i = 0; i < memberStringKeys.Count; i++)
            {
                keys[i] = Encoding.UTF8.GetBytes(memberStringKeys[i]!);
            }
            encodedKeys = keys;
        }

        SelectConstructor([.. names]);
    }

    // duck-typed like the [MessagePackObject] property reads: the attribute is matched by full name,
    // so the System.Runtime.Serialization reference stays the user's
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "attribute property reads are gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation; attributes applied in user code keep their properties rooted")]
    static (int Order, string? Name) ReadDataMemberArguments(object attribute)
    {
        var type = attribute.GetType();
        return (type.GetProperty("Order")?.GetValue(attribute) as int? ?? -1,
                type.GetProperty("Name")?.GetValue(attribute) as string);
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "instances only exist behind ReflectionFormatterFactory's RequiresDynamicCode gate; the caller has already opted into dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "the slot-creation methods are private members of this formatter, rooted by the typeof/nameof references below; their TMember argument derives from the discovered member types")]
    MemberSlot CreateSlot(in MemberCandidate candidate)
    {
        var formatterType = typeof(ReflectionObjectFormatter<TWriteBuffer, TReadBuffer, T>);
        if (candidate.Member is PropertyInfo property)
        {
            var createPropertySlot = formatterType.GetMethod(nameof(CreatePropertySlot), BindingFlags.NonPublic | BindingFlags.Static)!;
            return (MemberSlot)createPropertySlot.MakeGenericMethod(candidate.MemberType).Invoke(null, [property, allowPrivate])!;
        }
        var createFieldSlot = formatterType.GetMethod(nameof(CreateFieldSlot), BindingFlags.NonPublic | BindingFlags.Static)!;
        return (MemberSlot)createFieldSlot.MakeGenericMethod(candidate.MemberType).Invoke(null, [(FieldInfo)candidate.Member])!;
    }

    static void ApplyMemberMetadata(MemberSlot slot, in MemberCandidate candidate)
    {
        slot.MemberName = candidate.Member.Name;
        // the C# required modifier surfaces as a compiler-embedded attribute,
        // matched by name like the rest of the contract; constructor-parameter required-ness is added in
        // SelectConstructor once the deserialization constructor is known includes contract attributes declared on a
        // base virtual (see GetMemberAttributes)
            var attributes = GetMemberAttributes(candidate.Member);
        slot.IsRequired = FindAttribute(attributes, MessagePackAttributeNames.RequiredMember) is not null;
        slot.ValidateNull = NullableAnnotationReader.IsNonNullableReference(candidate.Member, candidate.MemberType);
        if (FindAttribute(attributes, MessagePackAttributeNames.Formatter) is { } formatterAttribute)
        {
            slot.CustomFormatter = CreateCustomFormatter(formatterAttribute, candidate.MemberType, candidate.Member.Name);
        }
    }

    // member-level [MessagePackFormatter]: the v3-form interpretation (and the rejection of the source generator's
    // expression-string form) lives in AttributeFormatterActivator
    static object CreateCustomFormatter(object attribute, Type memberType, string memberName)
    {
        var subject = $"{typeof(T).FullName}.{memberName}";
#if NET9_0_OR_GREATER
        return AttributeFormatterActivator.Create<TWriteBuffer, TReadBuffer>(attribute, memberType, subject);
#else
        return AttributeFormatterActivator.Create(attribute, memberType, typeof(TWriteBuffer), typeof(TReadBuffer), subject);
#endif
    }

    static IMessagePackFormatter<TWriteBuffer, TReadBuffer, TMember> ResolveSlotFormatter<TMember>(MessagePackFormatterResolver resolver, MemberSlot slot)
    {
        if (slot.CustomFormatter is { } custom)
        {
            if (custom is not IMessagePackFormatter<TWriteBuffer, TReadBuffer, TMember> typed)
            {
                throw new MessagePackSerializationException($"[MessagePackFormatter] on '{typeof(T).FullName}.{slot.MemberName}' produced '{custom.GetType().FullName}', which is not an IMessagePackFormatter for '{typeof(TMember).FullName}'.");
            }
            // the resolver initializes only its own products; this instance is ours
            typed.Initialize(resolver);
            return typed;
        }
        return resolver.GetFormatter<TWriteBuffer, TReadBuffer, TMember>();
    }

    static int GetDataMemberOrder(MemberInfo member) =>
        FindAttribute(GetMemberAttributes(member), MessagePackAttributeNames.DataMember) is { } dataMember
            ? ReadDataMemberArguments(dataMember).Order
            : int.MaxValue;

    // v3's serialization-inclusion writability: a public setter (any setter under allowPrivate)
    // or a non-initonly field. Distinct from MemberSlot.CanSet, which reports what the slot can physically write
    // (initonly fields included).
    static bool IsWritableMember(in MemberCandidate candidate, bool allowPrivate) => candidate.Member switch
    {
        PropertyInfo property => property.SetMethod is { } setMethod && (allowPrivate || setMethod.IsPublic),
        FieldInfo field => allowPrivate || !field.IsInitOnly, // v3: AllowPrivate counted even initonly fields
        _ => false,
    };

    // v3's contractless rule: a member that is not writable, not an explicit contract,
    // and not consumed by the selected constructor does not serialize at all - computed getter-only properties (`public
    // int Bad => throw ...`) are never touched
    void FilterNonContractSlots(List<bool> keepWithoutConstructor)
    {
        var keep = new List<int>(slots.Length);
        for (int i = 0; i < slots.Length; i++)
        {
            if (keepWithoutConstructor[i] || slots[i].ConstructorParameterIndex >= 0)
            {
                keep.Add(i);
            }
        }
        if (keep.Count == slots.Length)
        {
            return;
        }
        var filteredSlots = new MemberSlot[keep.Count];
        var filteredKeys = new byte[keep.Count][];
        for (int i = 0; i < keep.Count; i++)
        {
            filteredSlots[i] = slots[keep[i]];
            filteredKeys[i] = encodedKeys[keep[i]];
        }
        slots = filteredSlots;
        encodedKeys = filteredKeys;
    }

    // A property override inherits its base declaration's contract attributes.
    // Plain inherit: true cannot deliver them, [DataMember] (and friends)
    // declare AttributeUsage(Inherited = false), but v3 discovered the base declaration as its own candidate,
    // so those attributes always took effect; walk the base declarations by hand instead.
    // Derived declarations come first, so FindAttribute's first match lets an override's own attribute win over the
    // base one.
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "the base-declaration walk enumerates base types of the already-discovered member's declaring type; gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation like the discovery itself")]
    static object[] GetMemberAttributes(MemberInfo member)
    {
        if (member is not PropertyInfo property
            || property.GetMethod is not { IsVirtual: true } getMethod
            || getMethod.GetBaseDefinition() == getMethod)
        {
            return member.GetCustomAttributes(inherit: false);
        }

        var result = new List<object>(property.GetCustomAttributes(inherit: false));
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        for (var current = property.DeclaringType?.BaseType; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (current.GetProperty(property.Name, flags) is { } baseProperty)
            {
                result.AddRange(baseProperty.GetCustomAttributes(inherit: false));
            }
        }
        return [.. result];
    }

    // v3's quiet override collapse (AddEmittableMemberOrIgnore): the base-first walk enumerates both declarations of an
    // override chain; when the derived one's key lands on the already-added base declaration of the same name and that
    // declaration is overridable, it is the same storage seen twice and the later arrival is skipped.
    // Anything else on a taken key is a genuine duplicate and throws.
    static bool IsQuietOverrideCollapse(MemberCandidate added, MemberCandidate later)
    {
        return added.Member is PropertyInfo existing && later.Member is PropertyInfo
            && existing.Name == later.Member.Name
            && (existing.GetMethod is { IsVirtual: true, IsFinal: false }
                || existing.SetMethod is { IsVirtual: true, IsFinal: false });
    }

    static object? FindAttribute(object[] attributes, string fullName) =>
        AttributeFormatterActivator.FindAttribute(attributes, fullName);

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "attribute property reads are gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation; attributes applied in user code keep their properties rooted")]
    static bool ReadBoolProperty(object? attribute, string propertyName)
    {
        return attribute is not null && attribute.GetType().GetProperty(propertyName)?.GetValue(attribute) is true;
    }

    // duck-typed like ReadBoolProperty: a v3 annotations assembly's attribute has no KeyNamingPolicy property and lands
    // on None
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "attribute property reads are gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation; attributes applied in user code keep their properties rooted")]
    static KeyNamingPolicy ReadKeyNamingPolicyProperty(object? attribute)
    {
        var value = attribute?.GetType().GetProperty("KeyNamingPolicy")?.GetValue(attribute);
        return value is null ? KeyNamingPolicy.None : (KeyNamingPolicy)Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "attribute property reads are gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation; attributes applied in user code keep their properties rooted")]
    // the source generator's MsgPack021, enforced at runtime: an override declaring a different [Key] than the
    // declaration it overrides keys the same storage twice (v3's dynamic resolver wrote both slots, mpc kept one),
    // so the shape is refused in both tiers
    static void ThrowIfOverrideChangesKey(object[] attributes, int? intKey, string? stringKey, string name)
    {
        foreach (var attribute in attributes)
        {
            if (FindAttribute([attribute], MessagePackAttributeNames.Key) is null)
            {
                continue;
            }
            var (otherInt, otherString) = ReadKeyArguments(attribute);
            if (otherInt != intKey || otherString != stringKey)
            {
                throw new MessagePackSerializationException($"'{typeof(T).FullName}.{name}' overrides a property declared with a different [Key]; an override keeps the key of the declaration it overrides.");
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "attribute property reads are gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation; attributes applied in user code keep their properties rooted")]
    static (int? IntKey, string? StringKey) ReadKeyArguments(object keyAttribute)
    {
        var type = keyAttribute.GetType();
        return (type.GetProperty("IntKey")?.GetValue(keyAttribute) as int?,
                type.GetProperty("StringKey")?.GetValue(keyAttribute) as string);
    }

    // v3's rule: [SerializationConstructor] wins outright; otherwise iterate public constructors by parameter count
    // descending and take the first whose parameters all match a member by name (ordinal-ignore-case,
    // so camelCase parameters bind PascalCase members) with an assignable member type (an IEnumerable<T> parameter
    // binds an IReadOnlyList<T> member, an object parameter binds anything,
    // the argument path buffers values boxed and Invoke accepts any assignable value).
    [UnconditionalSuppressMessage("Trimming", "IL2090", Justification = "gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation, same as the member discovery above")]
    void SelectConstructor(string[] memberNames)
    {
        // [SerializationConstructor] is an explicit contract and is honored even on a non-public constructor regardless
        // of allowPrivate (v3 searched Public|NonPublic unconditionally; Invoke has no visibility constraint).
        // The descending fallback scan stays public unless allowPrivate widens it.
        var attributed = typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(static c => FindAttribute(c.GetCustomAttributes(inherit: false), MessagePackAttributeNames.SerializationConstructor) is not null);
        var constructorFlags = BindingFlags.Public | BindingFlags.Instance
            | (allowPrivate ? BindingFlags.NonPublic : 0);
        IEnumerable<ConstructorInfo> candidates = attributed is not null
            ? [attributed]
            : typeof(T).GetConstructors(constructorFlags).OrderByDescending(c => c.GetParameters().Length);
        foreach (var candidate in candidates)
        {
            var parameters = candidate.GetParameters();
            var slotIndices = new int[parameters.Length];
            var matched = true;
            for (int i = 0; i < parameters.Length; i++)
            {
                var slotIndex = -1;
                for (int s = 0; s < memberNames.Length; s++)
                {
                    if (string.Equals(memberNames[s], parameters[i].Name, StringComparison.OrdinalIgnoreCase) &&
                        parameters[i].ParameterType.IsAssignableFrom(slots[s].MemberType))
                    {
                        slotIndex = s;
                        break;
                    }
                }
                if (slotIndex < 0)
                {
                    matched = false;
                    break;
                }
                slotIndices[i] = slotIndex;
            }
            if (!matched)
            {
                continue;
            }

            if (parameters.Length == 0)
            {
                mode = ConstructionMode.Populate;
                return;
            }
            mode = ConstructionMode.Arguments;
            argumentConstructor = candidate;
            argumentCount = parameters.Length;
            for (int i = 0; i < slotIndices.Length; i++)
            {
                slots[slotIndices[i]].ConstructorParameterIndex = i;
                // a parameter without a default value is a required part of the contract
                slots[slotIndices[i]].IsRequired |= !parameters[i].HasDefaultValue;
            }
            return;
        }

        if (attributed is not null)
        {
            // an attributed constructor is an explicit contract; silently falling back to populate would deserialize
            // different members than the author declared
            throw new MessagePackSerializationException($"'{typeof(T).FullName}': every [SerializationConstructor] parameter must match a serialized member by name (case-insensitive) and assignable type.");
        }

        // no constructor matched: structs always default-construct, classes fall back to a (possibly non-public)
        // parameterless constructor
        if (typeof(T).IsValueType ||
            typeof(T).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, binder: null, Type.EmptyTypes, modifiers: null) != null)
        {
            mode = ConstructionMode.Populate;
        }
        else
        {
            mode = ConstructionMode.Unconstructable;
        }
    }

    /// <inheritdoc/>
    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }

        if (IsCallbackReceiver)
        {
            InvokeOnBeforeSerialize(ref value);
        }

        var memberSlots = slots;
        state.Enter();

        if (arrayFormat)
        {
            var slotByKey = arraySlotByKey;
            buffer.WriteArrayHeader(slotByKey.Length);
            for (int i = 0; i < slotByKey.Length; i++)
            {
                var slotIndex = slotByKey[i];
                if (slotIndex < 0)
                {
                    buffer.WriteNil(); // key hole
                }
                else
                {
                    memberSlots[slotIndex].Write(ref buffer, ref state, ref value);
                }
            }
        }
        else
        {
            var keys = encodedKeys;
            buffer.WriteMapHeader(memberSlots.Length);
            for (int i = 0; i < memberSlots.Length; i++)
            {
                buffer.WriteString(keys[i]);
                memberSlots[i].Write(ref buffer, ref state, ref value);
            }
        }

        state.Exit();
    }

    /// <inheritdoc/>
    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        if (buffer.TryReadNil())
        {
            if (default(T) is not null)
            {
                MessagePackSerializationException.ThrowUnexpectedNilWhileDeserializing<T>();
            }
            value = default!;
            return;
        }

        var count = arrayFormat ? buffer.ReadArrayHeader() : buffer.ReadMapHeader();

        // Argument-state path for fresh instances of constructor-matched types.
        // A class instance supplied by the caller keeps populate semantics (structs have no identity to preserve,
        // so they always reconstruct).
        if (mode == ConstructionMode.Arguments && (value is null || typeof(T).IsValueType))
        {
            DeserializeWithArguments(ref buffer, ref state, ref value, count);
            return;
        }

        // Populate contract: reuse the incoming instance; members absent from the payload keep their current values.
        if (value is null)
        {
            value = CreateInstance();
        }

        state.Enter();
        Span<bool> seenStorage = WantsStackallocSeenSlots(arrayFormat, slots.Length) ? stackalloc bool[StackallocSeenSlotsLimit] : default;
        var seenSlots = new SeenSlots(arrayFormat ? 0 : slots.Length, seenStorage);
        for (int i = 0; i < count; i++)
        {
            var slotIndex = NextSlot(ref buffer, i);
            if (slotIndex < 0)
            {
                buffer.Skip(); // unknown member / key hole: ignore its value (version tolerance)
            }
            else
            {
                if (!arrayFormat)
                {
                    seenSlots.MarkOrThrow(slotIndex);
                }
                slots[slotIndex].Read(ref buffer, ref state, ref value);
            }
        }
        state.Exit();

        if (validateRequired && hasRequiredSlots)
        {
            ThrowIfRequiredMemberMissing(count, in seenSlots);
        }

        if (IsCallbackReceiver)
        {
            InvokeOnAfterDeserialize(ref value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // keep the boxed argument-state machinery out of the populate hot path
    void DeserializeWithArguments(ref TReadBuffer buffer, ref DeserializeState state, ref T value, int count)
    {
        var memberSlots = slots;
        var arguments = new object?[argumentCount]; // absent parameters: reflection turns null into default(TParam)
        var pendingValues = new object?[memberSlots.Length];
        var pendingSet = new bool[memberSlots.Length];

        state.Enter();
        Span<bool> seenStorage = WantsStackallocSeenSlots(arrayFormat, memberSlots.Length) ? stackalloc bool[StackallocSeenSlotsLimit] : default;
        var seenSlots = new SeenSlots(arrayFormat ? 0 : memberSlots.Length, seenStorage);
        for (int i = 0; i < count; i++)
        {
            var slotIndex = NextSlot(ref buffer, i);
            if (slotIndex < 0)
            {
                buffer.Skip();
                continue;
            }
            if (!arrayFormat)
            {
                seenSlots.MarkOrThrow(slotIndex);
            }
            var slot = memberSlots[slotIndex];
            if (slot.ConstructorParameterIndex >= 0)
            {
                arguments[slot.ConstructorParameterIndex] = slot.ReadBoxed(ref buffer, ref state);
            }
            else if (slot.CanSet)
            {
                pendingValues[slotIndex] = slot.ReadBoxed(ref buffer, ref state);
                pendingSet[slotIndex] = true;
            }
            else
            {
                buffer.Skip(); // readable-only member outside the constructor
            }
        }

        if (validateRequired && hasRequiredSlots)
        {
            // before Invoke: a missing required argument should surface as the contract violation,
            // not as whatever the constructor does with its default
            ThrowIfRequiredMemberMissing(count, in seenSlots);
        }

        value = (T)argumentConstructor!.Invoke(arguments);
        for (int i = 0; i < pendingSet.Length; i++)
        {
            if (pendingSet[i])
            {
                memberSlots[i].SetBoxed(ref value, pendingValues[i]);
            }
        }
        state.Exit();

        if (IsCallbackReceiver)
        {
            InvokeOnAfterDeserialize(ref value);
        }
    }

    // Box round-trip so explicit interface implementations stay reachable and struct mutations land back in the value
    // being (de)serialized: the interface call mutates the box, which is then unboxed back (for a class the box is the
    // same reference, so this degrades to a plain cast-and-call).
    static void InvokeOnBeforeSerialize(ref T value)
    {
        object boxed = value!;
        ((IMessagePackSerializationCallbackReceiver)boxed).OnBeforeSerialize();
        value = (T)boxed;
    }

    static void InvokeOnAfterDeserialize(ref T value)
    {
        object boxed = value!;
        ((IMessagePackSerializationCallbackReceiver)boxed).OnAfterDeserialize();
        value = (T)boxed;
    }

    // array mode indexes directly into the key table; map mode consumes and matches the key token.
    // arrayFormat is fixed per instantiation, so the branch predicts perfectly.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int NextSlot(ref TReadBuffer buffer, int index)
    {
        if (arrayFormat)
        {
            var slotByKey = arraySlotByKey;
            return (uint)index < (uint)slotByKey.Length ? slotByKey[index] : -1;
        }
        return ReadKeyAndFindSlot(ref buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int ReadKeyAndFindSlot(ref TReadBuffer buffer)
    {
        var keyLength = buffer.ReadStringHeader();
        if (!buffer.TryGetSpan(keyLength, out var window))
        {
            ThrowTruncatedKey(keyLength);
        }
        var key = window.Slice(0, keyLength);

        // POCO member counts are single-digit in practice and the length mismatch exits early,
        // so linear memcmp stays. Decided against automata/bucket machinery (2026-08-17); if wide types ever bite,
        // try slot i for entry i first, payloads we wrote ourselves then match in O(n).
        var slotIndex = -1;
        var keys = encodedKeys;
        for (int i = 0; i < keys.Length; i++)
        {
            if (key.SequenceEqual(keys[i]))
            {
                slotIndex = i;
                break;
            }
        }
        buffer.Advance(keyLength);
        return slotIndex;
    }

    // array mode: presence is positional, so anything at or past `count` is missing;
    // map modes: replay the seen ledger against the required slots
    [MethodImpl(MethodImplOptions.NoInlining)]
    void ThrowIfRequiredMemberMissing(int count, in SeenSlots seenSlots)
    {
        if (arrayFormat)
        {
            if (count >= requiredArrayCount)
            {
                return;
            }
            for (int key = count; key < arraySlotByKey.Length; key++)
            {
                var slotIndex = arraySlotByKey[key];
                if (slotIndex >= 0 && slots[slotIndex].IsRequired)
                {
                    MessagePackSerializationException.ThrowMissingRequiredMember(typeof(T).FullName, slots[slotIndex].MemberName);
                }
            }
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsRequired && !seenSlots.Seen(i))
            {
                MessagePackSerializationException.ThrowMissingRequiredMember(typeof(T).FullName, slots[i].MemberName);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ThrowTruncatedKey(int keyLength)
    {
        throw new MessagePackSerializationException($"The map key for '{typeof(T).FullName}' claims {keyLength} bytes but the buffer ends before them.");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2087", Justification = "gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation; a trimmed-away constructor surfaces as the serialization exception below")]
    T CreateInstance()
    {
        if (typeof(T).IsValueType)
        {
            return default!;
        }
        if (mode == ConstructionMode.Unconstructable)
        {
            throw new MessagePackSerializationException($"Type '{typeof(T).FullName}' has no usable constructor: neither a parameterless one nor a public one whose parameters all match serialized members by name. Deserialize into an existing instance, or use source-generated formatters.");
        }
        return (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;
    }

    static MemberSlot CreatePropertySlot<TMember>(PropertyInfo property, bool allowPrivate)
    {
        return typeof(T).IsValueType ? new StructPropertySlot<TMember>(property, allowPrivate) : new ClassPropertySlot<TMember>(property, allowPrivate);
    }

    static FieldSlot<TMember> CreateFieldSlot<TMember>(FieldInfo field)
    {
        return new FieldSlot<TMember>(field);
    }

    // Duplicate known-key detection for the map wire forms (the array form is positional,
    // so a slot cannot repeat): ulong bitmask for the common narrow types, a Span<bool> beyond 64,
    // caller-provided stackalloc storage when it fits (see SeenSlotsStorage), heap array beyond.
    // Duplicate map keys are data errors, the same policy as the dictionary formatters.
    const int StackallocSeenSlotsLimit = 256;

    // the stackalloc must live in the caller's frame, so this only decides whether the use site should reserve one;
    // the constructor falls back to a heap array when the provided storage is too small (or absent)
    static bool WantsStackallocSeenSlots(bool arrayFormat, int slotCount) =>
        !arrayFormat && slotCount > 64 && slotCount <= StackallocSeenSlotsLimit;

    ref struct SeenSlots
    {
        ulong bits;
        readonly Span<bool> wide;

        public SeenSlots(int slotCount, Span<bool> wideStorage)
        {
            if (slotCount > 64)
            {
                wide = wideStorage.Length >= slotCount ? wideStorage.Slice(0, slotCount) : new bool[slotCount];
                wide.Clear(); // stackalloc'd storage is not zeroed under [SkipLocalsInit]
            }
        }

        public void MarkOrThrow(int slotIndex)
        {
            if (wide.IsEmpty)
            {
                var bit = 1UL << slotIndex;
                if ((bits & bit) != 0)
                {
                    MessagePackSerializationException.ThrowDuplicateMapKey();
                }
                bits |= bit;
            }
            else
            {
                if (wide[slotIndex])
                {
                    MessagePackSerializationException.ThrowDuplicateMapKey();
                }
                wide[slotIndex] = true;
            }
        }

        public readonly bool Seen(int slotIndex)
        {
            return wide.IsEmpty ? (bits & (1UL << slotIndex)) != 0 : wide[slotIndex];
        }
    }

    // ref-this accessor delegates so struct T is never copied per member
    delegate TMember MemberGetter<TMember>(ref T obj);
    delegate void MemberSetter<TMember>(ref T obj, TMember value);

    abstract class MemberSlot
    {
        public int ConstructorParameterIndex = -1;
        public string MemberName = "";
        public bool IsRequired;    // required modifier, or a no-default constructor parameter
        public bool ValidateNull;  // non-nullable annotated and the resolver enforces the annotation
        public object? CustomFormatter; // [MessagePackFormatter] instance, overrides resolver resolution

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected void ThrowNullValue()
        {
            MessagePackSerializationException.ThrowNullValueForNonNullableMember(typeof(T).FullName, MemberName);
        }

        public abstract Type MemberType { get; }
        public abstract bool CanSet { get; }
        public abstract void Initialize(MessagePackFormatterResolver resolver);
        public abstract void Write(ref TWriteBuffer buffer, ref SerializeState state, ref T obj);
        public abstract void Read(ref TReadBuffer buffer, ref DeserializeState state, ref T obj);
        public abstract object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state);
        public abstract void SetBoxed(ref T obj, object? value);

        // Called once after Initialize: a slot whose formatter resolved to the built-in Int32/String formatter returns
        // a direct-dispatch replacement (see the Direct* slots below); everything else stays as is.
        public virtual MemberSlot Specialize() => this;
    }

    sealed class ClassPropertySlot<TMember> : MemberSlot
    {
        readonly Func<T, TMember> getter;
        readonly Action<T, TMember>? setter; // null: read-only member, payload value is skipped
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, TMember> formatter = null!;

        public ClassPropertySlot(PropertyInfo property, bool allowPrivate)
        {
            getter = property.GetMethod!.CreateDelegate<Func<T, TMember>>();
            setter = property.SetMethod is { } setMethod && (setMethod.IsPublic || allowPrivate)
                ? setMethod.CreateDelegate<Action<T, TMember>>()
                : null;
        }

        public override Type MemberType => typeof(TMember);

        public override bool CanSet => setter != null;

        public override void Initialize(MessagePackFormatterResolver resolver)
        {
            formatter = ResolveSlotFormatter<TMember>(resolver, this);
        }

        public override MemberSlot Specialize()
        {
            // the built-in formatters are sealed, so `is` here means exactly the built-in;
            // a custom int/string formatter fails the test and keeps this slot
            return formatter switch
            {
                Int32Formatter<TWriteBuffer, TReadBuffer> => new DirectInt32Slot((Func<T, int>)(object)getter, (Action<T, int>?)(object?)setter),
                StringFormatter<TWriteBuffer, TReadBuffer> => new DirectStringSlot((Func<T, string?>)(object)getter, (Action<T, string?>?)(object?)setter),
                _ => this,
            };
        }

        public override void Write(ref TWriteBuffer buffer, ref SerializeState state, ref T obj)
        {
            formatter.Serialize(ref buffer, ref state, getter(obj));
        }

        public override void Read(ref TReadBuffer buffer, ref DeserializeState state, ref T obj)
        {
            if (setter == null)
            {
                buffer.Skip();
                return;
            }
            TMember member = default!;
            formatter.Deserialize(ref buffer, ref state, ref member);
            if (ValidateNull && member is null)
            {
                ThrowNullValue();
            }
            setter(obj, member);
        }

        public override object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state)
        {
            TMember member = default!;
            formatter.Deserialize(ref buffer, ref state, ref member);
            if (ValidateNull && member is null)
            {
                ThrowNullValue();
            }
            return member;
        }

        public override void SetBoxed(ref T obj, object? value)
        {
            setter!(obj, (TMember)value!);
        }
    }

    sealed class StructPropertySlot<TMember> : MemberSlot
    {
        readonly MemberGetter<TMember> getter;
        readonly MemberSetter<TMember>? setter;
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, TMember> formatter = null!;

        public StructPropertySlot(PropertyInfo property, bool allowPrivate)
        {
            getter = property.GetMethod!.CreateDelegate<MemberGetter<TMember>>();
            setter = property.SetMethod is { } setMethod && (setMethod.IsPublic || allowPrivate)
                ? setMethod.CreateDelegate<MemberSetter<TMember>>()
                : null;
        }

        public override Type MemberType => typeof(TMember);

        public override bool CanSet => setter != null;

        public override void Initialize(MessagePackFormatterResolver resolver)
        {
            formatter = ResolveSlotFormatter<TMember>(resolver, this);
        }

        public override MemberSlot Specialize()
        {
            return formatter switch
            {
                Int32Formatter<TWriteBuffer, TReadBuffer> => new DirectRefInt32Slot((MemberGetter<int>)(object)getter, (MemberSetter<int>?)(object?)setter),
                StringFormatter<TWriteBuffer, TReadBuffer> => new DirectRefStringSlot((MemberGetter<string?>)(object)getter, (MemberSetter<string?>?)(object?)setter),
                _ => this,
            };
        }

        public override void Write(ref TWriteBuffer buffer, ref SerializeState state, ref T obj)
        {
            formatter.Serialize(ref buffer, ref state, getter(ref obj));
        }

        public override void Read(ref TReadBuffer buffer, ref DeserializeState state, ref T obj)
        {
            if (setter == null)
            {
                buffer.Skip();
                return;
            }
            TMember member = default!;
            formatter.Deserialize(ref buffer, ref state, ref member);
            if (ValidateNull && member is null)
            {
                ThrowNullValue();
            }
            setter(ref obj, member);
        }

        public override object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state)
        {
            TMember member = default!;
            formatter.Deserialize(ref buffer, ref state, ref member);
            if (ValidateNull && member is null)
            {
                ThrowNullValue();
            }
            return member;
        }

        public override void SetBoxed(ref T obj, object? value)
        {
            setter!(ref obj, (TMember)value!);
        }
    }

    // Direct primitive slots (Nerdbank.MessagePack PR #1014's technique,
    // adapted): when a member's formatter resolved to the built-in Int32/String formatter,
    // the generic slot swaps itself for one of these, whose Write/Read call the buffer primitive directly.
    // Two wins over dispatching through the formatter interface: one indirection less per member,
    // and independence from guarded devirtualization. The latter matters most for string members:
    // ClassPropertySlot<string> shares __Canon code with every reference TMember in the process,
    // so its formatter call site's profile is polymorphic and PGO devirtualization is per-launch luck (measured as
    // 29<->50ns flips in ReflectionObjectBenchmark). These classes are not generic over TMember,
    // so the hot path has no dispatch left to devirtualize. Semantics are identical by construction: the built-in
    // formatters are one-line wrappers over the same buffer extensions.

    sealed class DirectInt32Slot(Func<T, int> getter, Action<T, int>? setter) : MemberSlot
    {
        public override Type MemberType => typeof(int);

        public override bool CanSet => setter != null;

        public override void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public override void Write(ref TWriteBuffer buffer, ref SerializeState state, ref T obj)
        {
            buffer.WriteInt32(getter(obj));
        }

        public override void Read(ref TReadBuffer buffer, ref DeserializeState state, ref T obj)
        {
            if (setter == null)
            {
                buffer.Skip();
                return;
            }
            setter(obj, buffer.ReadInt32());
        }

        public override object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state)
        {
            return buffer.ReadInt32();
        }

        public override void SetBoxed(ref T obj, object? value)
        {
            setter!(obj, (int)value!);
        }
    }

    sealed class DirectStringSlot(Func<T, string?> getter, Action<T, string?>? setter) : MemberSlot
    {
        public override Type MemberType => typeof(string);

        public override bool CanSet => setter != null;

        public override void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public override void Write(ref TWriteBuffer buffer, ref SerializeState state, ref T obj)
        {
            buffer.WriteString(getter(obj));
        }

        public override void Read(ref TReadBuffer buffer, ref DeserializeState state, ref T obj)
        {
            if (setter == null)
            {
                buffer.Skip();
                return;
            }
            var member = buffer.ReadString();
            if (ValidateNull && member is null)
            {
                ThrowNullValue();
            }
            setter(obj, member);
        }

        public override object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state)
        {
            var member = buffer.ReadString();
            if (ValidateNull && member is null)
            {
                ThrowNullValue();
            }
            return member;
        }

        public override void SetBoxed(ref T obj, object? value)
        {
            setter!(obj, (string?)value);
        }
    }

    sealed class DirectRefInt32Slot(MemberGetter<int> getter, MemberSetter<int>? setter) : MemberSlot
    {
        public override Type MemberType => typeof(int);

        public override bool CanSet => setter != null;

        public override void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public override void Write(ref TWriteBuffer buffer, ref SerializeState state, ref T obj)
        {
            buffer.WriteInt32(getter(ref obj));
        }

        public override void Read(ref TReadBuffer buffer, ref DeserializeState state, ref T obj)
        {
            if (setter == null)
            {
                buffer.Skip();
                return;
            }
            setter(ref obj, buffer.ReadInt32());
        }

        public override object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state)
        {
            return buffer.ReadInt32();
        }

        public override void SetBoxed(ref T obj, object? value)
        {
            setter!(ref obj, (int)value!);
        }
    }

    sealed class DirectRefStringSlot(MemberGetter<string?> getter, MemberSetter<string?>? setter) : MemberSlot
    {
        public override Type MemberType => typeof(string);

        public override bool CanSet => setter != null;

        public override void Initialize(MessagePackFormatterResolver resolver)
        {
        }

        public override void Write(ref TWriteBuffer buffer, ref SerializeState state, ref T obj)
        {
            buffer.WriteString(getter(ref obj));
        }

        public override void Read(ref TReadBuffer buffer, ref DeserializeState state, ref T obj)
        {
            if (setter == null)
            {
                buffer.Skip();
                return;
            }
            var member = buffer.ReadString();
            if (ValidateNull && member is null)
            {
                ThrowNullValue();
            }
            setter(ref obj, member);
        }

        public override object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state)
        {
            var member = buffer.ReadString();
            if (ValidateNull && member is null)
            {
                ThrowNullValue();
            }
            return member;
        }

        public override void SetBoxed(ref T obj, object? value)
        {
            setter!(ref obj, (string?)value);
        }
    }

#if NETSTANDARD2_0
    // netstandard2.0 has no DynamicMethod surface, so fields go through boxed FieldInfo access there (the ns2.0 build
    // is the compatibility tier; speed lives upstack).
    sealed class FieldSlot<TMember> : MemberSlot
    {
        readonly FieldInfo field;
        readonly bool writable;
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, TMember> formatter = null!;

        public FieldSlot(FieldInfo field)
        {
            this.field = field;
            writable = true; // readonly included: v3 wrote initonly fields, and FieldInfo.SetValue can
        }

        public override Type MemberType => typeof(TMember);

        public override bool CanSet => writable;

        public override void Initialize(MessagePackFormatterResolver resolver)
        {
            formatter = ResolveSlotFormatter<TMember>(resolver, this);
        }

        public override void Write(ref TWriteBuffer buffer, ref SerializeState state, ref T obj)
        {
            formatter.Serialize(ref buffer, ref state, (TMember)field.GetValue(obj)!);
        }

        public override void Read(ref TReadBuffer buffer, ref DeserializeState state, ref T obj)
        {
            if (!writable)
            {
                buffer.Skip();
                return;
            }
            TMember member = default!;
            formatter.Deserialize(ref buffer, ref state, ref member);
            if (ValidateNull && member is null)
            {
                ThrowNullValue();
            }
            SetBoxed(ref obj, member);
        }

        public override object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state)
        {
            TMember member = default!;
            formatter.Deserialize(ref buffer, ref state, ref member);
            if (ValidateNull && member is null)
            {
                ThrowNullValue();
            }
            return member;
        }

        public override void SetBoxed(ref T obj, object? value)
        {
            object boxed = obj!;
            field.SetValue(boxed, value);
            obj = (T)boxed; // struct T: writes land in the box, unbox them back
        }
    }
#else
    // Fields have no accessor MethodInfo to bind a delegate to, so the accessors are the one place this formatter
    // emits: a single-ldfld/stfld DynamicMethod per field, distilled into the same ref-this delegates the property
    // slots use. This is deliberately not a violation of the no-Emit policy,
    // that policy bans emitting whole formatters, not a one-instruction accessor (user decision 2026-08-17).
    // skipVisibility covers private fields (AllowPrivate) including base-class ones.
    sealed class FieldSlot<TMember> : MemberSlot
    {
        readonly MemberGetter<TMember> getter;
        readonly MemberSetter<TMember>? setter;
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, TMember> formatter = null!;

        public FieldSlot(FieldInfo field)
        {
            getter = CreateGetter(field);
            setter = CreateSetter(field); // readonly included: v3 wrote initonly fields, and skipVisibility stfld can
        }

        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "instances only exist behind ReflectionFormatterFactory's RequiresDynamicCode gate; the caller has already opted into dynamic code")]
        static MemberGetter<TMember> CreateGetter(FieldInfo field)
        {
            var method = new DynamicMethod($"ReflectionGet_{field.Name}", typeof(TMember), [typeof(T).MakeByRefType()], typeof(T).Module, skipVisibility: true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (!typeof(T).IsValueType)
            {
                il.Emit(OpCodes.Ldind_Ref); // ref T of a class is T&: load the reference itself
            }
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (MemberGetter<TMember>)method.CreateDelegate(typeof(MemberGetter<TMember>));
        }

        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "instances only exist behind ReflectionFormatterFactory's RequiresDynamicCode gate; the caller has already opted into dynamic code")]
        static MemberSetter<TMember> CreateSetter(FieldInfo field)
        {
            var method = new DynamicMethod($"ReflectionSet_{field.Name}", typeof(void), [typeof(T).MakeByRefType(), typeof(TMember)], typeof(T).Module, skipVisibility: true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (!typeof(T).IsValueType)
            {
                il.Emit(OpCodes.Ldind_Ref);
            }
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (MemberSetter<TMember>)method.CreateDelegate(typeof(MemberSetter<TMember>));
        }

        public override Type MemberType => typeof(TMember);

        public override bool CanSet => setter != null;

        public override void Initialize(MessagePackFormatterResolver resolver)
        {
            formatter = ResolveSlotFormatter<TMember>(resolver, this);
        }

        public override MemberSlot Specialize()
        {
            return formatter switch
            {
                Int32Formatter<TWriteBuffer, TReadBuffer> => new DirectRefInt32Slot((MemberGetter<int>)(object)getter, (MemberSetter<int>?)(object?)setter),
                StringFormatter<TWriteBuffer, TReadBuffer> => new DirectRefStringSlot((MemberGetter<string?>)(object)getter, (MemberSetter<string?>?)(object?)setter),
                _ => this,
            };
        }

        public override void Write(ref TWriteBuffer buffer, ref SerializeState state, ref T obj)
        {
            formatter.Serialize(ref buffer, ref state, getter(ref obj));
        }

        public override void Read(ref TReadBuffer buffer, ref DeserializeState state, ref T obj)
        {
            if (setter == null)
            {
                buffer.Skip();
                return;
            }
            TMember member = default!;
            formatter.Deserialize(ref buffer, ref state, ref member);
            if (ValidateNull && member is null)
            {
                ThrowNullValue();
            }
            setter(ref obj, member);
        }

        public override object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state)
        {
            TMember member = default!;
            formatter.Deserialize(ref buffer, ref state, ref member);
            if (ValidateNull && member is null)
            {
                ThrowNullValue();
            }
            return member;
        }

        public override void SetBoxed(ref T obj, object? value)
        {
            setter!(ref obj, (TMember)value!);
        }
    }
#endif
}

/// <summary>Creates <see cref="ReflectionObjectFormatter{TWriteBuffer, TReadBuffer, T}"/> instances for <typeparamref name="T"/>.</summary>
public sealed partial class ReflectionObjectFormatterFactory<T> : MessagePackFormatterFactory
{
    readonly bool allowPrivate;

    /// <summary>Creates a factory over the public members of <typeparamref name="T"/>.</summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(ReflectionFormatterFactory.RequiresUnreferencedCodeMessage)]
    public ReflectionObjectFormatterFactory()
        : this(allowPrivate: false)
    {
    }

    /// <summary>Creates a factory that, with <paramref name="allowPrivate"/>, also includes non-public members, accessors and constructors.</summary>
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(ReflectionFormatterFactory.RequiresUnreferencedCodeMessage)]
    public ReflectionObjectFormatterFactory(bool allowPrivate)
    {
        this.allowPrivate = allowPrivate;
    }

    // One method, two signatures. net9+ overrides the base virtual (constraints inherited),
    // while downlevel has no base member, so the constraints are spelled out.
    /// <summary>Creates the formatter for <paramref name="type"/>, which must be <typeparamref name="T"/>.</summary>
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
            return new ReflectionObjectFormatter<TWriteBuffer, TReadBuffer, T>(allowPrivate);
        }
        return null;
    }
}

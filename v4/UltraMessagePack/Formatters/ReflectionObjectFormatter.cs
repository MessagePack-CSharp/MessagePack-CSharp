// TODO: This formatter is not finished yet, it is a provisional implementation at the concept level.
// It needs to be developed further along with the progress of the Source Generator.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif
using System.Text;

namespace UltraMessagePack.Formatters;

// The attribute CONTRACT is MessagePack-CSharp's, matched by full name exactly like the
// source generator does: the core library takes no dependency on the MessagePack package,
// and the annotations assembly a v3 codebase already references keeps working unchanged.
static class MessagePackAttributeNames
{
    public const string MessagePackObject = "MessagePack.MessagePackObjectAttribute";
    public const string Key = "MessagePack.KeyAttribute";
    public const string IgnoreMember = "MessagePack.IgnoreMemberAttribute";
}

// Reflection-driven object formatter: a plain generic class driven by a member table built
// once in Initialize. Reflection is distilled into typed delegates at construction time;
// the steady-state serialize path is one virtual call plus one delegate call per member,
// boxing-free. Formatters are never emitted — the only IL generation in here is the
// one-instruction field accessors in FieldSlot (fields have no MethodInfo to bind a
// delegate to). It replaces v3's whole DynamicObject/DynamicContractlessObject resolver
// family with one class whose wire form follows the type:
//   [MessagePackObject] with [Key(int)]    - array of MaxKey+1 slots, nil holes
//                                            (byte-compatible with the source-generated
//                                            formatter and v3's DynamicObjectResolver)
//   [MessagePackObject] with [Key(string)]
//   or [MessagePackObject(true)]           - map of declared key to value
//   no attribute                           - map of member name to value (v3 contractless)
// Annotated shapes are validated at Initialize with the source generator's rules (UMP001
// missing key, UMP002 mixed keys, UMP003 duplicates, UMP008 negative), so a type cannot
// silently serialize differently depending on whether the generator covered it. One
// deliberate relaxation: a keyed member without a setter (UMP007, a generator error) is
// serialized and skipped on read here — the constructor-argument path below often fills
// it, which the runtime can verify and the generator cannot yet.
//
// Contractless members: public instance properties (public getter required, setter
// optional) and public instance fields. Construction follows v3's rule: the public
// constructor with the most parameters where EVERY parameter name-matches a member
// (ordinal-ignore-case, exact type) wins; parameterless is the natural last resort. A
// parameterized winner switches deserialization to the argument-state path (values
// buffered boxed, constructor invoked, leftovers applied through setters) — this is what
// makes records, anonymous types and getter-only members round-trip. The zero-boxing
// populate path still serves parameterless types and explicit populate calls.
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

    MemberSlot[] slots = null!;
    byte[][] encodedKeys = null!; // UTF-8 wire keys, parallel to slots (map modes only)
    bool arrayFormat;             // [Key(int)] (or an empty annotated type): array wire form
    int[] arraySlotByKey = [];    // wire index -> slot index, -1 = key hole (array mode only)
    ConstructionMode mode;
    ConstructorInfo? argumentConstructor; // Arguments mode only
    int argumentCount;

    public ReflectionObjectFormatter()
        : this(allowPrivate: false)
    {
    }

    /// <summary>
    /// allowPrivate widens discovery to non-public members, accessors and constructors
    /// (v3's AllowPrivate resolvers). No extra machinery is needed: delegate binding to a
    /// private accessor is just reflection, which is what made v3 grow a separate
    /// DynamicMethod-based resolver family.
    /// </summary>
    public ReflectionObjectFormatter(bool allowPrivate)
    {
        this.allowPrivate = allowPrivate;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        BuildMemberTable();
        foreach (var slot in slots)
        {
            slot.Initialize(resolver);
        }
    }

    readonly record struct MemberCandidate(MemberInfo Member, Type MemberType, bool IsPublic);

    [UnconditionalSuppressMessage("Trimming", "IL2090", Justification = "reflection member discovery is gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation; trimmed members are the documented risk the caller accepted")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "the allowPrivate hierarchy walk enumerates base types of T; same RequiresUnreferencedCode gate and documented trimming risk as the direct typeof(T) discovery")]
    void BuildMemberTable()
    {
        var objectAttribute = FindAttribute(typeof(T).GetCustomAttributes(inherit: false), MessagePackAttributeNames.MessagePackObject);
        allowPrivate |= ReadBoolProperty(objectAttribute, "AllowPrivate");

        // Discovery collects candidates in wire order (properties, then fields), shared by
        // both modes. Public mode reads the flattened public surface in one pass.
        // allowPrivate must walk the hierarchy with DeclaredOnly instead: reflection never
        // returns a base class's private members on the derived type. Derived-first order
        // plus the seenNames dedup keeps the most derived of any override/`new`-shadowed
        // member.
        var candidates = new List<MemberCandidate>();
        var seenNames = new HashSet<string>();
        var memberFlags = BindingFlags.Public | BindingFlags.Instance
            | (allowPrivate ? BindingFlags.NonPublic | BindingFlags.DeclaredOnly : 0);
        for (var current = typeof(T); current is not null && current != typeof(object); current = allowPrivate ? current.BaseType : null)
        {
            foreach (var property in current.GetProperties(memberFlags))
            {
                var getMethod = property.GetMethod;
                if (property.GetIndexParameters().Length != 0 ||
                    getMethod is null ||
                    (!allowPrivate && !getMethod.IsPublic) ||
                    !seenNames.Add(property.Name))
                {
                    continue;
                }
                candidates.Add(new MemberCandidate(property, property.PropertyType, getMethod.IsPublic));
            }

            foreach (var field in current.GetFields(memberFlags))
            {
                // compiler-generated fields are auto-property backing fields (and
                // friends): the property slot already owns that data
                if (field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) ||
                    !seenNames.Add(field.Name))
                {
                    continue;
                }
                candidates.Add(new MemberCandidate(field, field.FieldType, field.IsPublic));
            }
        }

        if (objectAttribute is null)
        {
            BuildContractlessTable(candidates);
        }
        else
        {
            BuildAttributedTable(candidates, objectAttribute);
        }
    }

    void BuildContractlessTable(List<MemberCandidate> candidates)
    {
        var memberSlots = new MemberSlot[candidates.Count];
        var keys = new byte[candidates.Count][];
        var names = new string[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            var name = candidates[i].Member.Name;
            memberSlots[i] = CreateSlot(candidates[i]);
            keys[i] = Encoding.UTF8.GetBytes(name);
            names[i] = name;
        }

        slots = memberSlots;
        encodedKeys = keys;
        SelectConstructor(names);
    }

    void BuildAttributedTable(List<MemberCandidate> candidates, object objectAttribute)
    {
        var keyAsPropertyName = ReadBoolProperty(objectAttribute, "KeyAsPropertyName");

        var memberSlots = new List<MemberSlot>();
        var names = new List<string>();
        var memberIntKeys = new List<int>();
        var memberStringKeys = new List<string?>();
        var seenIntKeys = new HashSet<int>();
        var seenStringKeys = new HashSet<string>();
        var hasIntKey = false;
        var hasStringKey = keyAsPropertyName;

        foreach (var candidate in candidates)
        {
            var name = candidate.Member.Name;
            var attributes = candidate.Member.GetCustomAttributes(inherit: false);
            if (FindAttribute(attributes, MessagePackAttributeNames.IgnoreMember) is not null)
            {
                continue;
            }

            int? intKey = null;
            string? stringKey = null;
            if (FindAttribute(attributes, MessagePackAttributeNames.Key) is { } keyAttribute)
            {
                (intKey, stringKey) = ReadKeyArguments(keyAttribute);
            }

            if (intKey is null && stringKey is null)
            {
                if (keyAsPropertyName)
                {
                    stringKey = name;
                }
                else if (candidate.IsPublic)
                {
                    // the source generator's UMP001, enforced at runtime so the reflection
                    // and generated interpretations of a type can never diverge
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
                if (!seenIntKeys.Add(key))
                {
                    throw new MessagePackSerializationException($"'{typeof(T).FullName}' declares key {key} more than once.");
                }
                hasIntKey = true;
            }
            else
            {
                if (!seenStringKeys.Add(stringKey!))
                {
                    throw new MessagePackSerializationException($"'{typeof(T).FullName}' declares key \"{stringKey}\" more than once.");
                }
                hasStringKey = true;
            }

            memberSlots.Add(CreateSlot(candidate));
            names.Add(name);
            memberIntKeys.Add(intKey ?? -1);
            memberStringKeys.Add(stringKey);
        }

        if (hasIntKey && hasStringKey)
        {
            throw new MessagePackSerializationException($"'{typeof(T).FullName}' mixes int keys and string keys; a type serializes as either an array (int keys) or a map (string keys), not both.");
        }

        slots = [.. memberSlots];
        if (!hasStringKey)
        {
            // int keys (or a memberless annotated type, which the generator also treats as
            // an empty array) serialize as the v3 array format with nil key holes
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

    static object? FindAttribute(object[] attributes, string fullName)
    {
        foreach (var attribute in attributes)
        {
            // walk the attribute's base chain so a derived annotation still matches,
            // the way typed GetCustomAttribute<TBase> would
            for (var type = attribute.GetType(); type is not null; type = type.BaseType)
            {
                if (type.FullName == fullName)
                {
                    return attribute;
                }
            }
        }
        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "attribute property reads are gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation; attributes applied in user code keep their properties rooted")]
    static bool ReadBoolProperty(object? attribute, string propertyName)
    {
        return attribute is not null && attribute.GetType().GetProperty(propertyName)?.GetValue(attribute) is true;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "attribute property reads are gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation; attributes applied in user code keep their properties rooted")]
    static (int? IntKey, string? StringKey) ReadKeyArguments(object keyAttribute)
    {
        var type = keyAttribute.GetType();
        return (type.GetProperty("IntKey")?.GetValue(keyAttribute) as int?,
                type.GetProperty("StringKey")?.GetValue(keyAttribute) as string);
    }

    // v3's rule: iterate public constructors by parameter count descending and take the
    // first whose parameters ALL match a member by name (ordinal-ignore-case, so
    // camelCase parameters bind PascalCase members) with the exact member type.
    [UnconditionalSuppressMessage("Trimming", "IL2090", Justification = "gated behind ReflectionFormatterFactory's RequiresUnreferencedCode annotation, same as the member discovery above")]
    void SelectConstructor(string[] memberNames)
    {
        var constructorFlags = BindingFlags.Public | BindingFlags.Instance
            | (allowPrivate ? BindingFlags.NonPublic : 0);
        foreach (var candidate in typeof(T).GetConstructors(constructorFlags)
                     .OrderByDescending(c => c.GetParameters().Length))
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
                        slots[s].MemberType == parameters[i].ParameterType)
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
            }
            return;
        }

        // no constructor matched: structs always default-construct, classes fall
        // back to a (possibly non-public) parameterless constructor
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

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
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

        // Argument-state path for fresh instances of constructor-matched types. A class
        // instance supplied by the caller keeps populate semantics (structs have no
        // identity to preserve, so they always reconstruct).
        if (mode == ConstructionMode.Arguments && (value is null || typeof(T).IsValueType))
        {
            DeserializeWithArguments(ref buffer, ref state, ref value, count);
            return;
        }

        // Populate contract: reuse the incoming instance; members absent from the
        // payload keep their current values.
        if (value is null)
        {
            value = CreateInstance();
        }

        state.Enter();
        for (int i = 0; i < count; i++)
        {
            var slotIndex = NextSlot(ref buffer, i);
            if (slotIndex < 0)
            {
                buffer.Skip(); // unknown member / key hole: ignore its value (version tolerance)
            }
            else
            {
                slots[slotIndex].Read(ref buffer, ref state, ref value);
            }
        }
        state.Exit();
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // keep the boxed argument-state machinery out of the populate hot path
    void DeserializeWithArguments(ref TReadBuffer buffer, ref DeserializeState state, ref T value, int count)
    {
        var memberSlots = slots;
        var arguments = new object?[argumentCount]; // absent parameters: reflection turns null into default(TParam)
        var pendingValues = new object?[memberSlots.Length];
        var pendingSet = new bool[memberSlots.Length];

        state.Enter();
        for (int i = 0; i < count; i++)
        {
            var slotIndex = NextSlot(ref buffer, i);
            if (slotIndex < 0)
            {
                buffer.Skip();
                continue;
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

        value = (T)argumentConstructor!.Invoke(arguments);
        for (int i = 0; i < pendingSet.Length; i++)
        {
            if (pendingSet[i])
            {
                memberSlots[i].SetBoxed(ref value, pendingValues[i]);
            }
        }
        state.Exit();
    }

    // array mode indexes directly into the key table; map mode consumes and matches the
    // key token. arrayFormat is fixed per instantiation, so the branch predicts perfectly.
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

        // POCO member counts are single-digit in practice and the length mismatch exits
        // early, so linear memcmp stays. Decided against automata/bucket machinery
        // (2026-08-17); if wide types ever bite, try slot i for entry i first — payloads
        // we wrote ourselves then match in O(n).
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

    static MemberSlot CreateFieldSlot<TMember>(FieldInfo field)
    {
        return new FieldSlot<TMember>(field);
    }

    // ref-this accessor delegates so struct T is never copied per member
    delegate TMember MemberGetter<TMember>(ref T obj);
    delegate void MemberSetter<TMember>(ref T obj, TMember value);

    abstract class MemberSlot
    {
        public int ConstructorParameterIndex = -1;

        public abstract Type MemberType { get; }
        public abstract bool CanSet { get; }
        public abstract void Initialize(MessagePackFormatterResolver resolver);
        public abstract void Write(ref TWriteBuffer buffer, ref SerializeState state, ref T obj);
        public abstract void Read(ref TReadBuffer buffer, ref DeserializeState state, ref T obj);
        public abstract object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state);
        public abstract void SetBoxed(ref T obj, object? value);
    }

    sealed class ClassPropertySlot<TMember> : MemberSlot
    {
        readonly Func<T, TMember> getter;
        readonly Action<T, TMember>? setter; // null: read-only member, payload value is skipped
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, TMember> formatter = null!;

        public ClassPropertySlot(PropertyInfo property, bool allowPrivate)
        {
            getter = (Func<T, TMember>)property.GetMethod!.CreateDelegate(typeof(Func<T, TMember>));
            setter = property.SetMethod is { } setMethod && (setMethod.IsPublic || allowPrivate)
                ? (Action<T, TMember>)setMethod.CreateDelegate(typeof(Action<T, TMember>))
                : null;
        }

        public override Type MemberType => typeof(TMember);

        public override bool CanSet => setter != null;

        public override void Initialize(MessagePackFormatterResolver resolver)
        {
            formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TMember>();
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
            setter(obj, member);
        }

        public override object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state)
        {
            TMember member = default!;
            formatter.Deserialize(ref buffer, ref state, ref member);
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
            getter = (MemberGetter<TMember>)property.GetMethod!.CreateDelegate(typeof(MemberGetter<TMember>));
            setter = property.SetMethod is { } setMethod && (setMethod.IsPublic || allowPrivate)
                ? (MemberSetter<TMember>)setMethod.CreateDelegate(typeof(MemberSetter<TMember>))
                : null;
        }

        public override Type MemberType => typeof(TMember);

        public override bool CanSet => setter != null;

        public override void Initialize(MessagePackFormatterResolver resolver)
        {
            formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TMember>();
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
            setter(ref obj, member);
        }

        public override object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state)
        {
            TMember member = default!;
            formatter.Deserialize(ref buffer, ref state, ref member);
            return member;
        }

        public override void SetBoxed(ref T obj, object? value)
        {
            setter!(ref obj, (TMember)value!);
        }
    }

#if NETSTANDARD2_0
    // netstandard2.0 has no DynamicMethod surface, so fields go through boxed FieldInfo
    // access there (the ns2.0 build is the compatibility tier; speed lives upstack).
    sealed class FieldSlot<TMember> : MemberSlot
    {
        readonly FieldInfo field;
        readonly bool writable;
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, TMember> formatter = null!;

        public FieldSlot(FieldInfo field)
        {
            this.field = field;
            writable = !field.IsInitOnly;
        }

        public override Type MemberType => typeof(TMember);

        public override bool CanSet => writable;

        public override void Initialize(MessagePackFormatterResolver resolver)
        {
            formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TMember>();
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
            SetBoxed(ref obj, member);
        }

        public override object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state)
        {
            TMember member = default!;
            formatter.Deserialize(ref buffer, ref state, ref member);
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
    // Fields have no accessor MethodInfo to bind a delegate to, so the accessors are the
    // one place this formatter emits: a single-ldfld/stfld DynamicMethod per field,
    // distilled into the same ref-this delegates the property slots use. This is
    // deliberately NOT a violation of the no-Emit policy — that policy bans emitting whole
    // formatters, not a one-instruction accessor (user decision 2026-08-17).
    // skipVisibility covers private fields (AllowPrivate) including base-class ones.
    sealed class FieldSlot<TMember> : MemberSlot
    {
        readonly MemberGetter<TMember> getter;
        readonly MemberSetter<TMember>? setter; // null: initOnly field, payload value is skipped
        IMessagePackFormatter<TWriteBuffer, TReadBuffer, TMember> formatter = null!;

        public FieldSlot(FieldInfo field)
        {
            getter = CreateGetter(field);
            setter = field.IsInitOnly ? null : CreateSetter(field);
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
            formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, TMember>();
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
            setter(ref obj, member);
        }

        public override object? ReadBoxed(ref TReadBuffer buffer, ref DeserializeState state)
        {
            TMember member = default!;
            formatter.Deserialize(ref buffer, ref state, ref member);
            return member;
        }

        public override void SetBoxed(ref T obj, object? value)
        {
            setter!(ref obj, (TMember)value!);
        }
    }
#endif
}

public sealed partial class ReflectionObjectFormatterFactory<T> : MessagePackFormatterFactory
{
    readonly bool allowPrivate;

    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(ReflectionFormatterFactory.RequiresUnreferencedCodeMessage)]
    public ReflectionObjectFormatterFactory()
        : this(allowPrivate: false)
    {
    }

    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(ReflectionFormatterFactory.RequiresUnreferencedCodeMessage)]
    public ReflectionObjectFormatterFactory(bool allowPrivate)
    {
        this.allowPrivate = allowPrivate;
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
            return new ReflectionObjectFormatter<TWriteBuffer, TReadBuffer, T>(allowPrivate);
        }
        return null;
    }
}

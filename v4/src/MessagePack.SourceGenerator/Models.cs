namespace MessagePack.SourceGenerator;

/// <summary>
/// Members that have a dedicated buffer read/write pair are emitted as direct calls (and batched into shared
/// reservations on the write side); everything else goes through an Initialize-resolved formatter field,
/// the shape the dispatch measurements picked (PocoPerValueVsBatch/NestedFormatterDispatch benchmarks: batch fixed-size
/// writes, loop+switch reads, interface fields over per-call resolution).
///
/// Only types whose wire form is fixed belong here. A type the factory chain can re-map
/// must stay on the formatter path, or the generated code silently pins the default
/// encoding and the configured chain is ignored for every member of a generated type.
/// Two types were pulled out for exactly that reason:
///   DateTime - DotNetOptimized rewrites timestamp ext to Kind-preserving ToBinary
///   String   - the obvious place for an interning / custom-encoding formatter
/// The move costs about one call's worth per member and no more, measured at ~1 ns on an
/// adversarial 5-DateTime poco and not measurable on Answer (DateTimeFormatterDispatchBenchmark,
/// StringFormatterDispatchBenchmark, AnswerBenchmark round 5). On the write side the delta
/// is the lost fused reservation rather than the callvirt: PGO devirtualizes the callsite,
/// so hand-removing the dispatch buys nothing. An Initialize-time "is this the stock
/// formatter" flag was measured and rejected; see DateTimeFormatterDispatchBenchmark.
/// </summary>
public enum DirectKind
{
    None,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Int16,
    UInt16,
    Byte,
    SByte,
    Boolean,
    Char,
    Single,
    Double,
}

/// <summary>How a member can receive a value during deserialization.</summary>
public enum SetterKind
{
    /// <summary>Ordinary setter (or mutable field): assignable on the populate path.</summary>
    Set,
    /// <summary>Init-only setter: assignable only through an object initializer (construction path).</summary>
    Init,
    /// <summary>No accessible setter (or readonly field): fed through a constructor parameter or skipped.</summary>
    None,
}

/// <summary>
/// How the generated code invokes an IMessagePackSerializationCallbackReceiver method.
/// Direct = a public implicit implementation, called straight on the value (mutations on struct values land in the
/// local being (de)serialized, matching the constrained-call semantics v3's Emit produced).
/// Cast = explicit implementation: reference types cast in place; value types go through SourceGeneratorHelper's
/// ref-taking constrained-call bridge, no box, mutations land in place.
/// </summary>
public enum CallbackStyle
{
    None,
    Direct,
    Cast,
}

/// <summary>
/// A member-level [MessagePackFormatter(...)] rendered to code at parse time.
/// Exactly one of the two creation expressions is set: FactoryNew is a `new SomeFormatterFactory(args)` expression (the
/// emitter appends the CreateFormatter call using TypeOfExpr), FormatterNew is a complete `new
/// SomeFormatter&lt;TWriteBuffer, TReadBuffer&gt;(args)` expression.
/// Argument binding (including the expression-string form, e.g. "StringComparer.OrdinalIgnoreCase")
/// happened against the chosen constructor in the parser, so these strings always compile.
/// </summary>
public sealed record CustomFormatterModel(
    string? FactoryNew,
    string? TypeOfExpr,
    string? FormatterNew);

public sealed record MemberModel(
    string Name,
    string TypeName,
    int IntKey,
    string StringKey,
    DirectKind Direct,
    // Direct describes Nullable<primitive>: the write is a nil-or-value expression inside the same fused reservation
    // (nil is 1 byte, under every primitive's max), the read is TryReadNil-or-ReadX, no formatter field,
    // same wire as NullableFormatter
    bool DirectNullable,
    SetterKind Setter,
    int ConstructorParameterIndex,
    // the selected constructor's parameter type when it differs from the member's (an object parameter bound to a
    // string member): the argument is cast to it so the emitted call binds the selected constructor,
    // not whatever overload C# would pick for the more specific member type
    string? ConstructorParameterTypeName,
    // required for deserialization: the C# required modifier, or a matched constructor parameter without a default
    // value. Enforced when the resolver's ValidateRequiredMembers is on (the default).
    bool IsRequired,
    // the language-level required modifier alone; drives the construction-shape emission (object-initializer coverage
    // for CS9035), not the wire validation
    bool HasRequiredModifier,
    // declared as a non-nullable reference type (top level only; type parameters and generic arguments are out of
    // scope). Enforced when ValidateNullableAnnotations is on.
    bool IsNonNullableReference,
    // member-level [MessagePackFormatter]: replaces the resolver.GetFormatter call in Initialize (and forces Direct =
    // None, so the member always rides the formatter field)
    CustomFormatterModel? CustomFormatter = null,
    // a `new`-shadowed base declaration: non-null carries the fully-qualified declaring type.
    // Shadowing is separate storage with its own key, and every declaration serializes (matching v3 and the reflection
    // tier), but C# binds `value.Name` to the most-derived declaration,
    // so this member is reached by casting the instance to its declarer,
    // and its emitted identifiers carry UniqueId instead of the (now ambiguous) name
    string? BaseCastType = null,
    string? UniqueId = null)
{
    public string Id => this.UniqueId ?? this.Name;
}

// a closed instantiation of a generic [MessagePackObject] type reachable from a model's serialized members: the factory
// emits a static closed construction for it, so Native AOT (which cannot MakeGenericType over the ref struct buffer
// arguments) resolves it, and CoreCLR skips the Activator path
public sealed record HarvestedGenericModel(
    string OpenTypeOf,
    string ClosedTypeName,
    EquatableArray<string> TypeArguments);

// a built-in generic instantiation (an enum, Nullable<T>, or a BCL collection/wrapper closed form like List<string> or
// string[]) reachable from the member graph: served at runtime by the RequiresDynamicCode GenericFormatterFactory tier,
// which the Native AOT chain omits entirely, so the factory emits a static construction and a registry Registration for
// each (v3's mpc generated enum formatters for the same reason)
public sealed record HarvestedBuiltInModel(
    string ClosedTypeName,
    string FormatterConstruction);

public sealed record ObjectModel(
    string FullTypeName,
    string FormatterName,
    string FormatterReference,
    bool IsValueType,
    // [MessagePackObject(AllowCircularReferences = true)]: the wire gains the [id,
    // body] envelope / ext-97 back-reference pair and deserialization registers the instance before populating members.
    // The parser guarantees this never coexists with IsValueType or UseConstruction.
    bool AllowCircularReferences,
    bool IsStringKey,
    int MaxIntKey,
    int ConstructorParameterCount,
    bool UseConstruction,
    // the selected constructor carries [SetsRequiredMembers]: required members need no object-initializer assignment to
    // satisfy the compiler
    bool ConstructorSetsRequiredMembers,
    bool EmitNested,
    EquatableArray<string> NestedDeclarations,
    string TypeParameterList,
    EquatableArray<string> WhereClauses,
    string OpenTypeOf,
    string OpenFormatterTypeOf,
    CallbackStyle OnBeforeSerialize,
    CallbackStyle OnAfterDeserialize,
    EquatableArray<MemberModel> Members,
    EquatableArray<HarvestedGenericModel> HarvestedGenerics,
    EquatableArray<HarvestedBuiltInModel> HarvestedBuiltIns,
    // the settable member of type MessagePackUnknownMembers, when declared: the read loops capture unknown
    // keys/trailing elements into it and Serialize replays them, instead of the skip-only version tolerance.
    // Never part of Members, carries no key.
    string? UnknownMembersName = null);

public sealed record ParseResult(
    ObjectModel? Model,
    EquatableArray<DiagnosticInfo> Diagnostics);

/// <summary>
/// The single pipeline result: discovery is driven by [MessagePackObject] alone,
/// and a type routes to exactly one of the two shapes, [UnionTag] present makes it a union base,
/// otherwise it is an object model.
/// </summary>
public sealed record TypeParseResult(
    ObjectModel? Object,
    UnionModel? Union,
    EquatableArray<DiagnosticInfo> Diagnostics);

/// <summary>
/// A type-level [MessagePackFormatter]: the generated factory serves this type through the attribute-directed
/// construction, ahead of any generated object formatter (v3's AttributeFormatterResolver priority,
/// decided at compile time), and registers it into SourceGeneratedFormatterFactory via the module initializer.
/// Only closed, accessible, non-opted-out types get here; the rest are MsgPack014 (no runtime tier serves them).
/// </summary>
public sealed record AttributeFormatterTypeModel(
    string FullTypeName,
    CustomFormatterModel Custom);

public sealed record AttributeFormatterParseResult(
    AttributeFormatterTypeModel? Model,
    EquatableArray<DiagnosticInfo> Diagnostics);

/// <summary>
/// A [MessagePackSerializable&lt;T&gt;]-annotated partial factory class (the JsonSerializerContext pattern): each
/// declared root type runs through the same harvest as serialized members,
/// and the generated partial supplies the MessagePackFormatterFactory half,
/// static closed constructions for the harvested set plus a module-initializer registration,
/// so root-only shapes (Person[], List&lt;Person&gt;) resolve on Native AOT.
/// Declarations is the namespace + partial-container chain ending with the factory class itself (the emitter appends
/// the base type to that last entry).
/// </summary>
public sealed record SerializableFactoryModel(
    string FullTypeName,
    string HintName,
    EquatableArray<string> Declarations,
    EquatableArray<HarvestedGenericModel> HarvestedGenerics,
    EquatableArray<HarvestedBuiltInModel> HarvestedBuiltIns);

public sealed record SerializableParseResult(
    SerializableFactoryModel? Model,
    EquatableArray<DiagnosticInfo> Diagnostics);

/// <summary>
/// One auto-registered surrogate association, discovered from an IMessagePackSurrogate&lt;TTarget,
/// TSurrogate&gt; implementation: the generated factory serves the target through a statically closed
/// SurrogateFormatter and registers it, so implementing the interface is the whole declaration (no attribute on the
/// target, which may be a type the user cannot annotate). Location-free: this flows into the factory Collect node.
/// </summary>
public sealed record SurrogateModel(
    string TargetTypeName,
    string SurrogateTypeName);

/// <summary>The location-carrying twin of <see cref="SurrogateModel"/>, collected separately for cross-type duplicate-target detection (MsgPack018).</summary>
public sealed record SurrogateTargetSite(
    string TargetTypeName,
    string SurrogateTypeName,
    LocationInfo? Location);

public sealed record SurrogateParseResult(
    EquatableArray<SurrogateTargetSite> Sites,
    EquatableArray<DiagnosticInfo> Diagnostics);

public sealed record UnionCaseModel(
    int Tag,
    string TypeName,
    bool IsValueType,
    string FieldName);

public sealed record UnionModel(
    string FullTypeName,
    string FormatterName,
    // a [Union]-pattern type (C# union declaration, or hand-written per the spec's union pattern): the formatter reads
    // the held case from Value/TryGetValue and rebuilds through the creation members,
    // instead of dispatching on an inheritance hierarchy
    bool IsPatternUnion,
    // struct root: the formatter closes over the bare value type (T? would be Nullable<T>)
    // and nil maps to default(T) instead of null
    bool IsStructRoot,
    // fully-qualified nested IUnionMembers interface when the union delegates its members to a provider (Value read
    // through an interface cast, creation through static Create); null = members live on the union type itself
    // (constructors + Value)
    string? ProviderInterface,
    // every tagged case has a TryGetValue(out C) on the union-defining type: serialize dispatches through them instead
    // of the object-typed Value (the spec's non-boxing union access pattern)
    bool UseNonBoxing,
    // generic pattern unions (union Result<T>(T, Error)): the formatter closes over the root's parameters like a
    // generic object formatter; empty strings for non-generic
    string TypeParameterList,
    EquatableArray<string> WhereClauses,
    string OpenTypeOf,
    string OpenFormatterTypeOf,
    EquatableArray<UnionCaseModel> Cases,
    // enum / Nullable case types need the same AOT harvesting as object members
    EquatableArray<HarvestedBuiltInModel> HarvestedBuiltIns);

public sealed record UnionParseResult(
    UnionModel? Model,
    EquatableArray<DiagnosticInfo> Diagnostics);

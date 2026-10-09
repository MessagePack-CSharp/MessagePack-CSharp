using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace MessagePack.SourceGenerator;

/// <summary>
/// Turns a [MessagePackObject] type symbol into an <see cref="ObjectModel"/>: member discovery (the reflection
/// enumeration order MessagePack-CSharp's resolvers use), key validation,
/// constructor selection (shared rule with ReflectionObjectFormatter: [SerializationConstructor] wins,
/// else most parameters with full name+type match), callback detection, and the AllowPrivate/generic shape gates.
/// </summary>
static class ObjectParser
{
    const string KeyAttributeName = "MessagePack.KeyAttribute";
    const string IgnoreMemberAttributeName = "MessagePack.IgnoreMemberAttribute";
    const string IgnoreDataMemberAttributeName = "System.Runtime.Serialization.IgnoreDataMemberAttribute";
    const string NonSerializedAttributeName = "System.NonSerializedAttribute";
    internal const string FormatterAttributeName = "MessagePack.MessagePackFormatterAttribute";
    const string FormatterFactoryBaseName = "MessagePack.MessagePackFormatterFactory";
    const string SerializationConstructorAttributeName = "MessagePack.SerializationConstructorAttribute";
    // name+namespace match (never ToDisplayString: the nullable annotation on `MessagePackUnknownMembers?` members
    // would defeat a string comparison)
    internal static bool IsUnknownMembersType(ITypeSymbol type) =>
        type is INamedTypeSymbol { Arity: 0, Name: "MessagePackUnknownMembers" }
        && type.ContainingNamespace is { Name: "MessagePack", ContainingNamespace.IsGlobalNamespace: true };
    const string SetsRequiredMembersAttributeName = "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute";
    const string CallbackReceiverInterfaceName = "MessagePack.IMessagePackSerializationCallbackReceiver";

    internal static readonly SymbolDisplayFormat FullyQualifiedWithNullability =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    // discovered member before key/constructor resolution. ShadowedBaseType: a `new`-shadowed base declaration's
    // fully-qualified declaring type (null for the most-derived declaration of a name,
    // which C#'s `value.Name` binding reaches unqualified). QualifyMapKey: in map-by-name mode this member's default
    // key is "{DeclaringType.FullName}.{Name}" instead of the policy name - every member of a shadowed-name group
    // except the base-most one, mirroring ReflectionObjectFormatter's qualification.
    readonly record struct Candidate(ISymbol Symbol, ITypeSymbol Type, SetterKind Setter, string? ShadowedBaseType = null, bool QualifyMapKey = false);

    public static ParseResult Parse(GeneratorAttributeSyntaxContext context)
    {
        var diagnostics = new List<DiagnosticInfo>();
        var type = (INamedTypeSymbol)context.TargetSymbol;
        var typeLocation = LocationInfo.From(type);
        var typeName = type.ToDisplayString();
        var objectAttribute = context.Attributes[0];

        var AllowCircularReferences = ReadNamedBool(objectAttribute, "AllowCircularReferences");

        if (ReadNamedBool(objectAttribute, "SuppressSourceGeneration"))
        {
            if (AllowCircularReferences)
            {
                // the runtime tiers cannot serve the envelope/back-reference wire
                diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidCircularReference, $"'{typeName}': AllowCircularReferences requires the source-generated formatter and cannot combine with SuppressSourceGeneration.", typeLocation));
                return Invalid(diagnostics);
            }
            // explicit opt-out: the runtime tiers (reflection) serve this type
            return new ParseResult(null, new EquatableArray<DiagnosticInfo>([]));
        }

        if (HasTypeLevelFormatterAttribute(type))
        {
            // the type-level registration serves the type (v3's AttributeFormatterResolver sat before the generated
            // tier): no object model, and none of its shape rules (constructor binding, key checks) apply
            return new ParseResult(null, new EquatableArray<DiagnosticInfo>([]));
        }

        foreach (var attribute in type.GetAttributes())
        {
            // the type-level registration pipeline matches the attribute class exactly (ForAttributeWithMetadataName,
            // one pipeline per exact name); a derived attribute would be silently outranked by this object formatter
            if (attribute.AttributeClass is { } attributeClass
                && attributeClass.ToDisplayString() != FormatterAttributeName
                && !(attributeClass.IsGenericType && attributeClass.OriginalDefinition.ToDisplayString() == FormatterAttributeName + "<TFactory>")
                && HasAttributeClass(attributeClass, FormatterAttributeName))
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.TypeLevelFormatterNotGenerated, $"'{typeName}': '[{attributeClass.Name}]' derives from MessagePackFormatterAttribute, which the type-level registration does not match (only [MessagePackFormatter] and [MessagePackFormatter<TFactory>] themselves are); the generated object formatter serves the type instead. Apply the base attribute directly, or register the factory in the resolver chain.", typeLocation));
            }
        }

        if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            // interfaces land here only without [UnionTag] (the pipeline routes tagged ones to UnionParser),
            // so the hint is the most likely fix
            diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedType, type.TypeKind == TypeKind.Interface
                ? $"'{typeName}' is skipped: an interface serializes only as a polymorphic base. Declare its case types with [UnionTag]."
                : $"'{typeName}' is skipped: only class and struct shapes are generated.", typeLocation));
            return Invalid(diagnostics);
        }

        if (type.IsAbstract)
        {
            // same routing as interfaces: an abstract (or closed, which is implicitly abstract)
            // class only reaches this parser without [UnionTag]. Without it the ctor matching below would fail with
            // MsgPack004, which points at the wrong fix: the type is either a polymorphic root missing its cases,
            // or a member carrier that should not be a serialized type at all (its keyed members are inherited either
            // way)
            diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedType, $"'{typeName}' is skipped: an abstract class serializes only as a polymorphic base. Declare its case types with [UnionTag], or drop [MessagePackObject] if it only carries members for its derived types.", typeLocation));
            return Invalid(diagnostics);
        }

        if (AllowCircularReferences && type.IsValueType)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidCircularReference, $"'{typeName}': AllowCircularReferences requires a class, because value types have no reference identity to preserve.", typeLocation));
            return Invalid(diagnostics);
        }

        var allowPrivate = ReadNamedBool(objectAttribute, "AllowPrivate");

        if (IsFileLocal(type))
        {
            // a `file` type is visible in its own source file only: neither the generated formatter nor the factory,
            // which live in generated files, can name it (CS0400)
            diagnostics.Add(new DiagnosticInfo(Diagnostics.TypeNotAccessible, $"'{typeName}' is a file-local type, which generated code in another file cannot reach; make it internal or public.", typeLocation));
            return Invalid(diagnostics);
        }

        // the generated formatter lives in another namespace of the same assembly (AllowPrivate nests it inside the
        // type, but the factory still needs to reach it)
        for (var accessible = type; accessible is not null; accessible = accessible.ContainingType)
        {
            if (accessible.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.TypeNotAccessible, $"'{typeName}' (or a containing type) is {type.DeclaredAccessibility}; generated formatters can only reach public or internal types.", typeLocation));
                return Invalid(diagnostics);
            }
        }

        for (var container = type.ContainingType; container is not null; container = container.ContainingType)
        {
            if (container.IsGenericType)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedType, $"'{typeName}' is skipped: types nested inside generic types are not supported yet.", typeLocation));
                return Invalid(diagnostics);
            }
        }

        if (type.IsGenericType)
        {
            if (allowPrivate)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedType, $"'{typeName}' is skipped: AllowPrivate on generic types is not supported yet (served by the reflection tier).", typeLocation));
                return Invalid(diagnostics);
            }
            foreach (var typeParameter in type.TypeParameters)
            {
                if (CollidesWithGeneratedCode(typeParameter.Name, context.SemanticModel.Compilation))
                {
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedType, $"'{typeName}' is skipped: a type parameter named '{typeParameter.Name}' would shadow a name the generated formatter uses (its buffer parameters, or a type of the MessagePack / SerializerFoundation namespaces); rename the type parameter.", typeLocation));
                    return Invalid(diagnostics);
                }
            }
        }

        if (allowPrivate && !IsPartialChain(type))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.AllowPrivateRequiresPartial, $"'{typeName}' uses AllowPrivate, so the type (and every containing type) must be declared partial: the formatter is generated as a nested class to reach private members.", typeLocation));
            return Invalid(diagnostics);
        }
        if (allowPrivate && ExternalBase(type) is { } externalBase)
        {
            // AllowPrivate promises the base chain's non-public members too (the reflection tier, like v3,
            // walks them), but another assembly's private members are never in the compilation's view: reference
            // assemblies strip them, and even an implementation assembly is imported without them
            // (MetadataImportOptions.Public). A member table built here would silently drop base state,
            // so the type is left to the reflection tier, which reads the loaded assembly.
            diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedType, $"'{typeName}' is skipped: AllowPrivate covers the base chain, but base type '{externalBase.ToDisplayString()}' lives in another assembly, whose non-public members the generator cannot see, so its member table cannot be trusted. Expose the base state through public/protected members (and drop AllowPrivate), or leave the type to the reflection tier.", typeLocation));
            return Invalid(diagnostics);
        }

        var keyAsPropertyName = false;
        var namingPolicy = KeyNamingPolicy.None;
        if (objectAttribute.ConstructorArguments.Length > 0)
        {
            switch (objectAttribute.ConstructorArguments[0].Value)
            {
                case bool b:
                    keyAsPropertyName = b;
                    break;
                case int policyValue: // the KeyNamingPolicy overload implies string-key mode
                    keyAsPropertyName = true;
                    namingPolicy = (KeyNamingPolicy)policyValue;
                    break;
            }
        }

        // properties across the hierarchy first, then fields, mirrors the reflection enumeration order
        // MessagePack-CSharp's resolvers serialize maps in. allowPrivate widens discovery to non-public members.
        // Dedup is by override chain, not by name: an override shares its base declaration's storage (derived-first
        // order keeps the most derived), but a `new`-shadowed member is separate storage with its own key,
        // and every declaration serializes, matching v3 and ReflectionObjectFormatter (NewPropertyInDerivedType
        // execution test).
        var candidates = new List<Candidate>();
        var seenOverrideRoots = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object && current.SpecialType != SpecialType.System_ValueType; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol { ExplicitInterfaceImplementations.Length: > 0 } explicitImplementation)
                {
                    // reachable only through the interface (a cast that boxes a struct and loses the write, and no
                    // object-initializer route at all): not generated, and said so when it was annotated as a member
                    if (HasAttribute(explicitImplementation, KeyAttributeName))
                    {
                        diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedType, $"'{typeName}.{explicitImplementation.Name}' is an explicit interface implementation, which the generated formatter does not serialize; expose the value through a regular member.", LocationInfo.From(explicitImplementation)));
                    }
                    continue;
                }
                if (member is IPropertySymbol { IsStatic: false, IsIndexer: false, IsImplicitlyDeclared: false } property
                    && property.GetMethod is { } getter
                    && (allowPrivate || (property.DeclaredAccessibility == Accessibility.Public && getter.DeclaredAccessibility == Accessibility.Public)))
                {
                    var root = (IPropertySymbol)property.OriginalDefinition;
                    while (root.OverriddenProperty is { } overridden)
                    {
                        root = (IPropertySymbol)overridden.OriginalDefinition;
                    }
                    if (!seenOverrideRoots.Add(root))
                    {
                        continue;
                    }
                    var setter = property.SetMethod is { } set && (allowPrivate || set.DeclaredAccessibility == Accessibility.Public)
                        ? (set.IsInitOnly ? SetterKind.Init : SetterKind.Set)
                        : SetterKind.None;
                    candidates.Add(new Candidate(property, property.Type, setter));
                }
            }
        }
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object && current.SpecialType != SpecialType.System_ValueType; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                // fields never override, so every declaration is its own storage.
                // [NonSerialized] opts a field out in every mode (the reflection tier's shared rule)
                // and does so here, before shadow grouping, so an excluded base field does not reserve the plain name
                // from a derived same-named one
                if (member is IFieldSymbol { IsStatic: false, IsConst: false, IsImplicitlyDeclared: false } field
                    && (allowPrivate || field.DeclaredAccessibility == Accessibility.Public)
                    && !HasAttribute(field, NonSerializedAttributeName))
                {
                    candidates.Add(new Candidate(field, field.Type, field.IsReadOnly ? SetterKind.None : SetterKind.Set));
                }
            }
        }

        // shadow annotation: among same-named candidates, the declaration nearest the leaf keeps the plain name (that
        // is what `value.Name` binds to); every declaration above it accesses through a cast to its declarer
        foreach (var nameGroup in candidates
            .Select(static (candidate, index) => (candidate, index))
            .GroupBy(static p => p.candidate.Symbol.Name))
        {
            if (nameGroup.Count() < 2)
            {
                continue;
            }
            var byDepth = nameGroup.OrderBy(p => InheritanceDepth(type, p.candidate.Symbol)).ToList();
            foreach (var (candidate, index) in byDepth.Skip(1))
            {
                candidates[index] = candidate with
                {
                    ShadowedBaseType = candidate.Symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                };
            }

            // map-by-name qualification is anchored the other way around: the base-most declaration keeps the plain
            // name (it serialized first in v3's base-first walk), everything nearer the leaf gets the qualified key
            foreach (var (_, index) in byDepth.Take(byDepth.Count - 1))
            {
                candidates[index] = candidates[index] with { QualifyMapKey = true };
            }
        }
        // a base declaration hidden by a derived member that is not a candidate (a [NonSerialized] `new` field, a
        // non-public `new` member the formatter can still see): `value.Name` binds the hider, so the base storage must
        // be reached through its declarer or the hider's value is what gets serialized and the base one is lost
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].ShadowedBaseType is null && IsHiddenInDerivedType(type, candidates[i].Symbol, allowPrivate))
            {
                candidates[i] = candidates[i] with
                {
                    ShadowedBaseType = candidates[i].Symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                };
            }
        }

        // v3 wire order, mirroring ReflectionObjectFormatter so both tiers emit the same map bytes - and v3 ordered the
        // two branches differently (oracle-probed): map-by-name enumerates per level (base level's properties then
        // fields, then the next level), explicit [Key] enumerates kind-major (every property base-first,
        // then every field base-first). An override sorts at its base declaration's level (v3 collapses the pair onto
        // the base slot). Both branches finish with a stable [DataMember(Order)] sort (absent = int.MaxValue,
        // attribute-without-Order = -1). The int-keyed wire is positioned by key, so the ordering is harmless there.
        candidates = [.. (keyAsPropertyName
                ? candidates.OrderByDescending(c => SortDepth(type, c.Symbol))
                : candidates.OrderBy(static c => c.Symbol is IFieldSymbol).ThenByDescending(c => SortDepth(type, c.Symbol)))
            .OrderBy(static c => GetDataMemberOrder(c.Symbol))];

        var entries = new List<(string Name, ITypeSymbol Type, SetterKind Setter, int IntKey, string StringKey, bool RequiredModifier, LocationInfo? Location, CustomFormatterModel? Custom, string? ShadowedBaseType, bool ExplicitKey, bool Writable)>();
        var hasIntKey = false;
        var hasStringKey = keyAsPropertyName;
        var intKeys = new HashSet<int>();
        var stringKeys = new HashSet<string>();
        var valid = true;
        string? unknownMembersName = null;
        var unknownMembersRequired = false;
        // string key -> v3's alias of a key qualified by a generic declaring type, as (prefix, suffix), see MemberModel
        // (keyed by the key, which is unique, because the entry list is filtered before the models are built)
        var legacyKeyByKey = new Dictionary<string, (string Prefix, string Suffix)>(StringComparer.Ordinal);
        // `required` members opted out of serialization: never read, but every generated `new` must still assign them
        var requiredDefaultedMembers = new List<RequiredDefaultModel>();

        foreach (var (symbol, memberType, setter, shadowedBaseType, qualifyMapKey) in candidates)
        {
            int? intKey = null;
            string? stringKey = null;
            var ignored = false;
            var rekeyedOverride = false;
            var unreadableDerivedKey = false;
            AttributeData? formatterAttribute = null;

            // MemberAttributes walks the override chain (an override without its own [Key] inherits the base virtual
            // declaration's, the reflection tier's inherit:true read); nearest declaration wins per category,
            // hence the null guards
            foreach (var attribute in MemberAttributes(symbol))
            {
                var attributeName = attribute.AttributeClass?.ToDisplayString();
                // [IgnoreDataMember] is an alias for [IgnoreMember] in both tiers (v3's dynamic resolvers honored it
                // but mpc did not, that inconsistency ends here)
                if (IsIgnoreAttribute(attribute.AttributeClass))
                {
                    ignored = true;
                }
                else if (IsKeyAttribute(attribute.AttributeClass))
                {
                    // a derived KeyAttribute (the attribute is not sealed, and the reflection tier reads the key off
                    // the instance) is readable here only when the key is its first constructor argument
                    var argument = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value : null;
                    if (argument is not (int or string) || (attributeName != KeyAttributeName && !ForwardsKeyToBase(attribute)))
                    {
                        if (attributeName != KeyAttributeName)
                        {
                            diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedType, $"'{typeName}.{symbol.Name}': '[{attribute.AttributeClass!.Name}]' derives from KeyAttribute, and the generator can only read a derived key that is passed unchanged from the first constructor parameter to KeyAttribute's own constructor (`: base(key)`, declared in this compilation); the reflection tier serves the type as-is. Forward the key as it is, or apply [Key] directly.", LocationInfo.From(symbol)));
                            unreadableDerivedKey = true;
                        }
                        continue;
                    }
                    if (intKey is null && stringKey is null)
                    {
                        if (argument is int i)
                        {
                            intKey = i;
                        }
                        else if (argument is string s)
                        {
                            stringKey = s;
                        }
                    }
                    else if (!(argument is int i2 && intKey == i2) && !(argument is string s2 && stringKey == s2))
                    {
                        // a farther declaration of the override chain keys the same storage differently: v3's tiers
                        // disagreed here (mpc kept one slot, the dynamic resolver wrote both),
                        // and a base-typed reader expects the base key, so the shape is refused rather than picked for
                        diagnostics.Add(new DiagnosticInfo(Diagnostics.OverrideChangesKey, $"'{typeName}.{symbol.Name}' overrides a property declared with a different [Key]; an override keeps the key of the declaration it overrides.", LocationInfo.From(symbol)));
                        rekeyedOverride = true;
                    }
                }
                else if (formatterAttribute is null)
                {
                    // base-chain walk: MessagePackFormatterAttribute is not sealed, and v3 codebases derive from it
                    for (var attributeType = attribute.AttributeClass; attributeType is not null; attributeType = attributeType.BaseType)
                    {
                        if (attributeType.ToDisplayString() == FormatterAttributeName)
                        {
                            formatterAttribute = attribute;
                            break;
                        }
                    }
                }
            }

            if (ignored)
            {
                continue;
            }
            if (rekeyedOverride || unreadableDerivedKey)
            {
                valid = false;
                continue;
            }
            if (allowPrivate && !SymbolEqualityComparer.Default.Equals(symbol.ContainingType.OriginalDefinition, type.OriginalDefinition)
                && !IsAccessibleFromGeneratedFormatter(context.SemanticModel.Compilation, symbol, setter, type, shadowedBaseType is null ? type : symbol.ContainingType))
            {
                // the nested formatter has the declaring type's own access rights and nothing more: a base type's
                // private (or, across assemblies, private protected /internal)
                // member or accessor would not compile (CS0122, CS0271/CS0272),
                // while the reflection tier reads them, so the type is left to that tier
                diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedType, $"'{typeName}' is skipped: '{symbol.ContainingType.Name}.{symbol.Name}' (or one of its accessors) is not accessible from '{type.Name}', so the nested generated formatter cannot reach it; make it protected or internal, or [IgnoreMember] it. The reflection tier serves the type as-is.", LocationInfo.From(symbol)));
                valid = false;
                continue;
            }

            // the retention packet member: set aside, never a keyed member.
            // [IgnoreMember] above still opts it out entirely (the type then keeps skip-only tolerance).
            if (IsUnknownMembersType(memberType))
            {
                var unknownLocation = LocationInfo.From(symbol);
                if (shadowedBaseType is not null || IsHiddenInDerivedType(type, symbol, allowPrivate))
                {
                    // the generated code reaches the packet through `result.Name`, which binds the most derived
                    // declaration of that name (a candidate or not: a [NonSerialized] `new` field hides it just the
                    // same): a hidden base packet would be captured into, or fail to compile against, the wrong member
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidUnknownMembersMember, $"'{typeName}': the MessagePackUnknownMembers member '{symbol.ContainingType.ToDisplayString()}.{symbol.Name}' is hidden by a derived declaration of the same name, which generated code cannot reach; rename one of them.", unknownLocation));
                    valid = false;
                    continue;
                }
                if (intKey is not null || stringKey is not null)
                {
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidUnknownMembersMember, $"'{typeName}.{symbol.Name}': a MessagePackUnknownMembers member carries no [Key]; it captures whatever keys the declared members do not.", unknownLocation));
                    valid = false;
                    continue;
                }
                if (formatterAttribute is not null)
                {
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidUnknownMembersMember, $"'{typeName}.{symbol.Name}': a MessagePackUnknownMembers member is not serialized through a formatter, so [MessagePackFormatter] has no meaning on it.", unknownLocation));
                    valid = false;
                    continue;
                }
                if (setter != SetterKind.Set)
                {
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidUnknownMembersMember, $"'{typeName}.{symbol.Name}': a MessagePackUnknownMembers member needs a plain settable accessor (init-only and read-only are not supported), because every deserialization assigns the freshly captured packet.", unknownLocation));
                    valid = false;
                    continue;
                }
                if (unknownMembersName is not null)
                {
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidUnknownMembersMember, $"'{typeName}' declares more than one MessagePackUnknownMembers member ('{unknownMembersName}' and '{symbol.Name}'); the capture destination must be unambiguous.", unknownLocation));
                    valid = false;
                    continue;
                }
                unknownMembersName = symbol.Name;
                unknownMembersRequired = IsRequiredMember(symbol);
                continue;
            }

            CustomFormatterModel? custom = null;
            if (formatterAttribute is not null)
            {
                if (!TryBuildCustomFormatter(formatterAttribute, memberType, $"{typeName}.{symbol.Name}", LocationInfo.From(symbol), context, diagnostics, out custom, accessScope: allowPrivate ? type : null))
                {
                    valid = false;
                    continue;
                }
            }

            var memberLocation = LocationInfo.From(symbol);
            var explicitKey = intKey is not null || stringKey is not null;
            if (keyAsPropertyName && intKey is not null)
            {
                // v3 rule (both of its tiers, and the reflection tier here): KeyAsPropertyName wins;
                // a stray int [Key] on a map-mode type is ignored and the member keeps its name-derived string key
                intKey = null;
            }
            if (intKey is null && stringKey is null)
            {
                if (keyAsPropertyName)
                {
                    // an explicit [Key("...")] never reaches here, so the policy only shapes defaults;
                    // a policy-induced collision ("Id"/"ID" both → "id") falls into MsgPack003 below.
                    // A shadowed name's non-basemost declarations get the reflection tier's qualified key instead -
                    // raw, never policy-shaped (v3's rule for the declarations it kept)
                    stringKey = qualifyMapKey
                        ? ReflectionFullName(symbol.ContainingType) + "." + symbol.Name
                        : KeyNamingPolicyConverter.ConvertName(namingPolicy, symbol.Name);
                    if (qualifyMapKey && symbol.ContainingType.IsGenericType)
                    {
                        // v3 spelled this qualifier as the closed FullName ("Ns.Middle`1[[System.Int32, <assembly>]].X"):
                        // the type arguments' assembly identities are a runtime detail, so the alias is matched by
                        // its invariant ends
                        legacyKeyByKey[stringKey] = (ReflectionFullName(symbol.ContainingType) + "[", "]." + symbol.Name);
                    }
                }
                else if (symbol.DeclaredAccessibility == Accessibility.Public
                    && (symbol is not IPropertySymbol { GetMethod: { } g } || g.DeclaredAccessibility == Accessibility.Public))
                {
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.MemberNeedsKey, $"'{typeName}.{symbol.Name}' is a public member of a [MessagePackObject] type and needs [Key] or [IgnoreMember] (or use [MessagePackObject(true)]).", memberLocation));
                    valid = false;
                    continue;
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
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidKey, $"'{typeName}.{symbol.Name}' has a negative key ({key}).", memberLocation));
                    valid = false;
                    continue;
                }
                if (!intKeys.Add(key))
                {
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.DuplicateKey, $"'{typeName}' declares key {key} more than once.", memberLocation));
                    valid = false;
                    continue;
                }
                hasIntKey = true;
            }
            else
            {
                if (!stringKeys.Add(stringKey!))
                {
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.DuplicateKey, $"'{typeName}' declares key \"{stringKey}\" more than once.", memberLocation));
                    valid = false;
                    continue;
                }
                hasStringKey = true;
            }

            var requiredModifier = IsRequiredMember(symbol);
            if (shadowedBaseType is not null && (setter == SetterKind.Init || requiredModifier))
            {
                // the construction shape reaches init-only and required members through the object initializer,
                // which can only bind the most-derived declaration of a name;
                // a shadowed base member has no generated-C# route there
                diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedType, $"'{typeName}' is skipped: the shadowed base member '{shadowedBaseType}.{symbol.Name}' is {(requiredModifier ? "required" : "init-only")}, which generated code can only assign through an object initializer, where the derived declaration hides it.", memberLocation));
                valid = false;
                continue;
            }
            // v3's inclusion writability (the reflection tier's IsWritableMember): an accessible setter,
            // or a field - under AllowPrivate even a readonly one (v3 counted those)
            var writable = setter != SetterKind.None || (allowPrivate && symbol is IFieldSymbol);
            entries.Add((symbol.Name, memberType, setter, intKey ?? -1, stringKey ?? "", requiredModifier, memberLocation, custom, shadowedBaseType, explicitKey, writable));
        }

        // every `required` member the formatter will not read, whatever excluded it ([IgnoreMember], [NonSerialized],
        // an accessor the generated code cannot reach): the compiler demands it in each generated `new` all the same
        var readNames = new HashSet<string>(entries.Select(static e => e.Name), StringComparer.Ordinal);
        if (unknownMembersName is not null)
        {
            readNames.Add(unknownMembersName);
        }
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object && current.SpecialType != SpecialType.System_ValueType; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol { IsStatic: false } or IFieldSymbol { IsStatic: false }
                    && IsRequiredMember(member)
                    && !readNames.Contains(member.Name)
                    && !requiredDefaultedMembers.Any(r => r.Name == member.Name))
                {
                    requiredDefaultedMembers.Add(new RequiredDefaultModel(member.Name, RequiredDefaultExpression(member, context.SemanticModel.Compilation, typeName, diagnostics)));
                }
            }
        }

        if (hasIntKey && hasStringKey)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.MixedKeys, $"'{typeName}' mixes int keys and string keys; a type serializes as either an array (int keys) or a map (string keys), not both.", typeLocation));
            valid = false;
        }

        if (!valid)
        {
            return Invalid(diagnostics);
        }

        // constructor selection: [SerializationConstructor] wins; otherwise the accessible constructor with the most
        // parameters where every parameter matches a serialized member by string key, else by member name
        // (ordinal-ignore-case, assignable type; see TryMatchConstructor), ReflectionObjectFormatter's rule,
        // so the generated and reflection interpretations of a type
        // never diverge
        var parameterIndexByEntry = new int[entries.Count];
        for (int i = 0; i < parameterIndexByEntry.Length; i++)
        {
            parameterIndexByEntry[i] = -1;
        }
        var constructorParameterCount = 0;
        var selectedConstructor = default(IMethodSymbol);

        var attributedConstructor = default(IMethodSymbol);
        foreach (var constructor in type.InstanceConstructors)
        {
            if (HasAttribute(constructor, SerializationConstructorAttributeName))
            {
                if (attributedConstructor is not null)
                {
                    // the attribute is AllowMultiple=false per constructor, but nothing stops a second constructor
                    // from carrying its own; picking the first in declaration order would make the contract depend
                    // on source layout (v3 crashed on SingleOrDefault here)
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidSerializationConstructor, $"'{typeName}' declares [SerializationConstructor] on more than one constructor; only one constructor can carry it.", LocationInfo.From(constructor) ?? typeLocation));
                    return Invalid(diagnostics);
                }
                attributedConstructor = constructor;
            }
        }

        if (attributedConstructor is not null)
        {
            if (!IsConstructorAccessible(attributedConstructor, allowPrivate, parameterless: attributedConstructor.Parameters.Length == 0)
                || !HasOnlyValueParameters(attributedConstructor)
                || !TryMatchConstructor(context.SemanticModel.Compilation, attributedConstructor, entries, parameterIndexByEntry))
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidSerializationConstructor, $"'{typeName}': the [SerializationConstructor] constructor must be accessible, take its parameters by value (no ref/out/in), and every parameter must match a serialized member by [Key] name or member name (case-insensitive) and assignable type.", LocationInfo.From(attributedConstructor) ?? typeLocation));
                return Invalid(diagnostics);
            }
            constructorParameterCount = attributedConstructor.Parameters.Length;
            selectedConstructor = attributedConstructor;
        }
        else
        {
            var matched = false;
            foreach (var constructor in type.InstanceConstructors
                .Where(c => IsConstructorAccessible(c, allowPrivate, parameterless: c.Parameters.Length == 0) && HasOnlyValueParameters(c))
                .OrderByDescending(c => c.Parameters.Length))
            {
                for (int i = 0; i < parameterIndexByEntry.Length; i++)
                {
                    parameterIndexByEntry[i] = -1;
                }
                if (TryMatchConstructor(context.SemanticModel.Compilation, constructor, entries, parameterIndexByEntry))
                {
                    constructorParameterCount = constructor.Parameters.Length;
                    selectedConstructor = constructor;
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                for (int i = 0; i < parameterIndexByEntry.Length; i++)
                {
                    parameterIndexByEntry[i] = -1;
                }
                if (!type.IsValueType)
                {
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.NoParameterlessConstructor, $"'{typeName}' needs an accessible parameterless constructor, or a constructor whose parameters all match serialized members by [Key] name or member name (case-insensitive), for deserialization; v3 bound int-keyed parameters by position, v4 binds by name only.", typeLocation));
                    return Invalid(diagnostics);
                }
            }
        }

        if (keyAsPropertyName)
        {
            // v3's contractless inclusion rule, applied to map mode by the reflection tier (FilterNonContractSlots): a
            // name-defaulted member that is not writable and not consumed by the selected constructor does not
            // serialize at all, so a computed getter-only property is never touched.
            // An explicit [Key] always serializes.
            var kept = new List<(string Name, ITypeSymbol Type, SetterKind Setter, int IntKey, string StringKey, bool RequiredModifier, LocationInfo? Location, CustomFormatterModel? Custom, string? ShadowedBaseType, bool ExplicitKey, bool Writable)>(entries.Count);
            var keptParameterIndex = new List<int>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].ExplicitKey || entries[i].Writable || parameterIndexByEntry[i] >= 0)
                {
                    kept.Add(entries[i]);
                    keptParameterIndex.Add(parameterIndexByEntry[i]);
                }
            }
            if (kept.Count != entries.Count)
            {
                entries = kept;
                parameterIndexByEntry = [.. keptParameterIndex];
            }
        }

        var anyInit = false;
        var anyRequiredModifier = false;
        var constructorSetsRequiredMembers = selectedConstructor is not null && HasAttribute(selectedConstructor, SetsRequiredMembersAttributeName);
        var models = new List<MemberModel>();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.Setter == SetterKind.Init)
            {
                anyInit = true;
            }
            anyRequiredModifier |= entry.RequiredModifier;
            if (entry.Setter == SetterKind.None && parameterIndexByEntry[i] < 0)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.MemberNotSettable, $"'{typeName}.{entry.Name}' has a key but no setter and no matching constructor parameter: it serializes, but its payload is skipped on deserialization.", entry.Location));
            }
            var requiredByConstructor = parameterIndexByEntry[i] >= 0
                && !selectedConstructor!.Parameters[parameterIndexByEntry[i]].HasExplicitDefaultValue;
            // a custom formatter overrides even the direct-writable primitives
            var (direct, directNullable) = entry.Custom is null ? ClassifyDirect(entry.Type) : (DirectKind.None, false);
            models.Add(new MemberModel(
                Name: entry.Name,
                // `dynamic` would make every formatter call on the member a dynamic invocation, which cannot take the
                // ref struct buffers (CS1978): the formatter is object's, and the member is read and written as object
                TypeName: entry.Type is IDynamicTypeSymbol ? "object?" : entry.Type.ToDisplayString(FullyQualifiedWithNullability),
                IsDynamic: entry.Type is IDynamicTypeSymbol,
                IntKey: entry.IntKey,
                StringKey: entry.StringKey,
                Direct: direct,
                DirectNullable: directNullable,
                Setter: entry.Setter,
                ConstructorParameterIndex: parameterIndexByEntry[i],
                ConstructorParameterTypeName: parameterIndexByEntry[i] >= 0
                    && selectedConstructor!.Parameters[parameterIndexByEntry[i]].Type is { } parameterType
                    && !SymbolEqualityComparer.Default.Equals(parameterType, entry.Type)
                    // a `dynamic` parameter takes an object argument: a `(dynamic)` cast would make the `new` a
                    // dynamic invocation that re-resolves the overload at run time (and could pick a sibling
                    // constructor over the [SerializationConstructor])
                    ? (parameterType is IDynamicTypeSymbol ? "object?" : parameterType.ToDisplayString(FullyQualifiedWithNullability))
                    : null,
                IsRequired: entry.RequiredModifier || requiredByConstructor,
                HasRequiredModifier: entry.RequiredModifier,
                IsNonNullableReference: entry.Type.IsReferenceType
                    && entry.Type.NullableAnnotation == NullableAnnotation.NotAnnotated
                    && entry.Type.TypeKind != TypeKind.TypeParameter,
                CustomFormatter: entry.Custom,
                BaseCastType: entry.ShadowedBaseType,
                UniqueId: entry.ShadowedBaseType is null ? null : entry.Name + "_" + Sanitize(entry.ShadowedBaseType),
                LegacyKeyPrefix: legacyKeyByKey.TryGetValue(entry.StringKey, out var legacyKey) ? legacyKey.Prefix : null,
                LegacyKeySuffix: legacyKeyByKey.TryGetValue(entry.StringKey, out legacyKey) ? legacyKey.Suffix : null));
        }

        // the generated identifiers (f/v_/has_ + Id) must be unique: a shadowed member's UniqueId (Name_Declarer)
        // can spell another member's plain name (X hidden by `new X` next to a member named X_Ns_Base)
        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < models.Count; i++)
        {
            var id = models[i].Id;
            var unique = id;
            var suffix = 2;
            while (!usedIds.Add(unique))
            {
                unique = id + "_" + suffix++;
            }
            if (unique != id)
            {
                models[i] = models[i] with { UniqueId = unique };
            }
        }

        // a class with required members cannot be `new T()`-ed by the populate path (CS9035): route it through the
        // construction shape, whose object initializer covers the required members.
        // Structs never `new` on the populate path, which is why a struct's explicit parameterless
        // [SerializationConstructor] (an initializer the author asked for) takes the construction shape too.
        var useConstruction = constructorParameterCount > 0 || anyInit || (anyRequiredModifier && !type.IsValueType)
            || (attributedConstructor is not null && type.IsValueType);

        if (AllowCircularReferences && useConstruction)
        {
            // register-before-populate is the whole mechanism: the instance must exist (parameterless)
            // and be fully fillable afterward, or a cycle back into it could never resolve
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidCircularReference, $"'{typeName}': AllowCircularReferences deserializes by creating the instance first and populating members afterward, so the type cannot use constructor-parameter matching, init-only accessors, or required members. Use a parameterless constructor with settable members (mark it with [SerializationConstructor] if a parameterized constructor is being matched).", typeLocation));
            return Invalid(diagnostics);
        }

        var (onBefore, onAfter) = DetectCallbacks(type);

        // AOT harvesting: closed instantiations of this compilation's generic [MessagePackObject] types,
        // plus enums, Nullable<T>, and BCL collection/wrapper closed forms (List<string>, string[],
        // Dictionary<K,V>, tuples, ...), found anywhere inside the serialized member types get static constructions in
        // the factory (see FactoryEmitter), the AOT route, since MakeGenericType cannot close over the ref struct
        // buffer arguments and the AOT chain omits GenericFormatterFactory
        var harvestedGenerics = new Dictionary<string, HarvestedGenericModel>();
        var harvestedBuiltIns = new Dictionary<string, HarvestedBuiltInModel>();
        var compilation = context.SemanticModel.Compilation;
        foreach (var entry in entries)
        {
            if (entry.Custom is not null)
            {
                continue; // the member's own [MessagePackFormatter] serves it; a default registration beside it is noise at best
            }
            HarvestSerializedType(entry.Type, compilation, harvestedGenerics, harvestedBuiltIns);
        }

        // AllowPrivate: member types the generated factory cannot name (a private nested enum, alone or inside
        // Nullable<>, arrays, List<>, Dictionary<,>, ...) are harvested again with the type itself as the
        // accessibility scope; what that run adds is registered by a factory nested in the partial type (see
        // ObjectEmitter), the one place that can spell those types
        var privateHarvestedBuiltIns = new List<HarvestedBuiltInModel>();
        if (allowPrivate)
        {
            var scopedGenerics = new Dictionary<string, HarvestedGenericModel>();
            var scopedBuiltIns = new Dictionary<string, HarvestedBuiltInModel>();
            privateAccessScope = type;
            try
            {
                foreach (var entry in entries)
                {
                    if (entry.Custom is null)
                    {
                        HarvestSerializedType(entry.Type, compilation, scopedGenerics, scopedBuiltIns);
                    }
                }
            }
            finally
            {
                privateAccessScope = null;
            }
            foreach (var harvested in scopedBuiltIns)
            {
                if (!harvestedBuiltIns.ContainsKey(harvested.Key))
                {
                    privateHarvestedBuiltIns.Add(harvested.Value);
                }
            }
            // a generic [MessagePackObject] of this compilation closed over a private type (Box<Mode>): its generated
            // open formatter is internal to the assembly, so the nested factory can close it where the generated
            // factory cannot name the argument
            foreach (var harvested in scopedGenerics)
            {
                if (!harvestedGenerics.ContainsKey(harvested.Key) && harvested.Value.OpenFormatterName.Length > 0)
                {
                    privateHarvestedBuiltIns.Add(new HarvestedBuiltInModel(
                        ClosedTypeName: harvested.Value.ClosedTypeName,
                        FormatterConstruction: $"new global::MessagePack.Generated.{harvested.Value.OpenFormatterName}<TWriteBuffer, TReadBuffer, {string.Join(", ", harvested.Value.TypeArguments.AsArray())}>()"));
                }
            }
        }

        var fullTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sanitized = Sanitize(fullTypeName);
        var formatterName = allowPrivate ? "GeneratedMessagePackFormatter" : sanitized + "Formatter";
        var formatterReference = allowPrivate
            ? fullTypeName + ".GeneratedMessagePackFormatter"
            : "global::MessagePack.Generated." + formatterName;

        var typeParameterList = type.IsGenericType
            ? string.Join(", ", type.TypeParameters.Select(static p => Identifier(p.Name)))
            : "";
        var whereClauses = type.IsGenericType ? BuildWhereClauses(type) : new EquatableArray<string>([]);
        var openTypeOf = type.IsGenericType
            ? StripTypeArguments(fullTypeName) + "<" + new string(',', type.TypeParameters.Length - 1) + ">"
            : fullTypeName;
        var openFormatterTypeOf = type.IsGenericType
            ? formatterReference + "<" + new string(',', type.TypeParameters.Length + 1) + ">"
            : "";

        if (type.IsGenericType)
        {
            foreach (var typeParameter in type.TypeParameters)
            {
                if (CollidesWithGeneratedSlot(typeParameter.Name, models.Select(static m => m.Id)))
                {
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.UnsupportedType, $"'{typeName}' is skipped: a type parameter named '{typeParameter.Name}' would shadow the generated formatter's field or local for a member; rename the type parameter.", typeLocation));
                    return Invalid(diagnostics);
                }
            }
        }

        var model = new ObjectModel(
            FullTypeName: fullTypeName,
            FormatterName: formatterName,
            FormatterReference: formatterReference,
            IsValueType: type.IsValueType,
            AllowCircularReferences: AllowCircularReferences,
            IsStringKey: hasStringKey,
            MaxIntKey: hasIntKey ? intKeys.Max() : -1,
            ConstructorParameterCount: constructorParameterCount,
            UseConstruction: useConstruction,
            ConstructorSetsRequiredMembers: constructorSetsRequiredMembers,
            EmitNested: allowPrivate,
            NestedDeclarations: allowPrivate ? BuildNestedDeclarations(type) : new EquatableArray<string>([]),
            TypeParameterList: typeParameterList,
            WhereClauses: whereClauses,
            OpenTypeOf: openTypeOf,
            OpenFormatterTypeOf: openFormatterTypeOf,
            OnBeforeSerialize: onBefore,
            OnAfterDeserialize: onAfter,
            Members: new EquatableArray<MemberModel>([.. models]),
            HarvestedGenerics: new EquatableArray<HarvestedGenericModel>([.. harvestedGenerics.Values.OrderBy(static h => h.ClosedTypeName, StringComparer.Ordinal)]),
            HarvestedBuiltIns: new EquatableArray<HarvestedBuiltInModel>([.. harvestedBuiltIns.Values.OrderBy(static h => h.ClosedTypeName, StringComparer.Ordinal)]),
            UnknownMembersName: unknownMembersName,
            UnknownMembersRequired: unknownMembersRequired,
            RequiredDefaultedMembers: new EquatableArray<RequiredDefaultModel>([.. requiredDefaultedMembers]),
            PrivateHarvestedBuiltIns: new EquatableArray<HarvestedBuiltInModel>([.. privateHarvestedBuiltIns.OrderBy(static h => h.ClosedTypeName, StringComparer.Ordinal)]));
        return new ParseResult(model, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
    }

    const string MessagePackObjectAttributeName = "MessagePack.MessagePackObjectAttribute";

    // A finite type graph terminates on its own: every closed type is harvested once (the userGenerics / builtIns
    // keys). What does not terminate is an expanding generic definition (the CLR's "infinitely expanding" shape: a
    // cycle through type-argument positions with a step that nests the parameter in a constructed type, class
    // C<T> : List<C<T[]>> opening C<int>, C<int[]>, C<int[][]>, ...; Node<T> { Node<List<T>> Next }). The walk stops
    // a closed type of such a definition once it nests MaxClosureGrowth levels deeper than the type the walk started
    // from, and nothing else: a finite graph, however deeply its own types nest (a 40-layer List<List<...>> member, a
    // long chain of distinct generics A<int> -> B<int> -> ... -> Z<int>), is harvested to its end, which Native AOT
    // (no dynamic fallback) depends on. closureDepth is informational only.
    const int MaxClosureGrowth = 32;

    // the nesting depth of the type the current walk started from (one top-level call per member type)
    [ThreadStatic]
    static int? harvestRootNesting;

    // per walk: whether a generic definition is expanding, and the edges of the expansion graph
    [ThreadStatic]
    static Dictionary<INamedTypeSymbol, bool>? expandingDefinitions;

    [ThreadStatic]
    static Dictionary<INamedTypeSymbol, List<(int SourceIndex, INamedTypeSymbol Target, int TargetIndex, bool Nested)>>? expansionEdges;

    // Expansion graph: node (D, i) is type parameter i of definition D; an edge (D, i) -> (E, k) exists when a type D
    // closes under the walk (an open member type for a [MessagePackObject] of this compilation, the arguments of the
    // collection view the catch-all serves a collection through) mentions E<...> with T_i in its k-th argument, nested when
    // T_i sits inside a constructed type there (E<List<T>>, E<T[]>) rather than being the argument itself. D expands
    // iff some (D, i) lies on a cycle with a nested edge: each round of the cycle then closes a strictly deeper type.
    static bool IsExpandingDefinition(INamedTypeSymbol definition, Compilation compilation)
    {
        expandingDefinitions ??= new Dictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);
        if (expandingDefinitions.TryGetValue(definition, out var known))
        {
            return known;
        }
        var expanding = false;
        for (var i = 0; i < definition.TypeParameters.Length && !expanding; i++)
        {
            var seen = new HashSet<(INamedTypeSymbol, int, bool)>();
            var pending = new Stack<(INamedTypeSymbol Definition, int Index, bool NestedSeen)>();
            pending.Push((definition, i, false));
            while (pending.Count > 0 && !expanding)
            {
                var (current, index, nestedSeen) = pending.Pop();
                foreach (var (sourceIndex, target, targetIndex, nested) in ExpansionEdges(current, compilation))
                {
                    if (sourceIndex != index)
                    {
                        continue;
                    }
                    var nextNested = nestedSeen || nested;
                    if (nextNested && SymbolEqualityComparer.Default.Equals(target, definition) && targetIndex == i)
                    {
                        expanding = true;
                        break;
                    }
                    if (seen.Add((target, targetIndex, nextNested)))
                    {
                        pending.Push((target, targetIndex, nextNested));
                    }
                }
            }
        }
        expandingDefinitions[definition] = expanding;
        return expanding;
    }

    static List<(int SourceIndex, INamedTypeSymbol Target, int TargetIndex, bool Nested)> ExpansionEdges(INamedTypeSymbol definition, Compilation compilation)
    {
        expansionEdges ??= new Dictionary<INamedTypeSymbol, List<(int, INamedTypeSymbol, int, bool)>>(SymbolEqualityComparer.Default);
        if (expansionEdges.TryGetValue(definition, out var edges))
        {
            return edges;
        }
        edges = new List<(int, INamedTypeSymbol, int, bool)>();
        expansionEdges[definition] = edges;
        foreach (var open in OpenClosureTypes(definition, compilation))
        {
            CollectExpansionEdges(open, definition, edges);
        }
        return edges;
    }

    // what the walk closes under a definition's type arguments
    static IEnumerable<ITypeSymbol> OpenClosureTypes(INamedTypeSymbol definition, Compilation compilation)
    {
        if (SymbolEqualityComparer.Default.Equals(definition.ContainingAssembly, compilation.Assembly) && HasMessagePackObjectAttribute(definition))
        {
            foreach (var member in ClosedSerializedMemberTypes(definition))
            {
                yield return member;
            }
            foreach (var caseType in ClosedUnionCaseTypes(definition))
            {
                yield return caseType;
            }
            yield break;
        }
        // a collection shape closes the type arguments of the view the catch-all serves it through (the same
        // selection, asked of the open definition), and an unserved shape closes nothing
        if (IsAccessibleToGeneratedCode(definition, compilation) && SelectCollectionShape(definition, compilation) is { } shape)
        {
            foreach (var argument in shape.Arguments)
            {
                yield return argument;
            }
        }
    }

    static void CollectExpansionEdges(ITypeSymbol term, INamedTypeSymbol source, List<(int, INamedTypeSymbol, int, bool)> edges)
    {
        switch (term)
        {
            case IArrayTypeSymbol array:
                CollectExpansionEdges(array.ElementType, source, edges);
                break;
            case INamedTypeSymbol { IsGenericType: true } named:
                var target = named.OriginalDefinition;
                // BCL generics (List<T>, Dictionary<,>) are harvested by their arguments, never closed through their
                // own members, so they are not nodes; everything else may be
                var isNode = target.SpecialType == SpecialType.None && !target.ContainingNamespace.ToDisplayString().StartsWith("System", StringComparison.Ordinal);
                for (var k = 0; k < named.TypeArguments.Length; k++)
                {
                    var argument = named.TypeArguments[k];
                    if (isNode)
                    {
                        for (var i = 0; i < source.TypeParameters.Length; i++)
                        {
                            var parameter = source.TypeParameters[i];
                            if (MentionsTypeParameter(argument, parameter))
                            {
                                edges.Add((i, target, k, !SymbolEqualityComparer.Default.Equals(argument, parameter)));
                            }
                        }
                    }
                    CollectExpansionEdges(argument, source, edges);
                }
                break;
        }
    }

    static bool MentionsTypeParameter(ITypeSymbol term, ITypeParameterSymbol parameter) => term switch
    {
        ITypeParameterSymbol p => SymbolEqualityComparer.Default.Equals(p, parameter),
        IArrayTypeSymbol array => MentionsTypeParameter(array.ElementType, parameter),
        INamedTypeSymbol { IsGenericType: true } named => named.TypeArguments.Any(a => MentionsTypeParameter(a, parameter)),
        _ => false,
    };

    static int TypeNestingDepth(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return 1 + TypeNestingDepth(array.ElementType);
            case INamedTypeSymbol { IsGenericType: true } named:
                var deepest = 0;
                foreach (var argument in named.TypeArguments)
                {
                    deepest = Math.Max(deepest, TypeNestingDepth(argument));
                }
                return 1 + deepest;
            default:
                return 0;
        }
    }

    internal static void HarvestSerializedType(ITypeSymbol type, Compilation compilation, Dictionary<string, HarvestedGenericModel> userGenerics, Dictionary<string, HarvestedBuiltInModel> builtIns, int closureDepth = 0)
    {
        var nesting = TypeNestingDepth(type);
        var outermost = harvestRootNesting is null;
        if (outermost)
        {
            harvestRootNesting = nesting;
        }
        try
        {
            if (nesting > harvestRootNesting + MaxClosureGrowth
                && type is INamedTypeSymbol { IsGenericType: true } grown
                && IsExpandingDefinition(grown.OriginalDefinition, compilation))
            {
                return; // an expanding definition's closures end here, see MaxClosureGrowth
            }
            HarvestSerializedTypeCore(type, compilation, userGenerics, builtIns, closureDepth);
        }
        finally
        {
            if (outermost)
            {
                harvestRootNesting = null;
                expandingDefinitions = null;
                expansionEdges = null;
            }
        }
    }

    static void HarvestSerializedTypeCore(ITypeSymbol type, Compilation compilation, Dictionary<string, HarvestedGenericModel> userGenerics, Dictionary<string, HarvestedBuiltInModel> builtIns, int closureDepth)
    {
        if (type is IArrayTypeSymbol array)
        {
            HarvestSerializedType(array.ElementType, compilation, userGenerics, builtIns, closureDepth);
            HarvestArray(array, compilation, builtIns);
            return;
        }
        if (type is not INamedTypeSymbol named)
        {
            return;
        }
        if (named.IsTupleType && named.TupleUnderlyingType is { } tupleUnderlying)
        {
            // element names are display-only: normalizing to the underlying ValueTuple keeps typeof strings legal and
            // dedupes (int a, string b) against (int x, string y)
            named = tupleUnderlying;
        }
        if (named.TypeKind == TypeKind.Enum)
        {
            // a type-level [MessagePackFormatter] on the enum is served by its declaring compilation's factory
            // (possibly another assembly's); harvesting the integer formatter here would register a competing default
            // for the same type
            if (!ContainsTypeParameter(named)
                && IsAccessibleToGeneratedCode(named, compilation)
                && !HasAttribute(named, FormatterAttributeName)
                && EnumVariantFormatter(named) is { } variant)
            {
                var enumTypeName = named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (!builtIns.ContainsKey(enumTypeName))
                {
                    builtIns.Add(enumTypeName, new HarvestedBuiltInModel(
                        ClosedTypeName: enumTypeName,
                        FormatterConstruction: $"new global::MessagePack.Formatters.{variant}<TWriteBuffer, TReadBuffer, {enumTypeName}>()"));
                }
            }
            return;
        }
        if (!named.IsGenericType)
        {
            // a non-generic collection shape (ArrayList, a Collection<T> subclass, ...)
            // still rides the catch-all tier at runtime, so it needs the same static registration
            HarvestCollectionCatchAll(named, compilation, userGenerics, builtIns, closureDepth);
            return;
        }
        foreach (var argument in named.TypeArguments)
        {
            HarvestSerializedType(argument, compilation, userGenerics, builtIns, closureDepth);
        }
        // only fully closed, factory-visible instantiations: open members of a generic container flow type parameters,
        // and a private nested type cannot be typeof'ed from the generated factory
        if (ContainsTypeParameter(named) || !IsAccessibleToGeneratedCode(named, compilation))
        {
            return;
        }
        if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var nullableTypeName = named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!builtIns.ContainsKey(nullableTypeName))
            {
                builtIns.Add(nullableTypeName, new HarvestedBuiltInModel(
                    ClosedTypeName: nullableTypeName,
                    FormatterConstruction: $"new global::MessagePack.Formatters.NullableFormatter<TWriteBuffer, TReadBuffer, {named.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()"));
            }
            return;
        }
        if (named.OriginalDefinition is { MetadataName: "Complex`1" } complexDefinition
            && complexDefinition.ContainingNamespace.ToDisplayString() == "System.Numerics")
        {
            var complexTypeName = named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!builtIns.ContainsKey(complexTypeName))
            {
                builtIns.Add(complexTypeName, new HarvestedBuiltInModel(
                    ClosedTypeName: complexTypeName,
                    FormatterConstruction: $"new global::MessagePack.Formatters.ComplexFormatter<TWriteBuffer, TReadBuffer, {named.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()"));
            }
            return;
        }
        // built-in generic collections and wrappers (List<T>, Dictionary<K,V>, tuples, immutable collections,
        // ...): GenericFormatterFactory closes these over the runtime type arguments,
        // which the Native AOT chain omits, so every closed instantiation reachable from the member graph registers a
        // static construction instead
        if (BuiltInGenericFormatterName(named) is { } genericFormatter)
        {
            if (IsBuiltInServedInstantiation(named))
            {
                // BuiltInFormatterFactory serves the closed form directly (bin-format byte containers,
                // codec-backed primitive List/Memory/ArraySegment); registering the plain generic formatter here would
                // shadow the faster one in every chain
                return;
            }
            // List<TEnum>: the underlying-integer codec loop (GenericFormatterFactory's routing);
            // the formatter is net9+ only (CollectionsMarshal), downlevel keeps ListFormatter
            if (genericFormatter == "ListFormatter"
                && named.TypeArguments[0] is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumArgument
                && EnumUnderlyingKeyword(enumArgument) is { } enumUnderlying
                && FormatterTypeExists(compilation, "EnumListFormatter", 2))
            {
                var enumListName = named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (!builtIns.ContainsKey(enumListName))
                {
                    builtIns.Add(enumListName, new HarvestedBuiltInModel(
                        ClosedTypeName: enumListName,
                        FormatterConstruction: $"new global::MessagePack.Formatters.EnumListFormatter<TWriteBuffer, TReadBuffer, {enumArgument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {enumUnderlying}>()"));
                }
                return;
            }
            // the formatter itself can be TFM-gated (Frozen*, ReadOnlySet, OrderedDictionary,
            // PriorityQueue): skip when the referenced MessagePack asset does not compile it
            if (FormatterTypeExists(compilation, genericFormatter, named.TypeArguments.Length))
            {
                var closedCollectionName = named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (!builtIns.ContainsKey(closedCollectionName))
                {
                    // comparer-shaped formatters have no parameterless constructor;
                    // null = the default comparer, matching the parameterless runtime factories
                    var arguments = FormatterTakesComparer(genericFormatter) ? "comparer: null" : "";
                    builtIns.Add(closedCollectionName, new HarvestedBuiltInModel(
                        ClosedTypeName: closedCollectionName,
                        FormatterConstruction: $"new global::MessagePack.Formatters.{genericFormatter}<TWriteBuffer, TReadBuffer, {string.Join(", ", named.TypeArguments.Select(static a => a.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}>({arguments})"));
                }
            }
            return;
        }
        // user generics: only this compilation's [MessagePackObject] definitions,
        // another assembly's generated formatters are internal to it (its own compilation harvests)
        if (!SymbolEqualityComparer.Default.Equals(named.OriginalDefinition.ContainingAssembly, compilation.Assembly)
            || !HasMessagePackObjectAttribute(named.OriginalDefinition)
            || IsNestedInGenericType(named))
        {
            // (a type nested in a generic, Outer<int>.Inner, is generic through its container only: the parser
            // refuses the shape (MsgPack005) and there is no open formatter to close, so it is not a user generic)
            HarvestCollectionCatchAll(named, compilation, userGenerics, builtIns, closureDepth);
            return;
        }
        var closedTypeName = named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (userGenerics.ContainsKey(closedTypeName))
        {
            return;
        }
        userGenerics.Add(closedTypeName, new HarvestedGenericModel(
            OpenTypeOf: StripTypeArguments(named.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)) + "<" + new string(',', named.TypeArguments.Length - 1) + ">",
            ClosedTypeName: closedTypeName,
            TypeArguments: new EquatableArray<string>([.. named.TypeArguments.Select(static a => a.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))]),
            OpenFormatterName: GeneratedOpenFormatterName(named.OriginalDefinition)));

        // The definition's own harvest drops every member type that mentions a type parameter (List<T>), so the closed
        // graph below it (List<string>, Box<int> for Box<T> { Box<int> Inner }) is reachable only through this closed
        // form: walk its members with the type arguments substituted, the way the generated formatter resolves them.
        foreach (var memberType in ClosedSerializedMemberTypes(named))
        {
            HarvestSerializedType(memberType, compilation, userGenerics, builtIns, closureDepth + 1);
        }
        // a closed generic union root (union Option<T>(None, Some<T>)): its cases are what it serializes, and the
        // definition's parse drops every case that mentions a type parameter, so Some<int> is reachable only here
        foreach (var caseType in ClosedUnionCaseTypes(named))
        {
            HarvestSerializedType(caseType, compilation, userGenerics, builtIns, closureDepth + 1);
        }
    }

    // the [UnionTag] case types of a closed generic union root, substituted: the unbound typeof form resolves against
    // the closed type's own creation members (already substituted), a type-parameter name against its type arguments
    static IEnumerable<ITypeSymbol> ClosedUnionCaseTypes(INamedTypeSymbol closed)
    {
        foreach (var attribute in closed.GetAttributes())
        {
            if (!UnionParser.IsUnionTagAttribute(attribute.AttributeClass))
            {
                continue;
            }
            var caseType = UnionParser.ResolveCaseType(attribute, closed);
            if (caseType is ITypeParameterSymbol parameter)
            {
                if (parameter.Ordinal >= closed.TypeArguments.Length)
                {
                    continue;
                }
                caseType = closed.TypeArguments[parameter.Ordinal];
            }
            if (caseType is not null)
            {
                yield return caseType;
            }
        }
    }

    // the serialized member types of a closed generic [MessagePackObject] instantiation, substituted. The same public
    // instance property/field sweep the model parse uses (AllowPrivate is not supported on generic types), minus the
    // opted-out members; a member that the parse would refuse for another reason still harvests, which only adds a
    // registration the formatter never asks for.
    static IEnumerable<ITypeSymbol> ClosedSerializedMemberTypes(INamedTypeSymbol closed)
    {
        for (var current = closed; current is not null && current.SpecialType != SpecialType.System_Object && current.SpecialType != SpecialType.System_ValueType; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                switch (member)
                {
                    case IPropertySymbol { IsStatic: false, IsIndexer: false, IsImplicitlyDeclared: false, DeclaredAccessibility: Accessibility.Public } property
                        when property.GetMethod is { DeclaredAccessibility: Accessibility.Public } && !IsOptedOut(property):
                        yield return property.Type;
                        break;
                    case IFieldSymbol { IsStatic: false, IsConst: false, IsImplicitlyDeclared: false, DeclaredAccessibility: Accessibility.Public } field
                        when !HasAttribute(field, NonSerializedAttributeName) && !IsOptedOut(field):
                        yield return field.Type;
                        break;
                }
            }
        }

        static bool IsOptedOut(ISymbol symbol)
        {
            foreach (var attribute in MemberAttributes(symbol))
            {
                if (IsIgnoreAttribute(attribute.AttributeClass))
                {
                    return true;
                }
            }
            return false;
        }
    }

    static string? EnumVariantFormatter(INamedTypeSymbol enumType) => enumType.EnumUnderlyingType?.SpecialType switch
    {
        SpecialType.System_Byte => "EnumByteFormatter",
        SpecialType.System_SByte => "EnumSByteFormatter",
        SpecialType.System_Int16 => "EnumInt16Formatter",
        SpecialType.System_UInt16 => "EnumUInt16Formatter",
        SpecialType.System_Int32 => "EnumInt32Formatter",
        SpecialType.System_UInt32 => "EnumUInt32Formatter",
        SpecialType.System_Int64 => "EnumInt64Formatter",
        SpecialType.System_UInt64 => "EnumUInt64Formatter",
        _ => null,
    };

    static void HarvestArray(IArrayTypeSymbol array, Compilation compilation, Dictionary<string, HarvestedBuiltInModel> builtIns)
    {
        if (array.Rank > 4)
        {
            return; // rank > 4 is unsupported at runtime too (v3-parity)
        }
        if (ContainsTypeParameter(array) || !IsAccessibleToGeneratedCode(array, compilation))
        {
            return;
        }
        if (array.Rank == 1 && IsBuiltInPrimitiveElement(array.ElementType))
        {
            return; // byte[] (bin-format) and the codec-backed primitive arrays live in BuiltInFormatterFactory
        }
        var closedTypeName = array.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (array.Rank == 1 && array.ElementType is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumElement && EnumUnderlyingKeyword(enumElement) is { } underlying)
        {
            // the underlying-integer codec loop (GenericFormatterFactory's routing for TEnum[]);
            // the element's own Enum*Formatter was harvested above, which the formatter's Initialize consults to decide
            // between the codec and the per-element fallback
            if (!builtIns.ContainsKey(closedTypeName))
            {
                builtIns.Add(closedTypeName, new HarvestedBuiltInModel(
                    ClosedTypeName: closedTypeName,
                    FormatterConstruction: $"new global::MessagePack.Formatters.EnumArrayFormatter<TWriteBuffer, TReadBuffer, {enumElement.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {underlying}>()"));
            }
            return;
        }
        var formatterName = array.Rank switch
        {
            1 => "ArrayFormatter",
            2 => "TwoDimensionalArrayFormatter",
            3 => "ThreeDimensionalArrayFormatter",
            _ => "FourDimensionalArrayFormatter",
        };
        if (!builtIns.ContainsKey(closedTypeName))
        {
            builtIns.Add(closedTypeName, new HarvestedBuiltInModel(
                ClosedTypeName: closedTypeName,
                FormatterConstruction: $"new global::MessagePack.Formatters.{formatterName}<TWriteBuffer, TReadBuffer, {array.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()"));
        }
    }

    // the C# keyword of an enum's underlying type, as the TUnderlying argument of the enum collection formatters;
    // null for the (theoretical) non-integer backings
    static string? EnumUnderlyingKeyword(INamedTypeSymbol enumType) => enumType.EnumUnderlyingType?.SpecialType switch
    {
        SpecialType.System_Byte => "byte",
        SpecialType.System_SByte => "sbyte",
        SpecialType.System_Int16 => "short",
        SpecialType.System_UInt16 => "ushort",
        SpecialType.System_Int32 => "int",
        SpecialType.System_UInt32 => "uint",
        SpecialType.System_Int64 => "long",
        SpecialType.System_UInt64 => "ulong",
        _ => null,
    };

    // the compile-time mirror of GenericFormatterFactory's type table: an open BCL definition maps to the formatter the
    // corresponding factory constructs, so the harvested closed instantiation behaves identically to the JIT chain's
    // result
    static string? BuiltInGenericFormatterName(INamedTypeSymbol named)
    {
        var definition = named.OriginalDefinition;
        return definition.ContainingNamespace.ToDisplayString() switch
        {
            "System.Collections.Generic" => definition.MetadataName switch
            {
                "List`1" => "ListFormatter",
                "Dictionary`2" => "DictionaryFormatter",
                "LinkedList`1" => "LinkedListFormatter",
                "Queue`1" => "QueueFormatter",
                "Stack`1" => "StackFormatter",
                "HashSet`1" => "HashSetFormatter",
                "SortedSet`1" => "SortedSetFormatter",
                "IEnumerable`1" => "InterfaceEnumerableFormatter",
                "ICollection`1" => "InterfaceCollectionFormatter",
                "IList`1" => "InterfaceListFormatter",
                "IReadOnlyCollection`1" => "InterfaceReadOnlyCollectionFormatter",
                "IReadOnlyList`1" => "InterfaceReadOnlyListFormatter",
                "ISet`1" => "InterfaceSetFormatter",
                "IReadOnlySet`1" => "InterfaceReadOnlySetFormatter",
                "IDictionary`2" => "InterfaceDictionaryFormatter",
                "IReadOnlyDictionary`2" => "InterfaceReadOnlyDictionaryFormatter",
                "SortedList`2" => "SortedListFormatter",
                "SortedDictionary`2" => "SortedDictionaryFormatter",
                "OrderedDictionary`2" => "OrderedDictionaryFormatter",
                "PriorityQueue`2" => "PriorityQueueFormatter",
                "KeyValuePair`2" => "KeyValuePairFormatter",
                _ => null,
            },
            "System.Collections.ObjectModel" => definition.MetadataName switch
            {
                "ReadOnlyCollection`1" => "ReadOnlyCollectionFormatter",
                "ObservableCollection`1" => "ObservableCollectionFormatter",
                "ReadOnlyObservableCollection`1" => "ReadOnlyObservableCollectionFormatter",
                "ReadOnlyDictionary`2" => "ReadOnlyDictionaryFormatter",
                "ReadOnlySet`1" => "ReadOnlySetFormatter",
                _ => null,
            },
            "System.Collections.Concurrent" => definition.MetadataName switch
            {
                "ConcurrentQueue`1" => "ConcurrentQueueFormatter",
                "ConcurrentStack`1" => "ConcurrentStackFormatter",
                "ConcurrentBag`1" => "ConcurrentBagFormatter",
                "ConcurrentDictionary`2" => "ConcurrentDictionaryFormatter",
                _ => null,
            },
            "System.Collections.Immutable" => definition.MetadataName switch
            {
                "ImmutableArray`1" => "ImmutableArrayFormatter",
                "ImmutableList`1" => "ImmutableListFormatter",
                "ImmutableHashSet`1" => "ImmutableHashSetFormatter",
                "ImmutableSortedSet`1" => "ImmutableSortedSetFormatter",
                "ImmutableQueue`1" => "ImmutableQueueFormatter",
                "ImmutableStack`1" => "ImmutableStackFormatter",
                "ImmutableDictionary`2" => "ImmutableDictionaryFormatter",
                "ImmutableSortedDictionary`2" => "ImmutableSortedDictionaryFormatter",
                "IImmutableList`1" => "InterfaceImmutableListFormatter",
                "IImmutableSet`1" => "InterfaceImmutableSetFormatter",
                "IImmutableQueue`1" => "InterfaceImmutableQueueFormatter",
                "IImmutableStack`1" => "InterfaceImmutableStackFormatter",
                "IImmutableDictionary`2" => "InterfaceImmutableDictionaryFormatter",
                _ => null,
            },
            "System.Collections.Frozen" => definition.MetadataName switch
            {
                "FrozenDictionary`2" => "FrozenDictionaryFormatter",
                "FrozenSet`1" => "FrozenSetFormatter",
                _ => null,
            },
            "System" => definition.MetadataName switch
            {
                "ArraySegment`1" => "ArraySegmentFormatter",
                "Memory`1" => "MemoryFormatter",
                "ReadOnlyMemory`1" => "ReadOnlyMemoryFormatter",
                "Lazy`1" => "LazyFormatter",
                "Tuple`1" or "Tuple`2" or "Tuple`3" or "Tuple`4" or "Tuple`5" or "Tuple`6" or "Tuple`7" or "Tuple`8" => "TupleFormatter",
                "ValueTuple`1" or "ValueTuple`2" or "ValueTuple`3" or "ValueTuple`4" or "ValueTuple`5" or "ValueTuple`6" or "ValueTuple`7" or "ValueTuple`8" => "ValueTupleFormatter",
                _ => null,
            },
            "System.Buffers" => definition.MetadataName == "ReadOnlySequence`1" ? "ReadOnlySequenceFormatter" : null,
            "System.Linq" => definition.MetadataName switch
            {
                "IGrouping`2" => "InterfaceGroupingFormatter",
                "ILookup`2" => "InterfaceLookupFormatter",
                _ => null,
            },
            _ => null,
        };
    }

    // the closed instantiations BuiltInFormatterFactory serves with specialized formatters (bin-format byte containers,
    // codec-backed primitive element loops)
    static bool IsBuiltInServedInstantiation(INamedTypeSymbol named)
    {
        if (named.TypeArguments.Length != 1 || named.ContainingNamespace.ToDisplayString() is not ("System" or "System.Buffers" or "System.Collections.Generic"))
        {
            return false;
        }
        return named.OriginalDefinition.MetadataName switch
        {
            "List`1" or "Memory`1" or "ReadOnlyMemory`1" or "ArraySegment`1" => IsBuiltInPrimitiveElement(named.TypeArguments[0]),
            "ReadOnlySequence`1" => named.TypeArguments[0].SpecialType == SpecialType.System_Byte,
            _ => false,
        };
    }

    static bool IsBuiltInPrimitiveElement(ITypeSymbol element) => element.SpecialType is
        SpecialType.System_Byte or SpecialType.System_SByte
        or SpecialType.System_Int16 or SpecialType.System_UInt16
        or SpecialType.System_Int32 or SpecialType.System_UInt32
        or SpecialType.System_Int64 or SpecialType.System_UInt64
        or SpecialType.System_Single or SpecialType.System_Double
        or SpecialType.System_Boolean;

    static bool FormatterTypeExists(Compilation compilation, string formatterName, int valueArity) =>
        compilation.GetTypeByMetadataName("MessagePack.Formatters." + formatterName + "`" + (2 + valueArity)) is not null;

    // these formatters preserve the collection's comparer semantics through an explicit constructor parameter (no
    // parameterless constructor exists on them)
    static bool FormatterTakesComparer(string formatterName) => formatterName is
        "HashSetFormatter" or "SortedSetFormatter" or "ReadOnlySetFormatter"
        or "InterfaceSetFormatter" or "InterfaceReadOnlySetFormatter"
        or "DictionaryFormatter" or "InterfaceDictionaryFormatter" or "InterfaceReadOnlyDictionaryFormatter"
        or "ReadOnlyDictionaryFormatter" or "ConcurrentDictionaryFormatter" or "OrderedDictionaryFormatter"
        or "ImmutableHashSetFormatter" or "ImmutableDictionaryFormatter"
        or "InterfaceImmutableSetFormatter" or "InterfaceImmutableDictionaryFormatter"
        or "FrozenDictionaryFormatter" or "FrozenSetFormatter"
        or "InterfaceLookupFormatter";

    // mirrors GenericFormatterFactory's collection catch-all (v3 DynamicGenericResolver's inherited-type rules): a
    // public parameterless constructor unlocks the Add-based formatters over IDictionary<K,V>/ICollection<T>/the
    // non-generic IList/IDictionary views, and a public single-parameter collection-accepting constructor unlocks the
    // construct-from-intermediate formatters (IReadOnlyDictionary<K,V>, IEnumerable<T>);
    // types another tier claims (SpecialType scalars, [MessagePackObject],
    // type-level [MessagePackFormatter], ExpandoObject) stay off the registry
    static void HarvestCollectionCatchAll(INamedTypeSymbol named, Compilation compilation, Dictionary<string, HarvestedGenericModel> userGenerics, Dictionary<string, HarvestedBuiltInModel> builtIns, int closureDepth = 0)
    {
        if (ContainsTypeParameter(named) || !IsAccessibleToGeneratedCode(named, compilation))
        {
            return;
        }
        var closedTypeName = named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (builtIns.ContainsKey(closedTypeName))
        {
            return;
        }
        if (SelectCollectionShape(named, compilation) is not { } shape)
        {
            return;
        }
        if (shape.Arguments.IsEmpty)
        {
            // a non-generic view (IList / IDictionary): nothing of its own to close
            builtIns.Add(closedTypeName, new HarvestedBuiltInModel(closedTypeName, $"new global::MessagePack.Formatters.{shape.FormatterName}<TWriteBuffer, TReadBuffer, {closedTypeName}>()"));
            return;
        }
        EmitCollectionHarvest(named, compilation, userGenerics, builtIns, closedTypeName, shape.Arguments, shape.FormatterName, closureDepth);
    }

    // the collection view the catch-all serves a type through, and the type arguments it closes (empty for a
    // non-generic view); null when the shape is not served. Usable on an open definition too: the expansion analysis
    // asks exactly what the harvest would close under it
    readonly record struct CollectionShape(System.Collections.Immutable.ImmutableArray<ITypeSymbol> Arguments, string FormatterName);

    static CollectionShape? SelectCollectionShape(INamedTypeSymbol named, Compilation compilation)
    {
        if (named.TypeKind != TypeKind.Class || named.IsAbstract || named.SpecialType != SpecialType.None)
        {
            return null;
        }
        if (HasMessagePackObjectAttribute(named.OriginalDefinition) || HasAttribute(named.OriginalDefinition, FormatterAttributeName))
        {
            return null;
        }
        if (named.ToDisplayString() == "System.Dynamic.ExpandoObject")
        {
            return null; // deliberately unserved by default (the deprecated quadratic-Add path)
        }
        // the Add-based formatters construct through `new()`, which a type with `required` members refuses unless its
        // parameterless constructor carries [SetsRequiredMembers] (CS9040 in the generated code otherwise)
        var hasDefaultConstructor = named.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public
            && (!HasRequiredMembers(named) || HasAttribute(c, SetsRequiredMembersAttributeName)));
        INamedTypeSymbol? dictionaryInterface = null;
        INamedTypeSymbol? collectionInterface = null;
        INamedTypeSymbol? readOnlyDictionaryInterface = null;
        List<INamedTypeSymbol>? enumerableInterfaces = null;
        var nonGenericList = false;
        var nonGenericDictionary = false;
        foreach (var iface in named.AllInterfaces)
        {
            var ns = iface.ContainingNamespace.ToDisplayString();
            if (iface.IsGenericType && ns == "System.Collections.Generic")
            {
                if (iface.OriginalDefinition.MetadataName == "IDictionary`2" && hasDefaultConstructor)
                {
                    dictionaryInterface = iface;
                    break; // the dictionary view always wins (runtime keeps scanning for it too)
                }
                if (collectionInterface is null && iface.OriginalDefinition.MetadataName == "ICollection`1")
                {
                    collectionInterface = iface;
                }
                if (readOnlyDictionaryInterface is null && iface.OriginalDefinition.MetadataName == "IReadOnlyDictionary`2")
                {
                    readOnlyDictionaryInterface = iface;
                }
                if (iface.OriginalDefinition.MetadataName == "IEnumerable`1")
                {
                    (enumerableInterfaces ??= new()).Add(iface);
                }
            }
            else if (ns == "System.Collections")
            {
                nonGenericList |= iface.MetadataName == "IList";
                nonGenericDictionary |= iface.MetadataName == "IDictionary";
            }
        }

        // runtime priority: IDictionary+new -> IReadOnlyDictionary+ctor ->
        // ICollection+new -> non-generic views+new -> IEnumerable<T>+ctor
        if (dictionaryInterface is null && readOnlyDictionaryInterface is not null
            && compilation.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2") is { } dictionaryDefinition
            && compilation.GetTypeByMetadataName("System.Collections.Generic.KeyValuePair`2") is { } kvpDefinition
            && compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1") is { } enumerableDefinition)
        {
            var args = readOnlyDictionaryInterface.TypeArguments.ToArray();
            ITypeSymbol[] acceptable =
            [
                dictionaryDefinition.Construct(args),
                readOnlyDictionaryInterface,
                enumerableDefinition.Construct(kvpDefinition.Construct(args)),
            ];
            if (HasCollectionAcceptingConstructor(named, acceptable, compilation))
            {
                return new CollectionShape(readOnlyDictionaryInterface.TypeArguments, "GenericReadOnlyDictionaryFormatter");
            }
        }
        if (hasDefaultConstructor && (dictionaryInterface is not null || collectionInterface is not null))
        {
            return new CollectionShape(
                (dictionaryInterface ?? collectionInterface)!.TypeArguments,
                dictionaryInterface is not null ? "GenericDictionaryFormatter" : "GenericCollectionFormatter");
        }
        if (hasDefaultConstructor && nonGenericList)
        {
            return new CollectionShape(System.Collections.Immutable.ImmutableArray<ITypeSymbol>.Empty, "NonGenericListFormatter");
        }
        if (hasDefaultConstructor && nonGenericDictionary)
        {
            return new CollectionShape(System.Collections.Immutable.ImmutableArray<ITypeSymbol>.Empty, "NonGenericDictionaryFormatter");
        }
        if (enumerableInterfaces is not null)
        {
            foreach (var iface in enumerableInterfaces)
            {
                if (HasCollectionAcceptingConstructor(named, [iface], compilation))
                {
                    return new CollectionShape(iface.TypeArguments, "GenericEnumerableFormatter");
                }
            }
        }
        return null;
    }

    static void EmitCollectionHarvest(INamedTypeSymbol named, Compilation compilation, Dictionary<string, HarvestedGenericModel> userGenerics, Dictionary<string, HarvestedBuiltInModel> builtIns, string closedTypeName, System.Collections.Immutable.ImmutableArray<ITypeSymbol> arguments, string formatterName, int closureDepth)
    {
        if (!arguments.All(a => IsAccessibleToGeneratedCode(a, compilation)))
        {
            return;
        }
        var argumentNames = string.Join(", ", arguments.Select(static a => a.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        var construction = $"new global::MessagePack.Formatters.{formatterName}<TWriteBuffer, TReadBuffer, {argumentNames}, {closedTypeName}>()";
        // register before recursing: a self-referential shape (class W : ICollection<W>)
        // must hit the ContainsKey guard on re-entry
        builtIns.Add(closedTypeName, new HarvestedBuiltInModel(closedTypeName, construction));
        foreach (var argument in arguments)
        {
            HarvestSerializedType(argument, compilation, userGenerics, builtIns, closureDepth + 1);
        }
    }

    // a public single-parameter constructor whose parameter accepts one of the given collection views (the runtime
    // probe's Roslyn twin)
    internal static bool HasCollectionAcceptingConstructor(INamedTypeSymbol type, ITypeSymbol[] acceptableArguments, Compilation compilation)
    {
        foreach (var constructor in type.InstanceConstructors)
        {
            if (constructor.DeclaredAccessibility != Accessibility.Public || constructor.Parameters.Length != 1)
            {
                continue;
            }
            foreach (var argument in acceptableArguments)
            {
                var conversion = compilation.ClassifyConversion(argument, constructor.Parameters[0].Type);
                if (conversion.IsIdentity || (conversion.IsImplicit && conversion.IsReference))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // the harvest's accessibility scope: the assembly (the generated factory lives in it), or, during the AllowPrivate
    // second pass, the partial type whose nested factory registers what only it can name
    [ThreadStatic]
    static INamedTypeSymbol? privateAccessScope;

    internal static bool IsAccessibleToGeneratedCode(ITypeSymbol type, Compilation compilation) => type switch
    {
        IArrayTypeSymbol array => IsAccessibleToGeneratedCode(array.ElementType, compilation),
        INamedTypeSymbol named => (privateAccessScope is { } scope ? compilation.IsSymbolAccessibleWithin(named, scope) : compilation.IsSymbolAccessibleWithin(named, compilation.Assembly))
            && named.TypeArguments.All(argument => argument is ITypeParameterSymbol || IsAccessibleToGeneratedCode(argument, compilation)),
        _ => true,
    };

    // a type nested in a generic (Outer<T>.Inner, Outer<T>.E) mentions T through its containing type, not its own
    // arguments, so the walk climbs ContainingType too
    internal static bool ContainsTypeParameter(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol => true,
        IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter)
            || (named.ContainingType is { } containing && ContainsTypeParameter(containing)),
        _ => false,
    };

    internal static bool HasMessagePackObjectAttribute(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            for (var attributeType = attribute.AttributeClass; attributeType is not null; attributeType = attributeType.BaseType)
            {
                if (attributeType.ToDisplayString() == MessagePackObjectAttributeName)
                {
                    return true;
                }
            }
        }
        return false;
    }

    static ParseResult Invalid(List<DiagnosticInfo> diagnostics) =>
        new(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));

    internal static bool ReadNamedBool(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is true)
            {
                return true;
            }
        }
        return false;
    }

    // a member, type or type parameter may legally be named after a reserved keyword (`@event`, `@class`);
    // a symbol's Name is the bare spelling, so every emitted identifier restores the escape.
    // Messages and map keys keep the bare name. (ToDisplayString with FullyQualifiedFormat escapes on its own;
    // this is for raw Names)
    // The generated formatter is generic over the model's own type parameters, whose names it cannot change (the
    // member types and where-clauses spell them): a name equal to one of its own buffer parameters, to a type the
    // generated file imports unqualified (SerializerFoundation, MessagePack, System.Buffers.Binary,
    // System.Runtime.CompilerServices) or to a static member of MessagePackPrimitives (used unqualified through
    // `using static`) would shadow it inside the formatter and break the generated code.
    internal static bool CollidesWithGeneratedCode(string name, Compilation compilation)
    {
        if (name is "TWriteBuffer" or "TReadBuffer" || GeneratedNames.Contains(name))
        {
            return true;
        }
        foreach (var namespaceName in GeneratedImports)
        {
            if (FindNamespace(compilation, namespaceName) is { } ns && ns.GetTypeMembers(name).Length > 0)
            {
                return true;
            }
        }
        return compilation.GetTypeByMetadataName("MessagePack.MessagePackPrimitives") is { } primitives && primitives.GetMembers(name).Length > 0;
    }

    static readonly string[] GeneratedImports = ["SerializerFoundation", "MessagePack", "System.Buffers.Binary", "System.Runtime.CompilerServices"];

    // members the generated formatter declares (a type parameter of the same name is CS0102) and the parameters and
    // locals its methods use (CS0136 / a different meaning of the name)
    static readonly HashSet<string> GeneratedNames = new(StringComparer.Ordinal)
    {
        "Initialize", "Serialize", "Deserialize", "DeserializePopulate", "DeserializeConstruct", "ThrowMissingRequiredMember", "ThrowNullValueForNonNullableMember",
        "declaredKeysUtf8", "validateRequired", "validateNull", "MessagePackRegisterPrivateTypes", "MessagePackPrivateTypesFactory", "GeneratedMessagePackFormatter", "Instance",
        "buffer", "state", "value", "resolver", "result", "constructed", "unknownMembers", "seenMembers", "heldValue", "length", "count", "key", "tag", "type", "i",
        "arrayCount", "byteCount", "mapCount", "envelopeCount", "unknownReplayedCount", "circularReferenceId", "circularReferenceDefinitionId", "member", "runtimeType", "d", "w", "v",
    };

    // f{id}, has_{id}, v_{id}: the per-member formatter fields and locals (id = the member's name, or its
    // shadow-qualified form), known only once the members are
    internal static bool CollidesWithGeneratedSlot(string name, IEnumerable<string> memberIds)
    {
        foreach (var id in memberIds)
        {
            if (name == "f" + id || name == "v_" + id || name == "has_" + id)
            {
                return true;
            }
        }
        return false;
    }

    static INamespaceSymbol? FindNamespace(Compilation compilation, string dottedName)
    {
        INamespaceSymbol current = compilation.GlobalNamespace;
        foreach (var part in dottedName.Split('.'))
        {
            INamespaceSymbol? next = null;
            foreach (var member in current.GetNamespaceMembers())
            {
                if (member.Name == part)
                {
                    next = member;
                    break;
                }
            }
            if (next is null)
            {
                return null;
            }
            current = next;
        }
        return current;
    }

    static bool IsRequiredMember(ISymbol symbol) => symbol is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true };

    // The expression a generated `new` assigns to a `required` member it never reads. The object initializer
    // overrides the member's own initializer, so a constant one is re-emitted as it is (the reflection tier, which
    // runs the constructor and the initializers, keeps it too); anything else cannot be spelled here and is reported.
    static string RequiredDefaultExpression(ISymbol member, Compilation compilation, string typeName, List<DiagnosticInfo> diagnostics)
    {
        if (member.DeclaringSyntaxReferences.Length == 0)
        {
            // declared in another assembly (a required member of a metadata base class): whether it has an initializer
            // is not visible here, so the assignment is default and the author is told
            diagnostics.Add(new DiagnosticInfo(Diagnostics.RequiredInitializerNotPreserved, $"'{typeName}.{member.Name}' is a required member the generated formatter never reads, declared in another assembly, so the generator cannot see whether it has an initializer: the generated object initializer (which the compiler demands for every required member) assigns default. Use a parameterless constructor marked [SetsRequiredMembers] if the initializer matters.", LocationInfo.From(member)));
            return "default!";
        }
        EqualsValueClauseSyntax? initializer = null;
        foreach (var reference in member.DeclaringSyntaxReferences)
        {
            initializer = reference.GetSyntax() switch
            {
                PropertyDeclarationSyntax property => property.Initializer,
                VariableDeclaratorSyntax variable => variable.Initializer,
                _ => null,
            };
            if (initializer is not null)
            {
                break;
            }
        }
        if (initializer is null)
        {
            return "default!";
        }
        var memberType = member is IPropertySymbol property1 ? property1.Type : ((IFieldSymbol)member).Type;
        var semanticModel = compilation.GetSemanticModel(initializer.SyntaxTree);
        var constant = semanticModel.GetConstantValue(initializer.Value);
        // the constant's own type, not the member's: `object O = DayOfWeek.Friday` must stay a boxed enum, not (object)5
        var constantType = semanticModel.GetTypeInfo(initializer.Value).Type ?? memberType;
        if (constant.HasValue)
        {
            switch (constant.Value)
            {
                case null:
                    return "default!";
                case bool b:
                    return b ? "true" : "false";
                case string or char:
                    return SymbolDisplay.FormatPrimitive(constant.Value, quoteStrings: true, useHexadecimalNumbers: false);
                case float f:
                    return float.IsNaN(f) ? "float.NaN" : float.IsPositiveInfinity(f) ? "float.PositiveInfinity" : float.IsNegativeInfinity(f) ? "float.NegativeInfinity" : f.ToString("R", CultureInfo.InvariantCulture) + "f";
                case double d:
                    return double.IsNaN(d) ? "double.NaN" : double.IsPositiveInfinity(d) ? "double.PositiveInfinity" : double.IsNegativeInfinity(d) ? "double.NegativeInfinity" : d.ToString("R", CultureInfo.InvariantCulture) + "d";
                case decimal m:
                    return m.ToString(CultureInfo.InvariantCulture) + "m";
                default:
                    // integers, and enums (whose constant is the underlying value): the constant's type names the target
                    return $"({constantType.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){SymbolDisplay.FormatPrimitive(constant.Value, quoteStrings: false, useHexadecimalNumbers: false)}";
            }
        }
        diagnostics.Add(new DiagnosticInfo(Diagnostics.RequiredInitializerNotPreserved, $"'{typeName}.{member.Name}' is a required member the generated formatter never reads, and its initializer is not a compile-time constant: the generated object initializer (which the compiler demands for every required member) assigns default instead. Use a constant initializer, a parameterless constructor marked [SetsRequiredMembers], or drop `required`.", LocationInfo.From(member)));
        return "default!";
    }

    // A derived KeyAttribute is readable at compile time only when its constructor hands its first parameter to
    // KeyAttribute's constructor as it is (`: base(key)`, or a primary constructor `: KeyAttribute(key)`): the
    // reflection tier reads the key the instance ends up with, which a transforming constructor
    // (`: base("prefix_" + key)`) would make differ from the attribute argument the generator sees. Requires the
    // attribute to derive from KeyAttribute directly and to be declared in this compilation (syntax is needed).
    static bool ForwardsKeyToBase(AttributeData attribute)
    {
        if (attribute.AttributeClass is not { BaseType: { } baseType } attributeClass
            || baseType.ToDisplayString() != KeyAttributeName
            || attribute.AttributeConstructor is not { Parameters.Length: > 0 } constructor)
        {
            return false;
        }
        var parameterName = constructor.Parameters[0].Name;
        foreach (var reference in constructor.DeclaringSyntaxReferences)
        {
            switch (reference.GetSyntax())
            {
                case ConstructorDeclarationSyntax { Initializer: { } initializer } declaration
                    when initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword)
                        && initializer.ArgumentList.Arguments.Count == 1
                        && initializer.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax { Identifier.ValueText: var forwarded }
                        && forwarded == parameterName:
                    return true;
                case TypeDeclarationSyntax { BaseList.Types: var baseTypes }:
                    // primary constructor (the constructor symbol's syntax is the type declaration itself): the base
                    // type's own argument list is the base call
                    foreach (var type in baseTypes)
                    {
                        if (type is PrimaryConstructorBaseTypeSyntax { ArgumentList.Arguments: { Count: 1 } arguments }
                            && arguments[0].Expression is IdentifierNameSyntax { Identifier.ValueText: var primaryForwarded }
                            && primaryForwarded == parameterName)
                        {
                            return true;
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }
        return false; // declared elsewhere: nothing to inspect
    }

    // [Key] or an attribute derived from it (not sealed; the reflection tier reads IntKey / StringKey off any instance)
    internal static bool IsKeyAttribute(INamedTypeSymbol? attributeClass)
    {
        for (var current = attributeClass; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == KeyAttributeName)
            {
                return true;
            }
        }
        return false;
    }

    // [IgnoreMember] (or an attribute derived from it: the attribute is not sealed, and the reflection tier honours a
    // derived one) and [IgnoreDataMember]
    internal static bool IsIgnoreAttribute(INamedTypeSymbol? attributeClass)
    {
        for (var current = attributeClass; current is not null; current = current.BaseType)
        {
            var name = current.ToDisplayString();
            if (name == IgnoreMemberAttributeName || name == IgnoreDataMemberAttributeName)
            {
                return true;
            }
        }
        return false;
    }

    static bool HasRequiredMembers(INamedTypeSymbol type)
    {
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (IsRequiredMember(member))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // true when a type between the leaf and the member's declarer declares an instance field or property of the same
    // name that the generated code can see, so that `value.Name` on the leaf binds that declaration rather than the
    // member (C# member lookup skips inaccessible declarations, so only a reachable one hides)
    static bool IsHiddenInDerivedType(INamedTypeSymbol leaf, ISymbol member, bool allowPrivate)
    {
        for (var derived = leaf; derived is not null && !SymbolEqualityComparer.Default.Equals(derived, member.ContainingType); derived = derived.BaseType)
        {
            foreach (var candidate in derived.GetMembers(member.Name))
            {
                // whatever the declaration is: a static member (CS0176) or a method (a method group) hides as surely
                // as an instance field or property
                if (candidate is IPropertySymbol or IFieldSymbol or IMethodSymbol { MethodKind: MethodKind.Ordinary } or IEventSymbol or INamedTypeSymbol
                    && IsVisibleToGeneratedCode(candidate, leaf, allowPrivate))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // the generated formatter lives in the assembly (AllowPrivate: nested in the leaf type)
    static bool IsVisibleToGeneratedCode(ISymbol member, INamedTypeSymbol leaf, bool allowPrivate) => member.DeclaredAccessibility switch
    {
        Accessibility.Public => true,
        Accessibility.Internal or Accessibility.ProtectedOrInternal => SymbolEqualityComparer.Default.Equals(member.ContainingAssembly, leaf.ContainingAssembly),
        Accessibility.Private => allowPrivate && SymbolEqualityComparer.Default.Equals(member.ContainingType, leaf),
        _ => allowPrivate, // protected, private protected: reachable from the nested formatter through the leaf
    };

    // the generated call passes locals by value: a ref / out / in parameter would need the modifier at the call site
    // (CS1620), so such a constructor is never the serialization constructor
    static bool HasOnlyValueParameters(IMethodSymbol constructor)
    {
        foreach (var parameter in constructor.Parameters)
        {
            if (parameter.RefKind != RefKind.None)
            {
                return false;
            }
        }
        return true;
    }

    // true when `type` is declared inside `container` (at any depth): the nested generated formatter and the partial
    // type's own members can name it even when the rest of the assembly cannot
    internal static bool HasAttributeClass(INamedTypeSymbol attributeClass, string fullName)
    {
        for (var current = attributeClass; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == fullName)
            {
                return true;
            }
        }
        return false;
    }

    static bool IsNestedWithin(INamedTypeSymbol type, INamedTypeSymbol container)
    {
        for (var current = type.ContainingType; current is not null; current = current.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, container.OriginalDefinition))
            {
                return true;
            }
        }
        return false;
    }

    // The private-scope factory (ObjectEmitter's MessagePackPrivateTypesFactory) constructs a harvested closure's
    // formatter by name, so the name is given only when this compilation generates that formatter: the generated
    // factory matches harvested closures against the generated models, this is the same gate decided from the
    // definition (a suppressed, AllowPrivate (unsupported on generics), inaccessible or file-local definition has
    // no open formatter to close).
    static string GeneratedOpenFormatterName(INamedTypeSymbol definition)
    {
        AttributeData? objectAttribute = null;
        foreach (var attribute in definition.GetAttributes())
        {
            for (var attributeType = attribute.AttributeClass; attributeType is not null; attributeType = attributeType.BaseType)
            {
                if (attributeType.ToDisplayString() == MessagePackObjectAttributeName)
                {
                    objectAttribute = attribute;
                    break;
                }
            }
            if (objectAttribute is not null)
            {
                break;
            }
        }
        if (objectAttribute is null
            || ReadNamedBool(objectAttribute, "SuppressSourceGeneration")
            || ReadNamedBool(objectAttribute, "AllowPrivate")
            || HasTypeLevelFormatterAttribute(definition) // served by its attributed factory, no object formatter
            || IsFileLocal(definition))
        {
            return "";
        }
        for (var accessible = definition; accessible is not null; accessible = accessible.ContainingType)
        {
            if (accessible.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                return "";
            }
        }
        return Sanitize(definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)) + "Formatter";
    }

    // [MessagePackFormatter(typeof(...))] or [MessagePackFormatter<TFactory>] on the type itself, the two exact forms
    // the type-level registration pipeline matches
    internal static bool HasTypeLevelFormatterAttribute(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass is { } attributeClass
                && (attributeClass.ToDisplayString() == FormatterAttributeName
                    || (attributeClass.IsGenericType && attributeClass.OriginalDefinition.ToDisplayString() == FormatterAttributeName + "<TFactory>")))
            {
                return true;
            }
        }
        return false;
    }

    // `file class X` (C# 11): the modifier is read off the syntax, the symbol API of the referenced Roslyn predates it
    internal static bool IsFileLocal(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            foreach (var reference in current.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is BaseTypeDeclarationSyntax declaration)
                {
                    foreach (var modifier in declaration.Modifiers)
                    {
                        if (modifier.ValueText == "file")
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    internal static bool IsNestedInGenericType(INamedTypeSymbol type)
    {
        for (var container = type.ContainingType; container is not null; container = container.ContainingType)
        {
            if (container.IsGenericType)
            {
                return true;
            }
        }
        return false;
    }

    internal static string Identifier(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

    internal static bool HasAttribute(ISymbol symbol, string fullName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            // walk the attribute's base chain so a derived annotation still matches
            for (var attributeType = attribute.AttributeClass; attributeType is not null; attributeType = attributeType.BaseType)
            {
                if (attributeType.ToDisplayString() == fullName)
                {
                    return true;
                }
            }
        }
        return false;
    }

    static bool IsConstructorAccessible(IMethodSymbol constructor, bool allowPrivate, bool parameterless)
    {
        if (allowPrivate)
        {
            return true; // the nested formatter reaches everything
        }
        // parity with ReflectionObjectFormatter: parameterized matching considers public constructors only;
        // `new T()` additionally accepts internal (same assembly)
        return constructor.DeclaredAccessibility == Accessibility.Public
            || (parameterless && constructor.DeclaredAccessibility == Accessibility.Internal);
    }

    // the runtime tiers (v3 and the reflection formatter) bind a member to a parameter through Type.IsAssignableFrom:
    // identity, reference (derived to base, class to interface) and boxing conversions - never numeric or user-defined
    // ones, and never T to Nullable<T>
    static bool IsParameterAssignable(Compilation compilation, ITypeSymbol memberType, ITypeSymbol parameterType)
    {
        if (SymbolEqualityComparer.Default.Equals(memberType, parameterType))
        {
            return true;
        }
        var conversion = compilation.ClassifyConversion(memberType, parameterType);
        return conversion.IsImplicit && !conversion.IsUserDefined && (conversion.IsIdentity || conversion.IsReference || conversion.IsBoxing);
    }

    // generated code reads every member and writes the ones with a usable setter,
    // from a formatter nested in `type`: the compiler's own accessibility rule (private,
    // private protected and internal across assemblies, protected through the derived type)
    // decides what it can reach, accessor by accessor. `throughType` is the static type of the emitted access
    // expression: `value` (the type itself), or the declaring base type for a shadowed base member,
    // which the emitter reaches as `((Base)value).Member` - a protected member is accessible through the derived type
    // but not through that base cast (CS1540)
    static bool IsAccessibleFromGeneratedFormatter(Compilation compilation, ISymbol symbol, SetterKind setter, INamedTypeSymbol type, ITypeSymbol throughType)
    {
        if (!compilation.IsSymbolAccessibleWithin(symbol, type, throughType))
        {
            return false;
        }
        return symbol is not IPropertySymbol property
            || ((property.GetMethod is null || compilation.IsSymbolAccessibleWithin(property.GetMethod, type, throughType))
                && (setter == SetterKind.None || property.SetMethod is null || compilation.IsSymbolAccessibleWithin(property.SetMethod, type, throughType)));
    }

    // the first base type (below the declared one, above object) declared in another assembly
    static INamedTypeSymbol? ExternalBase(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null && current.SpecialType != SpecialType.System_Object && current.SpecialType != SpecialType.System_ValueType; current = current.BaseType)
        {
            if (!SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, type.ContainingAssembly))
            {
                return current;
            }
        }
        return null;
    }

    // v3's DynamicObjectResolver rule (issue #1016): a parameter binds the member whose string key matches its
    // name, and only when no key matches, the member whose declared name matches; both ordinal-ignore-case with
    // an assignable member type. Int-keyed entries carry no string key, so only the name pass applies to them.
    // (v3's mpc matched declared names only, so a [Key("x_val")] member bound by an x_val parameter worked at
    // runtime but not under the generator; v4 aligns both tiers on the runtime rule.)
    static bool TryMatchConstructor(
        Compilation compilation,
        IMethodSymbol constructor,
        List<(string Name, ITypeSymbol Type, SetterKind Setter, int IntKey, string StringKey, bool RequiredModifier, LocationInfo? Location, CustomFormatterModel? Custom, string? ShadowedBaseType, bool ExplicitKey, bool Writable)> entries,
        int[] parameterIndexByEntry)
    {
        var parameters = constructor.Parameters;
        for (int p = 0; p < parameters.Length; p++)
        {
            var found = FindParameterEntry(compilation, entries, parameters[p], byStringKey: true);
            if (found < 0)
            {
                found = FindParameterEntry(compilation, entries, parameters[p], byStringKey: false);
            }
            // an already-claimed entry (two parameters differing only by case, or one parameter's key
            // being another's name) would emit a constructor call with a hole; reject the whole candidate instead
            if (found < 0 || parameterIndexByEntry[found] >= 0)
            {
                return false;
            }
            parameterIndexByEntry[found] = p;
        }
        return true;
    }

    static int FindParameterEntry(
        Compilation compilation,
        List<(string Name, ITypeSymbol Type, SetterKind Setter, int IntKey, string StringKey, bool RequiredModifier, LocationInfo? Location, CustomFormatterModel? Custom, string? ShadowedBaseType, bool ExplicitKey, bool Writable)> entries,
        IParameterSymbol parameter,
        bool byStringKey)
    {
        for (int e = 0; e < entries.Count; e++)
        {
            // a shadowed base declaration never binds a parameter: the name belongs to the derived member,
            // and constructors assign their own declarations
            if (entries[e].ShadowedBaseType is not null)
            {
                continue;
            }
            var candidate = byStringKey ? entries[e].StringKey : entries[e].Name;
            if (candidate.Length != 0
                && string.Equals(candidate, parameter.Name, StringComparison.OrdinalIgnoreCase)
                && IsParameterAssignable(compilation, entries[e].Type, parameter.Type))
            {
                return e;
            }
        }
        return -1;
    }

    // ---- member-level [MessagePackFormatter] binding ----------------------------------- The attribute names either a
    // MessagePackFormatterFactory (closed, args go to its constructor, the emitter appends the CreateFormatter call)
    // or a formatter as an unbound generic over the buffer pair (typeof(MyFormatter<,>)).
    // Arguments bind positionally to a constructor; a string argument whose target parameter is not string is bound as
    // a dotted static-member expression at the attribute site ("StringComparer.OrdinalIgnoreCase")
    // and emitted as direct code, this form exists only here: the runtime tier rejects it and points at the source
    // generator. memberType is the serialized value's (the member's) type.

    internal static bool TryBuildCustomFormatter(
        AttributeData attribute,
        ITypeSymbol memberType,
        string subject,
        LocationInfo? location,
        GeneratorAttributeSyntaxContext context,
        List<DiagnosticInfo> diagnostics,
        out CustomFormatterModel? custom,
        INamedTypeSymbol? accessScope = null)
    {
        custom = null;
        var member = subject;
        if (memberType is IDynamicTypeSymbol)
        {
            // a `dynamic` member is object to the generated code (its formatter field is object's, and typeof(dynamic)
            // is CS1962): the factory is asked for object, and a formatter is matched against object
            memberType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
        }

        // two attribute shapes: the Type-based constructor (first argument is the factory type,
        // optional params array behind it) and MessagePackFormatterAttribute<TFactory> (the factory is the attribute's
        // own type argument, the constructor carries only the params array)
        INamedTypeSymbol? formatterType = null;
        var arguments = ImmutableArray<TypedConstant>.Empty;
        if (attribute.AttributeClass is { IsGenericType: true, TypeArguments.Length: 1 } genericAttribute
            && genericAttribute.TypeArguments[0] is INamedTypeSymbol attributeTypeArgument)
        {
            formatterType = attributeTypeArgument;
        }
        foreach (var constructorArgument in attribute.ConstructorArguments)
        {
            if (formatterType is null && constructorArgument is { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol namedType })
            {
                formatterType = namedType;
            }
            else if (constructorArgument is { Kind: TypedConstantKind.Array, IsNull: false } argumentArray)
            {
                arguments = argumentArray.Values;
            }
        }
        if (formatterType is null)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidFormatterAttribute, $"'{member}': [MessagePackFormatter] does not carry a usable factory type.", location));
            return false;
        }

        var compilation = context.SemanticModel.Compilation;
        SemanticModel? semanticModel = null;
        var position = 0;
        if (attribute.ApplicationSyntaxReference is { } application)
        {
            semanticModel = application.SyntaxTree == context.SemanticModel.SyntaxTree
                ? context.SemanticModel
                : compilation.GetSemanticModel(application.SyntaxTree);
            position = application.Span.Start;
        }

        var isFactory = false;
        for (var baseType = formatterType.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.ToDisplayString() == FormatterFactoryBaseName)
            {
                isFactory = true;
                break;
            }
        }

        // the generated code constructs the type: from MessagePack.Generated (assembly scope), or from inside the
        // partial type for AllowPrivate (accessScope), where a factory nested privately in the DTO is reachable
        if (!compilation.IsSymbolAccessibleWithin(formatterType, accessScope is { } scope ? scope : compilation.Assembly))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidFormatterAttribute, $"'{member}': '{formatterType.ToDisplayString()}' is not accessible to the generated formatter (CS0122); make it (and its containing types) public or internal, or set AllowPrivate = true on the containing [MessagePackObject] when it is nested in that type.", location));
            return false;
        }

        if (isFactory)
        {
            if (formatterType.IsUnboundGenericType || formatterType.IsAbstract)
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidFormatterAttribute, $"'{member}': '{formatterType.ToDisplayString()}' must be a concrete, fully constructed MessagePackFormatterFactory (close every type argument inside the typeof).", location));
                return false;
            }
            if (!TryBindConstructor(formatterType.InstanceConstructors, arguments, compilation, semanticModel, position, member, diagnostics, location, out var factoryArguments, accessScope))
            {
                return false;
            }
            custom = new CustomFormatterModel(
                FactoryNew: $"new {formatterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({factoryArguments})",
                TypeOfExpr: memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                FormatterNew: null);
            return true;
        }

        var definition = formatterType.OriginalDefinition;
        INamedTypeSymbol? formatterInterface = null;
        foreach (var implemented in definition.AllInterfaces)
        {
            if (implemented.OriginalDefinition is { MetadataName: "IMessagePackFormatter`3" } original
                && original.ContainingNamespace.ToDisplayString() == "MessagePack")
            {
                // a formatter may implement the interface for several types: the one for the member's type is the
                // match, the first one is the diagnostic's subject when none matches
                if (formatterInterface is null || SymbolEqualityComparer.Default.Equals(implemented.TypeArguments[2], memberType))
                {
                    formatterInterface = implemented;
                }
                if (SymbolEqualityComparer.Default.Equals(implemented.TypeArguments[2], memberType))
                {
                    break;
                }
            }
        }
        if (formatterInterface is null)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidFormatterAttribute, $"'{member}': '{formatterType.ToDisplayString()}' is neither a MessagePackFormatterFactory nor an IMessagePackFormatter implementation.", location));
            return false;
        }

        // a closed formatter type would pin one buffer pair and can never satisfy the generated formatter's own
        // TWriteBuffer/TReadBuffer
        if (!formatterType.IsUnboundGenericType
            || definition.TypeParameters.Length != 2
            || formatterInterface.TypeArguments[0] is not ITypeParameterSymbol { Ordinal: 0 }
            || formatterInterface.TypeArguments[1] is not ITypeParameterSymbol { Ordinal: 1 })
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidFormatterAttribute, $"'{member}': pass a formatter as an unbound generic over the buffer pair, e.g. typeof(MyFormatter<,>) where MyFormatter<TWriteBuffer, TReadBuffer> implements IMessagePackFormatter for the member type; anything else needs a MessagePackFormatterFactory.", location));
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(formatterInterface.TypeArguments[2], memberType))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidFormatterAttribute, $"'{member}': '{formatterType.ToDisplayString()}' serializes '{formatterInterface.TypeArguments[2].ToDisplayString()}', but the member type is '{memberType.ToDisplayString()}'.", location));
            return false;
        }

        if (LacksRefStructAllowance(definition, context, out var why))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.InvalidFormatterAttribute, $"'{member}': '{formatterType.ToDisplayString()}' cannot be closed over the generated formatter's buffer types on this target because {why} (CS9244); specify a MessagePackFormatterFactory instead (the resolver reroutes a downlevel factory to the Compatible buffers), or multi-target the formatter library so its type parameters allow ref struct buffers.", location));
            return false;
        }
        if (!TryBindConstructor(definition.InstanceConstructors, arguments, compilation, semanticModel, position, member, diagnostics, location, out var formatterArguments, accessScope))
        {
            return false;
        }
        custom = new CustomFormatterModel(
            FactoryNew: null,
            TypeOfExpr: null,
            FormatterNew: $"new {StripTypeArguments(definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}<TWriteBuffer, TReadBuffer>({formatterArguments})");
        return true;
    }

    // The generated formatter on a net9+ target passes its own buffer type parameters, which allow ref struct
    // buffers, to the attributed formatter: that one's parameters must allow them too (CS9244 otherwise). The
    // referenced Roslyn predates the constraint, so a declaration in this compilation is read off its syntax, and a
    // referenced library is judged by its target framework (nothing before .NET 9 can declare the constraint).
    static bool LacksRefStructAllowance(INamedTypeSymbol definition, GeneratorAttributeSyntaxContext context, out string reason)
    {
        reason = "";
        if (!context.SemanticModel.SyntaxTree.Options.PreprocessorSymbolNames.Contains("NET9_0_OR_GREATER"))
        {
            return false; // a downlevel target closes downlevel formatters as they are
        }
        if (definition.DeclaringSyntaxReferences.Length > 0)
        {
            foreach (var typeParameter in definition.TypeParameters)
            {
                var allowed = false;
                foreach (var reference in definition.DeclaringSyntaxReferences)
                {
                    if (reference.GetSyntax() is TypeDeclarationSyntax declaration)
                    {
                        foreach (var clause in declaration.ConstraintClauses)
                        {
                            if (clause.Name.Identifier.ValueText == typeParameter.Name && clause.ToString().Contains("allows ref struct"))
                            {
                                allowed = true;
                            }
                        }
                    }
                }
                if (!allowed)
                {
                    reason = $"its type parameter '{typeParameter.Name}' has no `allows ref struct` constraint";
                    return true;
                }
            }
            return false;
        }
        foreach (var attribute in definition.ContainingAssembly.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == "System.Runtime.Versioning.TargetFrameworkAttribute"
                && attribute.ConstructorArguments.Length > 0
                && attribute.ConstructorArguments[0].Value is string framework)
            {
                const string coreApp = ".NETCoreApp,Version=v";
                if (framework.StartsWith(coreApp, StringComparison.Ordinal) && Version.TryParse(framework.Substring(coreApp.Length), out var version) && version.Major >= 9)
                {
                    return false;
                }
                reason = $"it is built for {framework}, whose type parameters cannot allow ref struct buffers";
                return true;
            }
        }
        return false;
    }

    static bool TryBindConstructor(
        ImmutableArray<IMethodSymbol> constructors,
        ImmutableArray<TypedConstant> arguments,
        Compilation compilation,
        SemanticModel? semanticModel,
        int position,
        string member,
        List<DiagnosticInfo> diagnostics,
        LocationInfo? location,
        out string rendered,
        INamedTypeSymbol? accessScope = null)
    {
        rendered = "";
        var matches = new List<(int FilledDefaults, int Exact, string Rendered, ITypeSymbol[] Parameters)>();
        string? failure = null;

        foreach (var constructor in constructors)
        {
            // trailing parameters beyond the supplied arguments are fine when they are all optional: the emitted `new
            // F(args)` is C# source, so the compiler fills the defaults (the runtime Activator path cannot,
            // but never runs for SG-bound types)
            if (constructor.Parameters.Length < arguments.Length
                || !(constructor.DeclaredAccessibility == Accessibility.Public
                    || (constructor.DeclaredAccessibility == Accessibility.Internal
                        && SymbolEqualityComparer.Default.Equals(constructor.ContainingAssembly, compilation.Assembly))))
            {
                continue;
            }
            var tailOptional = true;
            for (int i = arguments.Length; i < constructor.Parameters.Length && tailOptional; i++)
            {
                tailOptional = constructor.Parameters[i].IsOptional;
            }
            if (!tailOptional)
            {
                continue;
            }

            var parts = new string[arguments.Length];
            var bound = true;
            var exact = 0;
            for (int i = 0; i < arguments.Length && bound; i++)
            {
                if (!TryRenderArgument(arguments[i], constructor.Parameters[i].Type, constructor.Parameters[i].Name, compilation, semanticModel, position, out parts[i], out var reason, accessScope))
                {
                    bound = false;
                    failure ??= reason;
                }
                else if (arguments[i].Type is { } argumentType && SymbolEqualityComparer.Default.Equals(argumentType, constructor.Parameters[i].Type))
                {
                    exact++;
                }
            }
            if (bound)
            {
                matches.Add((constructor.Parameters.Length - arguments.Length, exact, string.Join(", ", parts), constructor.Parameters.Take(arguments.Length).Select(static p => p.Type).ToArray()));
            }
        }

        // mirror C# overload preference: the conversions of the supplied arguments decide first (an exact parameter
        // type beats a converting one: F(int) over F(long) for the literal 1; then the "better conversion target":
        // a parameter type that converts implicitly to the other candidate's, long into double and not the reverse,
        // is the better target), and only among conversion-equivalent candidates does the one filling fewer
        // defaults win (F(int, bool = false) still beats F(long) for the literal 10)
        if (matches.Count > 0)
        {
            var mostExact = matches.Max(static m => m.Exact);
            var winners = matches.Where(m => m.Exact == mostExact).ToList();
            if (winners.Count > 1)
            {
                var best = winners.Where(candidate => winners.All(other => ReferenceEquals(other.Parameters, candidate.Parameters) || IsBetterCandidate(candidate.Parameters, other.Parameters, compilation))).ToList();
                if (best.Count == 1)
                {
                    winners = best;
                }
            }
            if (winners.Count > 1)
            {
                var fewestDefaults = winners.Min(static m => m.FilledDefaults);
                winners = winners.Where(m => m.FilledDefaults == fewestDefaults).ToList();
            }
            if (winners.Count == 1)
            {
                rendered = winners[0].Rendered;
                return true;
            }
        }
        diagnostics.Add(new DiagnosticInfo(Diagnostics.FormatterAttributeArguments, matches.Count == 0
            ? $"'{member}': the [MessagePackFormatter] arguments do not bind to any accessible constructor taking {arguments.Length} argument(s).{(failure is null ? "" : " " + failure)}"
            : $"'{member}': the [MessagePackFormatter] arguments bind to more than one constructor; disambiguate the overloads.", location));
        return false;
    }

    // candidate A is better than B when every parameter of A is at least as good a conversion target as B's and one is
    // strictly better (A's type converts implicitly to B's, B's not to A's): the "better conversion target" part of
    // overload resolution for arguments of equal exactness
    static bool IsBetterCandidate(ITypeSymbol[] a, ITypeSymbol[] b, Compilation compilation)
    {
        var strictlyBetter = false;
        for (int i = 0; i < a.Length && i < b.Length; i++)
        {
            if (SymbolEqualityComparer.Default.Equals(a[i], b[i]))
            {
                continue;
            }
            var aToB = compilation.ClassifyConversion(a[i], b[i]).IsImplicit;
            var bToA = compilation.ClassifyConversion(b[i], a[i]).IsImplicit;
            if (aToB && !bToA)
            {
                strictlyBetter = true;
            }
            else if (bToA && !aToB)
            {
                return false;
            }
        }
        return strictlyBetter;
    }

    // every rendered argument carries an explicit cast to the chosen parameter type,
    // so the generated call can never re-run overload resolution to a different constructor
    static bool TryRenderArgument(
        TypedConstant argument,
        ITypeSymbol targetType,
        string parameterName,
        Compilation compilation,
        SemanticModel? semanticModel,
        int position,
        out string rendered,
        out string? failure,
        INamedTypeSymbol? accessScope = null)
    {
        rendered = "";
        failure = null;
        var cast = "(" + targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")";

        if (argument.IsNull)
        {
            if (targetType.IsValueType && targetType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
            {
                failure = $"null does not convert to parameter '{parameterName}' ({targetType.ToDisplayString()}).";
                return false;
            }
            rendered = cast + "null";
            return true;
        }

        switch (argument.Kind)
        {
            // a string that the parameter cannot take as a string ("StringComparer.OrdinalIgnoreCase" for an
            // IEqualityComparer<string>) is the expression form; one it can (an object parameter) is the string, as the
            // reflection tier binds it
            case TypedConstantKind.Primitive when argument.Value is string text
                && targetType.SpecialType != SpecialType.System_String
                && !(argument.Type is { } stringType && compilation.HasImplicitConversion(stringType, targetType)):
                return TryRenderExpressionArgument(text, targetType, cast, compilation, semanticModel, position, out rendered, out failure, accessScope);

            case TypedConstantKind.Primitive:
            case TypedConstantKind.Enum:
                if (argument.Type is null || !compilation.HasImplicitConversion(argument.Type, targetType))
                {
                    failure = $"A '{argument.Type?.ToDisplayString() ?? "?"}' argument does not convert to parameter '{parameterName}' ({targetType.ToDisplayString()}).";
                    return false;
                }
                rendered = argument.Kind == TypedConstantKind.Enum
                    ? $"{cast}({argument.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({RenderPrimitive(argument)})"
                    : $"{cast}({RenderPrimitive(argument)})";
                return true;

            case TypedConstantKind.Type:
                if (argument.Value is not ITypeSymbol typeValue
                    || compilation.GetTypeByMetadataName("System.Type") is not { } systemType
                    || !compilation.HasImplicitConversion(systemType, targetType))
                {
                    failure = $"A System.Type argument does not convert to parameter '{parameterName}' ({targetType.ToDisplayString()}).";
                    return false;
                }
                rendered = $"{cast}typeof({typeValue.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
                return true;

            case TypedConstantKind.Array:
                // the parameter is the array type itself, or one the array converts to (IEnumerable<int>, object):
                // the array keeps its own element type then
                if ((targetType as IArrayTypeSymbol ?? argument.Type as IArrayTypeSymbol) is not { } arrayType
                    || (argument.Type is { } attributeArrayType && !compilation.HasImplicitConversion(attributeArrayType, targetType)))
                {
                    failure = $"An array argument ({argument.Type?.ToDisplayString() ?? "?"}) does not convert to parameter '{parameterName}' ({targetType.ToDisplayString()}).";
                    return false;
                }
                var elements = new string[argument.Values.Length];
                for (int i = 0; i < elements.Length; i++)
                {
                    if (!TryRenderArgument(argument.Values[i], arrayType.ElementType, parameterName, compilation, semanticModel, position, out elements[i], out failure, accessScope))
                    {
                        return false;
                    }
                }
                rendered = $"{cast}new {arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}[] {{ {string.Join(", ", elements)} }}";
                return true;

            default:
                failure = $"The argument for parameter '{parameterName}' is not a supported attribute constant.";
                return false;
        }
    }

    static bool TryRenderExpressionArgument(
        string text,
        ITypeSymbol targetType,
        string cast,
        Compilation compilation,
        SemanticModel? semanticModel,
        int position,
        out string rendered,
        out string? failure,
        INamedTypeSymbol? accessScope = null)
    {
        rendered = "";
        if (semanticModel is null)
        {
            failure = $"\"{text}\": expression-form arguments need a source location to bind at.";
            return false;
        }

        // grammar is deliberately tiny: a dotted static-member reference, nothing else, no `new`, no invocations,
        // no indexers, so the string can never grow into a DSL
        var expression = SyntaxFactory.ParseExpression(text);
        if (expression.ContainsDiagnostics || expression is not MemberAccessExpressionSyntax || !IsDottedName(expression))
        {
            failure = $"\"{text}\" is not a dotted static-member reference (expected a form like \"StringComparer.OrdinalIgnoreCase\").";
            return false;
        }

        var symbol = semanticModel.GetSpeculativeSymbolInfo(position, expression, SpeculativeBindingOption.BindAsExpression).Symbol;
        (ITypeSymbol Type, INamedTypeSymbol Container)? resolved = symbol switch
        {
            IFieldSymbol { IsStatic: true } field => (field.Type, field.ContainingType),
            IPropertySymbol { IsStatic: true, GetMethod: not null } property => (property.Type, property.ContainingType),
            _ => null,
        };
        if (resolved is not { } target)
        {
            failure = $"\"{text}\" does not resolve to a static property or field at the attribute's location.";
            return false;
        }
        if (!compilation.HasImplicitConversion(target.Type, targetType))
        {
            failure = $"\"{text}\" has type '{target.Type.ToDisplayString()}', which does not convert to '{targetType.ToDisplayString()}'.";
            return false;
        }
        // the attribute site can name a private member of its own type; the generated formatter (in
        // MessagePack.Generated, or nested in the type for AllowPrivate) must be able to as well
        if (!compilation.IsSymbolAccessibleWithin(symbol!, accessScope is { } scope ? scope : compilation.Assembly))
        {
            failure = $"\"{text}\" is not accessible to the generated formatter (CS0122); make the member internal or public, or set AllowPrivate = true on the containing [MessagePackObject] when it is a member of that type.";
            return false;
        }

        rendered = $"{cast}{target.Container.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{Identifier(symbol!.Name)}"; // Identifier: `Comparers.@default` stays escaped
        failure = null;
        return true;
    }

    static bool IsDottedName(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression)
            && access.Name is IdentifierNameSyntax or GenericNameSyntax => IsDottedName(access.Expression),
        IdentifierNameSyntax or GenericNameSyntax or AliasQualifiedNameSyntax or PredefinedTypeSyntax => true,
        _ => false,
    };

    static string RenderPrimitive(TypedConstant argument) => argument.Value switch
    {
        string s => SymbolDisplay.FormatLiteral(s, quote: true),
        char c => SymbolDisplay.FormatLiteral(c, quote: true),
        bool b => b ? "true" : "false",
        float f when float.IsNaN(f) => "float.NaN",
        float f when float.IsPositiveInfinity(f) => "float.PositiveInfinity",
        float f when float.IsNegativeInfinity(f) => "float.NegativeInfinity",
        float f => f.ToString("R", CultureInfo.InvariantCulture) + "f",
        double d when double.IsNaN(d) => "double.NaN",
        double d when double.IsPositiveInfinity(d) => "double.PositiveInfinity",
        double d when double.IsNegativeInfinity(d) => "double.NegativeInfinity",
        double d => d.ToString("R", CultureInfo.InvariantCulture) + "d",
        long l => l.ToString(CultureInfo.InvariantCulture) + "L",
        ulong ul => ul.ToString(CultureInfo.InvariantCulture) + "UL",
        uint ui => ui.ToString(CultureInfo.InvariantCulture) + "U",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        var other => other?.ToString() ?? "null",
    };

    static (CallbackStyle OnBefore, CallbackStyle OnAfter) DetectCallbacks(INamedTypeSymbol type)
    {
        INamedTypeSymbol? callbackInterface = null;
        foreach (var implemented in type.AllInterfaces)
        {
            if (implemented.ToDisplayString() == CallbackReceiverInterfaceName)
            {
                callbackInterface = implemented;
                break;
            }
        }
        if (callbackInterface is null)
        {
            return (CallbackStyle.None, CallbackStyle.None);
        }

        var onBefore = CallbackStyle.Cast;
        var onAfter = CallbackStyle.Cast;
        foreach (var member in callbackInterface.GetMembers())
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }
            // Direct emits `value.OnBeforeSerialize()`, which binds by C# member lookup on the serialized type, not
            // by interface mapping: a derived class hiding an inherited implementation with `new` would receive the
            // call instead of the implementation. Direct is only safe when both agree.
            var style = type.FindImplementationForInterfaceMember(method) is IMethodSymbol
            {
                MethodKind: MethodKind.Ordinary,
                DeclaredAccessibility: Accessibility.Public,
            } implementation && implementation.Name == method.Name && DirectCallBinds(type, implementation)
                ? CallbackStyle.Direct
                : CallbackStyle.Cast;
            if (method.Name == "OnBeforeSerialize")
            {
                onBefore = style;
            }
            else if (method.Name == "OnAfterDeserialize")
            {
                onAfter = style;
            }
        }
        return (onBefore, onAfter);
    }

    // C# member lookup stops at the nearest type in the hierarchy that declares the name; a direct call binds only to
    // the interface implementation when that type is the implementation's own declarer and nothing else there hides it
    static bool DirectCallBinds(INamedTypeSymbol type, IMethodSymbol implementation)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var members = current.GetMembers(implementation.Name);
            if (members.Length == 0)
            {
                continue;
            }
            return SymbolEqualityComparer.Default.Equals(current, implementation.ContainingType)
                && members.Length == 1
                && SymbolEqualityComparer.Default.Equals(members[0], implementation);
        }
        return false;
    }

    internal static bool IsPartialChain(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            var partial = false;
            foreach (var reference in current.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is TypeDeclarationSyntax { } declaration
                    && declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    partial = true;
                    break;
                }
            }
            if (!partial)
            {
                return false;
            }
        }
        return true;
    }

    internal static EquatableArray<string> BuildWhereClauses(INamedTypeSymbol type)
    {
        var clauses = new List<string>();
        foreach (var typeParameter in type.TypeParameters)
        {
            var parts = new List<string>();
            if (typeParameter.HasReferenceTypeConstraint)
            {
                parts.Add("class");
            }
            if (typeParameter.HasUnmanagedTypeConstraint)
            {
                parts.Add("unmanaged");
            }
            else if (typeParameter.HasValueTypeConstraint)
            {
                parts.Add("struct");
            }
            if (typeParameter.HasNotNullConstraint)
            {
                parts.Add("notnull");
            }
            foreach (var constraintType in typeParameter.ConstraintTypes)
            {
                parts.Add(constraintType.ToDisplayString(FullyQualifiedWithNullability));
            }
            if (typeParameter.HasConstructorConstraint)
            {
                parts.Add("new()");
            }
            if (parts.Count > 0)
            {
                clauses.Add($"where {Identifier(typeParameter.Name)} : {string.Join(", ", parts)}");
            }
        }
        return new EquatableArray<string>([.. clauses]);
    }

    // outermost-first declaration headers ("namespace Ns", "partial class Outer", ...)
    // for the AllowPrivate nested emission; the emitter opens one block per entry
    internal static EquatableArray<string> BuildNestedDeclarations(INamedTypeSymbol type)
    {
        var chain = new List<INamedTypeSymbol>();
        for (var current = type; current is not null; current = current.ContainingType)
        {
            chain.Add(current);
        }
        chain.Reverse();

        var declarations = new List<string>();
        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            declarations.Add($"namespace {type.ContainingNamespace.ToDisplayString()}");
        }
        foreach (var declaration in chain)
        {
            declarations.Add($"partial {Keyword(declaration)} {Identifier(declaration.Name)}");
        }
        return new EquatableArray<string>([.. declarations]);

        static string Keyword(INamedTypeSymbol symbol) => symbol switch
        {
            { IsRecord: true, IsValueType: true } => "record struct",
            { IsRecord: true } => "record",
            { IsValueType: true } => "struct",
            { TypeKind: TypeKind.Interface } => "interface",
            _ => "class",
        };
    }

    internal static string StripTypeArguments(string fullTypeName)
    {
        var angle = fullTypeName.IndexOf('<');
        return angle < 0 ? fullTypeName : fullTypeName.Substring(0, angle);
    }

    // Nullable<primitive> joins the direct tier: nil is 1 byte, under every primitive's max,
    // so the fused reservation is unchanged and the write becomes a nil-or-value expression,
    // same wire as NullableFormatter, no formatter field
    static (DirectKind Direct, bool Nullable) ClassifyDirect(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            var inner = Classify(nullable.TypeArguments[0]);
            return (inner, inner != DirectKind.None);
        }
        return (Classify(type), false);
    }

    static DirectKind Classify(ITypeSymbol type) => type.SpecialType switch
    {
        SpecialType.System_Int32 => DirectKind.Int32,
        SpecialType.System_UInt32 => DirectKind.UInt32,
        SpecialType.System_Int64 => DirectKind.Int64,
        SpecialType.System_UInt64 => DirectKind.UInt64,
        SpecialType.System_Int16 => DirectKind.Int16,
        SpecialType.System_UInt16 => DirectKind.UInt16,
        SpecialType.System_Byte => DirectKind.Byte,
        SpecialType.System_SByte => DirectKind.SByte,
        SpecialType.System_Boolean => DirectKind.Boolean,
        SpecialType.System_Char => DirectKind.Char,
        SpecialType.System_Single => DirectKind.Single,
        SpecialType.System_Double => DirectKind.Double,
        // System_String and System_DateTime are deliberately absent: their wire form is factory-chain configurable (see
        // DirectKind), so they go through the resolved formatter
        _ => DirectKind.None,
    };

    // a property override's attributes live on whichever declaration in the chain carries them (Roslyn's GetAttributes
    // reads only the symbol's own): enumerate derived-first so the caller's first-match-wins guards give the nearest
    // declaration precedence, matching the reflection tier's inherit:true / base-declaration-walk reads
    internal static IEnumerable<AttributeData> MemberAttributes(ISymbol symbol)
    {
        if (symbol is IPropertySymbol property)
        {
            for (var current = property; current is not null; current = current.OverriddenProperty)
            {
                foreach (var attribute in current.GetAttributes())
                {
                    yield return attribute;
                }
            }
            yield break;
        }
        foreach (var attribute in symbol.GetAttributes())
        {
            yield return attribute;
        }
    }

    const string DataMemberAttributeName = "System.Runtime.Serialization.DataMemberAttribute";

    // Type.FullName's shape ("Ns.Outer+Inner"), so a generated qualified map key is byte-identical to the reflection
    // tier's DeclaringType.FullName-based one
    static string ReflectionFullName(INamedTypeSymbol type)
    {
        var parts = new List<string>();
        for (var current = type; current is not null; current = current.ContainingType)
        {
            parts.Add(current.MetadataName);
        }
        parts.Reverse();
        var ns = type.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace
            ? containingNamespace.ToDisplayString() + "."
            : "";
        return ns + string.Join("+", parts);
    }

    // ReflectionObjectFormatter.GetDataMemberOrder's rule: absent attribute sorts last (int.MaxValue),
    // attribute without an explicit Order sorts first (-1, DataMember's own unset sentinel), ties stay stable
    static int GetDataMemberOrder(ISymbol symbol)
    {
        foreach (var attribute in MemberAttributes(symbol))
        {
            if (attribute.AttributeClass?.ToDisplayString() == DataMemberAttributeName)
            {
                foreach (var named in attribute.NamedArguments)
                {
                    if (named.Key == "Order" && named.Value.Value is int order)
                    {
                        return order;
                    }
                }
                return -1;
            }
        }
        return int.MaxValue;
    }

    // wire-order depth: an override sorts at its base declaration's level (v3 collapses the pair onto the base slot);
    // shadowing keeps each declaration's own level
    static int SortDepth(INamedTypeSymbol type, ISymbol member)
    {
        if (member is IPropertySymbol property)
        {
            while (property.OverriddenProperty is { } overridden)
            {
                property = overridden;
            }
            return InheritanceDepth(type, property);
        }
        return InheritanceDepth(type, member);
    }

    // steps from the serialized type up to the member's declarer: 0 = declared on the type itself,
    // so ascending order puts the most-derived declaration of a shadowed name first
    static int InheritanceDepth(INamedTypeSymbol type, ISymbol member)
    {
        var depth = 0;
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, member.ContainingType))
            {
                return depth;
            }
            depth++;
        }
        return depth;
    }

    internal static string Sanitize(string fullTypeName)
    {
        var builder = new StringBuilder(fullTypeName.Length);
        // drop the "global::" prefix, then encode: every separator (., <, >, ,, [, ], +, ...) becomes a single '_'
        // and a literal '_' becomes "_0". The encoding must be injective or two types share one generated name (a
        // loud but cryptic CS0101 / duplicate-hint failure): an identifier never starts with a digit, so an '_'
        // followed by '0' can only be a literal underscore and any other '_' a separator, which keeps Ns.A_B / Ns.A.B,
        // Foo<T, U> / Foo<T_U> and Ns._A / Ns_.A (the case "__" for a literal got wrong) all distinct. The space of
        // ", " is dropped.
        var start = fullTypeName.StartsWith("global::", StringComparison.Ordinal) ? 8 : 0;
        for (int i = start; i < fullTypeName.Length; i++)
        {
            var c = fullTypeName[i];
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
            }
            else if (c == '_')
            {
                builder.Append("_0");
            }
            else if (c != ' ')
            {
                builder.Append('_');
            }
        }
        return builder.ToString();
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace MessagePack.SourceGenerator;

/// <summary>
/// Turns a [MessagePackObject] type symbol into an <see cref="ObjectModel"/>: member
/// discovery (the reflection enumeration order MessagePack-CSharp's resolvers use),
/// key validation, constructor selection (shared rule with ReflectionObjectFormatter:
/// [SerializationConstructor] wins, else most parameters with full name+type match),
/// callback detection, and the AllowPrivate/generic shape gates.
/// </summary>
static class ObjectParser
{
    const string KeyAttributeName = "MessagePack.KeyAttribute";
    const string IgnoreMemberAttributeName = "MessagePack.IgnoreMemberAttribute";
    const string IgnoreDataMemberAttributeName = "System.Runtime.Serialization.IgnoreDataMemberAttribute";
    const string FormatterAttributeName = "MessagePack.MessagePackFormatterAttribute";
    const string FormatterFactoryBaseName = "MessagePack.MessagePackFormatterFactory";
    const string SerializationConstructorAttributeName = "MessagePack.SerializationConstructorAttribute";
    // name+namespace match (never ToDisplayString: the nullable annotation on
    // `MessagePackUnknownMembers?` members would defeat a string comparison)
    internal static bool IsUnknownMembersType(ITypeSymbol type) =>
        type is INamedTypeSymbol { Arity: 0, Name: "MessagePackUnknownMembers" }
        && type.ContainingNamespace is { Name: "MessagePack", ContainingNamespace.IsGlobalNamespace: true };
    const string SetsRequiredMembersAttributeName = "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute";
    const string CallbackReceiverInterfaceName = "MessagePack.IMessagePackSerializationCallbackReceiver";

    internal static readonly SymbolDisplayFormat FullyQualifiedWithNullability =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    // discovered member before key/constructor resolution
    readonly record struct Candidate(ISymbol Symbol, ITypeSymbol Type, SetterKind Setter);

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
                diagnostics.Add(new DiagnosticInfo("MsgPack015", $"'{typeName}': AllowCircularReferences requires the source-generated formatter and cannot combine with SuppressSourceGeneration.", typeLocation));
                return Invalid(diagnostics);
            }
            // explicit opt-out: the runtime tiers (reflection) serve this type
            return new ParseResult(null, new EquatableArray<DiagnosticInfo>([]));
        }

        if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            // interfaces land here only WITHOUT [UnionTag] (the pipeline routes tagged
            // ones to UnionParser), so the hint is the most likely fix
            diagnostics.Add(new DiagnosticInfo("MsgPack005", type.TypeKind == TypeKind.Interface
                ? $"'{typeName}' is skipped: an interface serializes only as a polymorphic base — declare its case types with [UnionTag]."
                : $"'{typeName}' is skipped: only class and struct shapes are generated.", typeLocation));
            return Invalid(diagnostics);
        }

        if (AllowCircularReferences && type.IsValueType)
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack015", $"'{typeName}': AllowCircularReferences requires a class — value types have no reference identity to preserve.", typeLocation));
            return Invalid(diagnostics);
        }

        var allowPrivate = ReadNamedBool(objectAttribute, "AllowPrivate");

        // the generated formatter lives in another namespace of the same assembly
        // (AllowPrivate nests it inside the type, but the FACTORY still needs to reach it)
        for (var accessible = type; accessible is not null; accessible = accessible.ContainingType)
        {
            if (accessible.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack006", $"'{typeName}' (or a containing type) is {type.DeclaredAccessibility}; generated formatters can only reach public or internal types.", typeLocation));
                return Invalid(diagnostics);
            }
        }

        for (var container = type.ContainingType; container is not null; container = container.ContainingType)
        {
            if (container.IsGenericType)
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack005", $"'{typeName}' is skipped: types nested inside generic types are not supported yet.", typeLocation));
                return Invalid(diagnostics);
            }
        }

        if (type.IsGenericType)
        {
            if (allowPrivate)
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack005", $"'{typeName}' is skipped: AllowPrivate on generic types is not supported yet (served by the reflection tier).", typeLocation));
                return Invalid(diagnostics);
            }
            foreach (var typeParameter in type.TypeParameters)
            {
                if (typeParameter.Name is "TWriteBuffer" or "TReadBuffer")
                {
                    diagnostics.Add(new DiagnosticInfo("MsgPack005", $"'{typeName}' is skipped: a type parameter named '{typeParameter.Name}' collides with the generated formatter's buffer parameters.", typeLocation));
                    return Invalid(diagnostics);
                }
            }
        }

        if (allowPrivate && !IsPartialChain(type))
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack010", $"'{typeName}' uses AllowPrivate, so the type (and every containing type) must be declared partial: the formatter is generated as a nested class to reach private members.", typeLocation));
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

        // properties across the hierarchy first, then fields — mirrors the reflection
        // enumeration order MessagePack-CSharp's resolvers serialize maps in.
        // allowPrivate widens discovery to non-public members; derived-first order plus
        // the seenNames dedup keeps the most derived of any override/shadowed member,
        // matching ReflectionObjectFormatter's DeclaredOnly hierarchy walk.
        var candidates = new List<Candidate>();
        var seenNames = new HashSet<string>();
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object && current.SpecialType != SpecialType.System_ValueType; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol { IsStatic: false, IsIndexer: false, IsImplicitlyDeclared: false } property
                    && property.GetMethod is { } getter
                    && (allowPrivate || (property.DeclaredAccessibility == Accessibility.Public && getter.DeclaredAccessibility == Accessibility.Public))
                    && seenNames.Add(property.Name))
                {
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
                if (member is IFieldSymbol { IsStatic: false, IsConst: false, IsImplicitlyDeclared: false } field
                    && (allowPrivate || field.DeclaredAccessibility == Accessibility.Public)
                    && seenNames.Add(field.Name))
                {
                    candidates.Add(new Candidate(field, field.Type, field.IsReadOnly ? SetterKind.None : SetterKind.Set));
                }
            }
        }

        var entries = new List<(string Name, ITypeSymbol Type, SetterKind Setter, int IntKey, string StringKey, bool RequiredModifier, LocationInfo? Location, CustomFormatterModel? Custom)>();
        var hasIntKey = false;
        var hasStringKey = keyAsPropertyName;
        var intKeys = new HashSet<int>();
        var stringKeys = new HashSet<string>();
        var valid = true;
        string? unknownMembersName = null;

        foreach (var (symbol, memberType, setter) in candidates)
        {
            int? intKey = null;
            string? stringKey = null;
            var ignored = false;
            AttributeData? formatterAttribute = null;

            foreach (var attribute in symbol.GetAttributes())
            {
                var attributeName = attribute.AttributeClass?.ToDisplayString();
                // [IgnoreDataMember] is an alias for [IgnoreMember] in BOTH tiers (v3's
                // dynamic resolvers honored it but mpc did not — that inconsistency ends here)
                if (attributeName == IgnoreMemberAttributeName || attributeName == IgnoreDataMemberAttributeName)
                {
                    ignored = true;
                }
                else if (attributeName == KeyAttributeName && attribute.ConstructorArguments.Length > 0)
                {
                    var argument = attribute.ConstructorArguments[0].Value;
                    if (argument is int i)
                    {
                        intKey = i;
                    }
                    else if (argument is string s)
                    {
                        stringKey = s;
                    }
                }
                else
                {
                    // base-chain walk: MessagePackFormatterAttribute is not sealed, and v3
                    // codebases derive from it
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

            // the retention packet member: set aside, never a keyed member. [IgnoreMember]
            // above still opts it out entirely (the type then keeps skip-only tolerance).
            if (IsUnknownMembersType(memberType))
            {
                var unknownLocation = LocationInfo.From(symbol);
                if (intKey is not null || stringKey is not null)
                {
                    diagnostics.Add(new DiagnosticInfo("MsgPack020", $"'{typeName}.{symbol.Name}': a MessagePackUnknownMembers member carries no [Key]; it captures whatever keys the declared members do not.", unknownLocation));
                    valid = false;
                    continue;
                }
                if (formatterAttribute is not null)
                {
                    diagnostics.Add(new DiagnosticInfo("MsgPack020", $"'{typeName}.{symbol.Name}': a MessagePackUnknownMembers member is not serialized through a formatter, so [MessagePackFormatter] has no meaning on it.", unknownLocation));
                    valid = false;
                    continue;
                }
                if (setter != SetterKind.Set)
                {
                    diagnostics.Add(new DiagnosticInfo("MsgPack020", $"'{typeName}.{symbol.Name}': a MessagePackUnknownMembers member needs a plain settable accessor (init-only and read-only are not supported), because every deserialization assigns the freshly captured packet.", unknownLocation));
                    valid = false;
                    continue;
                }
                if (unknownMembersName is not null)
                {
                    diagnostics.Add(new DiagnosticInfo("MsgPack020", $"'{typeName}' declares more than one MessagePackUnknownMembers member ('{unknownMembersName}' and '{symbol.Name}'); the capture destination must be unambiguous.", unknownLocation));
                    valid = false;
                    continue;
                }
                unknownMembersName = symbol.Name;
                continue;
            }

            CustomFormatterModel? custom = null;
            if (formatterAttribute is not null)
            {
                if (!TryBuildCustomFormatter(formatterAttribute, memberType, $"{typeName}.{symbol.Name}", LocationInfo.From(symbol), context, diagnostics, out custom))
                {
                    valid = false;
                    continue;
                }
            }

            var memberLocation = LocationInfo.From(symbol);
            if (intKey is null && stringKey is null)
            {
                if (keyAsPropertyName)
                {
                    // an explicit [Key("...")] never reaches here, so the policy only shapes defaults;
                    // a policy-induced collision ("Id"/"ID" both → "id") falls into MsgPack003 below
                    stringKey = KeyNamingPolicyConverter.ConvertName(namingPolicy, symbol.Name);
                }
                else if (symbol.DeclaredAccessibility == Accessibility.Public
                    && (symbol is not IPropertySymbol { GetMethod: { } g } || g.DeclaredAccessibility == Accessibility.Public))
                {
                    diagnostics.Add(new DiagnosticInfo("MsgPack001", $"'{typeName}.{symbol.Name}' is a public member of a [MessagePackObject] type and needs [Key] or [IgnoreMember] (or use [MessagePackObject(true)]).", memberLocation));
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
                    diagnostics.Add(new DiagnosticInfo("MsgPack008", $"'{typeName}.{symbol.Name}' has a negative key ({key}).", memberLocation));
                    valid = false;
                    continue;
                }
                if (!intKeys.Add(key))
                {
                    diagnostics.Add(new DiagnosticInfo("MsgPack003", $"'{typeName}' declares key {key} more than once.", memberLocation));
                    valid = false;
                    continue;
                }
                hasIntKey = true;
            }
            else
            {
                if (!stringKeys.Add(stringKey!))
                {
                    diagnostics.Add(new DiagnosticInfo("MsgPack003", $"'{typeName}' declares key \"{stringKey}\" more than once.", memberLocation));
                    valid = false;
                    continue;
                }
                hasStringKey = true;
            }

            var requiredModifier = symbol is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true };
            entries.Add((symbol.Name, memberType, setter, intKey ?? -1, stringKey ?? "", requiredModifier, memberLocation, custom));
        }

        if (hasIntKey && hasStringKey)
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack002", $"'{typeName}' mixes int keys and string keys; a type serializes as either an array (int keys) or a map (string keys), not both.", typeLocation));
            valid = false;
        }

        if (!valid)
        {
            return Invalid(diagnostics);
        }

        // constructor selection: [SerializationConstructor] wins; otherwise the accessible
        // constructor with the most parameters where EVERY parameter name-matches a
        // serialized member (ordinal-ignore-case, exact type) — ReflectionObjectFormatter's
        // rule, so the generated and reflection interpretations of a type never diverge
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
                attributedConstructor = constructor;
                break;
            }
        }

        if (attributedConstructor is not null)
        {
            if (!IsConstructorAccessible(attributedConstructor, allowPrivate, parameterless: attributedConstructor.Parameters.Length == 0)
                || !TryMatchConstructor(attributedConstructor, entries, parameterIndexByEntry))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack009", $"'{typeName}': the [SerializationConstructor] constructor must be accessible and every parameter must match a serialized member by name (case-insensitive) and exact type.", LocationInfo.From(attributedConstructor) ?? typeLocation));
                return Invalid(diagnostics);
            }
            constructorParameterCount = attributedConstructor.Parameters.Length;
            selectedConstructor = attributedConstructor;
        }
        else
        {
            var matched = false;
            foreach (var constructor in type.InstanceConstructors
                .Where(c => IsConstructorAccessible(c, allowPrivate, parameterless: c.Parameters.Length == 0))
                .OrderByDescending(c => c.Parameters.Length))
            {
                for (int i = 0; i < parameterIndexByEntry.Length; i++)
                {
                    parameterIndexByEntry[i] = -1;
                }
                if (TryMatchConstructor(constructor, entries, parameterIndexByEntry))
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
                    diagnostics.Add(new DiagnosticInfo("MsgPack004", $"'{typeName}' needs an accessible parameterless constructor, or a constructor whose parameters all match serialized members by name, for deserialization.", typeLocation));
                    return Invalid(diagnostics);
                }
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
                diagnostics.Add(new DiagnosticInfo("MsgPack007", $"'{typeName}.{entry.Name}' has a key but no setter and no matching constructor parameter: it serializes, but its payload is skipped on deserialization.", entry.Location));
            }
            var requiredByConstructor = parameterIndexByEntry[i] >= 0
                && !selectedConstructor!.Parameters[parameterIndexByEntry[i]].HasExplicitDefaultValue;
            // a custom formatter overrides even the direct-writable primitives
            var (direct, directNullable) = entry.Custom is null ? ClassifyDirect(entry.Type) : (DirectKind.None, false);
            models.Add(new MemberModel(
                Name: entry.Name,
                TypeName: entry.Type.ToDisplayString(FullyQualifiedWithNullability),
                IntKey: entry.IntKey,
                StringKey: entry.StringKey,
                Direct: direct,
                DirectNullable: directNullable,
                Setter: entry.Setter,
                ConstructorParameterIndex: parameterIndexByEntry[i],
                IsRequired: entry.RequiredModifier || requiredByConstructor,
                HasRequiredModifier: entry.RequiredModifier,
                IsNonNullableReference: entry.Type.IsReferenceType
                    && entry.Type.NullableAnnotation == NullableAnnotation.NotAnnotated
                    && entry.Type.TypeKind != TypeKind.TypeParameter,
                CustomFormatter: entry.Custom));
        }

        // a class with required members cannot be `new T()`-ed by the populate path
        // (CS9035): route it through the construction shape, whose object initializer
        // covers the required members. Structs never `new` on the populate path.
        var useConstruction = constructorParameterCount > 0 || anyInit || (anyRequiredModifier && !type.IsValueType);

        if (AllowCircularReferences && useConstruction)
        {
            // register-before-populate is the whole mechanism: the instance must exist
            // (parameterless) and be fully fillable afterward, or a cycle back into it
            // could never resolve
            diagnostics.Add(new DiagnosticInfo("MsgPack015", $"'{typeName}': AllowCircularReferences deserializes by creating the instance first and populating members afterward, so the type cannot use constructor-parameter matching, init-only accessors, or required members. Use a parameterless constructor with settable members (mark it with [SerializationConstructor] if a parameterized constructor is being matched).", typeLocation));
            return Invalid(diagnostics);
        }

        var (onBefore, onAfter) = DetectCallbacks(type);

        // AOT harvesting: closed instantiations of this compilation's generic
        // [MessagePackObject] types, plus enums, Nullable<T>, and BCL collection/wrapper
        // closed forms (List<string>, string[], Dictionary<K,V>, tuples, ...), found
        // anywhere inside the serialized member types get static constructions in the
        // factory (see FactoryEmitter) — the AOT route, since MakeGenericType cannot close
        // over the ref struct buffer arguments and the AOT chain omits GenericFormatterFactory
        var harvestedGenerics = new Dictionary<string, HarvestedGenericModel>();
        var harvestedBuiltIns = new Dictionary<string, HarvestedBuiltInModel>();
        var compilation = context.SemanticModel.Compilation;
        foreach (var entry in entries)
        {
            HarvestSerializedType(entry.Type, compilation, harvestedGenerics, harvestedBuiltIns);
        }

        var fullTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var sanitized = Sanitize(fullTypeName);
        var formatterName = allowPrivate ? "GeneratedMessagePackFormatter" : sanitized + "Formatter";
        var formatterReference = allowPrivate
            ? fullTypeName + ".GeneratedMessagePackFormatter"
            : "global::MessagePack.Generated." + formatterName;

        var typeParameterList = type.IsGenericType
            ? string.Join(", ", type.TypeParameters.Select(static p => p.Name))
            : "";
        var whereClauses = type.IsGenericType ? BuildWhereClauses(type) : new EquatableArray<string>([]);
        var openTypeOf = type.IsGenericType
            ? StripTypeArguments(fullTypeName) + "<" + new string(',', type.TypeParameters.Length - 1) + ">"
            : fullTypeName;
        var openFormatterTypeOf = type.IsGenericType
            ? formatterReference + "<" + new string(',', type.TypeParameters.Length + 1) + ">"
            : "";

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
            UnknownMembersName: unknownMembersName);
        return new ParseResult(model, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
    }

    const string MessagePackObjectAttributeName = "MessagePack.MessagePackObjectAttribute";

    internal static void HarvestSerializedType(ITypeSymbol type, Compilation compilation, Dictionary<string, HarvestedGenericModel> userGenerics, Dictionary<string, HarvestedBuiltInModel> builtIns)
    {
        if (type is IArrayTypeSymbol array)
        {
            HarvestSerializedType(array.ElementType, compilation, userGenerics, builtIns);
            HarvestArray(array, compilation, builtIns);
            return;
        }
        if (type is not INamedTypeSymbol named)
        {
            return;
        }
        if (named.IsTupleType && named.TupleUnderlyingType is { } tupleUnderlying)
        {
            // element names are display-only: normalizing to the underlying ValueTuple keeps
            // typeof strings legal and dedupes (int a, string b) against (int x, string y)
            named = tupleUnderlying;
        }
        if (named.TypeKind == TypeKind.Enum)
        {
            if (!ContainsTypeParameter(named)
                && IsAccessibleToGeneratedCode(named, compilation)
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
            // a non-generic collection shape (ArrayList, a Collection<T> subclass, ...) still
            // rides the catch-all tier at runtime, so it needs the same static registration
            HarvestCollectionCatchAll(named, compilation, userGenerics, builtIns);
            return;
        }
        foreach (var argument in named.TypeArguments)
        {
            HarvestSerializedType(argument, compilation, userGenerics, builtIns);
        }
        // only fully closed, factory-visible instantiations: open members of a generic
        // container flow type parameters, and a private nested type cannot be typeof'ed
        // from the generated factory
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
        // built-in generic collections and wrappers (List<T>, Dictionary<K,V>, tuples,
        // immutable collections, ...): GenericFormatterFactory closes these over the runtime
        // type arguments, which the Native AOT chain omits, so every closed instantiation
        // reachable from the member graph registers a static construction instead
        if (BuiltInGenericFormatterName(named) is { } genericFormatter)
        {
            if (IsBuiltInServedInstantiation(named))
            {
                // BuiltInFormatterFactory serves the closed form directly (bin-format byte
                // containers, codec-backed primitive List/Memory/ArraySegment); registering
                // the plain generic formatter here would shadow the faster one in EVERY chain
                return;
            }
            // the formatter itself can be TFM-gated (Frozen*, ReadOnlySet, OrderedDictionary,
            // PriorityQueue): skip when the referenced MessagePack asset does not compile it
            if (FormatterTypeExists(compilation, genericFormatter, named.TypeArguments.Length))
            {
                var closedCollectionName = named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (!builtIns.ContainsKey(closedCollectionName))
                {
                    // comparer-shaped formatters have no parameterless constructor; null =
                    // the default comparer, matching the parameterless runtime factories
                    var arguments = FormatterTakesComparer(genericFormatter) ? "comparer: null" : "";
                    builtIns.Add(closedCollectionName, new HarvestedBuiltInModel(
                        ClosedTypeName: closedCollectionName,
                        FormatterConstruction: $"new global::MessagePack.Formatters.{genericFormatter}<TWriteBuffer, TReadBuffer, {string.Join(", ", named.TypeArguments.Select(static a => a.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}>({arguments})"));
                }
            }
            return;
        }
        // user generics: only THIS compilation's [MessagePackObject] definitions — another
        // assembly's generated formatters are internal to it (its own compilation harvests)
        if (!SymbolEqualityComparer.Default.Equals(named.OriginalDefinition.ContainingAssembly, compilation.Assembly)
            || !HasMessagePackObjectAttribute(named.OriginalDefinition))
        {
            HarvestCollectionCatchAll(named, compilation, userGenerics, builtIns);
            return;
        }
        var closedTypeName = named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (!userGenerics.ContainsKey(closedTypeName))
        {
            userGenerics.Add(closedTypeName, new HarvestedGenericModel(
                OpenTypeOf: StripTypeArguments(named.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)) + "<" + new string(',', named.TypeArguments.Length - 1) + ">",
                ClosedTypeName: closedTypeName,
                TypeArguments: new EquatableArray<string>([.. named.TypeArguments.Select(static a => a.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))])));
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
        var formatterName = array.Rank switch
        {
            1 => "ArrayFormatter",
            2 => "TwoDimensionalArrayFormatter",
            3 => "ThreeDimensionalArrayFormatter",
            _ => "FourDimensionalArrayFormatter",
        };
        var closedTypeName = array.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (!builtIns.ContainsKey(closedTypeName))
        {
            builtIns.Add(closedTypeName, new HarvestedBuiltInModel(
                ClosedTypeName: closedTypeName,
                FormatterConstruction: $"new global::MessagePack.Formatters.{formatterName}<TWriteBuffer, TReadBuffer, {array.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()"));
        }
    }

    // the compile-time mirror of GenericFormatterFactory's type table: an open BCL
    // definition maps to the formatter the corresponding factory constructs, so the
    // harvested closed instantiation behaves identically to the JIT chain's result
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

    // the closed instantiations BuiltInFormatterFactory serves with specialized formatters
    // (bin-format byte containers, codec-backed primitive element loops)
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

    // these formatters preserve the collection's comparer semantics through an explicit
    // constructor parameter (no parameterless constructor exists on them)
    static bool FormatterTakesComparer(string formatterName) => formatterName is
        "HashSetFormatter" or "SortedSetFormatter" or "ReadOnlySetFormatter"
        or "InterfaceSetFormatter" or "InterfaceReadOnlySetFormatter"
        or "DictionaryFormatter" or "InterfaceDictionaryFormatter" or "InterfaceReadOnlyDictionaryFormatter"
        or "ReadOnlyDictionaryFormatter" or "ConcurrentDictionaryFormatter" or "OrderedDictionaryFormatter"
        or "ImmutableHashSetFormatter" or "ImmutableDictionaryFormatter"
        or "InterfaceImmutableSetFormatter" or "InterfaceImmutableDictionaryFormatter"
        or "FrozenDictionaryFormatter" or "FrozenSetFormatter"
        or "InterfaceLookupFormatter";

    // mirrors GenericFormatterFactory's default-ctor catch-all: a concrete class with a
    // public parameterless constructor exposing IDictionary<K,V>, ICollection<T>, or the
    // non-generic IList/IDictionary views serializes through the generic catch-all
    // formatters; types another tier claims (SpecialType scalars, [MessagePackObject],
    // type-level [MessagePackFormatter], ExpandoObject) stay off the registry
    static void HarvestCollectionCatchAll(INamedTypeSymbol named, Compilation compilation, Dictionary<string, HarvestedGenericModel> userGenerics, Dictionary<string, HarvestedBuiltInModel> builtIns)
    {
        if (named.TypeKind != TypeKind.Class || named.IsAbstract || named.SpecialType != SpecialType.None)
        {
            return;
        }
        if (ContainsTypeParameter(named) || !IsAccessibleToGeneratedCode(named, compilation))
        {
            return;
        }
        if (HasMessagePackObjectAttribute(named.OriginalDefinition) || HasAttribute(named.OriginalDefinition, FormatterAttributeName))
        {
            return;
        }
        if (named.ToDisplayString() == "System.Dynamic.ExpandoObject")
        {
            return; // deliberately unserved by default (the deprecated quadratic-Add path)
        }
        // the catch-all formatters carry a new() constraint
        if (!named.InstanceConstructors.Any(static c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
        {
            return;
        }
        var closedTypeName = named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (builtIns.ContainsKey(closedTypeName))
        {
            return;
        }
        INamedTypeSymbol? dictionaryInterface = null;
        INamedTypeSymbol? collectionInterface = null;
        var nonGenericList = false;
        var nonGenericDictionary = false;
        foreach (var iface in named.AllInterfaces)
        {
            var ns = iface.ContainingNamespace.ToDisplayString();
            if (iface.IsGenericType && ns == "System.Collections.Generic")
            {
                if (iface.OriginalDefinition.MetadataName == "IDictionary`2")
                {
                    dictionaryInterface = iface;
                    break; // the dictionary view always wins (runtime keeps scanning for it too)
                }
                if (collectionInterface is null && iface.OriginalDefinition.MetadataName == "ICollection`1")
                {
                    collectionInterface = iface;
                }
            }
            else if (ns == "System.Collections")
            {
                nonGenericList |= iface.MetadataName == "IList";
                nonGenericDictionary |= iface.MetadataName == "IDictionary";
            }
        }
        if (dictionaryInterface is not null || collectionInterface is not null)
        {
            var arguments = (dictionaryInterface ?? collectionInterface)!.TypeArguments;
            if (!arguments.All(a => IsAccessibleToGeneratedCode(a, compilation)))
            {
                return;
            }
            var argumentNames = string.Join(", ", arguments.Select(static a => a.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            var construction = dictionaryInterface is not null
                ? $"new global::MessagePack.Formatters.GenericDictionaryFormatter<TWriteBuffer, TReadBuffer, {argumentNames}, {closedTypeName}>()"
                : $"new global::MessagePack.Formatters.GenericCollectionFormatter<TWriteBuffer, TReadBuffer, {argumentNames}, {closedTypeName}>()";
            // register BEFORE recursing: a self-referential shape (class W : ICollection<W>)
            // must hit the ContainsKey guard on re-entry
            builtIns.Add(closedTypeName, new HarvestedBuiltInModel(closedTypeName, construction));
            foreach (var argument in arguments)
            {
                HarvestSerializedType(argument, compilation, userGenerics, builtIns);
            }
            return;
        }
        if (nonGenericList)
        {
            builtIns.Add(closedTypeName, new HarvestedBuiltInModel(closedTypeName, $"new global::MessagePack.Formatters.NonGenericListFormatter<TWriteBuffer, TReadBuffer, {closedTypeName}>()"));
        }
        else if (nonGenericDictionary)
        {
            builtIns.Add(closedTypeName, new HarvestedBuiltInModel(closedTypeName, $"new global::MessagePack.Formatters.NonGenericDictionaryFormatter<TWriteBuffer, TReadBuffer, {closedTypeName}>()"));
        }
    }

    internal static bool IsAccessibleToGeneratedCode(ITypeSymbol type, Compilation compilation) => type switch
    {
        IArrayTypeSymbol array => IsAccessibleToGeneratedCode(array.ElementType, compilation),
        INamedTypeSymbol named => compilation.IsSymbolAccessibleWithin(named, compilation.Assembly)
            && named.TypeArguments.All(argument => argument is ITypeParameterSymbol || IsAccessibleToGeneratedCode(argument, compilation)),
        _ => true,
    };

    internal static bool ContainsTypeParameter(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol => true,
        IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter),
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
        // parity with ReflectionObjectFormatter: parameterized matching considers public
        // constructors only; `new T()` additionally accepts internal (same assembly)
        return constructor.DeclaredAccessibility == Accessibility.Public
            || (parameterless && constructor.DeclaredAccessibility == Accessibility.Internal);
    }

    static bool TryMatchConstructor(
        IMethodSymbol constructor,
        List<(string Name, ITypeSymbol Type, SetterKind Setter, int IntKey, string StringKey, bool RequiredModifier, LocationInfo? Location, CustomFormatterModel? Custom)> entries,
        int[] parameterIndexByEntry)
    {
        var parameters = constructor.Parameters;
        for (int p = 0; p < parameters.Length; p++)
        {
            var found = -1;
            for (int e = 0; e < entries.Count; e++)
            {
                if (string.Equals(entries[e].Name, parameters[p].Name, StringComparison.OrdinalIgnoreCase)
                    && SymbolEqualityComparer.Default.Equals(entries[e].Type, parameters[p].Type))
                {
                    found = e;
                    break;
                }
            }
            // an already-claimed entry (two parameters differing only by case) would emit
            // a constructor call with a hole; reject the whole candidate instead
            if (found < 0 || parameterIndexByEntry[found] >= 0)
            {
                return false;
            }
            parameterIndexByEntry[found] = p;
        }
        return true;
    }

    // ---- member-level [MessagePackFormatter] binding -----------------------------------
    // The attribute names either a MessagePackFormatterFactory (closed, args go to its
    // constructor, the emitter appends the CreateFormatter call) or a formatter as an
    // unbound generic over the buffer pair (typeof(MyFormatter<,>)). Arguments bind
    // positionally to a constructor; a string argument whose target parameter is not
    // string is bound as a dotted static-member EXPRESSION at the attribute site
    // ("StringComparer.OrdinalIgnoreCase") and emitted as direct code — this form exists
    // only here: the runtime tier rejects it and points at the source generator.
    // memberType is the serialized value's (the member's) type.

    internal static bool TryBuildCustomFormatter(
        AttributeData attribute,
        ITypeSymbol memberType,
        string subject,
        LocationInfo? location,
        GeneratorAttributeSyntaxContext context,
        List<DiagnosticInfo> diagnostics,
        out CustomFormatterModel? custom)
    {
        custom = null;
        var member = subject;

        // two attribute shapes: the Type-based constructor (first argument is the factory
        // type, optional params array behind it) and MessagePackFormatterAttribute<TFactory>
        // (the factory is the attribute's own type argument, the constructor carries only
        // the params array)
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
            diagnostics.Add(new DiagnosticInfo("MsgPack012", $"'{member}': [MessagePackFormatter] does not carry a usable factory type.", location));
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

        if (isFactory)
        {
            if (formatterType.IsUnboundGenericType || formatterType.IsAbstract)
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack012", $"'{member}': '{formatterType.ToDisplayString()}' must be a concrete, fully constructed MessagePackFormatterFactory (close every type argument inside the typeof).", location));
                return false;
            }
            if (!TryBindConstructor(formatterType.InstanceConstructors, arguments, compilation, semanticModel, position, member, diagnostics, location, out var factoryArguments))
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
                formatterInterface = implemented;
                break;
            }
        }
        if (formatterInterface is null)
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack012", $"'{member}': '{formatterType.ToDisplayString()}' is neither a MessagePackFormatterFactory nor an IMessagePackFormatter implementation.", location));
            return false;
        }

        // a closed formatter type would pin one buffer pair and can never satisfy the
        // generated formatter's own TWriteBuffer/TReadBuffer
        if (!formatterType.IsUnboundGenericType
            || definition.TypeParameters.Length != 2
            || formatterInterface.TypeArguments[0] is not ITypeParameterSymbol { Ordinal: 0 }
            || formatterInterface.TypeArguments[1] is not ITypeParameterSymbol { Ordinal: 1 })
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack012", $"'{member}': pass a formatter as an unbound generic over the buffer pair, e.g. typeof(MyFormatter<,>) where MyFormatter<TWriteBuffer, TReadBuffer> implements IMessagePackFormatter for the member type; anything else needs a MessagePackFormatterFactory.", location));
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(formatterInterface.TypeArguments[2], memberType))
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack012", $"'{member}': '{formatterType.ToDisplayString()}' serializes '{formatterInterface.TypeArguments[2].ToDisplayString()}', but the member type is '{memberType.ToDisplayString()}'.", location));
            return false;
        }

        if (!TryBindConstructor(definition.InstanceConstructors, arguments, compilation, semanticModel, position, member, diagnostics, location, out var formatterArguments))
        {
            return false;
        }
        custom = new CustomFormatterModel(
            FactoryNew: null,
            TypeOfExpr: null,
            FormatterNew: $"new {StripTypeArguments(definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))}<TWriteBuffer, TReadBuffer>({formatterArguments})");
        return true;
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
        out string rendered)
    {
        rendered = "";
        var matches = new List<(int FilledDefaults, string Rendered)>();
        string? failure = null;

        foreach (var constructor in constructors)
        {
            // trailing parameters beyond the supplied arguments are fine when they are all
            // optional: the emitted `new F(args)` is C# source, so the compiler fills the
            // defaults (the runtime Activator path cannot, but never runs for SG-bound types)
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
            for (int i = 0; i < arguments.Length && bound; i++)
            {
                if (!TryRenderArgument(arguments[i], constructor.Parameters[i].Type, constructor.Parameters[i].Name, compilation, semanticModel, position, out parts[i], out var reason))
                {
                    bound = false;
                    failure ??= reason;
                }
            }
            if (bound)
            {
                matches.Add((constructor.Parameters.Length - arguments.Length, string.Join(", ", parts)));
            }
        }

        // mirror C# overload preference: a candidate filling fewer defaults is better
        if (matches.Count > 0)
        {
            var fewestDefaults = matches.Min(static m => m.FilledDefaults);
            var winners = matches.Where(m => m.FilledDefaults == fewestDefaults).ToArray();
            if (winners.Length == 1)
            {
                rendered = winners[0].Rendered;
                return true;
            }
        }
        diagnostics.Add(new DiagnosticInfo("MsgPack013", matches.Count == 0
            ? $"'{member}': the [MessagePackFormatter] arguments do not bind to any accessible constructor taking {arguments.Length} argument(s).{(failure is null ? "" : " " + failure)}"
            : $"'{member}': the [MessagePackFormatter] arguments bind to more than one constructor; disambiguate the overloads.", location));
        return false;
    }

    // every rendered argument carries an explicit cast to the chosen parameter type, so
    // the generated call can never re-run overload resolution to a different constructor
    static bool TryRenderArgument(
        TypedConstant argument,
        ITypeSymbol targetType,
        string parameterName,
        Compilation compilation,
        SemanticModel? semanticModel,
        int position,
        out string rendered,
        out string? failure)
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
            case TypedConstantKind.Primitive when argument.Value is string text && targetType.SpecialType != SpecialType.System_String:
                return TryRenderExpressionArgument(text, targetType, cast, compilation, semanticModel, position, out rendered, out failure);

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
                if (targetType is not IArrayTypeSymbol arrayType)
                {
                    failure = $"An array argument requires an array-typed parameter, but '{parameterName}' is {targetType.ToDisplayString()}.";
                    return false;
                }
                var elements = new string[argument.Values.Length];
                for (int i = 0; i < elements.Length; i++)
                {
                    if (!TryRenderArgument(argument.Values[i], arrayType.ElementType, parameterName, compilation, semanticModel, position, out elements[i], out failure))
                    {
                        return false;
                    }
                }
                rendered = $"new {arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}[] {{ {string.Join(", ", elements)} }}";
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
        out string? failure)
    {
        rendered = "";
        if (semanticModel is null)
        {
            failure = $"\"{text}\": expression-form arguments need a source location to bind at.";
            return false;
        }

        // grammar is deliberately tiny: a dotted static-member reference, nothing else —
        // no `new`, no invocations, no indexers, so the string can never grow into a DSL
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

        rendered = $"{cast}{target.Container.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{symbol!.Name}";
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
            var style = type.FindImplementationForInterfaceMember(method) is IMethodSymbol
            {
                MethodKind: MethodKind.Ordinary,
                DeclaredAccessibility: Accessibility.Public,
            } implementation && implementation.Name == method.Name
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
                clauses.Add($"where {typeParameter.Name} : {string.Join(", ", parts)}");
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
            declarations.Add($"partial {Keyword(declaration)} {declaration.Name}");
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

    // Nullable<primitive> joins the direct tier: nil is 1 byte, under every primitive's
    // max, so the fused reservation is unchanged and the write becomes a nil-or-value
    // expression — same wire as NullableFormatter, no formatter field
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
        // System_String and System_DateTime are deliberately absent: their wire form is
        // factory-chain configurable (see DirectKind), so they go through the resolved formatter
        _ => DirectKind.None,
    };

    internal static string Sanitize(string fullTypeName)
    {
        var builder = new StringBuilder(fullTypeName.Length);
        // drop the "global::" prefix, then flatten every separator to '_'. The encoding
        // must be INJECTIVE or two types share one generated name (a loud but cryptic
        // CS0101 / duplicate-hint failure): a literal '_' doubles to "__" so identifier
        // underscores (even runs) never alias separator underscores (single), and the
        // space of ", " is dropped so a two-parameter list does not alias a doubled
        // underscore either — Ns.A_B / Ns.A.B and Foo<T, U> / Foo<T_U> all stay distinct.
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
                builder.Append("__");
            }
            else if (c != ' ')
            {
                builder.Append('_');
            }
        }
        return builder.ToString();
    }
}

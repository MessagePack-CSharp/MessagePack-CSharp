using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MessagePack.SourceGenerator.Analyzers;

/// <summary>
/// Every serialized member's type must be resolvable by the default formatter chain, or
/// deserialization dies at runtime with formatter-not-found. The set of "resolvable"
/// types is not hardcoded here, that table would silently drift from the library, it is
/// harvested from the referenced MessagePack assembly (and the user's own assembly):
/// every type implementing IMessagePackFormatter&lt;TWriteBuffer, TReadBuffer, TValue&gt;
/// contributes its TValue as a pattern, with the formatter's own type parameters acting
/// as wildcards whose bindings are checked recursively (ListFormatter's List&lt;T&gt;
/// pattern makes List&lt;X&gt; exactly as serializable as X). On top of the harvest:
///   - type parameters, enums (GenericFormatterFactory closes EnumFormatter) pass
///   - [MessagePackObject]/[UnionTag] annotated types pass (generated or reflection tier)
///   - [MessagePackFormatter] on the member, or type-level on a non-generic member type, passes
///   - [assembly: MessagePackKnownType(typeof(X))] passes X
/// Severity is Warning, not Error: the factory chain is composable at runtime, and the
/// analyzer can only approximate the default chain, the assembly attribute above is
/// the sanctioned way to tell it about custom tiers (harvested from every referenced
/// assembly too, so a library declares its coverage once for all consumers).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SerializableMemberTypeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MsgPack101";

    const string FormatterInterfaceMetadataName = "MessagePack.IMessagePackFormatter`3";
    const string MessagePackObjectAttributeName = "MessagePack.MessagePackObjectAttribute";
    const string UnionTagAttributeName = "MessagePack.UnionTagAttribute";
    const string KeyAttributeName = "MessagePack.KeyAttribute";
    const string IgnoreMemberAttributeName = "MessagePack.IgnoreMemberAttribute";
    const string IgnoreDataMemberAttributeName = "System.Runtime.Serialization.IgnoreDataMemberAttribute";
    const string DataContractAttributeName = "System.Runtime.Serialization.DataContractAttribute";
    const string FormatterAttributeName = "MessagePack.MessagePackFormatterAttribute";
    const string KnownTypeAttributeName = "MessagePack.MessagePackKnownTypeAttribute";

    static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Serialized member type has no resolvable formatter",
        "'{0}' (reached from '{1}') has no formatter the default chain can resolve: annotate it with [MessagePackObject] (plus [UnionTag] for a polymorphic base), point at a custom formatter with [MessagePackFormatter], or declare coverage with [assembly: MessagePackKnownType]",
        "MessagePack.SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A [MessagePackObject] member (or [UnionTag] case type) whose type no formatter serves fails at runtime with formatter-not-found. The serializable set is harvested from the formatter implementations visible to the compilation, so custom formatters in the user's own assembly are recognized automatically; coverage provided any other way (factories, runtime registration, other assemblies) is declared with [assembly: MessagePackKnownType].");

    // MsgPack108, the usage-site twin of MsgPack101 (v3's MsgPack003 territory): the payload type of a
    // MessagePackSerializer.Serialize<T>/Deserialize<T> call gets the same resolvability check as a serialized member,
    // catching root types that never appear as anyone's member. Same Warning rationale: the analyzer approximates the
    // default chain. A call passing its own options swaps that chain out entirely (contractless, custom resolvers),
    // so only default-chain calls are judged: no options argument, or an argument that is literally
    // MessagePackSerializerOptions.Default.
    public const string UsageDiagnosticId = "MsgPack108";

    const string SerializerTypeName = "MessagePack.MessagePackSerializer";

    static readonly DiagnosticDescriptor UsageRule = new(
        UsageDiagnosticId,
        "Serialized payload type has no resolvable formatter",
        "'{0}' (the payload of this {1} call) has no formatter the default chain can resolve: annotate it with [MessagePackObject] (plus [UnionTag] for a polymorphic base), point at a custom formatter with [MessagePackFormatter], or declare coverage with [assembly: MessagePackKnownType]",
        "MessagePack.SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A payload type passed to MessagePackSerializer that no formatter serves fails at runtime with formatter-not-found; the same harvest that checks serialized members checks the call site. Calls passing custom options (any options expression other than MessagePackSerializerOptions.Default) are not judged: their resolver chain replaces the default chain the analyzer models.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, UsageRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static startContext =>
        {
            var formatterInterface = startContext.Compilation.GetTypeByMetadataName(FormatterInterfaceMetadataName);
            if (formatterInterface is null)
            {
                return;
            }

            var registry = new Lazy<Registry>(() => Registry.Build(startContext.Compilation, formatterInterface));
            startContext.RegisterSymbolAction(
                context => AnalyzeType(context, registry.Value),
                SymbolKind.NamedType);
            startContext.RegisterOperationAction(
                context => AnalyzeInvocation(context, registry.Value),
                OperationKind.Invocation);
        });
    }

    static void AnalyzeType(SymbolAnalysisContext context, Registry registry)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (FindAttribute(type, UnionTagAttributeName) is not null)
        {
            // a union root's own members never ride the wire (the generator routes it to UnionParser,
            // not ObjectParser), so only the case types get checked, this also keeps a struct union's public Value
            // property out of the member walk
            AnalyzeUnionCases(context, registry, type);
            return;
        }

        var objectAttribute = FindAttribute(type, MessagePackObjectAttributeName);
        if (objectAttribute is null)
        {
            return;
        }

        // bool overload: true = map-by-name; KeyNamingPolicy overload (an int-typed argument): always map-by-name
        var keyAsPropertyName = objectAttribute.ConstructorArguments.Length > 0 && objectAttribute.ConstructorArguments[0].Value is true or int;
        var allowPrivate = ReadNamedBool(objectAttribute, "AllowPrivate");

        foreach (var (member, memberType) in SerializedMembers(type, keyAsPropertyName, allowPrivate))
        {
            if (HasAttributeOn(member, FormatterAttributeName))
            {
                continue; // member-level formatter override takes over resolution
            }
            if (registry.TryFindUnserializable(memberType, out var offender))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    PickLocation(member),
                    OffenderProperties(offender),
                    offender.ToDisplayString(),
                    $"{type.ToDisplayString()}.{member.Name}"));
            }
        }
    }

    static void AnalyzeInvocation(OperationAnalysisContext context, Registry registry)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        if (!method.IsGenericMethod
            || method.TypeArguments.Length != 1
            || method.ContainingType?.ToDisplayString() != SerializerTypeName)
        {
            return;
        }
        if (PassesCustomOptions(invocation))
        {
            return;
        }
        // open payloads pass (judged at the closed call, like member type parameters)
        if (registry.TryFindUnserializable(method.TypeArguments[0], out var offender))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                UsageRule,
                invocation.Syntax.GetLocation(),
                OffenderProperties(offender),
                offender.ToDisplayString(),
                method.Name));
        }
    }

    // A custom options argument carries its own resolver chain, which this analyzer cannot see into,
    // judging the call against the default chain would false-positive every contractless call site.
    // Only MessagePackSerializerOptions.Default keeps the check.
    static bool PassesCustomOptions(IInvocationOperation invocation)
    {
        foreach (var argument in invocation.Arguments)
        {
            if (!IsOptionsType(argument.Parameter?.Type))
            {
                continue;
            }
            if (argument.ArgumentKind != ArgumentKind.Explicit)
            {
                return false; // omitted optional options rides the default chain
            }
            var value = argument.Value;
            while (value is IConversionOperation conversion)
            {
                value = conversion.Operand;
            }
            if (value.ConstantValue is { HasValue: true, Value: null })
            {
                return false; // an explicit null routes to the default chain too
            }
            return value is not IPropertyReferenceOperation reference
                || !reference.Property.IsStatic
                || reference.Property.Name != "Default"
                || !IsOptionsType(reference.Property.ContainingType);
        }
        return false;
    }

    // symbol comparison, not display strings: the parameter may be nullable-annotated (MessagePackSerializerOptions?),
    // which a name comparison must not trip over
    static bool IsOptionsType(ITypeSymbol? type) =>
        type is INamedTypeSymbol { Name: "MessagePackSerializerOptions", ContainingType: null } named
        && named.ContainingNamespace?.ToDisplayString() == "MessagePack";

    static void AnalyzeUnionCases(SymbolAnalysisContext context, Registry registry, INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (!UnionParser.IsUnionTagAttribute(attribute.AttributeClass)
                || UnionParser.ResolveCaseType(attribute, type) is not { } caseType)
            {
                continue;
            }
            if (registry.TryFindUnserializable(caseType, out var offender))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    PickLocation(type),
                    OffenderProperties(offender),
                    offender.ToDisplayString(),
                    $"[UnionTag] of {type.ToDisplayString()}"));
            }
        }
    }

    // the generator's participation rules, reduced to "which member types ride the wire": properties across the
    // hierarchy then fields, derived-first dedup; public members (public getter) participate,
    // non-public only with allowPrivate; a member counts when it carries [Key] or the type uses map-by-name mode
    static IEnumerable<(ISymbol Member, ITypeSymbol Type)> SerializedMembers(INamedTypeSymbol type, bool keyAsPropertyName, bool allowPrivate)
    {
        // dedup by override chain, not by name, mirroring ObjectParser: an override shares its base declaration's
        // storage, but a `new`-shadowed member is separate storage and every declaration's type rides the wire
        var seenOverrideRoots = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object && current.SpecialType != SpecialType.System_ValueType; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol { IsStatic: false, IsIndexer: false, IsImplicitlyDeclared: false } property
                    && property.GetMethod is { } getter
                    && (allowPrivate || (property.DeclaredAccessibility == Accessibility.Public && getter.DeclaredAccessibility == Accessibility.Public)))
                {
                    var root = (IPropertySymbol)property.OriginalDefinition;
                    while (root.OverriddenProperty is { } overridden)
                    {
                        root = (IPropertySymbol)overridden.OriginalDefinition;
                    }
                    if (seenOverrideRoots.Add(root)
                        && Participates(property, keyAsPropertyName)
                        && !ObjectParser.IsUnknownMembersType(property.Type))
                    {
                        yield return (property, property.Type);
                    }
                }
            }
        }
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object && current.SpecialType != SpecialType.System_ValueType; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                // fields never override, so every declaration is its own storage
                if (member is IFieldSymbol { IsStatic: false, IsConst: false, IsImplicitlyDeclared: false } field
                    && (allowPrivate || field.DeclaredAccessibility == Accessibility.Public)
                    && Participates(field, keyAsPropertyName)
                    && !ObjectParser.IsUnknownMembersType(field.Type))
                {
                    yield return (field, field.Type);
                }
            }
        }
    }

    static bool Participates(ISymbol member, bool keyAsPropertyName)
    {
        var hasKey = false;
        // override-chain walk, mirroring the parser: an override inherits [Key]/[IgnoreMember] from the base virtual
        // declaration
        foreach (var attribute in ObjectParser.MemberAttributes(member))
        {
            var name = attribute.AttributeClass?.ToDisplayString();
            if (name == IgnoreMemberAttributeName || name == IgnoreDataMemberAttributeName)
            {
                return false;
            }
            if (name == KeyAttributeName)
            {
                hasKey = true;
            }
        }
        // an unkeyed public member outside map-by-name mode is the generator's MsgPack001 error;
        // no point stacking a type warning on top of it
        return hasKey || keyAsPropertyName;
    }

    // hands the code fix the offender's identity (the diagnostic sits on the member,
    // but the fix annotates the offending type, possibly in another document)
    static ImmutableDictionary<string, string?> OffenderProperties(ITypeSymbol offender) =>
        offender is INamedTypeSymbol { IsGenericType: false } named
            && DocumentationCommentId.CreateDeclarationId(named) is { } declarationId
            ? ImmutableDictionary<string, string?>.Empty.Add("OffenderDocId", declarationId)
            : ImmutableDictionary<string, string?>.Empty;

    static AttributeData? FindAttribute(ISymbol symbol, string fullName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            for (var attributeType = attribute.AttributeClass; attributeType is not null; attributeType = attributeType.BaseType)
            {
                if (attributeType.ToDisplayString() == fullName)
                {
                    return attribute;
                }
            }
        }
        return null;
    }

    static bool HasAttributeOn(ISymbol symbol, string fullName) => FindAttribute(symbol, fullName) is not null;

    static bool ReadNamedBool(AttributeData attribute, string name)
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

    static Location PickLocation(ISymbol symbol)
    {
        foreach (var location in symbol.Locations)
        {
            if (location.SourceTree is not null)
            {
                return location;
            }
        }
        return Location.None;
    }

    sealed class Registry
    {
        // patterns indexed by the served named type's original definition; array patterns (T[], T[,], ...)
        // keyed by rank instead
        readonly Dictionary<INamedTypeSymbol, List<ITypeSymbol>> namedPatterns = new(SymbolEqualityComparer.Default);
        readonly HashSet<int> arrayRanks = [];
        readonly HashSet<ITypeSymbol> knownTypes = new(SymbolEqualityComparer.Default);
        readonly HashSet<ITypeSymbol> surrogateTargets = new(SymbolEqualityComparer.Default);
        readonly ConcurrentDictionary<ITypeSymbol, bool> memo = new(SymbolEqualityComparer.Default);
        Compilation compilation = null!;

        public static Registry Build(Compilation compilation, INamedTypeSymbol formatterInterface)
        {
            var registry = new Registry { compilation = compilation };
            var interfaceDefinition = formatterInterface.OriginalDefinition;

            // the MessagePack assembly is the default-chain surface; the user's own assembly makes hand-written
            // formatters count without any declaration
            HarvestNamespace(formatterInterface.ContainingAssembly.GlobalNamespace, interfaceDefinition, registry);
            HarvestNamespace(compilation.Assembly.GlobalNamespace, interfaceDefinition, registry);

            // IMessagePackSurrogate implementations auto-register their targets through the generated factory,
            // so a target is serializable with no declaration of its own (same own-assembly scope as hand-written
            // formatters: another assembly's surrogates declare coverage with [assembly: MessagePackKnownType])
            if (compilation.GetTypeByMetadataName("MessagePack.IMessagePackSurrogate`2") is { } surrogateInterface)
            {
                HarvestSurrogateTargets(compilation.Assembly.GlobalNamespace, surrogateInterface, registry);
            }

            registry.HarvestAssemblyAttributes(compilation.Assembly);
            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    registry.HarvestAssemblyAttributes(assembly);
                }
            }
            return registry;
        }

        static void HarvestNamespace(INamespaceSymbol ns, INamedTypeSymbol interfaceDefinition, Registry registry)
        {
            foreach (var member in ns.GetMembers())
            {
                if (member is INamespaceSymbol nested)
                {
                    HarvestNamespace(nested, interfaceDefinition, registry);
                }
                else if (member is INamedTypeSymbol type)
                {
                    registry.HarvestFormatterType(type, interfaceDefinition);
                }
            }
        }

        static void HarvestSurrogateTargets(INamespaceSymbol ns, INamedTypeSymbol surrogateDefinition, Registry registry)
        {
            foreach (var member in ns.GetMembers())
            {
                if (member is INamespaceSymbol nested)
                {
                    HarvestSurrogateTargets(nested, surrogateDefinition, registry);
                }
                else if (member is INamedTypeSymbol type)
                {
                    registry.HarvestSurrogateType(type, surrogateDefinition);
                }
            }
        }

        void HarvestSurrogateType(INamedTypeSymbol type, INamedTypeSymbol surrogateDefinition)
        {
            foreach (var implemented in type.AllInterfaces)
            {
                // only the self-shaped, registrable implementations count: a generic surrogate is skipped by the
                // generator too (MsgPack019)
                if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, surrogateDefinition)
                    && SymbolEqualityComparer.Default.Equals(implemented.TypeArguments[1], type)
                    && !ObjectParser.ContainsTypeParameter(type)
                    && !ObjectParser.ContainsTypeParameter(implemented.TypeArguments[0]))
                {
                    surrogateTargets.Add(implemented.TypeArguments[0]);
                }
            }
            foreach (var nested in type.GetTypeMembers())
            {
                HarvestSurrogateType(nested, surrogateDefinition);
            }
        }

        void HarvestFormatterType(INamedTypeSymbol type, INamedTypeSymbol interfaceDefinition)
        {
            foreach (var implemented in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, interfaceDefinition))
                {
                    AddPattern(implemented.TypeArguments[2]);
                }
            }
            foreach (var nested in type.GetTypeMembers())
            {
                HarvestFormatterType(nested, interfaceDefinition);
            }
        }

        void AddPattern(ITypeSymbol pattern)
        {
            switch (pattern)
            {
                case ITypeParameterSymbol:
                    // a bare-wildcard pattern (EnumFormatter's T, blit formatters)
                    // would declare everything serializable; those tiers are modeled by the explicit enum rule instead
                    return;
                case IArrayTypeSymbol array:
                    arrayRanks.Add(array.Rank);
                    return;
                case INamedTypeSymbol named:
                    var key = named.OriginalDefinition;
                    if (!namedPatterns.TryGetValue(key, out var list))
                    {
                        namedPatterns[key] = list = [];
                    }
                    list.Add(named);
                    return;
            }
        }

        void HarvestAssemblyAttributes(IAssemblySymbol assembly)
        {
            foreach (var attribute in assembly.GetAttributes())
            {
                var name = attribute.AttributeClass?.ToDisplayString();
                if (attribute.ConstructorArguments.Length != 1 || attribute.ConstructorArguments[0].Value is not ITypeSymbol argument)
                {
                    continue;
                }
                if (name == KnownTypeAttributeName)
                {
                    knownTypes.Add(argument);
                    if (argument is INamedTypeSymbol { IsGenericType: true } generic)
                    {
                        knownTypes.Add(generic.OriginalDefinition);
                    }
                }
            }
        }

        /// <summary>Returns true when something inside <paramref name="type"/> cannot be resolved, with the innermost offending type.</summary>
        public bool TryFindUnserializable(ITypeSymbol type, out ITypeSymbol offender)
        {
            offender = type;
            return !IsSerializable(type, ref offender);
        }

        bool IsSerializable(ITypeSymbol type, ref ITypeSymbol offender)
        {
            switch (type)
            {
                case ITypeParameterSymbol:
                    return true; // judged at the closed instantiation
                case IDynamicTypeSymbol:
                    return true; // = object, PrimitiveObjectFormatter territory
                case IArrayTypeSymbol array:
                    if (!arrayRanks.Contains(array.Rank))
                    {
                        offender = array;
                        return false;
                    }
                    return IsSerializable(array.ElementType, ref offender);
                case INamedTypeSymbol named:
                    return IsNamedSerializable(named, ref offender);
                default:
                    offender = type; // pointers, function pointers, byref-like oddities
                    return false;
            }
        }

        bool IsNamedSerializable(INamedTypeSymbol type, ref ITypeSymbol offender)
        {
            if (type.TypeKind is TypeKind.Error or TypeKind.Enum)
            {
                return true; // errors are the compiler's report; enums close EnumFormatter
            }
            if (memo.TryGetValue(type, out var known))
            {
                if (!known)
                {
                    offender = type;
                }
                return known;
            }
            // optimistic seed breaks cycles (a self-referencing annotated type is fine)
            memo[type] = true;

            var result = ComputeNamedSerializable(type, ref offender);
            memo[type] = result;
            return result;
        }

        bool ComputeNamedSerializable(INamedTypeSymbol type, ref ITypeSymbol offender)
        {
            if (knownTypes.Contains(type) || knownTypes.Contains(type.OriginalDefinition))
            {
                return true;
            }
            if (surrogateTargets.Contains(type))
            {
                return true;
            }
            if (FindAttribute(type, MessagePackObjectAttributeName) is not null
                || FindAttribute(type, UnionTagAttributeName) is not null
                // served by the Default chain's reflection tail (SG stays out, v3-parity)
                || FindAttribute(type, DataContractAttributeName) is not null)
            {
                return true;
            }
            // type-level [MessagePackFormatter] registers through the annotated assembly's generated factory,
            // but only for non-generic types (MsgPack014 territory otherwise)
            if (type is not INamedTypeSymbol { IsGenericType: true }
                && FindAttribute(type, FormatterAttributeName) is not null)
            {
                return true;
            }

            if (namedPatterns.TryGetValue(type.OriginalDefinition, out var patterns))
            {
                // a pattern match binds the member type's arguments to the formatter's wildcards;
                // the binding must itself be serializable (List<X> ⇔ X)
                foreach (var pattern in patterns)
                {
                    var bindings = new List<ITypeSymbol>();
                    if (Match(type, pattern, bindings))
                    {
                        var allBindingsOk = true;
                        foreach (var binding in bindings)
                        {
                            if (!IsSerializable(binding, ref offender))
                            {
                                allBindingsOk = false;
                                break;
                            }
                        }
                        if (allBindingsOk)
                        {
                            return true;
                        }
                    }
                }
                // structurally served, but a type argument was the problem: offender already points at the innermost
                // failure
                if (!SymbolEqualityComparer.Default.Equals(offender, type))
                {
                    return false;
                }
            }

            // the runtime catch-all tier (v3 DynamicGenericResolver's inherited-type rules,
            // mirrored by the source generator's collection harvest for AOT): a public parameterless constructor
            // unlocks the Add-based formatters over IDictionary<K,V>/ICollection<T>/the non-generic views,
            // and a public single-parameter collection-accepting constructor unlocks the construct-from-intermediate
            // formatters (IReadOnlyDictionary<K,V>, IEnumerable<T>). Their TValue patterns are bare wildcards,
            // skipped by the harvest above, so the rule is modeled explicitly.
            if (type.TypeKind == TypeKind.Class
                && !type.IsAbstract
                && type.ToDisplayString() != "System.Dynamic.ExpandoObject")
            {
                var hasDefaultConstructor = HasPublicParameterlessConstructor(type);
                INamedTypeSymbol? dictionaryInterface = null;
                INamedTypeSymbol? collectionInterface = null;
                INamedTypeSymbol? readOnlyDictionaryInterface = null;
                List<INamedTypeSymbol>? enumerableInterfaces = null;
                var nonGenericView = false;
                foreach (var implemented in type.AllInterfaces)
                {
                    var interfaceNamespace = implemented.ContainingNamespace.ToDisplayString();
                    if (implemented.IsGenericType && interfaceNamespace == "System.Collections.Generic")
                    {
                        if (implemented.OriginalDefinition.MetadataName == "IDictionary`2" && hasDefaultConstructor)
                        {
                            dictionaryInterface = implemented;
                            break;
                        }
                        if (collectionInterface is null && implemented.OriginalDefinition.MetadataName == "ICollection`1")
                        {
                            collectionInterface = implemented;
                        }
                        if (readOnlyDictionaryInterface is null && implemented.OriginalDefinition.MetadataName == "IReadOnlyDictionary`2")
                        {
                            readOnlyDictionaryInterface = implemented;
                        }
                        if (implemented.OriginalDefinition.MetadataName == "IEnumerable`1")
                        {
                            (enumerableInterfaces ??= new()).Add(implemented);
                        }
                    }
                    else if (interfaceNamespace == "System.Collections" && implemented.MetadataName is "IList" or "IDictionary")
                    {
                        nonGenericView = true; // object elements ride PrimitiveObjectFormatter
                    }
                }

                // runtime priority: IDictionary+new -> IReadOnlyDictionary+ctor ->
                // ICollection+new -> non-generic views+new -> IEnumerable<T>+ctor
                INamedTypeSymbol? chosen = dictionaryInterface;
                if (chosen is null && readOnlyDictionaryInterface is not null
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
                    if (ObjectParser.HasCollectionAcceptingConstructor(type, acceptable, compilation))
                    {
                        chosen = readOnlyDictionaryInterface;
                    }
                }
                if (chosen is null && hasDefaultConstructor && collectionInterface is not null)
                {
                    chosen = collectionInterface;
                }
                if (chosen is null && hasDefaultConstructor && nonGenericView)
                {
                    return true;
                }
                if (chosen is null && enumerableInterfaces is not null)
                {
                    foreach (var implemented in enumerableInterfaces)
                    {
                        if (ObjectParser.HasCollectionAcceptingConstructor(type, [implemented], compilation))
                        {
                            chosen = implemented;
                            break;
                        }
                    }
                }
                if (chosen is not null)
                {
                    var elementsOk = true;
                    foreach (var argument in chosen.TypeArguments)
                    {
                        if (!IsSerializable(argument, ref offender))
                        {
                            elementsOk = false;
                            break;
                        }
                    }
                    if (elementsOk)
                    {
                        return true;
                    }
                    if (!SymbolEqualityComparer.Default.Equals(offender, type))
                    {
                        return false;
                    }
                }
            }

            offender = type;
            return false;
        }

        static bool HasPublicParameterlessConstructor(INamedTypeSymbol type)
        {
            foreach (var constructor in type.InstanceConstructors)
            {
                if (constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility == Accessibility.Public)
                {
                    return true;
                }
            }
            return false;
        }

        static bool Match(ITypeSymbol candidate, ITypeSymbol pattern, List<ITypeSymbol> bindings)
        {
            switch (pattern)
            {
                case ITypeParameterSymbol:
                    bindings.Add(candidate);
                    return true;
                case IArrayTypeSymbol patternArray:
                    return candidate is IArrayTypeSymbol candidateArray
                        && candidateArray.Rank == patternArray.Rank
                        && Match(candidateArray.ElementType, patternArray.ElementType, bindings);
                case INamedTypeSymbol { IsGenericType: true } patternNamed:
                    if (candidate is not INamedTypeSymbol candidateNamed
                        || !SymbolEqualityComparer.Default.Equals(candidateNamed.OriginalDefinition, patternNamed.OriginalDefinition))
                    {
                        return false;
                    }
                    for (int i = 0; i < patternNamed.TypeArguments.Length; i++)
                    {
                        if (!Match(candidateNamed.TypeArguments[i], patternNamed.TypeArguments[i], bindings))
                        {
                            return false;
                        }
                    }
                    return true;
                default:
                    return SymbolEqualityComparer.Default.Equals(candidate, pattern);
            }
        }
    }
}

using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator;

/// <summary>
/// Turns a [MessagePackObject] + [UnionTag] root — an interface, an abstract class, or a
/// pattern union (a C# union declaration, or a hand-written [Union]-pattern struct or
/// class, optionally delegating its members to a nested IUnionMembers provider) — into a
/// <see cref="UnionModel"/>: case collection in attribute order, tag/case-type validation
/// (duplicates, assignability or creation-member presence, accessibility), non-boxing
/// pattern detection, and the not-yet-supported gates (generic unions). Reached only
/// through the [MessagePackObject] pipeline — [UnionTag] alone is
/// invisible to the generator (the MsgPack103 analyzer errors on it).
/// </summary>
static class UnionParser
{
    const string UnionTagAttributeName = "MessagePack.UnionTagAttribute";

    public static bool HasUnionTag(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (IsUnionTagAttribute(attribute.AttributeClass))
            {
                return true;
            }
        }
        return false;
    }

    // base-chain walk: UnionTagAttribute<TCaseType> derives from UnionTagAttribute
    public static bool IsUnionTagAttribute(INamedTypeSymbol? attributeClass)
    {
        for (var attributeType = attributeClass; attributeType is not null; attributeType = attributeType.BaseType)
        {
            if (attributeType.ToDisplayString() == UnionTagAttributeName)
            {
                return true;
            }
        }
        return false;
    }

    // the attribute shapes: (typeof, tag) with a closed type, or an UNBOUND generic
    // (typeof(Some<>)) matched against the creation members' parameter types — the union's
    // own declaration knows how a generic case closes over the root's parameters;
    // UnionTagAttribute<TCaseType>(tag); and ("TypeParameterName", tag) for a case that IS
    // a type parameter of a generic union (typeof cannot express one). Null when nothing
    // resolves.
    public static ITypeSymbol? ResolveCaseType(AttributeData attribute, INamedTypeSymbol root)
    {
        if (attribute.AttributeClass is { IsGenericType: true, TypeArguments.Length: 1 } genericAttribute)
        {
            return genericAttribute.TypeArguments[0];
        }
        if (attribute.ConstructorArguments.Length != 2)
        {
            return null;
        }
        var caseArgument = attribute.ConstructorArguments[0].Value;
        if (caseArgument is INamedTypeSymbol { IsUnboundGenericType: true } unbound)
        {
            foreach (var parameterType in CreationParameterTypes(root))
            {
                if (parameterType is INamedTypeSymbol named
                    && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, unbound.OriginalDefinition))
                {
                    return named;
                }
            }
            return null;
        }
        if (caseArgument is ITypeSymbol caseType)
        {
            return caseType;
        }
        if (caseArgument is string parameterName)
        {
            foreach (var typeParameter in root.TypeParameters)
            {
                if (typeParameter.Name == parameterName)
                {
                    return typeParameter;
                }
            }
        }
        return null;
    }

    static IEnumerable<ITypeSymbol> CreationParameterTypes(INamedTypeSymbol root)
    {
        if (FindUnionMemberProvider(root) is { } provider)
        {
            foreach (var member in provider.GetMembers("Create"))
            {
                if (member is IMethodSymbol { IsStatic: true, Parameters.Length: 1 } factory
                    && SymbolEqualityComparer.Default.Equals(factory.ReturnType, root))
                {
                    yield return factory.Parameters[0].Type;
                }
            }
            yield break;
        }
        foreach (var constructor in root.InstanceConstructors)
        {
            if (constructor.DeclaredAccessibility is (Accessibility.Public or Accessibility.Internal)
                && constructor.Parameters.Length == 1)
            {
                yield return constructor.Parameters[0].Type;
            }
        }
    }

    public static UnionParseResult Parse(GeneratorAttributeSyntaxContext context)
    {
        var diagnostics = new List<DiagnosticInfo>();
        var type = (INamedTypeSymbol)context.TargetSymbol;
        var typeLocation = LocationInfo.From(type);
        var typeName = type.ToDisplayString();

        // context.Attributes[0] is the [MessagePackObject] the pipeline matched on
        if (ObjectParser.ReadNamedBool(context.Attributes[0], "SuppressSourceGeneration"))
        {
            // suppressed object types have a fallback (the runtime reflection tier serves the same wire format), but no runtime tier handles unions.
            // silently skipping would strand the root with no formatter and a runtime failure, so the combination is rejected here.
            diagnostics.Add(new DiagnosticInfo("MsgPack011", $"'{typeName}': SuppressSourceGeneration cannot be used on a union root. Suppressed object types fall back to the runtime reflection formatters, but unions are source-generator-only, so a suppressed union root would have no formatter at all. Remove SuppressSourceGeneration, or remove the [UnionTag] declarations.", typeLocation));
            return new UnionParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        if (ObjectParser.ReadNamedBool(context.Attributes[0], "AllowCircularReferences"))
        {
            // a union root's wire is [tag, case]; nesting the identity envelope into that
            // dispatch is an open design question. Concrete case types can carry the flag
            // themselves — identity then rides the case formatter, which the union path
            // already routes through.
            diagnostics.Add(new DiagnosticInfo("MsgPack015", $"'{typeName}': AllowCircularReferences is not supported on union roots yet; annotate the concrete case types instead.", typeLocation));
            return new UnionParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        var isPatternUnion = false;
        var isStructRoot = false;
        INamedTypeSymbol? providerInterface = null;
        var definingType = type;
        if (type.TypeKind == TypeKind.Interface || (type.TypeKind == TypeKind.Class && type.IsAbstract))
        {
            // inheritance union (v3): runtime-type dispatch over an abstract root
        }
        else if (type.TypeKind is TypeKind.Struct or TypeKind.Class)
        {
            // pattern union: a C# union declaration, or a hand-written type following the
            // spec's union pattern. Case pattern matching is compiler magic reserved for
            // these, so the formatter goes through Value/TryGetValue and the creation
            // members, which every union kind shares.
            isPatternUnion = HasUnionMarker(type);
            isStructRoot = type.TypeKind == TypeKind.Struct;
            if (!isPatternUnion)
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack011", $"'{typeName}': [UnionTag] on a concrete type requires a union: a C# union declaration, or a type carrying the [Union] pattern.", typeLocation));
                return Invalid(diagnostics);
            }

            // a nested public interface named IUnionMembers is a union member provider: the
            // union members (static Create factories + Value) live there, not on the type
            if (FindUnionMemberProvider(type) is { } provider)
            {
                if (provider.DeclaredAccessibility != Accessibility.Public
                    || !type.AllInterfaces.Contains(provider, SymbolEqualityComparer.Default))
                {
                    diagnostics.Add(new DiagnosticInfo("MsgPack011", $"'{typeName}': the IUnionMembers union member provider must be public and implemented by the union type.", typeLocation));
                    return Invalid(diagnostics);
                }
                providerInterface = provider;
                definingType = provider;
            }

            if (!HasPublicValueProperty(definingType))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack011", $"'{typeName}': a union root must expose a public Value property (on the type or its IUnionMembers provider) for the generated formatter to read the held case.", typeLocation));
                return Invalid(diagnostics);
            }
        }
        else
        {
            diagnostics.Add(new DiagnosticInfo("MsgPack011", $"'{typeName}': [UnionTag] is only supported on interfaces, abstract classes, and union types.", typeLocation));
            return Invalid(diagnostics);
        }
        if (type.IsGenericType)
        {
            // generic roots exist for pattern unions only (union Result<T>(T, Error)):
            // an inheritance union would need open-vs-open assignability semantics that
            // v3 never defined
            if (!isPatternUnion)
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack011", $"'{typeName}': generic [UnionTag] roots are supported only for unions (a C# union declaration or a [Union]-pattern type).", typeLocation));
                return Invalid(diagnostics);
            }
            foreach (var typeParameter in type.TypeParameters)
            {
                if (typeParameter.Name is "TWriteBuffer" or "TReadBuffer")
                {
                    diagnostics.Add(new DiagnosticInfo("MsgPack011", $"'{typeName}': a type parameter named '{typeParameter.Name}' collides with the generated formatter's buffer parameters.", typeLocation));
                    return Invalid(diagnostics);
                }
            }
        }
        for (var accessible = type; accessible is not null; accessible = accessible.ContainingType)
        {
            if (accessible.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack006", $"'{typeName}' (or a containing type) is {type.DeclaredAccessibility}; generated formatters can only reach public or internal types.", typeLocation));
                return Invalid(diagnostics);
            }
        }

        var cases = new List<UnionCaseModel>();
        var seenTags = new HashSet<int>();
        var seenCaseTypes = new HashSet<string>();
        var valid = true;
        var useNonBoxing = isPatternUnion;
        // enum / Nullable / BCL-collection case types ride the same AOT harvesting as
        // object members (the generic dictionary stays empty: generic case types are gated above)
        var harvestedGenerics = new Dictionary<string, HarvestedGenericModel>();
        var harvestedBuiltIns = new Dictionary<string, HarvestedBuiltInModel>();

        foreach (var attribute in type.GetAttributes())
        {
            // the tag rides last in every shape: (caseType, tag) and UnionTag<TCaseType>(tag)
            var arguments = attribute.ConstructorArguments;
            if (!IsUnionTagAttribute(attribute.AttributeClass)
                || arguments.Length == 0
                || arguments[arguments.Length - 1].Value is not int tag)
            {
                continue;
            }
            if (ResolveCaseType(attribute, type) is not { } caseType)
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack011", $"'{typeName}': [UnionTag] case types must use the typeof overload (closed, or unbound matching a declared case), UnionTag<TCaseType>, or a type-parameter name of a generic union, so the generator can resolve them.", typeLocation));
                valid = false;
                continue;
            }
            if (!isPatternUnion && caseType is INamedTypeSymbol { IsGenericType: true })
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack011", $"'{typeName}': generic [UnionTag] case type '{caseType.ToDisplayString()}' is not supported yet.", typeLocation));
                valid = false;
                continue;
            }
            var hasCreationMember = !isPatternUnion
                || (providerInterface is null ? HasCaseConstructor(type, caseType) : HasCaseFactory(definingType, type, caseType));
            if (isPatternUnion ? !hasCreationMember : !(caseType is INamedTypeSymbol namedCase && IsAssignableTo(namedCase, type)))
            {
                diagnostics.Add(new DiagnosticInfo(
                    "MsgPack011",
                    isPatternUnion
                        ? $"'{typeName}': [UnionTag] case type '{caseType.ToDisplayString()}' has no union creation member (single-parameter constructor or IUnionMembers.Create) that accepts it."
                        : $"'{typeName}': [UnionTag] case type '{caseType.ToDisplayString()}' does not derive from or implement it.",
                    typeLocation));
                valid = false;
                continue;
            }
            var caseTypeName = caseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!seenTags.Add(tag))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack011", $"'{typeName}': [UnionTag] tag {tag} is declared more than once.", typeLocation));
                valid = false;
                continue;
            }
            if (!seenCaseTypes.Add(caseTypeName))
            {
                diagnostics.Add(new DiagnosticInfo("MsgPack011", $"'{typeName}': [UnionTag] case type '{caseType.ToDisplayString()}' is declared more than once.", typeLocation));
                valid = false;
                continue;
            }
            useNonBoxing = useNonBoxing && HasTryGetValue(definingType, caseType);
            ObjectParser.HarvestSerializedType(caseType, context.SemanticModel.Compilation, harvestedGenerics, harvestedBuiltIns);
            cases.Add(new UnionCaseModel(
                Tag: tag,
                TypeName: caseTypeName,
                // a type-parameter case gets no "?" suffix either: T? on an unconstrained
                // parameter is only an annotation, and default(T) is the natural fresh value
                IsValueType: caseType.IsValueType || caseType is ITypeParameterSymbol,
                FieldName: "f" + ObjectParser.Sanitize(caseTypeName)));
        }

        if (!valid || cases.Count == 0)
        {
            return Invalid(diagnostics);
        }

        var fullTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var formatterName = ObjectParser.Sanitize(fullTypeName) + "Formatter";
        var model = new UnionModel(
            FullTypeName: fullTypeName,
            FormatterName: formatterName,
            IsPatternUnion: isPatternUnion,
            IsStructRoot: isStructRoot,
            ProviderInterface: providerInterface?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            UseNonBoxing: useNonBoxing,
            TypeParameterList: type.IsGenericType ? string.Join(", ", type.TypeParameters.Select(static p => p.Name)) : "",
            WhereClauses: type.IsGenericType ? ObjectParser.BuildWhereClauses(type) : new EquatableArray<string>([]),
            OpenTypeOf: type.IsGenericType
                ? ObjectParser.StripTypeArguments(fullTypeName) + "<" + new string(',', type.TypeParameters.Length - 1) + ">"
                : fullTypeName,
            OpenFormatterTypeOf: type.IsGenericType
                ? "global::MessagePack.Generated." + formatterName + "<" + new string(',', type.TypeParameters.Length + 1) + ">"
                : "",
            Cases: new EquatableArray<UnionCaseModel>([.. cases]),
            HarvestedBuiltIns: new EquatableArray<HarvestedBuiltInModel>([.. harvestedBuiltIns.Values.OrderBy(static h => h.ClosedTypeName, StringComparer.Ordinal)]));
        return new UnionParseResult(model, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
    }

    const string IUnionInterfaceName = "System.Runtime.CompilerServices.IUnion";
    const string UnionMarkerAttributeName = "System.Runtime.CompilerServices.UnionAttribute";

    public static INamedTypeSymbol? FindUnionMemberProvider(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers("IUnionMembers"))
        {
            if (nested.TypeKind == TypeKind.Interface)
            {
                return nested;
            }
        }
        return null;
    }

    // the compiler-recognized union markers: hand-written pattern unions carry [Union],
    // declared unions get both stamped by the compiler
    public static bool HasUnionMarker(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == UnionMarkerAttributeName)
            {
                return true;
            }
        }
        foreach (var implemented in type.AllInterfaces)
        {
            if (implemented.ToDisplayString() == IUnionInterfaceName)
            {
                return true;
            }
        }
        return false;
    }

    static bool HasPublicValueProperty(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers("Value"))
        {
            if (member is IPropertySymbol { IsStatic: false, GetMethod: { } getter }
                && getter.DeclaredAccessibility == Accessibility.Public)
            {
                return true;
            }
        }
        return false;
    }

    // the generated formatter rebuilds with `new TUnion(caseValue)`: a declared union has a
    // constructor per case, a hand-written pattern union may take each case's base type,
    // an interface, or a single object parameter instead
    static bool HasCaseConstructor(INamedTypeSymbol type, ITypeSymbol caseType)
    {
        foreach (var constructor in type.InstanceConstructors)
        {
            if (constructor.DeclaredAccessibility is (Accessibility.Public or Accessibility.Internal)
                && constructor.Parameters.Length == 1
                && ParameterAccepts(constructor.Parameters[0].Type, caseType))
            {
                return true;
            }
        }
        return false;
    }

    // provider-based creation: `TUnion.IUnionMembers.Create(caseValue)` — a static Create
    // with a single accepting parameter, returning the union type
    static bool HasCaseFactory(INamedTypeSymbol provider, INamedTypeSymbol unionType, ITypeSymbol caseType)
    {
        foreach (var member in provider.GetMembers("Create"))
        {
            if (member is IMethodSymbol { IsStatic: true, Parameters.Length: 1 } factory
                && SymbolEqualityComparer.Default.Equals(factory.ReturnType, unionType)
                && ParameterAccepts(factory.Parameters[0].Type, caseType))
            {
                return true;
            }
        }
        return false;
    }

    // the non-boxing union access pattern's per-case accessor: bool TryGetValue(out C)
    static bool HasTryGetValue(INamedTypeSymbol definingType, ITypeSymbol caseType)
    {
        foreach (var member in definingType.GetMembers("TryGetValue"))
        {
            if (member is IMethodSymbol { IsStatic: false, ReturnType.SpecialType: SpecialType.System_Boolean, Parameters.Length: 1 } accessor
                && accessor.DeclaredAccessibility == Accessibility.Public
                && accessor.Parameters[0].RefKind == RefKind.Out
                && SymbolEqualityComparer.Default.Equals(accessor.Parameters[0].Type, caseType))
            {
                return true;
            }
        }
        return false;
    }

    static bool ParameterAccepts(ITypeSymbol parameterType, ITypeSymbol caseType) =>
        SymbolEqualityComparer.Default.Equals(parameterType, caseType)
        || parameterType.SpecialType == SpecialType.System_Object
        || (parameterType is INamedTypeSymbol namedParameter && caseType is INamedTypeSymbol namedCase && IsAssignableTo(namedCase, namedParameter));

    static UnionParseResult Invalid(List<DiagnosticInfo> diagnostics) =>
        new(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));

    static bool IsAssignableTo(INamedTypeSymbol caseType, INamedTypeSymbol baseType)
    {
        for (var current = caseType.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }
        foreach (var implemented in caseType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(implemented, baseType))
            {
                return true;
            }
        }
        return false;
    }
}

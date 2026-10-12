using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator;

/// <summary>
/// Surrogates that referenced assemblies declare for types living in yet another assembly ("foreign-target"
/// surrogates). Their registration rides the declaring assembly's module initializer, which runs only when that
/// assembly's own code first executes; a consumer that serializes the target without ever calling into the declaring
/// assembly sees no registration, or sees it after the resolver cached a miss. A surrogate for a type of its own
/// assembly has no such gap: the registry's miss path runs the target's module initializer, which is the surrogate's.
/// So this compilation adopts foreign-target surrogates as its own: they go into its generated factory and its module
/// initializer, which has run before any of its code serializes anything. Pull instead of push, like v3's
/// <c>[assembly: MessagePackKnownFormatter]</c> but without a declaration: a surrogate for a type nobody in the graph
/// owns is part of every consumer's contract. Internal surrogates and surrogates whose target carries its own
/// serialization declaration are left to the declaring assembly, as the generator there leaves them (MsgPack019).
/// </summary>
static class ReferencedSurrogateScanner
{
    public readonly record struct ForeignSurrogate(INamedTypeSymbol Surrogate, ITypeSymbol Target, IAssemblySymbol DeclaringAssembly);

    /// <summary>The generator's view: sites without a location, plus a diagnostic per target two assemblies both claim.</summary>
    public static SurrogateParseResult Scan(Compilation compilation, CancellationToken cancellationToken)
    {
        var byTarget = new Dictionary<string, List<ForeignSurrogate>>(StringComparer.Ordinal);
        foreach (var found in Enumerate(compilation, cancellationToken))
        {
            var targetName = found.Target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (!byTarget.TryGetValue(targetName, out var list))
            {
                byTarget[targetName] = list = new List<ForeignSurrogate>();
            }
            list.Add(found);
        }

        var sites = new List<SurrogateTargetSite>();
        var diagnostics = new List<DiagnosticInfo>();
        foreach (var pair in byTarget.OrderBy(static p => p.Key, StringComparer.Ordinal))
        {
            var surrogateNames = pair.Value
                .Select(static f => f.Surrogate.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static n => n, StringComparer.Ordinal)
                .ToArray();
            if (surrogateNames.Length > 1)
            {
                // two libraries disagree on the wire shape of a type neither owns: adopting either would make the
                // choice depend on this compilation's reference order, so adopt neither and say so
                var assemblies = string.Join(", ", pair.Value.Select(static f => f.DeclaringAssembly.Name).Distinct().OrderBy(static n => n, StringComparer.Ordinal));
                diagnostics.Add(new DiagnosticInfo(Diagnostics.SurrogateNotRegistered, $"'{pair.Value[0].Target.ToDisplayString()}' has surrogates in more than one referenced assembly ({assemblies}: {string.Join(", ", surrogateNames)}); none is adopted by this assembly's generated factory. Compose SurrogateFormatterFactory for the one to use into an explicit chain.", null));
                continue;
            }
            sites.Add(new SurrogateTargetSite(pair.Key, surrogateNames[0], Location: null));
        }
        return new SurrogateParseResult(new EquatableArray<SurrogateTargetSite>([.. sites]), new EquatableArray<DiagnosticInfo>([.. diagnostics]));
    }

    /// <summary>
    /// Every accessible, closed surrogate struct in a referenced assembly that itself references MessagePack, whose
    /// target lives outside that assembly and carries no serialization declaration of its own.
    /// </summary>
    public static IEnumerable<ForeignSurrogate> Enumerate(Compilation compilation, CancellationToken cancellationToken)
    {
        if (compilation.GetTypeByMetadataName("MessagePack.IMessagePackSurrogate`2") is not { } surrogateDefinition)
        {
            yield break;
        }
        var messagePack = surrogateDefinition.ContainingAssembly;
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // only assemblies that reference MessagePack can implement its interface: the cheap filter that keeps the
            // walk away from the BCL and everything else
            if (SymbolEqualityComparer.Default.Equals(assembly, messagePack) || !ReferencesAssembly(assembly, messagePack.Name))
            {
                continue;
            }
            foreach (var found in EnumerateNamespace(assembly.GlobalNamespace, assembly, surrogateDefinition, compilation, cancellationToken))
            {
                yield return found;
            }
        }
    }

    static bool ReferencesAssembly(IAssemblySymbol assembly, string name)
    {
        foreach (var module in assembly.Modules)
        {
            foreach (var reference in module.ReferencedAssemblies)
            {
                if (reference.Name == name)
                {
                    return true;
                }
            }
        }
        return false;
    }

    static IEnumerable<ForeignSurrogate> EnumerateNamespace(INamespaceSymbol ns, IAssemblySymbol assembly, INamedTypeSymbol surrogateDefinition, Compilation compilation, CancellationToken cancellationToken)
    {
        foreach (var member in ns.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nested = member switch
            {
                INamespaceSymbol child => EnumerateNamespace(child, assembly, surrogateDefinition, compilation, cancellationToken),
                INamedTypeSymbol type => EnumerateType(type, assembly, surrogateDefinition, compilation),
                _ => null,
            };
            if (nested is null)
            {
                continue;
            }
            foreach (var found in nested)
            {
                yield return found;
            }
        }
    }

    static IEnumerable<ForeignSurrogate> EnumerateType(INamedTypeSymbol type, IAssemblySymbol assembly, INamedTypeSymbol surrogateDefinition, Compilation compilation)
    {
        // the same shape SurrogateParser registers from source: a closed struct the generated code can name
        if (type.TypeKind == TypeKind.Struct && !ObjectParser.ContainsTypeParameter(type) && ObjectParser.IsAccessibleToGeneratedCode(type, compilation))
        {
            foreach (var implemented in type.AllInterfaces)
            {
                if (!SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, surrogateDefinition)
                    || !SymbolEqualityComparer.Default.Equals(implemented.TypeArguments[1], type))
                {
                    continue;
                }
                var target = implemented.TypeArguments[0];
                if (ObjectParser.ContainsTypeParameter(target)
                    || SymbolEqualityComparer.Default.Equals(target.ContainingAssembly, assembly) // own-assembly target: the registry's miss path runs this module's initializer
                    || !ObjectParser.IsAccessibleToGeneratedCode(target, compilation))
                {
                    continue;
                }
                if (target is INamedTypeSymbol named
                    && (ObjectParser.HasMessagePackObjectAttribute(named.OriginalDefinition) || ObjectParser.HasAttribute(named.OriginalDefinition, SurrogateParser.FormatterAttributeName)))
                {
                    continue; // the target's own declaration wins, as in the declaring assembly
                }
                yield return new ForeignSurrogate(type, target, assembly);
            }
        }
        foreach (var nested in type.GetTypeMembers())
        {
            foreach (var found in EnumerateType(nested, assembly, surrogateDefinition, compilation))
            {
                yield return found;
            }
        }
    }
}

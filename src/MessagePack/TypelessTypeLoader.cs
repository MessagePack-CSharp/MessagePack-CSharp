using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using MessagePack.Formatters;

namespace MessagePack;

internal static partial class TypelessTypeNames
{
    // the ", AssemblyName" that follows a type name (at the end, or before the `]` closing a generic argument) once
    // Version/Culture/PublicKeyToken are gone: what is left is the namespace-qualified name alone
#if NET9_0_OR_GREATER
    [GeneratedRegex(@", [^,\[\]]+(?=\]|$)", RegexOptions.Compiled)]
    internal static partial Regex SubtractAssemblyNamesRegex { get; }
#else
    internal static readonly Regex SubtractAssemblyNamesRegex = new(@", [^,\[\]]+(?=\]|$)", RegexOptions.Compiled);
#endif

#if NET9_0_OR_GREATER
    // v3's pattern with the version dots escaped: v3 wrote `\d+.\d+` and the wildcard let a digit run match the
    // separator, which backtracks cubically on a payload name such as "X, Version=111...1" (17 s for 1000 digits);
    // with `\.` a digit can never stand in for the separator and the match is linear. Names v3 writes are always
    // dotted, so both sides still shorten identically (a hyphenated Culture still does not match, as in v3).
    [GeneratedRegex(@", Version=\d+\.\d+\.\d+\.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled)]
    internal static partial Regex SubtractFullNameRegex { get; }
#else
    internal static readonly Regex SubtractFullNameRegex = new(@", Version=\d+\.\d+\.\d+\.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled);
#endif
}

/// <summary>
/// Decides how the typeless reader turns a payload-embedded type name into a <see cref="Type"/>.
/// </summary>
public abstract class TypelessTypeLoader
{
    /// <summary>
    /// Returns the resolved type, or null to refuse the name (the read then fails with <see cref="MessagePackSerializationException"/>).
    /// </summary>
    public abstract Type? LoadType(string typeName);

    /// <summary>
    /// The v3-default behavior: resolve any payload-provided name with <see cref="Type.GetType(string)"/>.
    /// allowAssemblyVersionMismatch retries a failed resolution with the Version/Culture/PublicKeyToken-stripped name.
    /// </summary>
    [Obsolete(TypelessMessages.UntrustedData)]
    public static TypelessTypeLoader LoadAnyType(bool allowAssemblyVersionMismatch = false)
    {
        return new LoadAnyTypeLoader(allowAssemblyVersionMismatch);
    }

    /// <summary>
    /// Resolves only the listed types. Payload names match by assembly-qualified
    /// spelling, tolerating omitted or drifted Version/Culture/PublicKeyToken; nothing is
    /// ever loaded from payload input. List every type that can appear in an <c>object</c> or
    /// abstract slot, <see cref="Guid"/>, <see cref="DateTime"/>, enums and collection types included:
    /// everything but bool, the integers, float, double, string and byte[] is written with its type
    /// name, and a name outside the list fails to read.
    /// </summary>
    public static TypelessTypeLoader AllowedTypes(params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(types);
        if (Array.IndexOf(types, null) >= 0)
        {
            throw new ArgumentException("a type in the allowed list is null", nameof(types));
        }
        return new AllowedTypesLoader(types);
    }

    /// <summary>
    /// A custom mapping. The delegate is the sole authority: return null to refuse.
    /// </summary>
    public static TypelessTypeLoader Create(Func<string, Type?> loadType)
    {
        ArgumentNullException.ThrowIfNull(loadType);
        return new DelegateTypeLoader(loadType);
    }

    sealed class LoadAnyTypeLoader : TypelessTypeLoader
    {
        const int MaxTypeNameLength = 1024;

        readonly bool allowAssemblyVersionMismatch;

        // the running core library: mscorlib on .NET Framework (and Mono), System.Private.CoreLib on .NET. The
        // netstandard asset runs on both, so this is a runtime check (v3's IsMscorlib)
        static readonly bool IsMscorlib = typeof(object).Assembly.GetName().Name == "mscorlib";

        public LoadAnyTypeLoader(bool allowAssemblyVersionMismatch)
        {
            this.allowAssemblyVersionMismatch = allowAssemblyVersionMismatch;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "loading types by payload-provided names is this loader's contract; instances reach a formatter only through TypelessFormatterFactory, whose constructor carries RequiresUnreferencedCode (typeless is incompatible with trimming)")]
        public override Type? LoadType(string typeName)
        {
            // Type.GetType parses the name (generic nesting, assembly qualification) at a cost that grows with it, and
            // a payload picks the name: the cap bounds that work. An allow list has no such parse and no such cap.
            if (typeName.Length > MaxTypeNameLength)
            {
                throw new MessagePackSerializationException($"Typeless type name is implausibly long ({typeName.Length} chars; LoadAnyType accepts up to {MaxTypeNameLength}).");
            }
            try
            {
                var type = Type.GetType(typeName, throwOnError: false);
                if (type is null && allowAssemblyVersionMismatch)
                {
                    // v3's AllowAssemblyVersionMismatch: retry with the shortened spelling
                    type = Type.GetType(TypelessTypeNames.SubtractFullNameRegex.Replace(typeName, string.Empty), throwOnError: false);
                }
                if (type is null)
                {
                    // v3's cross-runtime swap: a payload written on the other runtime family names the other core
                    // library for the same type (System.Guid lives in mscorlib on .NET Framework, in
                    // System.Private.CoreLib on .NET). .NET reads mscorlib names through its facade, but .NET Framework
                    // has no System.Private.CoreLib at all, so swap the spelling and retry. Each binder resolves its own
                    // core library by simple name whatever Version/PublicKeyToken follows, so the other runtime's
                    // identity can stay in the name.
                    if (IsMscorlib && typeName.Contains("System.Private.CoreLib"))
                    {
                        type = Type.GetType(typeName.Replace("System.Private.CoreLib", "mscorlib"), throwOnError: false);
                    }
                    else if (!IsMscorlib && typeName.Contains("mscorlib"))
                    {
                        type = Type.GetType(typeName.Replace("mscorlib", "System.Private.CoreLib"), throwOnError: false);
                    }
                }
                return type;
            }
            catch (Exception ex) // the one catch-wrap: Type.GetType failures are data errors here
            {
                throw new MessagePackSerializationException($"Can't load type '{typeName}'.", ex);
            }
        }
    }

    sealed class AllowedTypesLoader : TypelessTypeLoader
    {
        // keyed by both the full assembly-qualified spelling and the Version/Culture/PublicKeyToken-stripped one,
        // built from the caller's live Type objects: payload input only ever does dictionary lookups
        readonly Dictionary<string, Type> typesByName;

        public AllowedTypesLoader(Type[] types)
        {
            typesByName = new Dictionary<string, Type>(types.Length * 3, StringComparer.Ordinal);
            var ambiguous = new HashSet<string>(StringComparer.Ordinal);
            foreach (var type in types)
            {
                var fullName = type.AssemblyQualifiedName ?? type.FullName
                    ?? throw new ArgumentException($"Type '{type}' has no name to match payload type names against.", nameof(types));
                typesByName[fullName] = type;
                var versionless = TypelessTypeNames.SubtractFullNameRegex.Replace(fullName, string.Empty);
                typesByName[versionless] = type;
                // the assembly-free spelling is shared when two registered types have the same namespace-qualified
                // name in different assemblies: then it names neither (the exact spellings above still do)
                var assemblyless = TypelessTypeNames.SubtractAssemblyNamesRegex.Replace(versionless, string.Empty);
                if (ambiguous.Contains(assemblyless))
                {
                    continue;
                }
                if (typesByName.TryGetValue(assemblyless, out var other) && other != type)
                {
                    ambiguous.Add(assemblyless);
                    typesByName.Remove(assemblyless);
                    continue;
                }
                typesByName[assemblyless] = type;
            }
        }

        public override Type? LoadType(string typeName)
        {
            if (typesByName.TryGetValue(typeName, out var type))
            {
                return type;
            }
            // a payload written by another version of the assembly spells Version=... differently: shorten and
            // compare version-insensitively
            var versionless = TypelessTypeNames.SubtractFullNameRegex.Replace(typeName, string.Empty);
            if (typesByName.TryGetValue(versionless, out type))
            {
                return type;
            }
            // and one written on another runtime names another assembly for the same type (System.Guid lives in
            // mscorlib on .NET Framework, in System.Private.CoreLib here): compare by the namespace-qualified names
            // alone. The list holds live types, so the match can only ever yield a type the caller registered.
            return typesByName.TryGetValue(TypelessTypeNames.SubtractAssemblyNamesRegex.Replace(versionless, string.Empty), out type) ? type : null;
        }
    }

    sealed class DelegateTypeLoader : TypelessTypeLoader
    {
        readonly Func<string, Type?> loadType;

        public DelegateTypeLoader(Func<string, Type?> loadType)
        {
            this.loadType = loadType;
        }

        public override Type? LoadType(string typeName)
        {
            return loadType(typeName);
        }
    }
}

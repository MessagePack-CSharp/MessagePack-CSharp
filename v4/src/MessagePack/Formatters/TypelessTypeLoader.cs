using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace MessagePack.Formatters;

internal static partial class TypelessTypeNames
{
#if NET
    // v3's exact pattern (bug-compat included: a hyphenated Culture would not match), so both sides shorten identically
    [GeneratedRegex(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled)]
    internal static partial Regex SubtractFullNameRegex { get; }
#else
    internal static readonly Regex SubtractFullNameRegex = new(@", Version=\d+.\d+.\d+.\d+, Culture=\w+, PublicKeyToken=\w+", RegexOptions.Compiled);
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
    /// ever loaded from payload input.
    /// </summary>
    public static TypelessTypeLoader AllowedTypes(params Type[] types)
    {
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
        readonly bool allowAssemblyVersionMismatch;

        public LoadAnyTypeLoader(bool allowAssemblyVersionMismatch)
        {
            this.allowAssemblyVersionMismatch = allowAssemblyVersionMismatch;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "loading types by payload-provided names is this loader's contract; instances reach a formatter only through TypelessFormatterFactory, whose constructor carries RequiresUnreferencedCode (typeless is incompatible with trimming)")]
        public override Type? LoadType(string typeName)
        {
            try
            {
                var type = Type.GetType(typeName, throwOnError: false);
                if (type is null && allowAssemblyVersionMismatch)
                {
                    // v3's AllowAssemblyVersionMismatch: retry with the shortened spelling
                    type = Type.GetType(TypelessTypeNames.SubtractFullNameRegex.Replace(typeName, string.Empty), throwOnError: false);
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
            typesByName = new Dictionary<string, Type>(types.Length * 2, StringComparer.Ordinal);
            foreach (var type in types)
            {
                var fullName = type.AssemblyQualifiedName ?? type.FullName
                    ?? throw new ArgumentException($"Type '{type}' has no name to match payload type names against.", nameof(types));
                typesByName[fullName] = type;
                typesByName[TypelessTypeNames.SubtractFullNameRegex.Replace(fullName, string.Empty)] = type;
            }
        }

        public override Type? LoadType(string typeName)
        {
            if (typesByName.TryGetValue(typeName, out var type))
            {
                return type;
            }
            // a payload written by another version of the assembly spells Version=...
            // differently: shorten and compare version-insensitively
            return typesByName.TryGetValue(TypelessTypeNames.SubtractFullNameRegex.Replace(typeName, string.Empty), out type) ? type : null;
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

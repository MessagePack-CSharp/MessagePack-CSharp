using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MessagePack;

// Reads the compiler's erased nullable-reference metadata for a member's top-level type.
// The attributes are matched by full name because they are usually embedded per assembly (there is no shared runtime
// type to typeof against, and netstandard2.0 has no NullabilityInfoContext), so one manual reader keeps every TFM on
// the same code path. Scope is deliberately the declared member type only. Generic type parameters and the
// element/argument positions inside a generic instantiation are not inspected, which is the same hole the source
// generator documents for nullable validation.
static class NullableAnnotationReader
{
    const string NullableAttributeName = "System.Runtime.CompilerServices.NullableAttribute";
    const string NullableContextAttributeName = "System.Runtime.CompilerServices.NullableContextAttribute";

    // blob byte meanings: 0 = oblivious, 1 = non-nullable, 2 = nullable
    const int NotAnnotated = 1;

    public static bool IsNonNullableReference(MemberInfo member, Type memberType)
    {
        if (memberType.IsValueType || memberType.IsGenericParameter)
        {
            return false;
        }

        // NullableAttribute on the member wins; without one the member follows the nearest enclosing NullableContext
        // (member, then the declaring type chain)
        var flag = ReadNullableFlag(member.GetCustomAttributes(inherit: false));
        if (flag < 0)
        {
            flag = ReadContextFlag(member.GetCustomAttributes(inherit: false));
        }
        for (var scope = member.DeclaringType; flag < 0 && scope is not null; scope = scope.DeclaringType)
        {
            flag = ReadContextFlag(scope.GetCustomAttributes(inherit: false));
        }
        return flag == NotAnnotated;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "reads a field of a compiler-embedded attribute type that is rooted by its own instantiation on the inspected member; only reached through ReflectionFormatterFactory's RequiresUnreferencedCode gate")]
    static int ReadNullableFlag(object[] attributes)
    {
        foreach (var attribute in attributes)
        {
            var type = attribute.GetType();
            if (type.FullName != NullableAttributeName)
            {
                continue;
            }
            // both ctor shapes, (byte) and (byte[]), store the blob in NullableFlags, and the first element describes
            // the member's top-level type
            return type.GetField("NullableFlags")?.GetValue(attribute) switch
            {
                byte b => b,
                byte[] { Length: > 0 } flags => flags[0],
                _ => -1,
            };
        }
        return -1;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "reads a field of a compiler-embedded attribute type that is rooted by its own instantiation on the inspected member; only reached through ReflectionFormatterFactory's RequiresUnreferencedCode gate")]
    static int ReadContextFlag(object[] attributes)
    {
        foreach (var attribute in attributes)
        {
            var type = attribute.GetType();
            if (type.FullName == NullableContextAttributeName)
            {
                return type.GetField("Flag")?.GetValue(attribute) is byte b ? b : -1;
            }
        }
        return -1;
    }
}

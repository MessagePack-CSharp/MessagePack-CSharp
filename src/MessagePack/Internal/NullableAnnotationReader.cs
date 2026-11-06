using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MessagePack;

// Reads the compiler's erased nullable-reference metadata for a member's top-level type, and the flow attributes
// that relax or tighten it per direction ([AllowNull]/[DisallowNull] on what is assigned, [MaybeNull]/[NotNull] on
// what is read). The attributes are matched by full name because they are usually embedded per assembly (there is no
// shared runtime type to typeof against, and netstandard2.0 has no NullabilityInfoContext), so one manual reader keeps
// every TFM on the same code path. The rules are NullabilityInfoContext's read/write states, and the source generator's
// ObjectParser.RejectsNull applies the same ones. Scope is deliberately the declared type only. Generic type parameters
// and the element/argument positions inside a generic instantiation are not inspected, nor are value types
// (Nullable<T> included), which is the same hole the source generator documents for nullable validation.
static class NullableAnnotationReader
{
    const string NullableAttributeName = "System.Runtime.CompilerServices.NullableAttribute";
    const string NullableContextAttributeName = "System.Runtime.CompilerServices.NullableContextAttribute";
    const string AllowNullAttributeName = "System.Diagnostics.CodeAnalysis.AllowNullAttribute";
    const string DisallowNullAttributeName = "System.Diagnostics.CodeAnalysis.DisallowNullAttribute";
    const string MaybeNullAttributeName = "System.Diagnostics.CodeAnalysis.MaybeNullAttribute";
    const string NotNullAttributeName = "System.Diagnostics.CodeAnalysis.NotNullAttribute";

    // blob byte meanings: 0 = oblivious, 1 = non-nullable, 2 = nullable
    const int NotAnnotated = 1;
    const int Annotated = 2;

    /// <summary>Whether assigning null to the member (deserialization's direction) breaks its declared contract.</summary>
    public static bool RejectsNullOnSet(MemberInfo member, Type memberType)
    {
        if (!IsCheckable(memberType) || IsDeclaredAsTypeParameter(member))
        {
            return false;
        }
        var attributes = member.GetCustomAttributes(inherit: false);
        var flags = member is PropertyInfo { SetMethod: { } setter } && setter.GetParameters() is { Length: > 0 } parameters
            ? parameters[parameters.Length - 1].GetCustomAttributes(inherit: false)
            : [];
        return Decide(MemberFlag(member, attributes), attributes, flags, AllowNullAttributeName, DisallowNullAttributeName);
    }

    /// <summary>Whether the member returning null (serialization's direction) breaks its declared contract.</summary>
    public static bool RejectsNullOnGet(MemberInfo member, Type memberType)
    {
        if (!IsCheckable(memberType) || IsDeclaredAsTypeParameter(member))
        {
            return false;
        }
        var attributes = member.GetCustomAttributes(inherit: false);
        var flags = member is PropertyInfo { GetMethod: { } getter }
            ? getter.ReturnParameter.GetCustomAttributes(inherit: false)
            : [];
        return Decide(MemberFlag(member, attributes), attributes, flags, MaybeNullAttributeName, NotNullAttributeName);
    }

    /// <summary>Whether passing null to the constructor parameter breaks its declared contract.</summary>
    public static bool RejectsNull(ParameterInfo parameter)
    {
        if (!IsCheckable(parameter.ParameterType) || IsDeclaredAsTypeParameter(parameter))
        {
            return false;
        }
        var attributes = parameter.GetCustomAttributes(inherit: false);
        // NullableAttribute on the parameter wins; without one it follows the constructor's NullableContext, then the
        // declaring type chain's
        var flag = ReadNullableFlag(attributes);
        if (flag < 0)
        {
            flag = ReadContextFlag(parameter.Member.GetCustomAttributes(inherit: false));
        }
        for (var scope = parameter.Member.DeclaringType; flag < 0 && scope is not null; scope = scope.DeclaringType)
        {
            flag = ReadContextFlag(scope.GetCustomAttributes(inherit: false));
        }
        return Decide(flag, attributes, [], AllowNullAttributeName, DisallowNullAttributeName);
    }

    static bool IsCheckable(Type type) => !type.IsValueType && !type.IsGenericParameter;

    // a member that a generic declaration types by its type parameter (`T Value` of Base<T>, closed as Base<string?>)
    // carries the annotation of T, not of the type argument (whose annotation lives at the use site, which for a closed
    // generic root is not in metadata at all), so it stays out of scope like an open T; the source generator skips the
    // same members through their original definition
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "the open definition's members mirror the closed type's, which member discovery already enumerated under ReflectionFormatterFactory's RequiresUnreferencedCode gate")]
    static bool IsDeclaredAsTypeParameter(MemberInfo member)
    {
        if (member.DeclaringType is not { IsGenericType: true, IsGenericTypeDefinition: false } declaring)
        {
            return false;
        }
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var definition = declaring.GetGenericTypeDefinition();
        var token = member.MetadataToken;
        return member switch
        {
            PropertyInfo => Array.Find(definition.GetProperties(all), p => p.MetadataToken == token)?.PropertyType.IsGenericParameter == true,
            FieldInfo => Array.Find(definition.GetFields(all), f => f.MetadataToken == token)?.FieldType.IsGenericParameter == true,
            _ => false,
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "the open definition's constructors mirror the closed type's, which constructor selection already enumerated under ReflectionFormatterFactory's RequiresUnreferencedCode gate")]
    static bool IsDeclaredAsTypeParameter(ParameterInfo parameter)
    {
        if (parameter.Member.DeclaringType is not { IsGenericType: true, IsGenericTypeDefinition: false } declaring)
        {
            return false;
        }
        var token = parameter.Member.MetadataToken;
        var definition = Array.Find(
            declaring.GetGenericTypeDefinition().GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
            c => c.MetadataToken == token);
        return definition is not null && definition.GetParameters()[parameter.Position].ParameterType.IsGenericParameter;
    }

    // NullableAttribute on the member wins; without one the member follows the nearest enclosing NullableContext
    // (member, then the declaring type chain)
    static int MemberFlag(MemberInfo member, object[] attributes)
    {
        var flag = ReadNullableFlag(attributes);
        if (flag < 0)
        {
            flag = ReadContextFlag(attributes);
        }
        for (var scope = member.DeclaringType; flag < 0 && scope is not null; scope = scope.DeclaringType)
        {
            flag = ReadContextFlag(scope.GetCustomAttributes(inherit: false));
        }
        return flag;
    }

    // non-nullable rejects null unless relaxed, nullable accepts it unless tightened, oblivious is never checked
    static bool Decide(int flag, object[] attributes, object[] accessorAttributes, string relax, string tighten) => flag switch
    {
        NotAnnotated => !HasAttribute(attributes, relax) && !HasAttribute(accessorAttributes, relax),
        Annotated => HasAttribute(attributes, tighten) || HasAttribute(accessorAttributes, tighten),
        _ => false,
    };

    static bool HasAttribute(object[] attributes, string fullName)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.GetType().FullName == fullName)
            {
                return true;
            }
        }
        return false;
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

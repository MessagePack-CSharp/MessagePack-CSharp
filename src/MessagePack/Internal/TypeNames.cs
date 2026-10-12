namespace MessagePack;

/// <summary>
/// C#-spelled type names for messages: <c>List&lt;int&gt;</c>, <c>Dictionary&lt;string, Order[]&gt;</c>, <c>int?</c>, where
/// reflection's <see cref="System.Reflection.MemberInfo.Name"/> says <c>List`1</c>. A hint that tells the reader what to paste.
/// </summary>
static class TypeNames
{
    public static string CSharp(Type type)
    {
        if (type.IsArray)
        {
            return CSharp(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        }
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return CSharp(underlying) + "?";
        }
        if (Keyword(type) is { } keyword)
        {
            return keyword;
        }
        var name = type.IsNested && !type.IsGenericParameter ? CSharp(type.DeclaringType!) + "." + type.Name : type.Name;
        if (!type.IsGenericType)
        {
            return name;
        }
        var tick = name.LastIndexOf('`');
        if (tick >= 0)
        {
            name = name.Substring(0, tick);
        }
        var arguments = type.GetGenericArguments();
        var parts = new string[arguments.Length];
        for (int i = 0; i < arguments.Length; i++)
        {
            parts[i] = CSharp(arguments[i]);
        }
        return name + "<" + string.Join(", ", parts) + ">";
    }

    static string? Keyword(Type type)
    {
        if (type == typeof(int)) return "int";
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(long)) return "long";
        if (type == typeof(double)) return "double";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(object)) return "object";
        if (type == typeof(float)) return "float";
        if (type == typeof(short)) return "short";
        if (type == typeof(char)) return "char";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(sbyte)) return "sbyte";
        return null;
    }
}

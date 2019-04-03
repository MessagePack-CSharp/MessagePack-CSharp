#if !UNITY_WSA

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace MessagePack.Internal
{
    internal static class ReflectionExtensions
    {
        internal static bool IsNullable(this System.Reflection.TypeInfo type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Nullable<>);
        }

        internal static bool IsPublic(this System.Reflection.TypeInfo type)
        {
            return type.IsPublic;
        }

        internal static bool IsAnonymous(this System.Reflection.TypeInfo type)
        {
            return type.GetCustomAttribute<CompilerGeneratedAttribute>() != null
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        internal static bool IsIndexer(this System.Reflection.PropertyInfo propertyInfo)
        {
            return propertyInfo.GetIndexParameters().Length > 0;
        }

#if !UNITY

        internal static bool IsConstructedGenericType(this System.Reflection.TypeInfo type)
        {
            return type.AsType().IsConstructedGenericType;
        }

        internal static MethodInfo GetGetMethod(this PropertyInfo propInfo)
        {
            return propInfo.GetMethod;
        }

        internal static MethodInfo GetSetMethod(this PropertyInfo propInfo)
        {
            return propInfo.SetMethod;
        }

#endif
    }
}

#endif

#if !UNITY_WSA

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MessagePack.Internal
{
    internal static class ReflectionExtensions
    {
        public static bool IsNullable(this System.Reflection.TypeInfo type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Nullable<>);
        }

        public static bool IsPublic(this System.Reflection.TypeInfo type)
        {
            return type.IsPublic;
        }

        public static bool IsAnonymous(this System.Reflection.TypeInfo type)
        {
            return type.GetCustomAttribute<CompilerGeneratedAttribute>() != null
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        public static bool IsIndexer(this System.Reflection.PropertyInfo propertyInfo)
        {
            return propertyInfo.GetIndexParameters().Length > 0;
        }

        // #if NETSTANDARD

        public static bool IsConstructedGenericType(this System.Reflection.TypeInfo type)
        {
            return type.AsType().IsConstructedGenericType;
        }

        public static MethodInfo GetGetMethodEx(this PropertyInfo propInfo, Type targetType)
        {
            return propInfo.GetMethod;
        }

        public static MethodInfo GetSetMethodEx(this PropertyInfo propInfo, Type targetType)
        {
            if (propInfo.CanWrite)
            {
                return propInfo.SetMethod;
            }
            else
            {
                var declaringProp = propInfo.DeclaringType.GetProperty(propInfo.Name, propInfo.PropertyType);
                return declaringProp.GetSetMethod(true);
            }
        }

        //#endif
    }
}

#endif
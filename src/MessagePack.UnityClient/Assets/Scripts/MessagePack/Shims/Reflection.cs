using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace System.Reflection
{
    public class TypeInfo
    {
        readonly Type type;

        public TypeInfo(Type type)
        {
            this.type = type;
        }

        public bool IsClass
        {
            get
            {
                return type.IsClass;
            }
        }

        public bool IsPublic
        {
            get
            {
                return type.IsPublic;
            }
        }

        public bool IsValueType
        {
            get
            {
                return type.IsValueType;
            }
        }

        public bool IsNestedPublic
        {
            get
            {
                return type.IsNestedPublic;
            }
        }

        public IEnumerable<ConstructorInfo> DeclaredConstructors
        {
            get
            {
                return type.GetConstructors().AsEnumerable();
            }
        }

        public bool IsGenericType
        {
            get
            {
                return type.IsGenericType;
            }
        }

        public Type GetGenericTypeDefinition()
        {
            return type.GetGenericTypeDefinition();
        }

        public Type AsType()
        {
            return type;
        }

        public MethodInfo GetDeclaredMethod(string name)
        {
            return type.GetMethod(name);
        }

        public Type[] GenericTypeArguments
        {
            get
            {
                return type.GetGenericArguments();
            }
        }

        public bool IsEnum
        {
            get
            {
                return type.IsEnum;
            }
        }

        public bool IsConstructedGenericType()
        {
            return type.IsGenericType && !type.IsGenericTypeDefinition;
        }

        public Type[] ImplementedInterfaces
        {
            get
            {
                return type.GetInterfaces();
            }
        }

        public MethodInfo[] GetRuntimeMethods()
        {
            return type.GetMethods();
        }

        public T GetCustomAttribute<T>(bool inherit = true)
            where T : Attribute
        {
            return type.GetCustomAttributes(inherit).OfType<T>().FirstOrDefault();
        }
        public IEnumerable<T> GetCustomAttributes<T>(bool inherit = true)
            where T : Attribute
        {
            return type.GetCustomAttributes(inherit).OfType<T>();
        }
    }

    public static class ReflectionExtensions
    {
        public static MethodInfo GetRuntimeMethod(this Type type, string name, Type[] types)
        {
            return type.GetMethod(name, types);
        }

        public static TypeInfo GetTypeInfo(this Type type)
        {
            return new TypeInfo(type);
        }

        public static TypeInfo CreateTypeInfo(this TypeBuilder type)
        {
            return new TypeInfo(type.CreateType());
        }

        public static MethodInfo GetRuntimeMethods(this Type type, string name)
        {
            return type.GetMethod(name);
        }

        public static MethodInfo[] GetRuntimeMethods(this Type type)
        {
            return type.GetMethods();
        }

        public static PropertyInfo GetRuntimeProperty(this Type type, string name)
        {
            return type.GetProperty(name);
        }

        public static PropertyInfo[] GetRuntimeProperties(this Type type)
        {
            return type.GetProperties();
        }

        public static FieldInfo GetRuntimeField(this Type type, string name)
        {
            return type.GetField(name);
        }

        public static FieldInfo[] GetRuntimeFields(this Type type)
        {
            return type.GetFields();
        }

        public static T GetCustomAttribute<T>(this FieldInfo type, bool inherit)
            where T : Attribute
        {
            return type.GetCustomAttributes(inherit).OfType<T>().FirstOrDefault();
        }

        public static T GetCustomAttribute<T>(this PropertyInfo type, bool inherit)
            where T : Attribute
        {
            return type.GetCustomAttributes(inherit).OfType<T>().FirstOrDefault();
        }

        public static T GetCustomAttribute<T>(this ConstructorInfo type, bool inherit)
            where T : Attribute
        {
            return type.GetCustomAttributes(inherit).OfType<T>().FirstOrDefault();
        }
    }
}

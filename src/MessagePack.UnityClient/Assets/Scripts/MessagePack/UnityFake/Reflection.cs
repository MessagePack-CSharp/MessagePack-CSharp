using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System.Reflection
{
    public struct TypeInfo
    {
        readonly Type type;

        public TypeInfo(Type type) : this()
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
    }

    // il.Emit(OpCodes.Newobj, typeof(System.NotImplementedException).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 0));

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
    }
}

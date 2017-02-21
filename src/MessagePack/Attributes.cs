using System;

namespace MessagePack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class MessagePackObjectAttribute : Attribute
    {
        public bool KeyAsPropertyName { get; private set; }

        public MessagePackObjectAttribute(bool keyAsPropertyName = false)
        {
            this.KeyAsPropertyName = keyAsPropertyName;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class KeyAttribute : Attribute
    {
        public int? IntKey { get; private set; }
        public string StringKey { get; private set; }

        public KeyAttribute(int x)
        {
            this.IntKey = x;
        }

        public KeyAttribute(string x)
        {
            this.StringKey = x;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class IgnoreAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class UnionAttribute : Attribute
    {
        public Type[] SubTypes { get; private set; }
        public Type FallbackType { get; private set; }

        public UnionAttribute(params Type[] subTypes)
        {
            this.SubTypes = subTypes;
        }

        public UnionAttribute(Type[] subTypes, Type fallbackType)
        {
            this.SubTypes = subTypes;
            this.FallbackType = fallbackType;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class UnionKeyAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = true)]
    public class SerializationConstructorAttribute : Attribute
    {

    }
}
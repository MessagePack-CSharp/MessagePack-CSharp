using System;

namespace MessagePack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class MessagePackObjectAttribute : Attribute
    {
        public bool KeyAsPropertyName { get; private set; }
        public bool AllowNil { get; private set; }

        public MessagePackObjectAttribute(bool keyAsPropertyName = false)
        {
            this.KeyAsPropertyName = keyAsPropertyName;
            this.AllowNil = true;
        }

        public MessagePackObjectAttribute(bool keyAsPropertyName, bool allowNil)
        {
            this.KeyAsPropertyName = keyAsPropertyName;
            this.AllowNil = allowNil;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class KeyAttribute : Attribute
    {
        public int? IntKey { get; private set; }
        public string StringKey { get; private set; }
        public bool AllowNil { get; private set; } = true;

        public KeyAttribute(int x)
        {
            this.IntKey = x;
        }

        public KeyAttribute(string x)
        {
            this.StringKey = x;
        }

        public KeyAttribute(int x, bool allowNil)
        {
            this.IntKey = x;
            AllowNil = allowNil;
        }

        public KeyAttribute(string x, bool allowNil)
        {
            this.StringKey = x;
            AllowNil = allowNil;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class IgnoreMemberAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class UnionAttribute : Attribute
    {
        public int Key { get; private set; }
        public Type SubType { get; private set; }

        public UnionAttribute(int key, Type subType)
        {
            this.Key = key;
            this.SubType = subType;
        }
    }

    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = true)]
    public class SerializationConstructorAttribute : Attribute
    {

    }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class MessagePackFormatterAttribute : Attribute
    {
        public Type FormatterType { get; private set; }
        public object[] Arguments { get; private set; }

        public MessagePackFormatterAttribute(Type formatterType)
        {
            this.FormatterType = formatterType;
        }

        public MessagePackFormatterAttribute(Type formatterType, params object[] arguments)
        {
            this.FormatterType = formatterType;
            this.Arguments = arguments;
        }
    }
}
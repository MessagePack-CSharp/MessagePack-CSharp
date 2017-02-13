using System;

namespace MessagePack
{
    public class MessagePackKeyAttribute : Attribute
    {
        public int? IntKey { get; private set; }
        public string StringKey { get; private set; }

        public MessagePackKeyAttribute(int x)
        {
            this.IntKey = x;
        }

        public MessagePackKeyAttribute(string x)
        {
            this.StringKey = x;
        }
    }

    public class MessagePackContract : Attribute
    {
        public bool KeyAsPropertyName { get; private set; }

        public MessagePackContract(bool keyAsPropertyName = false)
        {
            this.KeyAsPropertyName = keyAsPropertyName;
        }
    }
}

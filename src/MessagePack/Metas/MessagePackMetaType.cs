#pragma warning disable CS8618

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MessagePackv3.Metas
{
    public abstract class MessagePackMetaType
    {
        public MessagePackMetaMember[] Members { get; set; }
        public MessagePackMetaConstructor[] Constructors { get; set; }

        public MessagePackMetaMember[] WritableMembers => Members.Where(x => x.IsWritable).ToArray();
        public MessagePackMetaMember[] ReadableMembers => Members.Where(x => x.IsReadable).ToArray();
        // BestMatchedConstructor

        // Diagnostics of serialization.
        public override string ToString()
        {
            // Serialize:""

            // Deserialize:""

            return @"new Foo(int , int, int,...) {} ";
        }
    }

    public abstract class MessagePackMetaConstructor
    {
        public MessagePackMetaParameter[] Parameters { get; set; }
    }

    public abstract class MessagePackMetaMember
    {
        public bool IsIntKey { get; set; }
        public int IntKey { get; set; }
        public string? StringKey { get; set; }
        public bool IsWritable { get; set; }
        public bool IsReadable { get; set; }
    }

    public abstract class MessagePackMetaParameter
    {
        public string Name { get; set; }
        public bool HasDefaultValue { get; set; }
        public object DefaultValue { get; set; }
    }

}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePack.CodeGenerator.Generator
{
    public partial class ResolverTemplate
    {
        public string Namespace;
        public string ResolverName = "GeneratedResolver";
        public ObjectSerializationInfo[] ObjectTypes;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePack.CodeGenerator.Generator
{
    public partial class FormatterTemplate
    {
        public string Namespace;
        public ObjectSerializationInfo[] objectSerializationInfos;
    }

    public partial class ResolverTemplate
    {
        public string Namespace;
        public string ResolverName = "GeneratedResolver";
        public IResolverRegisterInfo[] registerInfos;
    }
    public partial class EnumTemplate
    {
        public string Namespace;
        public EnumSerializationInfo[] enumSerializationInfos;
    }
}

#pragma warning disable CS8618

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MessagePackv3.Metas
{
    public class ReflectionMetaType : MessagePackMetaType
    {
        public Type Type { get; set; }
    }

    public abstract class ReflectionMetaMember : MessagePackMetaMember
    {
        public MemberInfo MemberInfo { get; set; } // PropertyInfo or FieldInfo
    }


    public abstract class ReflectionMetaConstructor : MessagePackMetaConstructor
    {
        public ConstructorInfo ConstructorInfo { get; set; }
    }



    public abstract class ReflectionMetaParameter : MessagePackMetaParameter
    {
        public ParameterInfo ParameterInfo { get; set; }
    }

}

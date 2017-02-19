using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePack.CodeGenerator
{
    public class ObjectSerializationInfo
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Namespace { get; set; }
        public bool IsIntKey { get; set; }
        public bool IsStringKey { get { return !IsIntKey; } }
        public bool IsClass { get; set; }
        public bool IsStruct { get { return !IsClass; } }
        // public ConstructorInfo BestmatchConstructor { get; set; }
        public MemberSerializationInfo[] ConstructorParameters { get; set; }
        public MemberSerializationInfo[] Members { get; set; }
        public int WriteCount
        {
            get
            {
                return Members.Count(x => x.IsReadable);
            }
        }

        public string GetConstructorString()
        {
            // TODO:best match constructor and parameters...
            return $"{FullName}()";
        }
    }

    public class MemberSerializationInfo
    {
        public bool IsProperty { get; set; }
        public bool IsField { get; set; }
        public bool IsWritable { get; set; }
        public bool IsReadable { get; set; }
        public int IntKey { get; set; }
        public string StringKey { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }

        public string GetSerializeMethodString()
        {
            // return $"MessagePackBinary.WriteXxx(ref bytes, offset, value.{Name}, formatterResolver)";

            return $"formatterResolver.GetFormatterWithVerify<{Type}>().Serialize(ref bytes, offset, value.{Name}, formatterResolver)";
        }

        public string GetDeserializeMethodString()
        {
            // return $"MessagePackBinary.WriteXxx(ref bytes, offset, value.{Name}, formatterResolver)";

            return $"formatterResolver.GetFormatterWithVerify<{Type}>().Deserialize(bytes, offset, formatterResolver, out readSize)";
        }
    }

    // TODO:EnumFormatter
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePack.CodeGenerator
{
    public interface IResolverRegisterInfo
    {
        string FullName { get; }
        string FormatterName { get; }
    }

    public class ObjectSerializationInfo : IResolverRegisterInfo
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Namespace { get; set; }
        public bool IsIntKey { get; set; }
        public bool IsStringKey { get { return !IsIntKey; } }
        public bool IsClass { get; set; }
        public bool IsStruct { get { return !IsClass; } }
        public MemberSerializationInfo[] ConstructorParameters { get; set; }
        public MemberSerializationInfo[] Members { get; set; }
        public bool HasIMessagePackSerializationCallbackReceiver { get; set; }
        public bool NeedsCastOnBefore { get; set; }
        public bool NeedsCastOnAfter { get; set; }
        public string FormatterName => Namespace + "." + Name + "Formatter";

        public int WriteCount
        {
            get
            {
                if (IsStringKey)
                {
                    return Members.Count(x => x.IsReadable);
                }
                else
                {
                    return MaxKey;
                }
            }
        }

        public int MaxKey
        {
            get
            {
                return Members.Where(x => x.IsReadable).Select(x => x.IntKey).DefaultIfEmpty(0).Max();
            }
        }

        public MemberSerializationInfo GetMember(int index)
        {
            return Members.FirstOrDefault(x => x.IntKey == index);
        }

        public string GetConstructorString()
        {
            var args = string.Join(", ", ConstructorParameters.Select(x => "__" + x.Name + "__"));
            return $"{FullName}({args})";
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
        public string ShortTypeName { get; set; }

        readonly HashSet<string> primitiveTypes = new HashSet<string>(new string[]
        {
            "short",
            "int",
            "long",
            "ushort",
            "uint",
            "ulong",
            "float",
            "double",
            "bool",
            "byte",
            "sbyte",
            "global::System.DateTime",
            "char",
            "byte[]",
            "string",
        });

        public string GetSerializeMethodString()
        {
            if (primitiveTypes.Contains(Type))
            {
                return $"MessagePackBinary.Write{ShortTypeName.Replace("[]", "s")}(ref bytes, offset, value.{Name})";
            }
            else
            {
                return $"formatterResolver.GetFormatterWithVerify<{Type}>().Serialize(ref bytes, offset, value.{Name}, formatterResolver)";
            }
        }

        public string GetDeserializeMethodString()
        {
            if (primitiveTypes.Contains(Type))
            {
                return $"MessagePackBinary.Read{ShortTypeName.Replace("[]", "s")}(bytes, offset, out readSize)";
            }
            else
            {
                return $"formatterResolver.GetFormatterWithVerify<{Type}>().Deserialize(bytes, offset, formatterResolver, out readSize)";
            }
        }
    }

    public class EnumSerializationInfo : IResolverRegisterInfo
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string UnderlyingType { get; set; }

        public string FormatterName => Namespace + "." + Name + "Formatter";
    }

    public class GenericSerializationInfo : IResolverRegisterInfo, IEquatable<GenericSerializationInfo>
    {
        public string FullName { get; set; }

        public string FormatterName { get; set; }
        public string ElementTypeArgs { get; set; }
        public string GenericFormatterName { get; set; }

        public bool Equals(GenericSerializationInfo other)
        {
            return FullName.Equals(other.FullName);
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }
    }

    public class UnionSerializationInfo : IResolverRegisterInfo
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string FormatterName => Namespace + "." + Name + "Formatter";
        public UnionSubTypeInfo[] SubTypes { get; set; }
    }

    public class UnionSubTypeInfo
    {
        public string Type { get; set; }
        public int Key { get; set; }
    }
}
// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePackCompiler.CodeAnalysis
{
    public interface INamespaceInfo
    {
        string? Namespace { get; }
    }

    public interface IResolverRegisterInfo
    {
        string FullName { get; }

        string FormatterName { get; }
    }

    public partial class ObjectSerializationInfo : IResolverRegisterInfo, INamespaceInfo
    {
        public string Name { get; }

        public string FullName { get; }

        public string? Namespace { get; }

        public GenericTypeParameterInfo[] GenericTypeParameters { get; }

        public bool IsOpenGenericType { get; }

        public bool IsIntKey { get; }

        public bool IsStringKey
        {
            get { return !this.IsIntKey; }
        }

        public bool IsClass { get; }

        public MemberSerializationInfo[] ConstructorParameters { get; }

        public MemberSerializationInfo[] Members { get; }

        public bool HasIMessagePackSerializationCallbackReceiver { get; }

        public bool NeedsCastOnBefore { get; }

        public bool NeedsCastOnAfter { get; }

        public string FormatterName => this.Namespace == null ? FormatterNameWithoutNameSpace : this.Namespace + "." + FormatterNameWithoutNameSpace;

        public string FormatterNameWithoutNameSpace => this.Name + "Formatter" + (this.IsOpenGenericType ? $"<{string.Join(", ", this.GenericTypeParameters.Select(x => x.Name))}>" : string.Empty);

        public void AppendFormatterNameWithoutNameSpace(StringBuilder builder)
        {
            builder.Append(Name);
            builder.Append("Formatter");
            if (!IsOpenGenericType)
            {
                return;
            }

            builder.Append('<');
            for (int i = 0; i < GenericTypeParameters.Length; i++)
            {
                if (i != 0)
                {
                    builder.Append(", ");
                }

                builder.Append(GenericTypeParameters[i].Name);
            }

            builder.Append('>');
        }

        public int WriteCount
        {
            get
            {
                if (this.IsStringKey)
                {
                    return this.Members.Count(x => x.IsReadable);
                }
                else
                {
                    return this.MaxKey;
                }
            }
        }

        public int MaxKey
        {
            get
            {
                return this.Members.Where(x => x.IsReadable).Select(x => x.IntKey).DefaultIfEmpty(-1).Max();
            }
        }

        public MemberSerializationInfo? GetMember(int index)
        {
            return this.Members.FirstOrDefault(x => x.IntKey == index);
        }

        public void AppendConstructor(StringBuilder builder)
        {
            builder.Append(FullName);
            builder.Append('(');
            if (ConstructorParameters.Length > 0)
            {
                builder.Append("__");
                builder.Append(ConstructorParameters[0].Name);
                builder.Append("__");

                for (int i = 1; i < ConstructorParameters.Length; i++)
                {
                    builder.Append(", __");
                    builder.Append(ConstructorParameters[i].Name);
                    builder.Append("__");
                }
            }

            builder.Append(')');
        }

        public ObjectSerializationInfo(bool isClass, bool isOpenGenericType, GenericTypeParameterInfo[] genericTypeParameterInfos, MemberSerializationInfo[] constructorParameters, bool isIntKey, MemberSerializationInfo[] members, string name, string fullName, string? @namespace, bool hasSerializationConstructor, bool needsCastOnAfter, bool needsCastOnBefore)
        {
            IsClass = isClass;
            IsOpenGenericType = isOpenGenericType;
            GenericTypeParameters = genericTypeParameterInfos;
            ConstructorParameters = constructorParameters;
            IsIntKey = isIntKey;
            Members = members;
            Name = name;
            FullName = fullName;
            Namespace = @namespace;
            HasIMessagePackSerializationCallbackReceiver = hasSerializationConstructor;
            NeedsCastOnAfter = needsCastOnAfter;
            NeedsCastOnBefore = needsCastOnBefore;
        }
    }

    public class GenericTypeParameterInfo
    {
        public string Name { get; }

        public string Constraints { get; }

        public bool HasConstraints { get; }

        public GenericTypeParameterInfo(string name, string constraints)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Constraints = constraints ?? throw new ArgumentNullException(nameof(name));
            HasConstraints = constraints != string.Empty;
        }
    }

    public partial class MemberSerializationInfo
    {
        public bool IsProperty { get; }

        public bool IsWritable { get; }

        public bool IsReadable { get; }

        public int IntKey { get; }

        public string StringKey { get; }

        public string Type { get; }

        public string Name { get; }

        public string ShortTypeName { get; }

        public string? CustomFormatterTypeName { get; }

        private static readonly HashSet<string> PrimitiveTypes = new(Generator.ShouldUseFormatterResolverHelper.PrimitiveTypes);

        public MemberSerializationInfo(bool isProperty, bool isWritable, bool isReadable, int intKey, string stringKey, string name, string type, string shortTypeName, string? customFormatterTypeName)
        {
            IsProperty = isProperty;
            IsWritable = isWritable;
            IsReadable = isReadable;
            IntKey = intKey;
            StringKey = stringKey;
            Type = type;
            Name = name;
            ShortTypeName = shortTypeName;
            CustomFormatterTypeName = customFormatterTypeName;
        }

        public void AppendSerializeMethod(StringBuilder builder)
        {
            if (CustomFormatterTypeName != null)
            {
                builder.Append("this.__");
                builder.Append(Name);
                builder.Append("CustomFormatter__.Serialize(ref writer, value.");
                builder.Append(Name);
                builder.Append(", options)");
            }
            else if (PrimitiveTypes.Contains(Type))
            {
                builder.Append("writer.Write(value.");
                builder.Append(Name);
                builder.Append(')');
            }
            else
            {
                builder.Append("global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<");
                builder.Append(Type);
                builder.Append(">(formatterResolver).Serialize(ref writer, value.");
                builder.Append(Name);
                builder.Append(", options)");
            }
        }

        public void AppendDeserializeMethod(StringBuilder builder)
        {
            if (CustomFormatterTypeName != null)
            {
                builder.Append("this.__");
                builder.Append(Name);
                builder.Append("CustomFormatter__.Deserialize(ref reader, options)");
            }
            else if (Type == "byte[]")
            {
                builder.Append("global::MessagePack.Internal.CodeGenHelpers.GetArrayFromNullableSequence(reader.ReadBytes())");
            }
            else if (PrimitiveTypes.Contains(Type))
            {
                builder.Append("reader.Read");
                builder.Append(ShortTypeName);
                builder.Append("()");
            }
            else
            {
                builder.Append("global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<");
                builder.Append(Type);
                builder.Append(">(formatterResolver).Deserialize(ref reader, options)");
            }
        }
    }

    public class EnumSerializationInfo : IResolverRegisterInfo, INamespaceInfo
    {
        public EnumSerializationInfo(string? @namespace, string name, string fullName, string underlyingType)
        {
            Namespace = @namespace;
            Name = name;
            FullName = fullName;
            UnderlyingType = underlyingType;
        }

        public string? Namespace { get; }

        public string Name { get; }

        public string FullName { get; }

        public string UnderlyingType { get; }

        public string FormatterName => (this.Namespace == null ? this.Name : this.Namespace + "." + this.Name) + "Formatter";
    }

    public class GenericSerializationInfo : IResolverRegisterInfo, IEquatable<GenericSerializationInfo>
    {
        public string FullName { get; }

        public string FormatterName { get; }

        public bool IsOpenGenericType { get; }

        public bool Equals(GenericSerializationInfo? other)
        {
            return this.FullName.Equals(other?.FullName);
        }

        public override int GetHashCode()
        {
            return this.FullName.GetHashCode();
        }

        public GenericSerializationInfo(string fullName, string formatterName, bool isOpenGenericType)
        {
            FullName = fullName;
            FormatterName = formatterName;
            IsOpenGenericType = isOpenGenericType;
        }
    }

    public class UnionSerializationInfo : IResolverRegisterInfo, INamespaceInfo
    {
        public string? Namespace { get; }

        public string Name { get; }

        public string FullName { get; }

        public string FormatterName => (this.Namespace == null ? this.Name : this.Namespace + "." + this.Name) + "Formatter";

        public UnionSubTypeInfo[] SubTypes { get; }

        public UnionSerializationInfo(string? @namespace, string name, string fullName, UnionSubTypeInfo[] subTypes)
        {
            Namespace = @namespace;
            Name = name;
            FullName = fullName;
            SubTypes = subTypes;
        }
    }

    public class UnionSubTypeInfo
    {
        public UnionSubTypeInfo(int key, string type)
        {
            Key = key;
            Type = type;
        }

        public int Key { get; }

        public string Type { get; }
    }
}

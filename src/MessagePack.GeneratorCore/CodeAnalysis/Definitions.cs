// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

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

    public class ObjectSerializationInfo : IResolverRegisterInfo, INamespaceInfo
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

        public string GetConstructorString()
        {
            var args = string.Join(", ", this.ConstructorParameters.Select(x => "__" + x.Name + "__"));
            return $"{this.FullName}({args})";
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

    public class MemberSerializationInfo
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

        private readonly HashSet<string> primitiveTypes = new(Generator.ShouldUseFormatterResolverHelper.PrimitiveTypes);

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

        public string GetSerializeMethodString()
        {
            if (CustomFormatterTypeName != null)
            {
                return $"this.__{this.Name}CustomFormatter__.Serialize(ref writer, value.{this.Name}, options)";
            }
            else if (this.primitiveTypes.Contains(this.Type))
            {
                return "writer.Write(value." + this.Name + ")";
            }
            else
            {
                return $"global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<{this.Type}>(formatterResolver).Serialize(ref writer, value.{this.Name}, options)";
            }
        }

        public string GetDeserializeMethodString()
        {
            if (CustomFormatterTypeName != null)
            {
                return $"this.__{this.Name}CustomFormatter__.Deserialize(ref reader, options)";
            }
            else if (this.primitiveTypes.Contains(this.Type))
            {
                if (this.Type == "byte[]")
                {
                    return "global::MessagePack.Internal.CodeGenHelpers.GetArrayFromNullableSequence(reader.ReadBytes())";
                }
                else
                {
                    return $"reader.Read{this.ShortTypeName!.Replace("[]", "s")}()";
                }
            }
            else
            {
                return $"global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<{this.Type}>(formatterResolver).Deserialize(ref reader, options)";
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

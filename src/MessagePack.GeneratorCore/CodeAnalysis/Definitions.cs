// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using StringLiteral;

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

        [Utf8("Formatter")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralFormatter();

        private static void Append(ref Utf8ValueStringBuilder builder, ReadOnlySpan<byte> span)
        {
            var destination = builder.GetSpan(span.Length);
            span.CopyTo(destination);
            builder.Advance(span.Length);
        }

        public void AppendFormatterNameWithoutNameSpace(ref Utf8ValueStringBuilder builder)
        {
            builder.Append(Name);
            Append(ref builder, GetUtf8ConstLiteralFormatter());
            if (!IsOpenGenericType)
            {
                return;
            }

            builder.GetSpan(1)[0] = (byte)'<';
            builder.Advance(1);
            builder.AppendJoin(", ", GenericTypeParameters.Select(x => x.Name));
            builder.GetSpan(1)[0] = (byte)'>';
            builder.Advance(1);
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

        public string GetConstructorString()
        {
            var args = string.Join(", ", this.ConstructorParameters.Select(x => "__" + x.Name + "__"));
            return $"{this.FullName}({args})";
        }

        [Utf8(", __")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralCommaSpaceUnderscoreUnderscore();

        [Utf8("__")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralUnderscoreUnderscore();

        public void AppendConstructor(ref Utf8ValueStringBuilder builder)
        {
            builder.Append(FullName);
            builder.GetSpan(1)[0] = (byte)'(';
            builder.Advance(1);
            if (ConstructorParameters.Length > 0)
            {
                Append(ref builder, GetUtf8ConstLiteralUnderscoreUnderscore());
                builder.Append(ConstructorParameters[0].Name);
                Append(ref builder, GetUtf8ConstLiteralUnderscoreUnderscore());

                for (int i = 1; i < ConstructorParameters.Length; i++)
                {
                    Append(ref builder, GetUtf8ConstLiteralCommaSpaceUnderscoreUnderscore());
                    builder.Append(ConstructorParameters[i].Name);
                    Append(ref builder, GetUtf8ConstLiteralUnderscoreUnderscore());
                }
            }

            builder.GetSpan(1)[0] = (byte)')';
            builder.Advance(1);
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

        [Utf8("this.__")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralThis();

        [Utf8("CustomFormatter__.Serialize(ref writer, value.")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralCustomFormatterSerialize();

        [Utf8(", options)")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralOptions();

        [Utf8("writer.Write(value.")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralWriterWrite();

        [Utf8("global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralFormatterWithVerify();

        [Utf8(">(formatterResolver).Serialize(ref writer, value.")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralFormatterSerialize();

        [Utf8("CustomFormatter__.Deserialize(ref reader, options)")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralCustomFormatterDeserialize();

        [Utf8("global::MessagePack.Internal.CodeGenHelpers.GetArrayFromNullableSequence(reader.ReadBytes())")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralByteArrayDeserialize();

        [Utf8("reader.Read")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralReaderRead();

        [Utf8("()")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralParenPair();

        [Utf8(">(formatterResolver).Deserialize(ref reader, options)")]
        private static partial ReadOnlySpan<byte> GetUtf8ConstLiteralFormatterDeserialize();

        private static void Append(ref Utf8ValueStringBuilder builder, ReadOnlySpan<byte> span)
        {
            var destination = builder.GetSpan(span.Length);
            span.CopyTo(destination);
            builder.Advance(span.Length);
        }

        public void AppendSerializeMethod(ref Utf8ValueStringBuilder builder)
        {
            if (CustomFormatterTypeName != null)
            {
                Append(ref builder, GetUtf8ConstLiteralThis());
                builder.Append(Name);
                Append(ref builder, GetUtf8ConstLiteralCustomFormatterSerialize());
                builder.Append(Name);
                Append(ref builder, GetUtf8ConstLiteralOptions());
            }
            else if (PrimitiveTypes.Contains(Type))
            {
                Append(ref builder, GetUtf8ConstLiteralWriterWrite());
                builder.Append(Name);
                builder.GetSpan(1)[0] = (byte)')';
                builder.Advance(1);
            }
            else
            {
                Append(ref builder, GetUtf8ConstLiteralFormatterWithVerify());
                builder.Append(Type);
                Append(ref builder, GetUtf8ConstLiteralFormatterSerialize());
                builder.Append(Name);
                Append(ref builder, GetUtf8ConstLiteralOptions());
            }
        }

        public void AppendDeserializeMethod(ref Utf8ValueStringBuilder builder)
        {
            if (CustomFormatterTypeName != null)
            {
                Append(ref builder, GetUtf8ConstLiteralThis());
                builder.Append(Name);
                Append(ref builder, GetUtf8ConstLiteralCustomFormatterDeserialize());
            }
            else if (Type == "byte[]")
            {
                Append(ref builder, GetUtf8ConstLiteralByteArrayDeserialize());
            }
            else if (PrimitiveTypes.Contains(Type))
            {
                Append(ref builder, GetUtf8ConstLiteralReaderRead());
                builder.Append(ShortTypeName);
                Append(ref builder, GetUtf8ConstLiteralParenPair());
            }
            else
            {
                Append(ref builder, GetUtf8ConstLiteralFormatterWithVerify());
                builder.Append(Type);
                Append(ref builder, GetUtf8ConstLiteralFormatterDeserialize());
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

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

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

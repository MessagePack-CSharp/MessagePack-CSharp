// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public record ObjectSerializationInfo : ResolverRegisterInfo
{
    public required bool IsClass { get; init; }

    public bool IncludesPrivateMembers { get; init; }

    public required GenericTypeParameterInfo[] GenericTypeParameters { get; init; }

    public required MemberSerializationInfo[] ConstructorParameters { get; init; }

    public required bool IsIntKey { get; init; }

    public required MemberSerializationInfo[] Members { get; init; }

    public required bool HasIMessagePackSerializationCallbackReceiver { get; init; }

    public required bool NeedsCastOnAfter { get; init; }

    public required bool NeedsCastOnBefore { get; init; }

    public bool IsStringKey => !this.IsIntKey;

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

    public static ObjectSerializationInfo Create(
        INamedTypeSymbol dataType,
        bool isClass,
        bool includesPrivateMembers,
        GenericTypeParameterInfo[] genericTypeParameters,
        MemberSerializationInfo[] constructorParameters,
        bool isIntKey,
        MemberSerializationInfo[] members,
        bool hasIMessagePackSerializationCallbackReceiver,
        bool needsCastOnAfter,
        bool needsCastOnBefore,
        ResolverOptions resolverOptions)
    {
        ResolverRegisterInfo basicInfo = ResolverRegisterInfo.Create(
            dataType,
            resolverOptions,
            includesPrivateMembers ? FormatterPosition.UnderDataType : FormatterPosition.UnderResolver);
        return new ObjectSerializationInfo
        {
            DataType = basicInfo.DataType,
            Formatter = basicInfo.Formatter,
            IsClass = isClass,
            IncludesPrivateMembers = includesPrivateMembers,
            GenericTypeParameters = genericTypeParameters,
            ConstructorParameters = constructorParameters,
            IsIntKey = isIntKey,
            Members = members,
            HasIMessagePackSerializationCallbackReceiver = hasIMessagePackSerializationCallbackReceiver,
            NeedsCastOnAfter = needsCastOnAfter,
            NeedsCastOnBefore = needsCastOnBefore,
        };
    }

    public MemberSerializationInfo? GetMember(int index)
    {
        return this.Members.FirstOrDefault(x => x.IntKey == index);
    }

    public string GetConstructorString()
    {
        var args = string.Join(", ", this.ConstructorParameters.Select(x => "__" + x.Name + "__"));
        return $"{this.DataType.GetQualifiedName(Qualifiers.GlobalNamespace, GenericParameterStyle.Identifiers)}({args})";
    }

    public virtual bool Equals(ObjectSerializationInfo? other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (this.GetType() != other.GetType())
        {
            return false;
        }

        // Compare all the properties by value
        return base.Equals(other)
            && IsClass == other.IsClass
            && GenericTypeParameters.SequenceEqual(other.GenericTypeParameters)
            && ConstructorParameters.SequenceEqual(other.ConstructorParameters)
            && IsIntKey == other.IsIntKey
            && Members.SequenceEqual(other.Members)
            && HasIMessagePackSerializationCallbackReceiver == other.HasIMessagePackSerializationCallbackReceiver
            && NeedsCastOnAfter == other.NeedsCastOnAfter
            && NeedsCastOnBefore == other.NeedsCastOnBefore;
    }

    public override int GetHashCode() => base.GetHashCode();
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public record ObjectSerializationInfo : ResolverRegisterInfo
{
    public required bool IsClass { get; init; }

    public required GenericTypeParameterInfo[] GenericTypeParameters { get; init; }

    public required MemberSerializationInfo[] ConstructorParameters { get; init; }

    /// <summary>
    /// Gets the members that are either init-only properties or required (and therefore must appear in an object initializer).
    /// </summary>
    public required MemberSerializationInfo[] InitMembers { get; init; }

    public bool MustDeserializeFieldsFirst => this.ConstructorParameters.Length > 0 || this.InitMembers.Length > 0;

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
        bool nestedFormatterRequired,
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
            nestedFormatterRequired ? FormatterPosition.UnderDataType : FormatterPosition.UnderResolver);
        return new ObjectSerializationInfo
        {
            DataType = basicInfo.DataType,
            Formatter = basicInfo.Formatter,
            IsClass = isClass,
            GenericTypeParameters = genericTypeParameters,
            ConstructorParameters = constructorParameters,
            InitMembers = members.Where(x => ((x.IsProperty && x.IsInitOnly) || x.IsRequired) && !constructorParameters.Contains(x)).ToArray(),
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
        StringBuilder builder = new();
        builder.Append(this.DataType.GetQualifiedName(Qualifiers.GlobalNamespace, GenericParameterStyle.Identifiers));

        builder.Append('(');

        for (int i = 0; i < this.ConstructorParameters.Length; i++)
        {
            if (i != 0)
            {
                builder.Append(", ");
            }

            builder.Append(this.ConstructorParameters[i].LocalVariableName);
        }

        builder.Append(')');

        if (this.InitMembers.Length > 0)
        {
            builder.Append(" { ");

            for (int i = 0; i < this.InitMembers.Length; i++)
            {
                if (i != 0)
                {
                    builder.Append(", ");
                }

                // Strictly speaking, we should only be assigning these init-only properties if values for them
                // was provided in the deserialized stream.
                // However the C# language does not provide a means to do this, so we always assign them.
                // https://github.com/dotnet/csharplang/issues/6117
                builder.Append(this.InitMembers[i].Name);
                builder.Append(" = ");
                builder.Append(this.InitMembers[i].LocalVariableName);
            }

            builder.Append(" }");
        }

        return builder.ToString();
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

    public override int GetHashCode()
    {
        int hash = base.GetHashCode();

        hash = Hash(hash, this.IsClass ? 1 : 2);
        hash = Hash(hash, this.GenericTypeParameters.Length);

        return hash;

        static int Hash(int hashCode, int value)
        {
            return (hashCode * 31) + value;
        }
    }
}

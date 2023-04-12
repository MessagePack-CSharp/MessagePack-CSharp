// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePack.Analyzers.CodeAnalysis;

public record ObjectSerializationInfo(
    bool IsClass,
    bool IsOpenGenericType,
    GenericTypeParameterInfo[] GenericTypeParameters,
    MemberSerializationInfo[] ConstructorParameters,
    bool IsIntKey,
    MemberSerializationInfo[] Members,
    string Name,
    string FullName,
    string? Namespace,
    bool HasIMessagePackSerializationCallbackReceiver,
    bool NeedsCastOnAfter,
    bool NeedsCastOnBefore) : IResolverRegisterInfo
{
    public bool IsStringKey
    {
        get { return !this.IsIntKey; }
    }

    public string FileNameHint => $"{CodeAnalysisUtilities.AppendNameToNamespace("Formatters", this.Namespace)}.{this.FormatterNameWithoutNamespace}";

    public string FormatterName => CodeAnalysisUtilities.QualifyWithOptionalNamespace(this.FormatterNameWithoutNamespace, $"Formatters::{this.Namespace}");

    public string FormatterNameWithoutNamespace => this.Name + "Formatter" + (this.IsOpenGenericType ? $"<{string.Join(", ", this.GenericTypeParameters.Select(x => x.Name))}>" : string.Empty);

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

    public IReadOnlyCollection<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();

    public MemberSerializationInfo? GetMember(int index)
    {
        return this.Members.FirstOrDefault(x => x.IntKey == index);
    }

    public string GetConstructorString()
    {
        var args = string.Join(", ", this.ConstructorParameters.Select(x => "__" + x.Name + "__"));
        return $"{this.FullName}({args})";
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
        return IsClass == other.IsClass &&
               IsOpenGenericType == other.IsOpenGenericType &&
               GenericTypeParameters.SequenceEqual(other.GenericTypeParameters) &&
               ConstructorParameters.SequenceEqual(other.ConstructorParameters) &&
               IsIntKey == other.IsIntKey &&
               Members.SequenceEqual(other.Members) &&
               Name == other.Name &&
               FullName == other.FullName &&
               Namespace == other.Namespace &&
               HasIMessagePackSerializationCallbackReceiver == other.HasIMessagePackSerializationCallbackReceiver &&
               NeedsCastOnAfter == other.NeedsCastOnAfter &&
               NeedsCastOnBefore == other.NeedsCastOnBefore;
    }

    public override int GetHashCode() => throw new NotImplementedException();
}

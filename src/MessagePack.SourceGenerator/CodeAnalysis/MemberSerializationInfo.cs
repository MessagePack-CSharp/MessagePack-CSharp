// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.CodeAnalysis;

public record MemberSerializationInfo(
    bool IsProperty,
    bool IsWritable,
    bool IsReadable,
    bool IsInitOnly,
    bool IsRequired,
    int IntKey,
    string StringKey,
    string Name,
    string Type,
    string ShortTypeName,
    FormatterDescriptor? CustomFormatter)
{
    private static readonly IReadOnlyCollection<string> PrimitiveTypes = new HashSet<string>(AnalyzerUtilities.PrimitiveTypes);

    public string LocalVariableName => $"__{this.UniqueIdentifier}__";

    public string UniqueIdentifier => this.DeclaringType is null ? this.Name : $"{this.DeclaringType.Name}_{this.Name}";

    /// <summary>
    /// Gets the declaring type of this member, if a derived type declares a new member
    /// with the same identifier, thus requiring a source generator to cast
    /// the target to the member's declaring type in order to access it.
    /// </summary>
    public required QualifiedNamedTypeName? DeclaringType { get; init; }

    public string GetSerializeMethodString()
    {
        string memberRead = this.GetMemberAccess("value");

        if (CustomFormatter is not null)
        {
            return $"this.__{this.Name}CustomFormatter__.Serialize(ref writer, {memberRead}, options)";
        }
        else if (PrimitiveTypes.Contains(this.Type))
        {
            return "writer.Write(value." + this.Name + ")";
        }
        else
        {
            return $"MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<{this.Type}>(formatterResolver).Serialize(ref writer, {memberRead}, options)";
        }
    }

    public string GetDeserializeMethodString()
    {
        if (CustomFormatter is not null)
        {
            return $"this.__{this.Name}CustomFormatter__.Deserialize(ref reader, options)";
        }
        else if (PrimitiveTypes.Contains(this.Type))
        {
            if (this.Type == "byte[]")
            {
                return "MsgPack::Internal.CodeGenHelpers.GetArrayFromNullableSequence(reader.ReadBytes())";
            }
            else
            {
                return $"reader.Read{this.ShortTypeName!.Replace("[]", "s")}()";
            }
        }
        else
        {
            return $"MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<{this.Type}>(formatterResolver).Deserialize(ref reader, options)";
        }
    }

    public string GetMemberAccess(string targetObject) => this.DeclaringType is null ? $"{targetObject}.{this.Name}" : $"(({this.DeclaringType.GetQualifiedName()}){targetObject}).{this.Name}";
}

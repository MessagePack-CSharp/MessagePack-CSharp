// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.CodeAnalysis;

public record MemberSerializationInfo(
    bool IsProperty,
    bool IsWritable,
    bool IsReadable,
    int IntKey,
    string StringKey,
    string Name,
    string Type,
    string ShortTypeName,
    FormatterDescriptor? CustomFormatter)
{
    private static readonly IReadOnlyCollection<string> PrimitiveTypes = new HashSet<string>(AnalyzerUtilities.PrimitiveTypes);

    public string GetSerializeMethodString()
    {
        if (CustomFormatter is not null)
        {
            return $"this.__{this.Name}CustomFormatter__.Serialize(ref writer, value.{this.Name}, options)";
        }
        else if (PrimitiveTypes.Contains(this.Type))
        {
            return "writer.Write(value." + this.Name + ")";
        }
        else
        {
            return $"MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<{this.Type}>(formatterResolver).Serialize(ref writer, value.{this.Name}, options)";
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
}

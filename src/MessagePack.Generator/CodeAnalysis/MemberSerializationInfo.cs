// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Generator.Transforms;

namespace MessagePack.Generator.CodeAnalysis;

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

    private readonly HashSet<string> primitiveTypes = new(ShouldUseFormatterResolverHelper.PrimitiveTypes);

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

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;

public class ExcludedCustomFormatter
{
    [Fact]
    public void ExcludedFormatterIsIgnored()
    {
        // This would normally succeed because of our custom formatter and the auto-generated resolver,
        // but because of the attribute applied to the formatter, it should not be included in the resolver,
        // and thus our custom type should fail to serialize as an unknown type.
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Serialize(default(CustomType), MessagePackSerializerOptions.Standard));
    }

    internal struct CustomType;

    [ExcludeFormatterFromSourceGeneratedResolver]
    internal class CustomFormatter : IMessagePackFormatter<CustomType>
    {
        public CustomType Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            reader.Skip();
            return default;
        }

        public void Serialize(ref MessagePackWriter writer, CustomType value, MessagePackSerializerOptions options)
        {
            writer.WriteNil();
        }
    }
}

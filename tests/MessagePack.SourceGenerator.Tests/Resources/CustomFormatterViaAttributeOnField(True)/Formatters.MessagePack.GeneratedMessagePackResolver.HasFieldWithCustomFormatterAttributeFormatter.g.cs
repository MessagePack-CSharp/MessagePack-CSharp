﻿// <auto-generated />

#pragma warning disable 618, 612, 414, 168, CS1591, SA1129, SA1309, SA1312, SA1403, SA1649

using MsgPack = global::MessagePack;

namespace MessagePack {
partial class GeneratedMessagePackResolver {
	internal sealed class HasFieldWithCustomFormatterAttributeFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::HasFieldWithCustomFormatterAttribute>
	{
		private readonly global::UnserializableRecordFormatter __CustomValueCustomFormatter__ = new global::UnserializableRecordFormatter();
		// CustomValue
		private static global::System.ReadOnlySpan<byte> GetSpan_CustomValue() => new byte[1 + 11] { 171, 67, 117, 115, 116, 111, 109, 86, 97, 108, 117, 101 };

		public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::HasFieldWithCustomFormatterAttribute value, global::MessagePack.MessagePackSerializerOptions options)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteMapHeader(1);
			writer.WriteRaw(GetSpan_CustomValue());
			this.__CustomValueCustomFormatter__.Serialize(ref writer, value.CustomValue, options);
		}

		public global::HasFieldWithCustomFormatterAttribute Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			options.Security.DepthStep(ref reader);
			var length = reader.ReadMapHeader();
			var ____result = new global::HasFieldWithCustomFormatterAttribute();

			for (int i = 0; i < length; i++)
			{
				var stringKey = global::MessagePack.Internal.CodeGenHelpers.ReadStringSpan(ref reader);
				switch (stringKey.Length)
				{
					default:
					FAIL:
					  reader.Skip();
					  continue;
					case 11:
					    if (!global::System.MemoryExtensions.SequenceEqual(stringKey, GetSpan_CustomValue().Slice(1))) { goto FAIL; }

					    ____result.CustomValue = this.__CustomValueCustomFormatter__.Deserialize(ref reader, options);
					    continue;

				}
			}

			reader.Depth--;
			return ____result;
		}
	}

}
}

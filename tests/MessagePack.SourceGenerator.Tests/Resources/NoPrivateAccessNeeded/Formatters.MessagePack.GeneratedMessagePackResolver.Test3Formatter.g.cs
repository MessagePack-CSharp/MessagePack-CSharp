﻿// <auto-generated />

#pragma warning disable 618, 612, 414, 168, CS1591, SA1129, SA1309, SA1312, SA1403, SA1649

#pragma warning disable CS8669 // We may leak nullable annotations into generated code.

using MsgPack = global::MessagePack;

namespace MessagePack {
partial class GeneratedMessagePackResolver {

	internal sealed class Test3Formatter : MsgPack::Formatters.IMessagePackFormatter<global::Test3>
	{

		public void Serialize(ref MsgPack::MessagePackWriter writer, global::Test3 value, MsgPack::MessagePackSerializerOptions options)
		{
			if (value == null)
			{
				writer.WriteNil();
				return;
			}

			MsgPack::IFormatterResolver formatterResolver = options.Resolver;
			writer.WriteArrayHeader(1);
			MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.Name, options);
		}

		public global::Test3 Deserialize(ref MsgPack::MessagePackReader reader, MsgPack::MessagePackSerializerOptions options)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			options.Security.DepthStep(ref reader);
			MsgPack::IFormatterResolver formatterResolver = options.Resolver;
			var length = reader.ReadArrayHeader();
			var __Name__ = default(string);

			for (int i = 0; i < length; i++)
			{
				switch (i)
				{
					case 0:
						__Name__ = MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
						break;
					default:
						reader.Skip();
						break;
				}
			}

			var ____result = new global::Test3(__Name__);
			reader.Depth--;
			return ____result;
		}
	}
}
}

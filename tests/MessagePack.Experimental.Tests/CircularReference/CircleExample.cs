// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Experimental.Tests.CircularReference
{
    [MessagePackObject]
    public sealed class CircleExample
    {
        [Key(0)]
        public CircleExample? Parent { get; set; }

        [Key(1)]
        public CircleExample? Child0 { get; set; }

        [Key(2)]
        public CircleExample? Child1 { get; set; }

        [Key(3)]
        public int Id { get; set; }
    }

    public sealed class CircleExampleOverwriter : IMessagePackDeserializeOverwriter<CircleExample>
    {
        public static readonly CircleExampleOverwriter Instance = new CircleExampleOverwriter();

        private CircleExampleOverwriter()
        {
        }

        public void DeserializeOverwrite(ref MessagePackReader reader, MessagePackSerializerOptions options, CircleExample value)
        {
            var length = reader.ReadArrayHeader();
            if (length == 0)
            {
                return;
            }

            var selfFormatter = options.Resolver.GetFormatterWithVerify<CircleExample>();
            for (var i = 0; i < length; i++)
            {
                switch (i)
                {
                    default:
                        reader.Skip();
                        break;
                    case 0:
                        value.Parent = selfFormatter.Deserialize(ref reader, options);
                        break;
                    case 1:
                        value.Child0 = selfFormatter.Deserialize(ref reader, options);
                        break;
                    case 2:
                        value.Child1 = selfFormatter.Deserialize(ref reader, options);
                        break;
                    case 3:
                        value.Id = reader.ReadInt32();
                        break;
                }
            }
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Experimental.Tests.CircularReference
{
    [MessagePackObject]
    public class TwinExample0
    {
        [Key(0)]
        public string? Name { get; set; }

        [Key(1)]
        public TwinExample1? Partner { get; set; }
    }

    [MessagePackObject]
    public class TwinExample1
    {
        [Key(0)]
        public string? Name { get; set; }

        [Key(1)]
        public TwinExample0? Partner { get; set; }
    }

    public sealed class TwinExample0Overwriter : IMessagePackDeserializeOverwriter<TwinExample0>
    {
        public static readonly TwinExample0Overwriter Instance = new TwinExample0Overwriter();

        private TwinExample0Overwriter()
        {
        }

        public void DeserializeOverwrite(ref MessagePackReader reader, MessagePackSerializerOptions options, TwinExample0 value)
        {
            var length = reader.ReadArrayHeader();
            if (length == 0)
            {
                return;
            }

            var resolver = options.Resolver;
            for (var i = 0; i < length; i++)
            {
                switch (i)
                {
                    default:
                        reader.Skip();
                        break;
                    case 0:
                        value.Name = resolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        value.Partner = resolver.GetFormatterWithVerify<TwinExample1>().Deserialize(ref reader, options);
                        break;
                }
            }
        }
    }

    public sealed class TwinExample1Overwriter : IMessagePackDeserializeOverwriter<TwinExample1>
    {
        public static readonly TwinExample1Overwriter Instance = new TwinExample1Overwriter();

        private TwinExample1Overwriter()
        {
        }

        public void DeserializeOverwrite(ref MessagePackReader reader, MessagePackSerializerOptions options, TwinExample1 value)
        {
            var length = reader.ReadArrayHeader();
            if (length == 0)
            {
                return;
            }

            var resolver = options.Resolver;
            for (var i = 0; i < length; i++)
            {
                switch (i)
                {
                    default:
                        reader.Skip();
                        break;
                    case 0:
                        value.Name = resolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                        break;
                    case 1:
                        value.Partner = resolver.GetFormatterWithVerify<TwinExample0>().Deserialize(ref reader, options);
                        break;
                }
            }
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable SA1300 // Element should begin with uppercase letter

namespace MessagePack.Tests
{
    public class LZ4Test
    {
#if !UNITY_2018_3_OR_NEWER

        private readonly ITestOutputHelper logger;

        public LZ4Test(ITestOutputHelper logger)
        {
            this.logger = logger;
        }

#endif

        [Fact]
        public void Lz4Compress()
        {
            Execute(1);
            Execute(10);
            Execute(100);
            Execute(1000);
            Execute(10000);
        }

        private void Execute(int count)
        {
            // Large
            {
                var data = Enumerable.Range(1, count)
                    .Select(x => new SharedData.SimpleStringKeyData
                    {
                        Prop1 = x,
                        Prop2 = SharedData.ByteEnum.A,
                        Prop3 = unchecked(x * x),
                    })
                    .ToArray();

                var lz4Option = MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4Block);
                var lz4Contiguous = MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.Lz4BlockArray);

                // check bin1, 2, 3 size...
                var bin2 = MessagePackSerializer.Serialize(data, lz4Option);
                var bin3 = MessagePackSerializer.Serialize(data, lz4Contiguous);

#if !UNITY_2018_3_OR_NEWER
                var bin1 = MessagePackSerializer.Serialize(data, MessagePackSerializer.DefaultOptions.WithCompression(MessagePackCompression.None));
                logger.WriteLine("Len:" + count + " NoneSize:" + bin1.Length);
                logger.WriteLine("Len:" + count + " Lz4BlockSize:" + bin2.Length);
                logger.WriteLine("Len:" + count + " Lz4ContiguousBlockSize:" + bin3.Length);
#endif

                var d1 = MessagePackSerializer.Deserialize<SharedData.SimpleStringKeyData[]>(bin2, lz4Option);
                var d2 = MessagePackSerializer.Deserialize<SharedData.SimpleStringKeyData[]>(bin2, lz4Contiguous);
                var d3 = MessagePackSerializer.Deserialize<SharedData.SimpleStringKeyData[]>(bin3, lz4Option);
                var d4 = MessagePackSerializer.Deserialize<SharedData.SimpleStringKeyData[]>(bin3, lz4Contiguous);

                SequenceStructuralEqual(d1, data);
                SequenceStructuralEqual(d2, data);
                SequenceStructuralEqual(d3, data);
                SequenceStructuralEqual(d4, data);
            }
        }

        private static void SequenceStructuralEqual(SharedData.SimpleStringKeyData[] actual, SharedData.SimpleStringKeyData[] expected)
        {
            actual.Length.Is(expected.Length);

            for (int i = 0; i < actual.Length; i++)
            {
                actual[i].Prop1.Is(expected[i].Prop1);
                actual[i].Prop2.Is(expected[i].Prop2);
                actual[i].Prop3.Is(expected[i].Prop3);
            }
        }
    }
}

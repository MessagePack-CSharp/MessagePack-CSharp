// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class MultiDimensionalArrayTest
    {
        private T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }

        [Theory]
        [InlineData(100, 100, 10, 5)]
        [InlineData(10, 20, 15, 5)]
        [InlineData(3, 5, 10, 15)]
        public void MultiDimensional(int dataI, int dataJ, int dataK, int dataL)
        {
            var two = new ValueTuple<int, int>[dataI, dataJ];
            var three = new ValueTuple<int, int, int>[dataI, dataJ, dataK];
            var four = new ValueTuple<int, int, int, int>[dataI, dataJ, dataK, dataL];

            for (int i = 0; i < dataI; i++)
            {
                for (int j = 0; j < dataJ; j++)
                {
                    two[i, j] = (i, j);
                    for (int k = 0; k < dataK; k++)
                    {
                        three[i, j, k] = (i, j, k);
                        for (int l = 0; l < dataL; l++)
                        {
                            four[i, j, k, l] = (i, j, k, l);
                        }
                    }
                }
            }

            (int, int)[,] cTwo = this.Convert(two);
            (int, int, int)[,,] cThree = this.Convert(three);
            (int, int, int, int)[,,,] cFour = this.Convert(four);

            cTwo.Length.Is(two.Length);
            cThree.Length.Is(three.Length);
            cFour.Length.Is(four.Length);

            for (int i = 0; i < dataI; i++)
            {
                for (int j = 0; j < dataJ; j++)
                {
                    cTwo[i, j].Is(two[i, j]);
                    for (int k = 0; k < dataK; k++)
                    {
                        cThree[i, j, k].Is(three[i, j, k]);
                        for (int l = 0; l < dataL; l++)
                        {
                            cFour[i, j, k, l].Is(four[i, j, k, l]);
                        }
                    }
                }
            }
        }

        [Fact]
        [Trait("CWE", "789")]
        public void RejectsTwoDimensionalArrayWithMismatchedElementCount()
        {
            byte[] payload =
            {
                0x93,
                0xCE, 0x00, 0x00, 0x07, 0xD0,
                0xCE, 0x00, 0x00, 0x07, 0xD0,
                0x90,
            };

            AssertRejects<byte[,]>(payload);
        }

        [Fact]
        [Trait("CWE", "789")]
        public void RejectsThreeDimensionalArrayWithMismatchedElementCount()
        {
            byte[] payload =
            {
                0x94,
                0xCC, 0x80,
                0xCC, 0x80,
                0xCC, 0x80,
                0x90,
            };

            AssertRejects<byte[,,]>(payload);
        }

        [Fact]
        [Trait("CWE", "789")]
        public void RejectsFourDimensionalArrayWithMismatchedElementCount()
        {
            byte[] payload =
            {
                0x95,
                0x20,
                0x20,
                0x20,
                0x20,
                0x90,
            };

            AssertRejects<byte[,,,]>(payload);
        }

        private void AssertRejects<T>(byte[] payload)
        {
            var options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

            Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<T>(payload, options));
        }
    }
}

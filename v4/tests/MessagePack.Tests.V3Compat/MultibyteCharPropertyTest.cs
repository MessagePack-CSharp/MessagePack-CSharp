// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace MessagePack.Tests
{
    public class MultibyteCharPropertyTest
    {
        [MessagePackObject(true)]
        public class データ
        {
            public int A { get; set; }

            public int B { get; set; }

            public int にほんご { get; set; }

            public int 简体字 { get; set; }

            public int 훈민정음 { get; set; }
        }

        [Fact]
        public void ConvertMultibyteCharProperty()
        {
            var data = new データ
            {
                A = 1,
                B = 2,
                にほんご = 3,
                简体字 = 4,
                훈민정음 = 5,
            };

            byte[] bytes = MessagePackSerializer.Serialize(data);

            MessagePackSerializer.ConvertToJson(bytes).Is(@"{""A"":1,""B"":2,""にほんご"":3,""简体字"":4,""훈민정음"":5}");
            var a = MessagePackSerializer.Deserialize<データ>(bytes);
            a.A.Is(data.A);
            a.B.Is(data.B);
            a.にほんご.Is(data.にほんご);
            a.简体字.Is(data.简体字);
            a.훈민정음.Is(data.훈민정음);
        }
    }
}

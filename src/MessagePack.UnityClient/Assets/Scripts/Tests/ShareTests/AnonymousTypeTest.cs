// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !ENABLE_IL2CPP

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
    public class AnonymousTypeTest
    {
        [Fact]
        public void SerializeAndConvertToJson()
        {
            var testData = new { Hoge = 100, Huga = true, Yaki = new { Rec = 1, T = 10 }, Nano = "nanoanno" };

            var data = MessagePackSerializer.Serialize(testData, ContractlessStandardResolver.Options);

            MessagePackSerializer.ConvertToJson(data).Is(@"{""Hoge"":100,""Huga"":true,""Yaki"":{""Rec"":1,""T"":10},""Nano"":""nanoanno""}");
        }
    }
}

#endif

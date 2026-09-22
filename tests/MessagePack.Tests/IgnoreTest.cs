// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class IgnoreTest
    {
        [Fact]
        public void DataContract()
        {
            var vm = new ViewModel() { NoModel = 999, MyProperty1 = 10, MyProperty2 = 100 };
            var bin = MessagePackSerializer.Serialize(vm);
            ViewModel d = MessagePackSerializer.Deserialize<ViewModel>(bin);
            var json = MessagePackSerializer.ConvertToJson(bin);

            d.NoModel.Is(0);
            d.MyProperty1.Is(10);
            d.MyProperty2.Is(100);
            json.Is(@"{""MyProperty1"":10,""MyProperty2"":100}");
        }

        [MessagePackObject(true)]
        public class ViewModel
        {
            [IgnoreDataMember]
            public int NoModel { get; set; }

            public int MyProperty1 { get; set; }

            public int MyProperty2 { get; set; }
        }
    }
}

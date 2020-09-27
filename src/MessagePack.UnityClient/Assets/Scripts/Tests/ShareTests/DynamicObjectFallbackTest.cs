﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !ENABLE_IL2CPP

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class DynamicObjectFallbackTest
    {
        [Fact]
        public void DynamicObject()
        {
            var testData = new object[]
            {
                new DynamicObjectFallbackTestContainer
                {
                    MyProperty = 100,
                    MoreObject = new string[] { "a", "b", "c" },
                },

                new DynamicObjectFallbackTestContainer
                {
                    MyProperty = 300,
                    MoreObject = new SharedData.SimpleStringKeyData
                    {
                        Prop1 = 10,
                        Prop2 = SharedData.ByteEnum.C,
                        Prop3 = 99999,
                    },
                },
            };

            var data1 = MessagePackSerializer.Serialize(testData);

            var json = MessagePackSerializer.ConvertToJson(data1);

            json.Is(@"[[100,[""a"",""b"",""c""]],[300,{""Prop1"":10,""Prop2"":2,""Prop3"":99999}]]");
        }

        [Fact]
        public void FallbackObjectType()
        {
            var data = new DynamicObjectFallbackTestContainer
            {
                MyProperty = 3,
                MoreObject = 10,
            };

            var bytes = MessagePackSerializer.Serialize(data);

            dynamic obj = MessagePackSerializer.Deserialize<object>(bytes);

            object v = obj[1];

            v.GetType().Is(typeof(int));
        }

        [MessagePackObject]
        public class DynamicObjectFallbackTestContainer
        {
            [Key(0)]
            public int MyProperty { get; set; }

            [Key(1)]
            public object MoreObject { get; set; }
        }
    }
}

#endif

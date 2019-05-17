﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    [MessagePackObject]
    public class DynamicObjectFallbackTestContainer
    {
        [Key(0)]
        public int MyProperty { get; set; }

        [Key(1)]
        public object MoreObject { get; set; }
    }


    public class DynamicObjectFallbackTest
    {
        private MessagePackSerializer serializer = new MessagePackSerializer();

        [Fact]
        public void DynamicObject()
        {
            var testData = new object[]
            {
                new DynamicObjectFallbackTestContainer
                {
                    MyProperty = 100,
                    MoreObject = new string[]{"a", "b", "c" }
                },

                new DynamicObjectFallbackTestContainer
                {
                    MyProperty = 300,
                    MoreObject = new SharedData.SimpleStringKeyData
                    {
                        Prop1 = 10,
                        Prop2 = SharedData.ByteEnum.C,
                        Prop3 = 99999
                    }
                },
            };

            var data1 = serializer.Serialize(testData);

            var json = serializer.ConvertToJson(data1);

            json.Is(@"[[100,[""a"",""b"",""c""]],[300,{""Prop1"":10,""Prop2"":2,""Prop3"":99999}]]");

        }
    }
}

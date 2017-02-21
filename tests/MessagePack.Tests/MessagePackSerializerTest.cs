using MessagePack.Resolvers;
using SharedData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class MessagePackSerializerTest
    {
        T ConvertNonGeneric<T>(T obj)
        {
            var t = obj.GetType();
            return (T)MessagePackSerializer.NonGeneric.Deserialize(t, MessagePackSerializer.NonGeneric.Serialize(t, obj));
        }

        [Fact]
        public void NonGeneric()
        {
            var data = new FirstSimpleData { Prop1 = 9, Prop2 = "hoge", Prop3 = 999 };
            var t = typeof(FirstSimpleData);
            var ms = new MemoryStream();

            var data1 = MessagePackSerializer.NonGeneric.Deserialize(t, MessagePackSerializer.NonGeneric.Serialize(t, data)) as FirstSimpleData;
            var data2 = MessagePackSerializer.NonGeneric.Deserialize(t, MessagePackSerializer.NonGeneric.Serialize(t, data, DefaultResolver.Instance)) as FirstSimpleData;

            MessagePackSerializer.NonGeneric.Serialize(t, ms, data);
            ms.Position = 0;
            var data3 = MessagePackSerializer.NonGeneric.Deserialize(t, ms) as FirstSimpleData;

            ms = new MemoryStream();
            MessagePackSerializer.NonGeneric.Serialize(t, ms, data, DefaultResolver.Instance);
            ms.Position = 0;
            var data4 = MessagePackSerializer.NonGeneric.Deserialize(t, ms, DefaultResolver.Instance) as FirstSimpleData;

            new[] { data1.Prop1, data2.Prop1, data3.Prop1, data4.Prop1 }.Distinct().Is(data.Prop1);
            new[] { data1.Prop2, data2.Prop2, data3.Prop2, data4.Prop2 }.Distinct().Is(data.Prop2);
            new[] { data1.Prop3, data2.Prop3, data3.Prop3, data4.Prop3 }.Distinct().Is(data.Prop3);
        }
    }
}

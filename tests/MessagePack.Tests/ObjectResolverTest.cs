using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class ObjectResolverTest
    {
        T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }


        [Fact]
        public void Hoge()
        {
            var o = new SimpleIntKeyData()
            {
                Prop1 = 100,
                Prop2 = ByteEnum.C,
                Prop3 = "abcde",
                Prop4 = new SimlpeStringKeyData
                {
                    Prop1 = 99999,
                    Prop2 = ByteEnum.E,
                    Prop3 = 3
                },
                Prop5 = new SimpleStructIntKeyData
                {
                    X = 100,
                    Y = 300,
                    BytesSpecial = new byte[] { 9, 99, 122 }
                },
                Prop6 = new SimpleStructStringKeyData
                {
                    X = 9999,
                    Y = new[] { 1, 10, 100 }
                },
                 BytesSpecial = new byte[] { 1, 4, 6 }
            };

            var ddd = MessagePackSerializer.Serialize(default(SimpleStructStringKeyData));
            var hoge = MessagePackSerializer.Deserialize< SimpleStructStringKeyData>(ddd);
            var nano = MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(o)); ;

            //Convert(o).IsStructuralEqual(o);

            //var huga = MessagePackSerializer.ToJson(MessagePackSerializer.Serialize(o));

        }
    }
}

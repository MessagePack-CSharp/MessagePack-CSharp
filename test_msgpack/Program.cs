using System;
using MessagePack;
using System.Collections.Generic;

namespace TestMessagePack
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var a = 123123;
            var p = MessagePackSerializer.Serialize(a.GetType(), a);

            var c = (1, 2, 3, "123123");
            var p2 = MessagePackSerializer.Serialize(c.GetType(), c);

            foreach (var i in p2)
            {
                Console.Write($"{i},");
            }

            var d = new Foo
            {
                A = 1,
                B = "33333",
            };

            var p3 = MessagePackSerializer.Serialize(d.GetType(), d);

            var c1 = new byte[] { 166, 49, 50, 51, 49, 50, 51 };
            var x1 = MessagePackSerializer.Deserialize(typeof(string), c1);

            var t1 = new List<string>();
            t1.Add("1111111");
            t1.Add("2222222222");
            var t2 = MessagePackSerializer.Serialize(typeof(List<string>), t1);

            t1 = MessagePackSerializer.Deserialize(typeof(List<string>), t2) as List<string>;

            var g1 = new List<Guid>();
            g1.Add(Guid.NewGuid());
            g1.Add(Guid.NewGuid());
            var g2 = MessagePackSerializer.Serialize(typeof(List<Guid>), g1);
            g1 = MessagePackSerializer.Deserialize(typeof(List<Guid>), g2) as List<Guid>;


        }
    }
}

using System;
using MessagePack;

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

        }
    }
}

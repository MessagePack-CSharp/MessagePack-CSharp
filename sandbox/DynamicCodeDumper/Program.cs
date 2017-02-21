using MessagePack.Resolvers;
using SharedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCodeDumper
{
    public class Program
    {
        static void Main(string[] args)
        {
            DynamicObjectResolver.Instance.GetFormatter<FirstSimpleData>();
            DynamicObjectResolver.Instance.GetFormatter<Version0>();
            DynamicObjectResolver.Instance.GetFormatter<Version1>();
            DynamicObjectResolver.Instance.GetFormatter<Version2>();
            DynamicObjectResolver.Instance.GetFormatter<Vector2>();
            // DynamicObjectResolver.Instance.GetFormatter<Vector2_String>();
            DynamicObjectResolver.Instance.GetFormatter<Callback1>();
            DynamicObjectResolver.Instance.GetFormatter<Callback1_2>();
            DynamicObjectResolver.Instance.GetFormatter<Callback2>();
            DynamicObjectResolver.Instance.GetFormatter<Callback2_2>();

            DynamicObjectResolver.Instance.Save();
            Console.WriteLine("Saved");
        }
    }
}

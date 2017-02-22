using MessagePack;
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
            var f1 = DynamicObjectResolver.Instance.GetFormatter<Callback1_2>();
            DynamicObjectResolver.Instance.GetFormatter<Callback2>();
            DynamicObjectResolver.Instance.GetFormatter<Callback2_2>();

            DynamicUnionResolver.Instance.GetFormatter<IHogeMoge>();
            var f = DynamicUnionResolver.Instance.GetFormatter<IUnionChecker>();
            DynamicUnionResolver.Instance.GetFormatter<IUnionChecker2>();
            

            DynamicObjectResolver.Instance.Save();
            DynamicUnionResolver.Instance.Save();
            Console.WriteLine("Saved");

            var mii =f.GetType().GetMethods();

            byte[] xs = null;
            var huga = f.Serialize(ref xs, 0,new MySubUnion1(), DynamicUnionResolver.Instance);
            Console.WriteLine(huga);
        }
    }

    [Union(0, typeof(HogeMoge1))]
    [Union(1, typeof(HogeMoge2))]
    public interface IHogeMoge
    {
    }

    public class HogeMoge1 : IHogeMoge
    {
    }

    public class HogeMoge2: IHogeMoge
    {
    }

}

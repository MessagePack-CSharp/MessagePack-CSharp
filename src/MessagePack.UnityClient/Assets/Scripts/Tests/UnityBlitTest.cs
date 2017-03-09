using MessagePack.Formatters;
using MessagePack.Resolvers;
using MessagePack.Unity.Extension;
using RuntimeUnitTestToolkit;
using System.Linq;
using UnityEngine;

namespace MessagePack.UnityClient.Tests
{
    public class WithUnityBlitResolver : IFormatterResolver
    {
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return (UnityBlitWithPrimitiveArrayResolver.Instance.GetFormatter<T>()
                 ?? StandardResolver.Instance.GetFormatter<T>());
        }
    }

    public class UnityBlitTest
    {
        T Convert<T>(T value)
        {
            var resolver = new WithUnityBlitResolver();
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, resolver), resolver);
        }


        public void Blit()
        {
            var o = Enumerable.Range(1, 123).Select(x => new Vector3(x, x, x)).ToArray();
            var o2 = Convert(o);
            for (int i = 0; i < o.Length; i++)
            {
                o[i].x.Is(o2[i].x);
                o[i].y.Is(o2[i].y);
                o[i].z.Is(o2[i].z);
            }
        }
    }
}
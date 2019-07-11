using MessagePack.Formatters;
using MessagePack.Resolvers;
using MessagePack.Unity;
using MessagePack.Unity.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Xunit;

namespace MessagePack.Tests.ExtensionTests
{
    public class WithUnityBlitResolver : IFormatterResolver
    {
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return (UnityBlitWithPrimitiveArrayResolver.Instance.GetFormatter<T>()
                 ?? StandardResolver.Instance.GetFormatter<T>());
        }
    }

    public class UnityShimTest
    {
        T ConvertBlit<T>(T value)
        {
            var options = MessagePackSerializerOptions.Default.WithResolver(new WithUnityBlitResolver());
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, options), options);
        }

        T ConvertStandard<T>(T value)
        {
            var options = MessagePackSerializerOptions.Default.WithResolver(UnityResolver.Instance);
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value, options), options);
        }

        public static object[][] testData = new object[][]
        {
            new object[]{ BlitContainer<Vector2>.Create(Enumerable.Range(1, 123).Select(x => new Vector2(x, x)))},
            new object[]{ BlitContainer<Vector3>.Create(Enumerable.Range(1, 123).Select(x => new Vector3(x,x, x)))},
            new object[]{ BlitContainer<Vector4>.Create(Enumerable.Range(1, 123).Select(x => new Vector4(x,x,x, x)))},
            new object[]{ BlitContainer<Quaternion>.Create(Enumerable.Range(1, 123).Select(x => new Quaternion(x,x,x, x)))},
            new object[]{ BlitContainer<Color>.Create(Enumerable.Range(1, 123).Select(x => new Color(x,x,x, x)))},
            new object[]{ BlitContainer<Bounds>.Create(Enumerable.Range(1, 123).Select(x => new Bounds(new Vector3(x,x,x), new Vector3(x,x,x))))},
            new object[]{ BlitContainer<Rect>.Create(Enumerable.Range(1, 123).Select(x => new Rect(x,x,x, x)))},
            new object[]{ BlitContainer<int>.Create(Enumerable.Range(1, 123).Select(x => x))},
            new object[]{ BlitContainer<float>.Create(Enumerable.Range(1, 123).Select(x => (float)x))},
            new object[]{ BlitContainer<double>.Create(Enumerable.Range(1, 123).Select(x => (double)x))},
        };

        [Theory]
        [MemberData(nameof(testData))]
        public void BlitTest<T>(BlitContainer<T> blit)
        {
            var huga = ConvertBlit(blit.Array);
            huga.IsStructuralEqual(blit.Array);

            ConvertBlit(blit).IsStructuralEqual(blit);
            blit.Array = null;
            ConvertBlit(blit).Array.IsNull();
        }

        public static object[][] testStandardData = new object[][]
        {
            new object[]{ new Vector2(10,20) },
            new object[]{ new Vector3(10,20, 30) },
            new object[]{ new Vector4(10,20,30, 40) },
            new object[]{ new Quaternion(10,20,30, 40) },
            new object[]{ new Color(0.3f,32.1f,32.9f,324.32f) },
            new object[]{ new Bounds(new Vector3(1,2,3), new Vector3(4,5,6)) },
            new object[]{ new Rect(1,2,3, 4) },
        };

        [Theory]
        [MemberData(nameof(testStandardData))]
        public void StandardFormatterTest<T>(T data)
        {
            ConvertStandard(data).IsStructuralEqual(data);
        }
    }

    [MessagePackObject]
    public class BlitContainer<T>
    {
        [Key(0)]
        public string Dummy1 { get; set; }
        [Key(1)]
        public T[] Array { get; set; }
        [Key(2)]
        public string Dummy2 { get; set; }

        public static BlitContainer<T> Create(IEnumerable<T> array)
        {
            return new BlitContainer<T>()
            {
                Array = array.ToArray(),
                Dummy1 = Guid.NewGuid().ToString(),
                Dummy2 = Guid.NewGuid().ToString()
            };
        }
    }
}

using MessagePack;
using MessagePack.Formatters;
using SharedData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Tests
{
    public static class Loader
    {
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Register()
        {
            // adhoc resolver registration to running test.

            var resolver = MessagePack.Resolvers.CompositeResolver.Create(new IMessagePackFormatter[]
            {
                new GenericEnumFormatter<UShortEnum>(),
                new GenericEnumFormatter<IntEnum>(),
                new NullableFormatter<Vector2>()
            },
            new[]{
                MessagePack.Resolvers.GeneratedResolver.Instance,
                MessagePack.Resolvers.StandardResolver.Instance
            });

            MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        }
    }
}

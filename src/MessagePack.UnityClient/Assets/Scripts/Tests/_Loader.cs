using RuntimeUnitTestToolkit;
using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Text;

namespace MessagePack.UnityClient.Tests
{
    public static class UnitTestLoader
    {
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Register()
        {
#if !UNITY_METRO

            // Register Tests
            UnitTest.RegisterAllMethods<SimpleTest>();
            UnitTest.RegisterAllMethods<FormatterTest>();
            UnitTest.RegisterAllMethods<UnionTest>();
            UnitTest.RegisterAllMethods<ObjectResolverTest>();
            UnitTest.RegisterAllMethods<MultiDimensionalArrayTest>();
            UnitTest.RegisterAllMethods<CollectionFormatterTest>();
            UnitTest.RegisterAllMethods<Contractless>();
#if ENABLE_UNSAFE_MSGPACK
            UnitTest.RegisterAllMethods<UnityBlitTest>();
#endif

            UnitTest.RegisterAllMethods<LZ4Test>();

            UnitTest.RegisterAllMethods<PerformanceTest>();

#endif
        }
    }
}
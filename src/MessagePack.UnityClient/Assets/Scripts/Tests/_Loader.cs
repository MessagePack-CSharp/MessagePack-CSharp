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
        // [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Register()
        {
            // Register Tests
            UnitTest.RegisterAllMethods<SimpleTest>();
            UnitTest.RegisterAllMethods<FormatterTest>();
            UnitTest.RegisterAllMethods<UnionTest>();
            UnitTest.RegisterAllMethods<ObjectResolverTest>();
            UnitTest.RegisterAllMethods<MultiDimentionalArrayTest>();
            UnitTest.RegisterAllMethods<CollectionFormatterTest>();
            UnitTest.RegisterAllMethods<UnityBlitTest>();


            UnitTest.RegisterAllMethods<PerformanceTest>();
        }
    }
}
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
            // Register Tests
            UnitTest.RegisterAllMethods<SimpleTest>();
        }
    }
}
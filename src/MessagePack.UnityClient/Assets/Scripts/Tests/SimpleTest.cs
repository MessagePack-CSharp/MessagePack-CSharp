using UnityEngine;
using RuntimeUnitTestToolkit;
using System.Collections;

using System.Collections.Generic;
using System;

namespace MessagePack.UnityClient.Tests
{
    public class SimpleTest
    {
        public void Hello()
        {
            "OK".Is("OK");
        }
    }
}
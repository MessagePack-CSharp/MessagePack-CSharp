using UnityEngine;
using RuntimeUnitTestToolkit;
using System.Collections;

using System.Collections.Generic;
using System;
using MessagePack.Resolvers;
using MessagePack.Internal;
using MessagePack.Formatters;
using SharedData;
using System.Diagnostics;

namespace MessagePack.UnityClient.Tests
{
    [MessagePackObject]
    [System.Serializable]
    public class TestObject
    {
        [MessagePackObject]
        [System.Serializable]
        public class PrimitiveObject
        {
            [Key(0)]
            public int v_int;

            [Key(1)]
            public string v_str;

            [Key(2)]
            public float v_float;

            [Key(3)]
            public bool v_bool;
            public PrimitiveObject(int vi, string vs, float vf, bool vb)
            {
                v_int = vi; v_str = vs; v_float = vf; v_bool = vb;
            }
        }

        [Key(0)]
        public PrimitiveObject[] objectArray;

        [Key(1)]
        public List<PrimitiveObject> objectList;

        [Key(2)]
        public Dictionary<string, PrimitiveObject> objectMap;

        public void CreateArray(int num)
        {
            objectArray = new PrimitiveObject[num];
            for (int i = 0; i < num; i++)
            {
                objectArray[i] = new PrimitiveObject(i, i.ToString(), (float)i, i % 2 == 0 ? true : false);
            }
        }

        public void CreateList(int num)
        {
            objectList = new List<PrimitiveObject>(num);
            for (int i = 0; i < num; i++)
            {
                objectList.Add(new PrimitiveObject(i, i.ToString(), (float)i, i % 2 == 0 ? true : false));
            }
        }

        public void CreateMap(int num)
        {
            objectMap = new Dictionary<string, PrimitiveObject>(num);
            for (int i = 0; i < num; i++)
            {
                objectMap.Add(i.ToString(), new PrimitiveObject(i, i.ToString(), (float)i, i % 2 == 0 ? true : false));
            }
        }
        // I only tested with array
        public static TestObject TestBuild()
        {
            TestObject to = new TestObject();
            //to.CreateArray(1000000);
            to.CreateArray(1);

            return to;
        }
    }


    public class SimpleTest
    {
        public void Hello()
        {
            try
            {
                TestObject to = TestObject.TestBuild();

                Stopwatch sw = new Stopwatch();
                sw.Start();
                string junity = JsonUtility.ToJson(to);
                sw.Stop();
                UnityEngine.Debug.LogFormat("*[Object To JsonString] - Unity :  {0}ms.", sw.ElapsedMilliseconds);

                Stopwatch sw1 = new Stopwatch();
                sw1.Start();
                string jmsgPack = MessagePack.MessagePackSerializer.ToJson<TestObject>(to);
                sw1.Stop();
                UnityEngine.Debug.LogFormat("*[Object To JsonString] - MsgPack :  {0}ms.", sw1.ElapsedMilliseconds);

                Stopwatch sw3 = new Stopwatch();
                sw3.Start();
                TestObject toUnity = JsonUtility.FromJson<TestObject>(junity);
                sw3.Stop();
                UnityEngine.Debug.LogFormat("*[JsonString To Object] - Unity :  {0}ms.", sw3.ElapsedMilliseconds);

                Stopwatch sw4 = new Stopwatch();
                sw4.Start();
                TestObject toMsgPack = MessagePack.MessagePackSerializer.Deserialize<TestObject>(MessagePack.MessagePackSerializer.FromJson(jmsgPack));
                sw4.Stop();
                UnityEngine.Debug.LogFormat("*[JsonString To Object] - MsgPack :  {0}ms.", sw4.ElapsedMilliseconds);




            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                throw;
            }
        }
    }

    [MessagePackObject]
    public class DynamicTestCheck
    {
        [Key(0)]
        public int A { get; set; }
        [Key(1)]
        public string B { get; set; }
    }
}
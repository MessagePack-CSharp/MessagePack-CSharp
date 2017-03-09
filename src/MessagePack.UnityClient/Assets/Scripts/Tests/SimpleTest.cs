using UnityEngine;
using RuntimeUnitTestToolkit;
using System.Collections;

using System.Collections.Generic;
using System;
using MessagePack.Resolvers;
using MessagePack.Internal;
using MessagePack.Formatters;
using SharedData;

namespace MessagePack.UnityClient.Tests
{
    public class SimpleTest
    {
        public void Hello()
        {
            try
            {
                //var bytes = MessagePackSerializer.Serialize(new DynamicTestCheck { A = 100, B = "huga" });
                //MessagePackSerializer.ToJson(bytes).Is("[100,\"huga\"]");

                //MessagePackSerializer.Deserialize<DynamicTestCheck>(bytes).A.Is(100);
                //MessagePackSerializer.Deserialize<DynamicTestCheck>(bytes).B.Is("huga");



                //var nullable = DynamicGenericResolver.Instance.GetFormatter<Vector2?>();

                //var f = DynamicGenericResolverGetFormatterHelper.GetFormatter(typeof(Vector2?));

                var f = DynamicUnionResolver.Instance.GetFormatter<IUnionChecker>();

                byte[] b = null;
                f.Serialize(ref b, 0, null, StandardResolver.Instance);

                //var data = new MySubUnion1 { One = 23 };
                //var data2 = new MySubUnion1 { One = 23 };

                //var unionData1 = MessagePackSerializer.Serialize<IUnionChecker>(data);
                //var unionData2 = MessagePackSerializer.Serialize<IUnionChecker2>(data2);

                //var reData1 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1);
                //var reData2 = MessagePackSerializer.Deserialize<IUnionChecker>(unionData1);

                //reData1.IsInstanceOf<IUnionChecker>();
                //reData2.IsInstanceOf<IUnionChecker2>();

                //var null1 = MessagePackSerializer.Serialize<IUnionChecker>(null);



            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
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
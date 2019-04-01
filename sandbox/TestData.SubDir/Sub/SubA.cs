using System;
using MessagePack;

namespace TestData.SubDir.Sub
{

    [MessagePackObject(true)]
    public class SubA
    {
        public int Y { get; set; }
    }
}

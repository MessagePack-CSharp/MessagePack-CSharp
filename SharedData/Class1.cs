using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharedData
{
    [MessagePackObject]
    public class FirstSimpleData
    {
        [Key(0)]
        public int Prop1 { get; set; }
        [Key(1)]
        public string Prop2 { get; set; }
        [Key(2)]
        public int Prop3 { get; set; }
    }


    [MessagePackObject]
    public class Version1
    {
        [Key(340)]
        public int MyProperty1 { get; set; }
        [Key(101)]
        public int MyProperty2 { get; set; }
        [Key(252)]
        public int MyProperty3 { get; set; }
    }


    [MessagePackObject]
    public class Version2
    {
        [Key(340)]
        public int MyProperty1 { get; set; }
        [Key(101)]
        public int MyProperty2 { get; set; }
        [Key(252)]
        public int MyProperty3 { get; set; }
        [Key(3009)]
        public int MyProperty4 { get; set; }
        [Key(201)]
        public int MyProperty5 { get; set; }
    }


    [MessagePackObject]
    public class Version0
    {
        [Key(340)]
        public int MyProperty1 { get; set; }
    }

    [MessagePackObject]
    public class HolderV1
    {
        [Key(0)]
        public Version1 MyProperty1 { get; set; }
        [Key(1)]
        public int After { get; set; }
    }

    [MessagePackObject]
    public class HolderV2
    {
        [Key(0)]
        public Version2 MyProperty1 { get; set; }
        [Key(1)]
        public int After { get; set; }
    }

    [MessagePackObject]
    public class HolderV0
    {
        [Key(0)]
        public Version0 MyProperty1 { get; set; }
        [Key(1)]
        public int After { get; set; }
    }
}
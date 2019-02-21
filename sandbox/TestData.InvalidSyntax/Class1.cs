using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestData.InvalidSyntax
{
    // delibrated syntax error
    abcde
    [MessagePackObject(true)]
    public class A { public int a; public List<B> bs; public C c; }

    [MessagePackObject(true)]
    public class B { public List<A> ass; public C c; public int a; }

    [MessagePackObject(true)]
    public class C { public B b; public int a; }


    [MessagePackObject(true)]
    public class PropNameCheck1
    {
        public string MyProperty1 { get; set; }
        public virtual string MyProperty2 { get; set; }
    }

    [MessagePackObject(true)]
    public class PropNameCheck2 : PropNameCheck1
    {
        public override string MyProperty2
        {
            get => base.MyProperty2;
             set => base.MyProperty2 = value; }
    }
}

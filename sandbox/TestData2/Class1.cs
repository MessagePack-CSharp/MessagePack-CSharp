using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestData2
{
    [MessagePackObject(true)]
    public class A { public int a; public List<B> bs; public C c; }

    [MessagePackObject(true)]
    public class B { public List<A> ass; public C c; public int a; }

    [MessagePackObject(true)]
    public class C { public B b; public int a; }
}

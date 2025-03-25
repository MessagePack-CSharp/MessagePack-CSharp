// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MessagePack;

Console.WriteLine("foo");

//[MessagePackObject]
//public class Foo<T>
//{
//    [Key(0)]
//    public T Member { get; set; }
//}

//[MessagePackObject]
//public class Bar
//{
//    [Key(0)]
//    public Foo<int> MemberUserGeneric { get; set; }

//    [Key(1)]
//    public System.Collections.Generic.List<int> MemberKnownGeneric { get; set; }
//}


public class ClassA<T> where T : ClassA<T>.ClassB
{
    public class ClassB
    {
    }
}

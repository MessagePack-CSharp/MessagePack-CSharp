using System;
using System.Collections.Generic;
using System.Text;

namespace MessagePack.Formatters
{
    // TODO:not yet implemented
    class ObjectFormatter
    {
        // getMap
        // getLength
        // for(...){

        // var type = TryReadType();
        // if(type.IsInteger() -> ReadInt -> switch() case... field = getValue...;
        // else if(type == string) -> ReadString -> switch() case... field = getValue...;

        // constructor matching
        // new()... set...
        // return...
    }

    // [SerializationConstructor]

    //public class MetaObject
    //{
    //    public bool IsIntKey { get; set; }
    //    public bool IsStringKey { get { return !IsIntKey; } }
    //    public bool IsClass { get; set; }
    //    public bool IsStruct { get { return !IsClass; } }
    //    public ConstructorInfo BestmatchConstructor { get; set; }

    //    public class Member
    //    {
    //        public bool IsProperty { get; set; }
    //        public bool IsField { get; set; }
    //    }
    //}
}

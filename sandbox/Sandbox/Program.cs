// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1009, SA1111, SA1649, SA1402, SA1401, SA1307

using System;
using System.Collections.Generic;
using Issue2101;
using MessagePack;
using MessagePack.Resolvers;

Dictionary<string, IPrimaryKeyItem> itemList = new Dictionary<string, IPrimaryKeyItem>();

//itemList.Add("1", new PrimaryKeyItem<int>(1));
//itemList.Add("2", new PrimaryKeyItem<string>("2"));

var json = MessagePackSerializer.SerializeToJson(itemList, GetResolver());

Console.WriteLine(json);

MessagePackSerializerOptions GetResolver()
{
    var resolver = CompositeResolver.Create(
        NativeDecimalResolver.Instance,
        NativeGuidResolver.Instance,
        NativeDateTimeResolver.Instance,
        TypelessObjectResolver.Instance,
        // StandardResolver.Instance

        BuiltinResolver.Instance,
        AttributeFormatterResolver.Instance
    // SourceGeneratedFormatterResolver.Instance
    );

    return MessagePackSerializerOptions.Standard.WithResolver(resolver).WithOmitAssemblyVersion(true);
}

Console.WriteLine("foo");

namespace Issue2101
{
    [MessagePackObject(AllowPrivate = true, SuppressSourceGeneration = true)]
    public partial class PrimaryKey
    {
        [Key(0)]
        private Dictionary<string, IPrimaryKeyItem> itemList = new Dictionary<string, IPrimaryKeyItem>();
    }

    public interface IPrimaryKeyItem
    {
        object ObjectValue
        {
            get; // set;
        }
    }

    // [MessagePackObject(AllowPrivate = true, SuppressSourceGeneration = true)]
    //public abstract class PrimaryKeyItemBase
    //{
    //    internal abstract void SetValue(object value);
    //}

    //[MessagePackObject(AllowPrivate = true, SuppressSourceGeneration = true)]
    //public class PrimaryKeyItem<TType> : PrimaryKeyItemBase, IPrimaryKeyItem
    //{
    //    [Key(0)]
    //    private TType value;

    //    [SerializationConstructor]
    //    private PrimaryKeyItem()
    //    {
    //    }

    //    public PrimaryKeyItem(TType value)
    //    {
    //        if (value == null)
    //            throw new ArgumentNullException("value");

    //        this.value = value;
    //    }

    //    [IgnoreMember]
    //    public object ObjectValue
    //    {
    //        get
    //        {
    //            return value;
    //        }
    //    }

    //    internal override void SetValue(object value)
    //    {
    //        this.value = (TType)value;
    //    }
    //}




}

using System.Collections;
using System.Collections.ObjectModel;
using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// KeyAttribute is not sealed: a derived attribute that passes its key through the constructor keys the member in the
// generated formatter exactly as the reflection tier (which reads the key off the instance) does
public sealed class WireKeyAttribute : KeyAttribute
{
    public WireKeyAttribute(string key)
        : base(key)
    {
    }
}

[MessagePackObject(true)]
public class WireKeyModel
{
    [WireKey("wire_name")] public int Value { get; set; }
}

[MessagePackObject(true, SuppressSourceGeneration = true)]
public class WireKeyReflectionModel
{
    [WireKey("wire_name")] public int Value { get; set; }
}

public class CountingCollection<T> : Collection<T>
{
    public int Inserted { get; private set; }

    protected override void InsertItem(int index, T item)
    {
        Inserted++;
        base.InsertItem(index, item);
    }
}

public class StringBag : Dictionary<string, int>
{
}

public class ReviewRound13RegressionTests
{
    [Fact]
    public void DerivedKeyAttribute_KeysTheMemberInBothTiers()
    {
        var generated = V4.Serialize(new WireKeyModel { Value = 1 });
        Assert.Equal(V4.Serialize(new WireKeyReflectionModel { Value = 1 }), generated);
        Assert.Contains("wire_name", System.Text.Encoding.UTF8.GetString(generated));
        Assert.Equal(1, V4.Deserialize<WireKeyModel>(generated)!.Value);
    }

    // the construct-by-Add formatters (a user collection with a default constructor, a user dictionary, the non-generic
    // IList / IDictionary views) refill a mutable instance in place, as every other formatter does
    [Fact]
    public void AddBasedFormatters_PopulateTheExistingInstance()
    {
        var collection = new CountingCollection<int> { 1 };
        var sameCollection = collection;
        V4.Deserialize([0x92, 0x05, 0x06], ref collection);
        Assert.Same(sameCollection, collection);
        Assert.Equal([5, 6], collection);
        Assert.Equal(3, collection.Inserted); // the derived collection saw the inserts

        var bag = new StringBag { ["old"] = 1 };
        var sameBag = bag;
        V4.Deserialize(V4.Serialize(new StringBag { ["k"] = 2 }), ref bag);
        Assert.Same(sameBag, bag);
        Assert.Equal(new[] { "k" }, bag.Keys);

        var arrayList = new ArrayList { "old" };
        var sameArrayList = arrayList;
        V4.Deserialize([0x91, 0x01], ref arrayList);
        Assert.Same(sameArrayList, arrayList);
        Assert.Equal(1, Convert.ToInt32(Assert.Single(arrayList))); // (the object formatter reads a fixint as its narrowest type)

        var hashtable = new Hashtable { ["old"] = 1 };
        var sameHashtable = hashtable;
        V4.Deserialize([0x81, 0xA1, (byte)'k', 0x02], ref hashtable);
        Assert.Same(sameHashtable, hashtable);
        Assert.Equal(2, Convert.ToInt32(hashtable["k"]));
        Assert.False(hashtable.ContainsKey("old"));

        // the non-generic interface views too: a mutable instance behind IList / IDictionary stays (an ArrayList keeps
        // accepting Add afterwards, a Hashtable keeps its comparer), a fixed-size one is replaced
        IList viewList = new ArrayList { "old" };
        var sameViewList = viewList;
        V4.Deserialize([0x91, 0x01], ref viewList);
        Assert.Same(sameViewList, viewList);
        Assert.Equal(1, Convert.ToInt32(Assert.Single(viewList.Cast<object>())));
        viewList.Add("more");
        IList fixedView = new object[] { "x" };
        V4.Deserialize([0x91, 0x01], ref fixedView);
        Assert.IsType<object?[]>(fixedView);
        IDictionary viewDictionary = new Hashtable(StringComparer.OrdinalIgnoreCase) { ["old"] = 1 };
        var sameViewDictionary = viewDictionary;
        V4.Deserialize([0x81, 0xA1, (byte)'k', 0x02], ref viewDictionary);
        Assert.Same(sameViewDictionary, viewDictionary);
        Assert.Equal(2, Convert.ToInt32(viewDictionary["K"])); // the instance's comparer survived
        Assert.False(viewDictionary.Contains("old"));

        // a read-only instance is replaced, not mutated
        var frozen = new ReadOnlyCollection<int>([1]) as IList<int>;
        var wrapped = new CountingCollection<int>();
        IList<int> fixedSize = new int[] { 9 };
        V4.Deserialize([0x91, 0x07], ref fixedSize);
        Assert.Equal([7], fixedSize);
        Assert.NotNull(frozen);
        Assert.NotNull(wrapped);
    }
}

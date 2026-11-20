using System.Collections;
using System.Collections.ObjectModel;
using MessagePack;

namespace MessagePack.Tests.Trimmed;

public enum TrimColor
{
    Red,
    Green,
}

public class TrimColorBag : Collection<TrimColor>
{
}

// GenericFormatterFactoryBase closes an open factory definition over the requested type and constructs it through
// Activator. The definition arrives as a typeof from GetOpenFactoryType, which keeps the type and not its
// constructors, so a trimmed application failed every one of these with MissingMethodException ("Constructor on type
// 'ListFormatterFactory`1[...]' not found") until the return value carried DynamicallyAccessedMembers.
// Every case involves TrimColor, an enum of this assembly, so no closed built-in table can serve it and it always
// reaches the run-time tier.
public class TrimmedGenericFactoryTests
{
    [Fact]
    public void Enum()
    {
        var bytes = Trimmed.Serialize(TrimColor.Green, Chains.Default);
        Assert.Equal(new byte[] { 0x01 }, bytes);
        Assert.Equal(TrimColor.Green, Trimmed.Deserialize<TrimColor>(bytes, Chains.Default));
    }

    [Fact]
    public void NullableEnum()
    {
        var bytes = Trimmed.Serialize<TrimColor?>(TrimColor.Green, Chains.Default);
        Assert.Equal(new byte[] { 0x01 }, bytes);
        Assert.Equal(TrimColor.Green, Trimmed.Deserialize<TrimColor?>(bytes, Chains.Default));
        Assert.Null(Trimmed.Deserialize<TrimColor?>([0xc0], Chains.Default));
    }

    [Fact]
    public void EnumArray()
    {
        var bytes = Trimmed.Serialize(new[] { TrimColor.Red, TrimColor.Green }, Chains.Default);
        Assert.Equal(new byte[] { 0x92, 0x00, 0x01 }, bytes);
        Assert.Equal([TrimColor.Red, TrimColor.Green], Trimmed.Deserialize<TrimColor[]>(bytes, Chains.Default));
    }

    [Fact]
    public void List()
    {
        var bytes = Trimmed.Serialize(new List<TrimColor> { TrimColor.Red, TrimColor.Green }, Chains.Default);
        Assert.Equal(new byte[] { 0x92, 0x00, 0x01 }, bytes);
        Assert.Equal([TrimColor.Red, TrimColor.Green], Trimmed.Deserialize<List<TrimColor>>(bytes, Chains.Default));
    }

    [Fact]
    public void HashSet()
    {
        var bytes = Trimmed.Serialize(new HashSet<TrimColor> { TrimColor.Green }, Chains.Default);
        Assert.Equal(new byte[] { 0x91, 0x01 }, bytes);
        Assert.Equal([TrimColor.Green], Trimmed.Deserialize<HashSet<TrimColor>>(bytes, Chains.Default));
    }

    [Fact]
    public void Dictionary()
    {
        var bytes = Trimmed.Serialize(new Dictionary<TrimColor, string> { [TrimColor.Green] = "a" }, Chains.Default);
        Assert.Equal(new byte[] { 0x81, 0x01, 0xa1, 0x61 }, bytes);
        Assert.Equal("a", Trimmed.Deserialize<Dictionary<TrimColor, string>>(bytes, Chains.Default)[TrimColor.Green]);
    }

    [Fact]
    public void ValueTuple()
    {
        var bytes = Trimmed.Serialize((TrimColor.Green, "a"), Chains.Default);
        Assert.Equal(new byte[] { 0x92, 0x01, 0xa1, 0x61 }, bytes);
        Assert.Equal((TrimColor.Green, "a"), Trimmed.Deserialize<(TrimColor, string)>(bytes, Chains.Default));
    }

    [Fact]
    public void KeyValuePair()
    {
        var bytes = Trimmed.Serialize(new KeyValuePair<TrimColor, int>(TrimColor.Green, 2), Chains.Default);
        Assert.Equal(new byte[] { 0x92, 0x01, 0x02 }, bytes);
        Assert.Equal(new KeyValuePair<TrimColor, int>(TrimColor.Green, 2), Trimmed.Deserialize<KeyValuePair<TrimColor, int>>(bytes, Chains.Default));
    }

    // the catch-all for a concrete collection shape: a public default constructor and ICollection<T>
    [Fact]
    public void CustomCollection()
    {
        var bytes = Trimmed.Serialize(new TrimColorBag { TrimColor.Red, TrimColor.Green }, Chains.Default);
        Assert.Equal(new byte[] { 0x92, 0x00, 0x01 }, bytes);
        Assert.Equal([TrimColor.Red, TrimColor.Green], Trimmed.Deserialize<TrimColorBag>(bytes, Chains.Default));
    }

    // a factory constructed with arguments: Activator binds EnumAsStringFormatterFactory<T>(bool)
    [Fact]
    public void EnumAsString_FactoryWithConstructorArguments()
    {
        var bytes = Trimmed.Serialize(TrimColor.Green, Chains.EnumAsStringIgnoreCase);
        Assert.Equal(new byte[] { 0xa5, (byte)'G', (byte)'r', (byte)'e', (byte)'e', (byte)'n' }, bytes);
        Assert.Equal(TrimColor.Green, Trimmed.Deserialize<TrimColor>(bytes, Chains.EnumAsStringIgnoreCase));

        // ignoreCase reached the constructor
        byte[] lower = [0xa5, (byte)'g', (byte)'r', (byte)'e', (byte)'e', (byte)'n'];
        Assert.Equal(TrimColor.Green, Trimmed.Deserialize<TrimColor>(lower, Chains.EnumAsStringIgnoreCase));
    }

    // TypelessNonGenericCollectionFormatterFactory, the third GenericFormatterFactoryBase in the library
    [Fact]
    public void Typeless_NonGenericCollection()
    {
        var bytes = Trimmed.Serialize(new ArrayList { 1, "a" }, Chains.Typeless);
        var back = Trimmed.Deserialize<ArrayList>(bytes, Chains.Typeless);
        Assert.Equal(2, back.Count);
        Assert.Equal(1, back[0]);
        Assert.Equal("a", back[1]);
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using MessagePack;

namespace MessagePack.Tests.Trimmed;

[MessagePackObject]
public class TrimIntKeyed
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
}

[MessagePackObject]
public class TrimStringKeyed
{
    [Key("id")] public int Id { get; set; }
}

[MessagePackObject(true)]
public class TrimKeyAsPropertyName
{
    public int Id { get; set; }
}

[MessagePackObject(KeyNamingPolicy.CamelCase)]
public class TrimCamelCase
{
    public int UserId { get; set; }
}

[MessagePackObject(AllowPrivate = true)]
public class TrimAllowPrivate
{
    [Key(0)] int secret;

    public static TrimAllowPrivate Create(int secret) => new() { secret = secret };

    public int Reveal() => secret;
}

[MessagePackObject(EnumAsString = true)]
public class TrimEnumAsString
{
    [Key(0)] public TrimColor Color { get; set; }
}

[MessagePackObject]
public class TrimMemberFormatter
{
    [Key(0)]
    [MessagePackFormatter(typeof(GenericEnumAsStringFormatterFactory), true)]
    public TrimColor Color { get; set; }
}

[DataContract]
public class TrimDataContractOrdered
{
    [DataMember(Order = 0)] public int Id { get; set; }
    [DataMember(Order = 1)] public string? Name { get; set; }
    public int NotAMember { get; set; }
}

[DataContract]
public class TrimDataContractNamed
{
    [DataMember(Name = "n")] public int Id { get; set; }
}

[MessagePackObject]
[UnionTag(typeof(TrimCircle), 0)]
[UnionTag<TrimSquare>(1)]
public interface ITrimShape
{
}

[MessagePackObject]
public class TrimCircle : ITrimShape
{
    [Key(0)] public int Radius { get; set; }
}

[MessagePackObject]
public class TrimSquare : ITrimShape
{
    [Key(0)] public int Side { get; set; }
}

public class TrimPlain
{
    public int Id { get; set; }
}

// The reflection formatters read their attributes by name (GetProperty), the way they serve a v3 annotations
// assembly, so nothing references the attribute getters statically. Applying an attribute keeps its constructor and
// the setters of its named arguments, not its getters. Trimmed, a property removed whole read as "not set" ([Key(0)]
// as no key at all, "needs [Key] or [IgnoreMember]"), and one that survived through its setter alone threw "Property
// Get method was not found". The "not set" reading changes the format without failing, so each test pins the bytes
// and not only the round trip.
public class TrimmedReflectionTierTests
{
    [Fact]
    public void IntKeys()
    {
        var bytes = Trimmed.Serialize(new TrimIntKeyed { Id = 1, Name = "a" }, Chains.Default);
        Assert.Equal(new byte[] { 0x92, 0x01, 0xa1, 0x61 }, bytes);
        var back = Trimmed.Deserialize<TrimIntKeyed>(bytes, Chains.Default);
        Assert.Equal(1, back.Id);
        Assert.Equal("a", back.Name);
    }

    [Fact]
    public void StringKeys()
    {
        var bytes = Trimmed.Serialize(new TrimStringKeyed { Id = 1 }, Chains.Default);
        Assert.Equal(new byte[] { 0x81, 0xa2, (byte)'i', (byte)'d', 0x01 }, bytes);
        Assert.Equal(1, Trimmed.Deserialize<TrimStringKeyed>(bytes, Chains.Default).Id);
    }

    [Fact]
    public void KeyAsPropertyName()
    {
        var bytes = Trimmed.Serialize(new TrimKeyAsPropertyName { Id = 1 }, Chains.Default);
        Assert.Equal(new byte[] { 0x81, 0xa2, (byte)'I', (byte)'d', 0x01 }, bytes);
        Assert.Equal(1, Trimmed.Deserialize<TrimKeyAsPropertyName>(bytes, Chains.Default).Id);
    }

    [Fact]
    public void KeyNamingPolicy_FromTheConstructorArgument()
    {
        var bytes = Trimmed.Serialize(new TrimCamelCase { UserId = 1 }, Chains.Default);
        Assert.Equal(new byte[] { 0x81, 0xa6, (byte)'u', (byte)'s', (byte)'e', (byte)'r', (byte)'I', (byte)'d', 0x01 }, bytes);
        Assert.Equal(1, Trimmed.Deserialize<TrimCamelCase>(bytes, Chains.Default).UserId);
    }

    [Fact]
    public void AllowPrivate_FromTheNamedArgument()
    {
        var bytes = Trimmed.Serialize(TrimAllowPrivate.Create(7), Chains.Default);
        Assert.Equal(new byte[] { 0x91, 0x07 }, bytes);
        Assert.Equal(7, Trimmed.Deserialize<TrimAllowPrivate>(bytes, Chains.Default).Reveal());
    }

    [Fact]
    public void EnumAsString_FromTheNamedArgument()
    {
        var bytes = Trimmed.Serialize(new TrimEnumAsString { Color = TrimColor.Green }, Chains.Default);
        Assert.Equal(new byte[] { 0x91, 0xa5, (byte)'G', (byte)'r', (byte)'e', (byte)'e', (byte)'n' }, bytes);
        Assert.Equal(TrimColor.Green, Trimmed.Deserialize<TrimEnumAsString>(bytes, Chains.Default).Color);
    }

    // FactoryType and Arguments of a member-level [MessagePackFormatter]. The factory's own constructor is the
    // application's to keep (the attribute's Type parameter carries no annotation), hence the DynamicDependency.
    // The factory is the non-generic one on purpose: naming EnumAsStringFormatterFactory<T> here would keep the very
    // constructor TrimmedGenericFactoryTests expects the library to keep.
    [Fact]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(GenericEnumAsStringFormatterFactory))]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "the run-time tiers under trimming are the subject of this project")]
    public void MemberLevelFormatterAttribute()
    {
        var bytes = Trimmed.Serialize(new TrimMemberFormatter { Color = TrimColor.Green }, Chains.Default);
        Assert.Equal(new byte[] { 0x91, 0xa5, (byte)'G', (byte)'r', (byte)'e', (byte)'e', (byte)'n' }, bytes);

        // the argument (ignoreCase: true) reached the factory
        byte[] lower = [0x91, 0xa5, (byte)'g', (byte)'r', (byte)'e', (byte)'e', (byte)'n'];
        Assert.Equal(TrimColor.Green, Trimmed.Deserialize<TrimMemberFormatter>(lower, Chains.Default).Color);
    }

    // a trimmed-away Order getter reads as -1, which keys the member by its name: a map instead of this array
    [Fact]
    public void DataContract_OrderIsTheIntKey()
    {
        var bytes = Trimmed.Serialize(new TrimDataContractOrdered { Id = 1, Name = "a", NotAMember = 9 }, Chains.Default);
        Assert.Equal(new byte[] { 0x92, 0x01, 0xa1, 0x61 }, bytes);
        var back = Trimmed.Deserialize<TrimDataContractOrdered>(bytes, Chains.Default);
        Assert.Equal(1, back.Id);
        Assert.Equal("a", back.Name);
        Assert.Equal(0, back.NotAMember);
    }

    [Fact]
    public void DataContract_NameIsTheStringKey()
    {
        var bytes = Trimmed.Serialize(new TrimDataContractNamed { Id = 1 }, Chains.Default);
        Assert.Equal(new byte[] { 0x81, 0xa1, (byte)'n', 0x01 }, bytes);
        Assert.Equal(1, Trimmed.Deserialize<TrimDataContractNamed>(bytes, Chains.Default).Id);
    }

    // ReflectionUnionFormatterFactory<T>, constructed without arguments, and [UnionTag]'s Tag and CaseType
    [Fact]
    public void Union()
    {
        // the case types are named only by the attributes, so their members are kept here
        Trimmed.Serialize(new TrimCircle(), Chains.Default);
        Trimmed.Serialize(new TrimSquare(), Chains.Default);

        var bytes = Trimmed.Serialize<ITrimShape>(new TrimSquare { Side = 5 }, Chains.Default);
        Assert.Equal(new byte[] { 0x92, 0x01, 0x91, 0x05 }, bytes);
        var back = Assert.IsType<TrimSquare>(Trimmed.Deserialize<ITrimShape>(bytes, Chains.Default));
        Assert.Equal(5, back.Side);
    }

    // ReflectionObjectFormatterFactory<T>(bool allowPrivate) for a type without attributes
    [Fact]
    public void Contractless()
    {
        var bytes = Trimmed.Serialize(new TrimPlain { Id = 1 }, Chains.Contractless);
        Assert.Equal(new byte[] { 0x81, 0xa2, (byte)'I', (byte)'d', 0x01 }, bytes);
        Assert.Equal(1, Trimmed.Deserialize<TrimPlain>(bytes, Chains.Contractless).Id);
    }
}

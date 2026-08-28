extern alias V3;
namespace MessagePack.Tests;

public class ContractlessFormatterTests
{
    static readonly MessagePackSerializerOptions options = new MessagePackSerializerOptions(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless()]));

    public class Person
    {
        public int Age { get; set; }
        public string? Name { get; set; }
        public List<int>? Scores { get; set; }
        public Person? Partner { get; set; }
    }

    [Fact]
    public void ClassRoundTrip()
    {
        var person = new Person
        {
            Age = 30,
            Name = "foo",
            Scores = [1, 2, 3],
            Partner = new Person { Age = 28, Name = "bar" },
        };

        var result = MessagePackSerializer.Deserialize<Person>(MessagePackSerializer.Serialize(person, options), options)!;

        Assert.Equal(30, result.Age);
        Assert.Equal("foo", result.Name);
        Assert.Equal([1, 2, 3], result.Scores);
        Assert.Equal(28, result.Partner!.Age);
        Assert.Equal("bar", result.Partner.Name);
        Assert.Null(result.Partner.Partner);
    }

    [Fact]
    public void WireFormat_IsMapOfMemberNames()
    {
        // the payload must be readable as an ordinary string-keyed map: that IS the
        // v3-contractless wire contract
        var payload = MessagePackSerializer.Serialize(new Person { Age = 42, Name = "x" }, options);
        var map = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(payload, options)!;

        Assert.Equal(4, map.Count);
        Assert.Equal(42, Convert.ToInt32(map["Age"]));
        Assert.Equal("x", map["Name"]);
        Assert.Null(map["Scores"]);
        Assert.Null(map["Partner"]);
    }

    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    [Fact]
    public void StructRoundTrip()
    {
        var result = MessagePackSerializer.Deserialize<Point>(MessagePackSerializer.Serialize(new Point { X = 3, Y = -4 }, options), options);
        Assert.Equal(3, result.X);
        Assert.Equal(-4, result.Y);
    }

    public class FieldHolder
    {
        public int Count;
        public string? Label;
        public readonly int Sealed = 99; // serialized, but skipped on read
    }

    [Fact]
    public void PublicFields_RoundTrip()
    {
        var payload = MessagePackSerializer.Serialize(new FieldHolder { Count = 7, Label = "L" }, options);
        var result = MessagePackSerializer.Deserialize<FieldHolder>(payload, options)!;

        Assert.Equal(7, result.Count);
        Assert.Equal("L", result.Label);
        Assert.Equal(99, result.Sealed);
    }

    public struct FieldPoint
    {
        public int X;
        public int Y;
    }

    [Fact]
    public void StructFields_RoundTrip()
    {
        // exercises the emitted ldfld/stfld accessors over a byref struct receiver
        var result = MessagePackSerializer.Deserialize<FieldPoint>(
            MessagePackSerializer.Serialize(new FieldPoint { X = 11, Y = -12 }, options), options);
        Assert.Equal(11, result.X);
        Assert.Equal(-12, result.Y);
    }

    public class ReadOnlyMember
    {
        public int Id { get; set; }
        public int Doubled => Id * 2; // getter-only: written to the payload, skipped on read
    }

    [Fact]
    public void GetterOnlyProperty_WrittenButNotRead()
    {
        var payload = MessagePackSerializer.Serialize(new ReadOnlyMember { Id = 5 }, options);

        var map = MessagePackSerializer.Deserialize<Dictionary<string, int>>(payload, options)!;
        Assert.Equal(5, map["Id"]);
        Assert.Equal(10, map["Doubled"]);

        var result = MessagePackSerializer.Deserialize<ReadOnlyMember>(payload, options)!;
        Assert.Equal(5, result.Id);
    }

    [Fact]
    public void UnknownKeys_AreSkipped()
    {
        // extra keys of every shape (str, array, nested map) must be skipped, not break
        var payload = MessagePackSerializer.Serialize(new Dictionary<string, object?>
        {
            ["Age"] = 21,
            ["Unknown1"] = "text",
            ["Unknown2"] = new object?[] { 1, "two", null },
            ["Name"] = "keep",
            ["Unknown3"] = new Dictionary<string, object?> { ["nested"] = 1 },
        }, options);

        var result = MessagePackSerializer.Deserialize<Person>(payload, options)!;
        Assert.Equal(21, result.Age);
        Assert.Equal("keep", result.Name);
    }

    [Fact]
    public void Populate_ReusesInstanceAndKeepsAbsentMembers()
    {
        var payload = MessagePackSerializer.Serialize(new Dictionary<string, object?> { ["Age"] = 50 }, options);

        var target = new Person { Age = 1, Name = "original" };
        var incoming = target;
        MessagePackSerializer.Deserialize(payload, ref incoming, options);

        Assert.Same(target, incoming);
        Assert.Equal(50, target.Age);
        Assert.Equal("original", target.Name); // absent from payload: untouched
    }

    public class Node
    {
        public int Value { get; set; }
        public Node? Next { get; set; }
    }

    [Fact]
    public void RecursiveType_RoundTrips()
    {
        var chain = new Node { Value = 1, Next = new Node { Value = 2, Next = new Node { Value = 3 } } };

        var result = MessagePackSerializer.Deserialize<Node>(MessagePackSerializer.Serialize(chain, options), options)!;

        Assert.Equal(1, result.Value);
        Assert.Equal(2, result.Next!.Value);
        Assert.Equal(3, result.Next.Next!.Value);
        Assert.Null(result.Next.Next.Next);
    }

    [Fact]
    public void NullRoot_RoundTripsAsNil()
    {
        var payload = MessagePackSerializer.Serialize<Person?>(null, options);
        Assert.Equal(new byte[] { 0xC0 }, payload);
        Assert.Null(MessagePackSerializer.Deserialize<Person?>(payload, options));
    }

    [Fact]
    public void NilIntoStruct_Throws()
    {
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<Point>([0xC0], options));
    }

    public class NoDefaultConstructor
    {
        public NoDefaultConstructor(int id) => Id = id;
        public int Id { get; set; }
    }

    [Fact]
    public void CtorParameter_MatchedCaseInsensitively()
    {
        // ctor parameter `id` binds property `Id` (ordinal-ignore-case, v3's rule)
        var payload = MessagePackSerializer.Serialize(new NoDefaultConstructor(9), options);
        Assert.Equal(9, MessagePackSerializer.Deserialize<NoDefaultConstructor>(payload, options)!.Id);

        // populate path still works: the caller supplies the instance
        var target = new NoDefaultConstructor(0);
        var incoming = target;
        MessagePackSerializer.Deserialize(payload, ref incoming, options);
        Assert.Same(target, incoming);
        Assert.Equal(9, target.Id);
    }

    public class Unconstructable
    {
        public Unconstructable(string secret)
        {
        }

        public int Value { get; set; }
    }

    [Fact]
    public void NoUsableConstructor_SerializesButThrowsOnFreshDeserialize()
    {
        var payload = MessagePackSerializer.Serialize(new Unconstructable("x") { Value = 5 }, options);

        var ex = Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<Unconstructable>(payload, options));
        Assert.Contains("constructor", ex.Message);

        // populate path still works
        var target = new Unconstructable("y");
        var incoming = target;
        MessagePackSerializer.Deserialize(payload, ref incoming, options);
        Assert.Equal(5, target.Value);
    }

    public class ImmutablePerson
    {
        public ImmutablePerson(int id, string? name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string? Name { get; }
        public int Doubled => Id * 2; // computed: written, never read back
        public string? Note { get; set; } // settable extra: applied after construction
    }

    [Fact]
    public void CtorPlusSetters_Compose()
    {
        var payload = MessagePackSerializer.Serialize(new ImmutablePerson(4, "i") { Note = "n" }, options);
        var result = MessagePackSerializer.Deserialize<ImmutablePerson>(payload, options)!;

        Assert.Equal(4, result.Id);
        Assert.Equal("i", result.Name);
        Assert.Equal("n", result.Note);
    }

    [Fact]
    public void MissingCtorParameterKey_ThrowsByDefault_BecomesDefaultOnOptOut()
    {
        var payload = MessagePackSerializer.Serialize(new Dictionary<string, object?> { ["Name"] = "only" }, options);

        // a no-default constructor parameter is a required member: absence is a contract
        // violation under the default resolver configuration
        var ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<ImmutablePerson>(payload, options));
        Assert.Contains("'Id'", ex.Message);

        var lenient = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [MessagePackFormatterFactory.Default.WithContractless()], validateRequiredMembers: false));
        var result = MessagePackSerializer.Deserialize<ImmutablePerson>(payload, lenient)!;
        Assert.Equal(0, result.Id); // absent parameter: default(int)
        Assert.Equal("only", result.Name);
    }

    public class ManyConstructors
    {
        public ManyConstructors()
        {
        }

        public ManyConstructors(int a) => A = a;

        public ManyConstructors(int a, int b)
        {
            A = a;
            B = b;
        }

        public int A { get; }
        public int B { get; }
    }

    [Fact]
    public void BiggestFullyMatchedCtor_Wins()
    {
        // getter-only members prove the 2-arg ctor ran, not parameterless + setters
        var payload = MessagePackSerializer.Serialize(new ManyConstructors(1, 2), options);
        var result = MessagePackSerializer.Deserialize<ManyConstructors>(payload, options)!;

        Assert.Equal(1, result.A);
        Assert.Equal(2, result.B);
    }

    class PrivateConstructor
    {
        PrivateConstructor()
        {
        }

        public int Value { get; set; }

        public static PrivateConstructor Create(int value) => new() { Value = value };
    }

    [Fact]
    public void NonPublicParameterlessConstructor_Works()
    {
        var payload = MessagePackSerializer.Serialize(PrivateConstructor.Create(11), options);
        Assert.Equal(11, MessagePackSerializer.Deserialize<PrivateConstructor>(payload, options)!.Value);
    }

    public record RecordPerson(int Age, string Name);

    [Fact]
    public void Record_RoundTrips()
    {
        var payload = MessagePackSerializer.Serialize(new RecordPerson(3, "r"), options);
        Assert.Equal(new RecordPerson(3, "r"), MessagePackSerializer.Deserialize<RecordPerson>(payload, options));
    }

    public readonly record struct RecordPoint(int X, int Y);

    [Fact]
    public void ReadonlyRecordStruct_RoundTrips()
    {
        var payload = MessagePackSerializer.Serialize(new RecordPoint(7, -8), options);
        Assert.Equal(new RecordPoint(7, -8), MessagePackSerializer.Deserialize<RecordPoint>(payload, options));
    }

    [Fact]
    public void AnonymousType_RoundTrips()
    {
        static TAnonymous RoundTrip<TAnonymous>(TAnonymous value)
            => MessagePackSerializer.Deserialize<TAnonymous>(MessagePackSerializer.Serialize(value, options), options)!;

        var result = RoundTrip(new { Age = 20, Name = "anon" });
        Assert.Equal(20, result.Age);
        Assert.Equal("anon", result.Name);
    }

    [Fact]
    public void Record_CrossCompatibleWithV3Contractless()
    {
        var v3Options = V3::MessagePack.Resolvers.ContractlessStandardResolver.Options;
        var record = new RecordPerson(31, "rv3");

        Assert.Equal(record, MessagePackSerializer.Deserialize<RecordPerson>(
            V3::MessagePack.MessagePackSerializer.Serialize(record, v3Options), options));
        Assert.Equal(record, V3::MessagePack.MessagePackSerializer.Deserialize<RecordPerson>(
            MessagePackSerializer.Serialize(record, options), v3Options));
    }

    [Fact]
    public void EarlierTiers_StillClaimTheirTypes()
    {
        // the contractless tail must not disturb the primitive/collection tiers
        Assert.Equal(123, MessagePackSerializer.Deserialize<int>(MessagePackSerializer.Serialize(123, options), options));
        Assert.Equal([1, 2], MessagePackSerializer.Deserialize<int[]>(MessagePackSerializer.Serialize(new[] { 1, 2 }, options), options));
        Assert.Equal("s", MessagePackSerializer.Deserialize<string>(MessagePackSerializer.Serialize("s", options), options));
    }

    [Fact]
    public void CrossCompatible_WithV3ContractlessStandardResolver()
    {
        var v3Options = V3::MessagePack.Resolvers.ContractlessStandardResolver.Options;
        var person = new Person { Age = 30, Name = "foo", Scores = [1, 2, 3], Partner = new Person { Age = 28 } };

        // v3-written payload reads back through UMP contractless
        var fromV3 = MessagePackSerializer.Deserialize<Person>(
            V3::MessagePack.MessagePackSerializer.Serialize(person, v3Options), options)!;
        Assert.Equal(30, fromV3.Age);
        Assert.Equal("foo", fromV3.Name);
        Assert.Equal([1, 2, 3], fromV3.Scores);
        Assert.Equal(28, fromV3.Partner!.Age);

        // UMP-written payload reads back through v3 contractless
        var fromUmp = V3::MessagePack.MessagePackSerializer.Deserialize<Person>(
            MessagePackSerializer.Serialize(person, options), v3Options);
        Assert.Equal(30, fromUmp.Age);
        Assert.Equal("foo", fromUmp.Name);
        Assert.Equal([1, 2, 3], fromUmp.Scores);
        Assert.Equal(28, fromUmp.Partner!.Age);
    }

    static readonly MessagePackSerializerOptions allowPrivateOptions = new MessagePackSerializerOptions(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless(allowPrivate: true)]));

    public class PrivateState
    {
        public int Id { get; private set; }
        string? secretName;           // private field
        int SecretAge { get; set; }   // private auto-property; its backing field must not double-serialize

        public void Fill(int id, string name, int age)
        {
            Id = id;
            secretName = name;
            SecretAge = age;
        }

        public (int Id, string? Name, int Age) Snapshot() => (Id, secretName, SecretAge);
    }

    [Fact]
    public void AllowPrivate_RoundTripsPrivateState()
    {
        var value = new PrivateState();
        value.Fill(1, "s", 40);

        var result = MessagePackSerializer.Deserialize<PrivateState>(
            MessagePackSerializer.Serialize(value, allowPrivateOptions), allowPrivateOptions)!;
        Assert.Equal((1, "s", 40), result.Snapshot());
    }

    [Fact]
    public void AllowPrivate_SkipsCompilerGeneratedBackingFields()
    {
        var value = new PrivateState();
        value.Fill(1, "s", 40);

        var map = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(
            MessagePackSerializer.Serialize(value, allowPrivateOptions), allowPrivateOptions)!;
        Assert.Equal(["Id", "SecretAge", "secretName"], map.Keys.OrderBy(k => k, StringComparer.Ordinal));
    }

    [Fact]
    public void DefaultContractless_StaysPublicOnly()
    {
        var value = new PrivateState();
        value.Fill(1, "s", 40);

        var map = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(
            MessagePackSerializer.Serialize(value, options), options)!;
        Assert.Equal(["Id"], map.Keys);

        // and without allowPrivate the private setter is unusable on the way back
        var back = MessagePackSerializer.Deserialize<PrivateState>(
            MessagePackSerializer.Serialize(value, options), options)!;
        Assert.Equal(0, back.Id);
    }

    public class BaseSecret
    {
        int hidden;

        public void SetHidden(int value) => hidden = value;
        public int GetHidden() => hidden;
    }

    public class DerivedCarrier : BaseSecret
    {
        public int Visible { get; set; }
    }

    [Fact]
    public void AllowPrivate_IncludesBaseClassPrivates()
    {
        var value = new DerivedCarrier { Visible = 2 };
        value.SetHidden(77);

        var result = MessagePackSerializer.Deserialize<DerivedCarrier>(
            MessagePackSerializer.Serialize(value, allowPrivateOptions), allowPrivateOptions)!;
        Assert.Equal(2, result.Visible);
        Assert.Equal(77, result.GetHidden());
    }

    public class PrivateCtorImmutable
    {
        PrivateCtorImmutable(int id) => Id = id;

        public int Id { get; }

        public static PrivateCtorImmutable Create(int id) => new(id);
    }

    [Fact]
    public void AllowPrivate_MatchesPrivateConstructor()
    {
        var payload = MessagePackSerializer.Serialize(PrivateCtorImmutable.Create(6), allowPrivateOptions);
        Assert.Equal(6, MessagePackSerializer.Deserialize<PrivateCtorImmutable>(payload, allowPrivateOptions)!.Id);

        // default mode has no usable constructor for this type
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<PrivateCtorImmutable>(
            MessagePackSerializer.Serialize(PrivateCtorImmutable.Create(6), options), options));
    }

    [Fact]
    public void AllowPrivate_CrossCompatibleWithV3()
    {
        var v3Options = V3::MessagePack.MessagePackSerializerOptions.Standard.WithResolver(
            V3::MessagePack.Resolvers.ContractlessStandardResolverAllowPrivate.Instance);
        var value = new PrivateState();
        value.Fill(9, "v3", 55);

        var fromV3 = MessagePackSerializer.Deserialize<PrivateState>(
            V3::MessagePack.MessagePackSerializer.Serialize(value, v3Options), allowPrivateOptions)!;
        Assert.Equal((9, "v3", 55), fromV3.Snapshot());

        var fromUmp = V3::MessagePack.MessagePackSerializer.Deserialize<PrivateState>(
            MessagePackSerializer.Serialize(value, allowPrivateOptions), v3Options);
        Assert.Equal((9, "v3", 55), fromUmp.Snapshot());
    }

    [Fact]
    public void GenericPoco_RoundTrips()
    {
        var value = new Wrapper<string> { Inner = "wrapped" };
        var result = MessagePackSerializer.Deserialize<Wrapper<string>>(MessagePackSerializer.Serialize(value, options), options)!;
        Assert.Equal("wrapped", result.Inner);
    }

    public class Wrapper<T>
    {
        public T? Inner { get; set; }
    }
}

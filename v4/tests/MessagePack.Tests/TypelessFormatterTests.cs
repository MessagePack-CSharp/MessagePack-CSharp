extern alias V3;
#pragma warning disable CS0618 // typeless is [Obsolete]-gated by design; these tests are the consenting caller

using MessagePack.Formatters;

namespace MessagePack.Tests;

public class TypelessFormatterTests
{
    static readonly MessagePackSerializerOptions options = new MessagePackSerializerOptions(
        new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())]));

    static object? RoundTrip(object? value)
        => MessagePackSerializer.Deserialize<object?>(MessagePackSerializer.Serialize<object?>(value, options), options);

    public class TypelessPerson
    {
        public int Age { get; set; }
        public string? Name { get; set; }
    }

    public class ObjectHolder
    {
        public object? Payload { get; set; }
    }

    [Fact]
    public void Primitives_PreserveClrType()
    {
        // the wire format width is the type tag: every primitive must come back as the
        // exact CLR type it went in as
        object?[] values =
        [
            true, (sbyte)-5, (byte)7, (short)-300, (ushort)300, 42, -42, 42u, 42L, 42ul,
            1.5f, 1.5, "hello", new byte[] { 1, 2, 3 }, null,
        ];
        foreach (var value in values)
        {
            var back = RoundTrip(value);
            Assert.Equal(value?.GetType(), back?.GetType());
            Assert.Equal(value, back);
        }
    }

    [Fact]
    public void WrappedScalars_RoundTrip()
    {
        object[] values =
        [
            'A',
            new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Local),
            new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(9)),
            1.5m,
            Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10"),
            DayOfWeek.Friday,
            TimeSpan.FromSeconds(90),
        ];
        foreach (var value in values)
        {
            var back = RoundTrip(value)!;
            Assert.Equal(value.GetType(), back.GetType());
            Assert.Equal(value, back);
        }

        // DateTimeKind survives (the whole point of the ToBinary encoding)
        Assert.Equal(DateTimeKind.Local, ((DateTime)RoundTrip(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Local))!).Kind);
        Assert.Equal(DateTimeKind.Utc, ((DateTime)RoundTrip(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc))!).Kind);
    }

    [Fact]
    public void Poco_ReconstructsConcreteType()
    {
        var back = RoundTrip(new TypelessPerson { Age = 30, Name = "t" });
        var person = Assert.IsType<TypelessPerson>(back);
        Assert.Equal(30, person.Age);
        Assert.Equal("t", person.Name);
    }

    [Fact]
    public void NestedObjectMember_Reconstructs()
    {
        var back = Assert.IsType<ObjectHolder>(RoundTrip(new ObjectHolder { Payload = new TypelessPerson { Age = 1, Name = "n" } }));
        var inner = Assert.IsType<TypelessPerson>(back.Payload);
        Assert.Equal(1, inner.Age);

        var intHolder = Assert.IsType<ObjectHolder>(RoundTrip(new ObjectHolder { Payload = 42 }));
        Assert.IsType<int>(intHolder.Payload);
        Assert.Equal(42, intHolder.Payload);
    }

    [Fact]
    public void CollectionsAsObject_ReconstructConcreteTypes()
    {
        var array = Assert.IsType<int[]>(RoundTrip(new[] { 1, 2 }));
        Assert.Equal([1, 2], array);

        var list = Assert.IsType<List<string>>(RoundTrip(new List<string> { "x", "y" }));
        Assert.Equal(["x", "y"], list);

        var objects = Assert.IsType<object[]>(RoundTrip(new object[] { 1, "two", new TypelessPerson { Age = 3 } }));
        Assert.Equal(42 - 41, objects[0]); // int 1
        Assert.Equal("two", objects[1]);
        Assert.IsType<TypelessPerson>(objects[2]);
    }

    [Fact]
    public void CrossCompatible_WithV3Typeless_BothDirections()
    {
        object[] values =
        [
            new TypelessPerson { Age = 30, Name = "v3" },
            42, (byte)7, "hello",
            new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Local),
            new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(9)),
            1.5m, Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10"), 'A', DayOfWeek.Friday,
            new[] { 1, 2 },
            new List<string> { "x" },
        ];

        foreach (var value in values)
        {
            // v3 bytes -> UMP
            var fromV3 = MessagePackSerializer.Deserialize<object?>(
                V3::MessagePack.MessagePackSerializer.Typeless.Serialize(value), options)!;
            Assert.Equal(value.GetType(), fromV3.GetType());
            AssertValueEqual(value, fromV3);

            // UMP bytes -> v3
            var fromUmp = V3::MessagePack.MessagePackSerializer.Typeless.Deserialize(
                MessagePackSerializer.Serialize<object?>(value, options))!;
            Assert.Equal(value.GetType(), fromUmp.GetType());
            AssertValueEqual(value, fromUmp);
        }

        static void AssertValueEqual(object expected, object actual)
        {
            if (expected is TypelessPerson person)
            {
                var back = (TypelessPerson)actual;
                Assert.Equal(person.Age, back.Age);
                Assert.Equal(person.Name, back.Name);
                return;
            }
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void CrossCompatible_NestedGraph()
    {
        var value = new ObjectHolder { Payload = new TypelessPerson { Age = 2, Name = "nest" } };

        var fromV3 = Assert.IsType<ObjectHolder>(MessagePackSerializer.Deserialize<object?>(
            V3::MessagePack.MessagePackSerializer.Typeless.Serialize(value), options));
        Assert.Equal(2, Assert.IsType<TypelessPerson>(fromV3.Payload).Age);

        var fromUmp = Assert.IsType<ObjectHolder>(V3::MessagePack.MessagePackSerializer.Typeless.Deserialize(
            MessagePackSerializer.Serialize<object?>(value, options)));
        Assert.Equal(2, Assert.IsType<TypelessPerson>(fromUmp.Payload).Age);
    }

    [Fact]
    public void PrimitiveBytes_MatchV3Exactly()
    {
        object?[] values = [42, (byte)7, (sbyte)-5, 42L, "hello", true, 1.5, null];
        foreach (var value in values)
        {
            Assert.Equal(
                V3::MessagePack.MessagePackSerializer.Typeless.Serialize(value),
                MessagePackSerializer.Serialize<object?>(value, options));
        }
    }

    [Fact]
    public void ForeignPlainData_ReadsByWireType()
    {
        // data written WITHOUT typeless (smallest formats) still reads, by v3 wire-type rules
        var plain = MessagePackSerializer.Serialize(42); // positive fixint
        var back = MessagePackSerializer.Deserialize<object?>(plain, options);
        Assert.IsType<byte>(back); // v3 rule: positive fixint reads as byte

        var timestamp = MessagePackSerializer.Serialize(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        Assert.IsType<DateTime>(MessagePackSerializer.Deserialize<object?>(timestamp, options));
    }

    [Fact]
    public void UnknownTypeName_Throws()
    {
        var payload = BuildTypelessPayload("No.Such.Type, NoSuchAssembly");
        var ex = Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<object?>(payload, options));
        Assert.Contains("No.Such.Type", ex.Message);
    }

    [Fact]
    public void TypeLoaderHook_IsSoleAuthority()
    {
        var hooked = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            MessagePackFormatterFactory.Combine(
                new TypelessFormatterFactory(TypelessTypeLoader.Create(name => name.StartsWith("MessagePack.Tests.TypelessFormatterTests+TypelessPerson,", StringComparison.Ordinal) ? typeof(TypelessPerson) : null)),
                SourceGeneratedFormatterFactory.Instance,
                BuiltInFormatterFactory.Instance,
                GenericFormatterFactory.Instance,
                new ReflectionFormatterFactory()),
        ]));

        // allowed type: round-trips
        var payload = MessagePackSerializer.Serialize<object?>(new TypelessPerson { Age = 5 }, options);
        Assert.Equal(5, Assert.IsType<TypelessPerson>(MessagePackSerializer.Deserialize<object?>(payload, hooked)).Age);

        // any other type: refused even though Type.GetType could load it
        var denied = MessagePackSerializer.Serialize<object?>(new ObjectHolder(), options);
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<object?>(denied, hooked));
    }

    [Fact]
    public void AllowedTypes_ResolvesListedAndRefusesOthers()
    {
        var allowed = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            MessagePackFormatterFactory.Combine(
                new TypelessFormatterFactory(TypelessTypeLoader.AllowedTypes(typeof(TypelessPerson))),
                SourceGeneratedFormatterFactory.Instance,
                BuiltInFormatterFactory.Instance,
                GenericFormatterFactory.Instance,
                new ReflectionFormatterFactory()),
        ]));

        // listed type: round-trips
        var payload = MessagePackSerializer.Serialize<object?>(new TypelessPerson { Age = 6, Name = "a" }, options);
        Assert.Equal(6, Assert.IsType<TypelessPerson>(MessagePackSerializer.Deserialize<object?>(payload, allowed)).Age);

        // any other type: refused even though Type.GetType could load it
        var denied = MessagePackSerializer.Serialize<object?>(new ObjectHolder(), options);
        Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<object?>(denied, allowed));
    }

    [Fact]
    public void AllowedTypes_MatchesShortenedAndDriftedSpellings()
    {
        var allowed = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            MessagePackFormatterFactory.Combine(
                new TypelessFormatterFactory(TypelessTypeLoader.AllowedTypes(typeof(TypelessPerson))),
                SourceGeneratedFormatterFactory.Instance,
                BuiltInFormatterFactory.Instance,
                GenericFormatterFactory.Instance,
                new ReflectionFormatterFactory()),
        ]));
        var inner = MessagePackSerializer.Serialize(new TypelessPerson { Age = 9 },
            new MessagePackSerializerOptions(new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless()])));
        var fullName = typeof(TypelessPerson).AssemblyQualifiedName!;

        // omitAssemblyVersion writers spell the name without Version/Culture/PublicKeyToken
        var shortened = System.Text.RegularExpressions.Regex.Replace(
            fullName, @", Version=\d+\.\d+\.\d+\.\d+, Culture=\w+, PublicKeyToken=\w+", "");
        Assert.DoesNotContain("Version=", shortened);
        Assert.Equal(9, Assert.IsType<TypelessPerson>(MessagePackSerializer.Deserialize<object?>(
            BuildTypelessPayload(shortened, inner), allowed)).Age);

        // a payload from another version of the assembly still matches (version-insensitive)
        var drifted = System.Text.RegularExpressions.Regex.Replace(
            fullName, @"Version=\d+\.\d+\.\d+\.\d+", "Version=9.9.9.9");
        Assert.Contains("Version=9.9.9.9", drifted);
        Assert.Equal(9, Assert.IsType<TypelessPerson>(MessagePackSerializer.Deserialize<object?>(
            BuildTypelessPayload(drifted, inner), allowed)).Age);
    }

    [Fact]
    public void OmitAssemblyVersion_MatchesV3AndCrossReads()
    {
        var omitOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            MessagePackFormatterFactory.Combine(
                new TypelessFormatterFactory(TypelessTypeLoader.LoadAnyType(), omitAssemblyVersion: true),
                SourceGeneratedFormatterFactory.Instance,
                BuiltInFormatterFactory.Instance,
                GenericFormatterFactory.Instance,
                new ReflectionFormatterFactory()),
        ]));
        var v3OmitOptions = V3::MessagePack.Resolvers.TypelessContractlessStandardResolver.Options.WithOmitAssemblyVersion(true);
        var value = new TypelessPerson { Age = 4, Name = "omit" };

        // identical shortened wire, both directions readable with DEFAULT options
        var umpBytes = MessagePackSerializer.Serialize<object?>(value, omitOptions);
        Assert.Equal(V3::MessagePack.MessagePackSerializer.Serialize<object>(value, v3OmitOptions), umpBytes);
        Assert.Equal(4, Assert.IsType<TypelessPerson>(MessagePackSerializer.Deserialize<object?>(umpBytes, options)).Age);
        Assert.Equal(4, Assert.IsType<TypelessPerson>(V3::MessagePack.MessagePackSerializer.Typeless.Deserialize(umpBytes)).Age);
    }

    [Fact]
    public void AllowAssemblyVersionMismatch_ReadsMismatchedIdentity()
    {
        // NOTE: modern .NET's default binder is already lenient about version/PKT
        // mismatches for loaded assemblies, so the shortened-name RETRY cannot be
        // provoked in-process here (the full name resolves anyway); the retry matters
        // under strict binders (.NET Framework). This pins the option's plumbing: a
        // rewritten identity still reads with the option enabled.
        var mismatchedName = System.Text.RegularExpressions.Regex.Replace(
            typeof(TypelessPerson).AssemblyQualifiedName!, @"Version=\d+\.\d+\.\d+\.\d+", "Version=9.9.9.9");
        Assert.Contains("Version=9.9.9.9", mismatchedName);
        var inner = MessagePackSerializer.Serialize(new TypelessPerson { Age = 8 },
            new MessagePackSerializerOptions(new MessagePackFormatterResolver([MessagePackFormatterFactory.Default.WithContractless()])));
        var payload = BuildTypelessPayload(mismatchedName, inner);

        var tolerant = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            MessagePackFormatterFactory.Combine(
                new TypelessFormatterFactory(TypelessTypeLoader.LoadAnyType(allowAssemblyVersionMismatch: true)),
                SourceGeneratedFormatterFactory.Instance,
                BuiltInFormatterFactory.Instance,
                GenericFormatterFactory.Instance,
                new ReflectionFormatterFactory()),
        ]));
        Assert.Equal(8, Assert.IsType<TypelessPerson>(MessagePackSerializer.Deserialize<object?>(payload, tolerant)).Age);
    }

    [Fact]
    public void DisallowedGadgetTypes_AreRefused()
    {
        // v3's default deny list, exact-FullName matched after resolution
        foreach (var gadget in new[] { typeof(System.Diagnostics.Process), typeof(System.Security.Claims.ClaimsIdentity) })
        {
            var payload = BuildTypelessPayload(gadget.AssemblyQualifiedName!);
            var ex = Assert.Throws<MessagePackSerializationException>(
                () => MessagePackSerializer.Deserialize<object?>(payload, options));
            Assert.Contains("disallowed", ex.Message);
        }
    }

    [Fact]
    public void DisallowedGadgetTypes_BlockedEvenThroughTypeLoaderHook()
    {
        // the deny list is defense in depth: it applies to loader results too
        var hooked = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
        [
            MessagePackFormatterFactory.Combine(
                new TypelessFormatterFactory(TypelessTypeLoader.Create(_ => typeof(System.Diagnostics.Process))),
                SourceGeneratedFormatterFactory.Instance,
                BuiltInFormatterFactory.Instance,
                GenericFormatterFactory.Instance,
                new ReflectionFormatterFactory()),
        ]));

        var payload = BuildTypelessPayload("anything");
        var ex = Assert.Throws<MessagePackSerializationException>(
            () => MessagePackSerializer.Deserialize<object?>(payload, hooked));
        Assert.Contains("disallowed", ex.Message);
    }

    [Fact]
    public void DisallowedGadgetTypes_RefusedWhenWrappedInArrayOrGeneric()
    {
        // CVE-2026-48517 class: the deny list must see through wrappers. A gadget nested as
        // Process[], Process[][], List<Process>, or Dictionary<string, Process> resolves to a
        // type whose OWN FullName is not on the list, and the array/generic formatter then
        // reconstructs the element WITHOUT re-entering typeless, so an outer-type-only check
        // lets the gadget ride in. The loader is hooked to return the wrapper directly so the
        // test exercises the deny-list recursion, not the platform's generic-name resolution.
        Type[] wrappers =
        [
            typeof(System.Diagnostics.Process[]),
            typeof(System.Diagnostics.Process[][]),
            typeof(List<System.Diagnostics.Process>),
            typeof(Dictionary<string, System.Diagnostics.Process>),
        ];
        foreach (var wrapper in wrappers)
        {
            var hooked = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [
                MessagePackFormatterFactory.Combine(
                    new TypelessFormatterFactory(TypelessTypeLoader.Create(_ => wrapper)),
                    SourceGeneratedFormatterFactory.Instance,
                    BuiltInFormatterFactory.Instance,
                    GenericFormatterFactory.Instance,
                    new ReflectionFormatterFactory()),
            ]));

            var payload = BuildTypelessPayload("anything");
            var ex = Assert.Throws<MessagePackSerializationException>(
                () => MessagePackSerializer.Deserialize<object?>(payload, hooked));
            Assert.Contains("disallowed", ex.Message);
        }
    }

    // hand-built ext(100) { str typeName, inner } payload
    static byte[] BuildTypelessPayload(string typeName, byte[]? inner = null)
    {
        var nameUtf8 = System.Text.Encoding.UTF8.GetBytes(typeName);
        var body = new List<byte>();
        body.Add(0xD9); // str8
        body.Add((byte)nameUtf8.Length);
        body.AddRange(nameUtf8);
        body.AddRange(inner ?? [0xC0]); // nil when no inner payload given

        var payload = new List<byte> { 0xC7, (byte)body.Count, 0x64 }; // ext8, len, code 100
        payload.AddRange(body);
        return [.. payload];
    }
}

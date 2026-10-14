using System.Reflection;
using AnnotationsOnlyLibrary;
using MessagePack;
using V3Library;

// Each check prints its name and the exception on failure; the process exits non-zero after the first failure so the
// CI step fails with the reason in its log.
var failures = 0;

Check("packaged source generator produced this assembly's formatter (DefaultAot has no reflection tier)", () =>
{
    var options = MessagePackSerializerOptions.DefaultAot;
    var bytes = MessagePackSerializer.Serialize(new SmokeDto { Id = 1, Name = "n", Tags = [1, 2] }, options);
    var back = MessagePackSerializer.Deserialize<SmokeDto>(bytes, options)!;
    Expect(back.Id == 1 && back.Name == "n" && back.Tags!.Length == 2, "round trip");
});

Check("the Annotations shim's dependency brings the generator into an Annotations-only project", () =>
{
    var options = MessagePackSerializerOptions.DefaultAot;
    var bytes = MessagePackSerializer.Serialize(new AnnotationsOnlyDto { Id = 2, Name = "a" }, options);
    Expect(MessagePackSerializer.ConvertToJson(bytes) == "[2,\"a\"]", "wire");
    Expect(MessagePackSerializer.Deserialize<AnnotationsOnlyDto>(bytes, options)!.Name == "a", "round trip");
});

Check("a v3-built assembly's attribute references bind to the 4.x shim's forwarders", () =>
{
    // the first GetCustomAttributes on a v3-built type loads "MessagePack.Annotations, Version=3.1.8.0"; the shim must
    // satisfy that identity (public key, version not lower) and forward to MessagePack's attribute types
    var attributes = typeof(V3Dto).GetCustomAttributes(inherit: false);
    Expect(attributes.OfType<MessagePackObjectAttribute>().Any(), "MessagePackObjectAttribute forwarded");
    var key = typeof(V3Dto).GetProperty(nameof(V3Dto.Id))!.GetCustomAttribute<KeyAttribute>();
    Expect(key is { IntKey: 0 }, "KeyAttribute forwarded");
    Expect(typeof(V3Dto).GetProperty(nameof(V3Dto.NotSerialized))!.GetCustomAttribute<IgnoreMemberAttribute>() is not null, "IgnoreMemberAttribute forwarded");
    var annotations = typeof(V3Dto).Assembly.GetReferencedAssemblies().Single(static a => a.Name == "MessagePack.Annotations");
    Expect(annotations.Version == new Version(3, 1, 8, 0), "the library still references 3.1.8.0: " + annotations.Version);
    Expect(typeof(MessagePackObjectAttribute).Assembly.GetName().Name == "MessagePack", "the forwarded type lives in MessagePack");
});

Check("the reflection tier serializes v3-built types through the shim", () =>
{
    var options = MessagePackSerializerOptions.Default; // the reflection tier serves annotated types no generator saw
    var dto = new V3Dto { Id = 3, Name = "v3", Inner = new V3Inner { Value = 1.5 }, NotSerialized = 99 };
    var bytes = MessagePackSerializer.Serialize(dto, options);
    Expect(MessagePackSerializer.ConvertToJson(bytes) == "[3,\"v3\",{\"Value\":1.5}]", "wire: " + MessagePackSerializer.ConvertToJson(bytes));
    var back = MessagePackSerializer.Deserialize<V3Dto>(bytes, options)!;
    Expect(back.Inner!.Value == 1.5 && back.NotSerialized == 0, "round trip");

    var immutable = MessagePackSerializer.Deserialize<V3Immutable>(MessagePackSerializer.Serialize(new V3Immutable(4, "l"), options), options)!;
    Expect(immutable.Id == 4 && immutable.Label == "l", "[SerializationConstructor] forwarded and honoured");
});

Check("the shim's loadable v3 [Union] describes a v3-built hierarchy to the reflection tier", () =>
{
    var options = MessagePackSerializerOptions.Default;
    V3Shape[] shapes = [new V3Circle { Radius = 1 }, new V3Square { Side = 2 }];
    var bytes = MessagePackSerializer.Serialize(shapes, options);
    var back = MessagePackSerializer.Deserialize<V3Shape[]>(bytes, options)!;
    Expect(back[0] is V3Circle { Radius: 1 } && back[1] is V3Square { Side: 2 }, "union round trip: " + MessagePackSerializer.ConvertToJson(bytes));
});

Check("MessagePack.LZ4 and MessagePack.Zstandard load their native libraries from the packages", () =>
{
    var value = Enumerable.Range(0, 10_000).Select(static i => i % 100).ToArray();
    var lz4 = MessagePackSerializerOptions.Default.WithLz4Frame();
    Expect(MessagePackSerializer.Deserialize<int[]>(MessagePackSerializer.Serialize(value, lz4), lz4)!.SequenceEqual(value), "LZ4 frame round trip");
    var zstd = MessagePackSerializerOptions.Default.WithZstandardFrame(3);
    Expect(MessagePackSerializer.Deserialize<int[]>(MessagePackSerializer.Serialize(value, zstd), zstd)!.SequenceEqual(value), "Zstandard frame round trip");
});

Console.WriteLine(failures == 0 ? "package smoke: OK" : $"package smoke: {failures} failure(s)");
return failures == 0 ? 0 : 1;

void Check(string name, Action check)
{
    try
    {
        check();
        Console.WriteLine($"ok   {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {name}");
        Console.Error.WriteLine(ex);
    }
}

static void Expect(bool condition, string what)
{
    if (!condition)
    {
        throw new InvalidOperationException("expected: " + what);
    }
}

[MessagePackObject]
public class SmokeDto
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
    [Key(2)] public int[]? Tags { get; set; }
}

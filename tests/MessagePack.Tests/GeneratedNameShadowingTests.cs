using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// The generated code names library and BCL types by their simple names. C# resolves a simple name through the
// enclosing namespaces (innermost first, each one's own using directives right after its members) before it reaches
// the using directives at the top of the file, so a consumer type in the global namespace named like one of those
// (Unsafe, IWriteBuffer, BinaryPrimitives, MaxInt32Length, ...) used to hide it and break the build. The emitters now
// put their using directives INSIDE the MessagePack.Generated block (consulted before the global namespace), and a
// formatter nested in the consumer's own type (AllowPrivate) spells every type name in full (ObjectEmitter.Names).
public class GeneratedNameShadowingTests
{
    // every simple name the emitted code relies on, declared as a consumer type of a different shape
    const string Shadows = """
        public class Unsafe { }
        public class BinaryPrimitives { }
        public class MethodImpl { }
        public class MethodImplOptions { }
        public class MaxInt32Length { }
        public class MaxArrayHeaderLength { }
        public class MaxMapHeaderLength { }
        public interface IWriteBuffer { }
        public interface IReadBuffer { }
        public class IMessagePackFormatter { }
        public class MessagePackFormatterResolver { }
        public class MessagePackFormatterFactory { }
        public class SourceGeneratedFormatterFactory { }
        public class SourceGeneratorHelper { }
        public class MessagePackSerializationException { }
        public class MessagePackPrimitives { }
        public class SerializeState { }
        public class DeserializeState { }
        public class ModuleInitializer { }
        public class Formatters { }
        public class StringFormatter { }
        """;

    [Fact]
    public void GlobalNamespaceTypes_DoNotHideTheGeneratedCodesNames()
    {
        var assembly = AnalyzerTestHost.CompileAndLoad("using MessagePack;" + Environment.NewLine + Shadows + """

            [MessagePackObject] public class Keyed { [Key(0)] public int X { get; set; } [Key(1)] public string? Name { get; set; } [Key(2)] public int? Maybe { get; set; } }
            [MessagePackObject(true)] public class Mapped { [Key("id")] public required int Id { get; init; } [Key("name")] public string? Name { get; set; } }
            [MessagePackObject(AllowPrivate = true)] public partial class Hidden { [Key(0)] private int secret = 7; [Key(1)] public int Shown { get; set; } [IgnoreMember] public int Secret => secret; }
            [MessagePackObject] public class Cat { [Key(0)] public string? Name { get; set; } }
            [MessagePackObject] public class Dog { [Key(0)] public string? Name { get; set; } }
            [UnionTag(typeof(Cat), 0)] [UnionTag(typeof(Dog), 1)] public abstract class Pet { }
            [MessagePackSerializable<Keyed[]>] public partial class Roots { }
            """, withGenerator: true);

        // DefaultAot has no reflection fallback: a type reaches it only through the generated factory
        var options = MessagePackSerializerOptions.DefaultAot;
        var keyed = assembly.GetType("Keyed")!;
        var instance = Activator.CreateInstance(keyed)!;
        keyed.GetProperty("X")!.SetValue(instance, 42);
        keyed.GetProperty("Name")!.SetValue(instance, "n");
        Assert.Equal("[42,\"n\",null]", V4.ConvertToJson(V4.Serialize(keyed, instance, options)));

        var mapped = assembly.GetType("Mapped")!;
        var mappedInstance = Activator.CreateInstance(mapped)!;
        mapped.GetProperty("Id")!.SetValue(mappedInstance, 3);
        Assert.Equal("{\"id\":3,\"name\":null}", V4.ConvertToJson(V4.Serialize(mapped, mappedInstance, options)));

        var hidden = assembly.GetType("Hidden")!;
        Assert.Equal("[7,0]", V4.ConvertToJson(V4.Serialize(hidden, Activator.CreateInstance(hidden), options)));

        var roots = assembly.GetType("Keyed[]")!;
        var array = Array.CreateInstance(keyed, 1);
        array.SetValue(instance, 0);
        Assert.Equal("[[42,\"n\",null]]", V4.ConvertToJson(V4.Serialize(roots, array, options)));
    }

    [Fact]
    public void SameNamespaceTypes_DoNotHideTheNestedFormattersNames()
    {
        // AllowPrivate puts the formatter inside the consumer's type, in THEIR namespace, where their own types win over
        // any using directive: the nested formatter must not depend on one
        var assembly = AnalyzerTestHost.CompileAndLoad("""
            using MessagePack;
            namespace Ns
            {
            """ + Shadows + """

                [MessagePackObject(true, AllowPrivate = true)] public partial class Hidden { [Key("secret")] private int secret = 7; [Key("shown")] public int Shown { get; set; } [Key("label")] public string? Label { get; set; } [IgnoreMember] public int Secret => secret; }
                [MessagePackSerializable<Hidden[]>] public partial class Roots { }
            }
            """, withGenerator: true);

        var options = MessagePackSerializerOptions.DefaultAot;
        var hidden = assembly.GetType("Ns.Hidden")!;
        var instance = Activator.CreateInstance(hidden)!;
        hidden.GetProperty("Shown")!.SetValue(instance, 1);
        hidden.GetProperty("Label")!.SetValue(instance, "l");
        var bytes = V4.Serialize(hidden, instance, options);
        Assert.Equal("{\"shown\":1,\"label\":\"l\",\"secret\":7}", V4.ConvertToJson(bytes));
        var back = V4.Deserialize(hidden, bytes, options)!;
        Assert.Equal(7, hidden.GetProperty("Secret")!.GetValue(back));
        Assert.Equal(1, hidden.GetProperty("Shown")!.GetValue(back));
        Assert.Equal("l", hidden.GetProperty("Label")!.GetValue(back));
    }
}

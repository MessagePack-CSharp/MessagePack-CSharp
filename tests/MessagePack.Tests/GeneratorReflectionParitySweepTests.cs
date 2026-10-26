using System.Collections;
using System.Reflection;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// The generated formatter and ReflectionObjectFormatter are two implementations of one contract, and
// GeneratorReflectionParityTests compares them on hand-picked shapes. This sweep takes every concrete [MessagePackObject]
// type this assembly declares, builds a value for it from a seeded generator (so nulls, empties and nesting vary), and
// serializes it through both tiers: the bytes must be identical, and each tier must read the other's bytes back to the
// same value. The shapes the reflection tier declares generator-only (MessagePackUnknownMembers,
// AllowCircularReferences, the surrogate and type-level [MessagePackFormatter] registrations the generated factory
// performs, the generator's expression-form formatter arguments) are skipped by the reflection tier's own refusal,
// every skip is written to the test output, and the aggregate test keeps the compared share from quietly shrinking.
public class GeneratorReflectionParitySweepTests(ITestOutputHelper output)
{
    static readonly MessagePackSerializerOptions Generated = MessagePackSerializerOptions.Default;

    static readonly MessagePackSerializerOptions Reflection = new(new MessagePackFormatterResolver(
    [
        BuiltInFormatterFactory.Instance,
        GenericFormatterFactory.Instance,
        new ReflectionFormatterFactory(annotatedOnly: true),
    ]));

    // AllowPrivate types read their private members in both tiers only when the reflection factory is told so
    static readonly MessagePackSerializerOptions ReflectionPrivate = new(new MessagePackFormatterResolver(
    [
        BuiltInFormatterFactory.Instance,
        GenericFormatterFactory.Instance,
        new ReflectionFormatterFactory(annotatedOnly: true, allowPrivate: true),
    ]));

    const int Seeds = 4;

    public static IEnumerable<object[]> AnnotatedTypes() => SweptTypes().Select(static t => new object[] { t });

    static IEnumerable<Type> SweptTypes()
    {
        foreach (var type in typeof(GeneratorReflectionParitySweepTests).Assembly.GetTypes().OrderBy(static t => t.FullName, StringComparer.Ordinal))
        {
            if (type.GetCustomAttribute<MessagePackObjectAttribute>(inherit: false) is not { } attribute
                || type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition || !type.IsVisible
                || attribute.SuppressSourceGeneration // both tiers are the reflection tier
                || type.GetCustomAttributes<UnionTagAttribute>().Any())
            {
                continue;
            }
            yield return type;
        }
    }

    [Theory]
    [MemberData(nameof(AnnotatedTypes))]
    public void BothTiers_WriteTheSameBytes_AndReadEachOthers(Type type)
    {
        var outcome = Sweep(type);
        if (outcome is not null)
        {
            output.WriteLine($"SKIP {type.FullName}: {outcome}");
        }
    }

    [Fact]
    public void MostAnnotatedTypes_AreCompared()
    {
        // the per-type theory above tolerates a skip; this keeps the skips a minority. If a change makes the reflection
        // tier refuse a shape it used to serve, or the builder stops constructing a family of types, the share drops
        var types = SweptTypes().ToArray();
        var skipped = new List<string>();
        foreach (var type in types)
        {
            if (Sweep(type) is { } reason)
            {
                skipped.Add($"{type.Name}: {reason}");
            }
        }
        output.WriteLine($"{types.Length - skipped.Count} of {types.Length} annotated types compared; skipped:");
        foreach (var line in skipped)
        {
            output.WriteLine("  " + line);
        }
        Assert.True(types.Length >= 100, $"only {types.Length} annotated types found; the sweep's type discovery is broken");
        Assert.True(skipped.Count * 5 <= types.Length, $"{skipped.Count} of {types.Length} types skipped (more than a fifth):{Environment.NewLine}{string.Join(Environment.NewLine, skipped)}");
    }

    // null when the type was compared (and agreed), otherwise the reason it was skipped; a disagreement fails here
    static string? Sweep(Type type)
    {
        var attribute = type.GetCustomAttribute<MessagePackObjectAttribute>()!;
        var reflection = attribute.AllowPrivate ? ReflectionPrivate : Reflection;
        for (int seed = 0; seed < Seeds; seed++)
        {
            object? value;
            try
            {
                value = new ValueBuilder(new Random(seed * 7919 + type.MetadataToken)).Build(type, 0);
            }
            catch (Exception ex)
            {
                return $"the builder cannot construct it ({ex.GetType().Name}: {ex.Message})";
            }
            if (value is null)
            {
                return "the builder produced null";
            }

            byte[] generated;
            try
            {
                generated = V4.Serialize(type, value, Generated);
            }
            catch (Exception ex)
            {
                return $"the generated tier refuses the built value ({ex.GetType().Name}: {ex.Message})";
            }

            byte[] reflected;
            try
            {
                reflected = V4.Serialize(type, value, reflection);
            }
            catch (Exception ex) when (IsGeneratorOnly(ex))
            {
                return $"generator-only shape ({ex.GetType().Name}: {Head(ex.Message)})";
            }

            Assert.True(generated.AsSpan().SequenceEqual(reflected),
                $"{type.FullName} (seed {seed}) differs between tiers.{Environment.NewLine}generated:  {V4.ConvertToJson(generated)}{Environment.NewLine}reflection: {V4.ConvertToJson(reflected)}");

            // each tier reads what the other wrote (the same bytes, as just asserted)
            object? valueByReflection = null, valueByGenerated = null;
            var readByReflection = Record.Exception(() => valueByReflection = V4.Deserialize(type, generated, reflection));
            var readByGenerated = Record.Exception(() => valueByGenerated = V4.Deserialize(type, reflected, Generated));
            Assert.True((readByReflection is null) == (readByGenerated is null),
                $"{type.FullName} (seed {seed}): one tier reads the bytes and the other refuses them.{Environment.NewLine}reflection: {readByReflection?.Message ?? "ok"}{Environment.NewLine}generated:  {readByGenerated?.Message ?? "ok"}");

            // and both read the same value: written back through one tier, the two results agree. Comparing with the
            // original bytes instead would fail on the fixtures' deliberately lossy member formatters
            if (readByReflection is null)
            {
                var backFromReflection = V4.Serialize(type, valueByReflection, Generated);
                var backFromGenerated = V4.Serialize(type, valueByGenerated, Generated);
                Assert.True(backFromReflection.AsSpan().SequenceEqual(backFromGenerated),
                    $"{type.FullName} (seed {seed}): the tiers read different values from the same bytes.{Environment.NewLine}read by reflection: {V4.ConvertToJson(backFromReflection)}{Environment.NewLine}read by generated:  {V4.ConvertToJson(backFromGenerated)}");
            }
        }
        return null;
    }

    // the reflection tier's own statements that a shape belongs to the generator: NotSupportedException for
    // MessagePackUnknownMembers and AllowCircularReferences, a registration the generated factory performs (surrogate
    // targets, type-level [MessagePackFormatter]) that a reflection-only chain therefore lacks, and the generator's
    // compile-time expression form of [MessagePackFormatter] arguments
    static bool IsGeneratorOnly(Exception ex) => ex switch
    {
        NotSupportedException => true,
        InvalidOperationException { Message: var m } when m.Contains(" is not found in ", StringComparison.Ordinal) => true,
        MessagePackSerializationException { Message: var m } when m.Contains("[MessagePackFormatter]", StringComparison.Ordinal) => true,
        _ => false,
    };

    static string Head(string message)
    {
        var end = message.IndexOfAny([':', ';', '.'], Math.Min(40, message.Length));
        return end < 0 ? message : message[..end];
    }

    // builds a value of a type from its public surface: constructor parameters, then settable members, with the
    // BCL leaves drawn from the seeded Random; depth-limited so self-referential shapes end in null
    sealed class ValueBuilder(Random random)
    {
        const int MaxDepth = 3;

        public object? Build(Type type, int depth)
        {
            if (type == typeof(string)) return random.Next(4) == 0 ? "" : "s" + random.Next(1000);
            if (type == typeof(bool)) return random.Next(2) == 0;
            if (type == typeof(byte)) return (byte)random.Next(256);
            if (type == typeof(sbyte)) return (sbyte)random.Next(-128, 128);
            if (type == typeof(short)) return (short)random.Next(short.MinValue, short.MaxValue);
            if (type == typeof(ushort)) return (ushort)random.Next(ushort.MaxValue);
            if (type == typeof(int)) return random.Next(4) == 0 ? random.Next() : random.Next(-300, 300);
            if (type == typeof(uint)) return (uint)random.Next();
            if (type == typeof(long)) return random.NextInt64();
            if (type == typeof(ulong)) return (ulong)random.NextInt64();
            if (type == typeof(float)) return (float)random.NextDouble() * 100;
            if (type == typeof(double)) return random.NextDouble() * 1000;
            if (type == typeof(decimal)) return (decimal)random.Next(-100000, 100000) / 100m;
            if (type == typeof(char)) return (char)random.Next('a', 'z' + 1);
            if (type == typeof(DateTime)) return new DateTime(2000 + random.Next(30), 1 + random.Next(12), 1 + random.Next(28), random.Next(24), random.Next(60), random.Next(60), DateTimeKind.Utc);
            if (type == typeof(DateTimeOffset)) return new DateTimeOffset(2000 + random.Next(30), 1 + random.Next(12), 1 + random.Next(28), random.Next(24), 0, 0, TimeSpan.FromHours(random.Next(-5, 6)));
            if (type == typeof(TimeSpan)) return TimeSpan.FromSeconds(random.Next(100000));
            if (type == typeof(Guid)) return new Guid(Enumerable.Range(0, 16).Select(_ => (byte)random.Next(256)).ToArray());
            if (type == typeof(byte[])) return Enumerable.Range(0, random.Next(4)).Select(_ => (byte)random.Next(256)).ToArray();
            if (type == typeof(object)) return random.Next(3) switch { 0 => random.Next(100), 1 => "o", _ => null };
            if (type.IsEnum)
            {
                var values = Enum.GetValues(type);
                return values.Length == 0 ? Enum.ToObject(type, 0) : values.GetValue(random.Next(values.Length));
            }
            if (Nullable.GetUnderlyingType(type) is { } underlying)
            {
                return random.Next(3) == 0 ? null : Build(underlying, depth);
            }
            if (depth > MaxDepth)
            {
                return type.IsValueType ? Activator.CreateInstance(type) : null;
            }
            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                {
                    return null;
                }
                var element = type.GetElementType()!;
                var array = Array.CreateInstance(element, random.Next(3));
                for (int i = 0; i < array.Length; i++)
                {
                    array.SetValue(Build(element, depth + 1), i);
                }
                return array;
            }
            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                var arguments = type.GetGenericArguments();
                if (definition == typeof(List<>) || definition == typeof(IList<>) || definition == typeof(IReadOnlyList<>) || definition == typeof(IEnumerable<>)
                    || definition == typeof(ICollection<>) || definition == typeof(IReadOnlyCollection<>))
                {
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(arguments))!;
                    for (int i = random.Next(3); i > 0; i--)
                    {
                        list.Add(Build(arguments[0], depth + 1));
                    }
                    return list;
                }
                if (definition == typeof(HashSet<>) || definition == typeof(ISet<>) || definition == typeof(IReadOnlySet<>))
                {
                    var set = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(arguments))!;
                    var add = set.GetType().GetMethod("Add")!;
                    for (int i = random.Next(3); i > 0; i--)
                    {
                        add.Invoke(set, [Build(arguments[0], depth + 1)]);
                    }
                    return set;
                }
                if (definition == typeof(Dictionary<,>) || definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
                {
                    var dictionary = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(arguments))!;
                    for (int i = random.Next(3); i > 0; i--)
                    {
                        var key = Build(arguments[0], depth + 1);
                        if (key is not null && !dictionary.Contains(key))
                        {
                            dictionary[key] = Build(arguments[1], depth + 1);
                        }
                    }
                    return dictionary;
                }
                if (definition == typeof(KeyValuePair<,>))
                {
                    return Activator.CreateInstance(type, Build(arguments[0], depth + 1), Build(arguments[1], depth + 1));
                }
            }

            // a union root: one of its declared cases
            var cases = type.GetCustomAttributes<UnionTagAttribute>().Select(static a => a.CaseType).OfType<Type>().Where(static c => !c.IsAbstract && !c.IsGenericTypeDefinition).ToArray();
            if (cases.Length > 0)
            {
                return Build(cases[random.Next(cases.Length)], depth + 1);
            }
            if (type.IsAbstract || type.IsInterface)
            {
                return null;
            }
            if (type.GetCustomAttribute<MessagePackObjectAttribute>() is null && !type.IsValueType)
            {
                return null; // something no tier serializes by default; stays null
            }
            return Construct(type, depth);
        }

        object Construct(Type type, int depth)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var constructor = constructors.FirstOrDefault(static c => c.GetCustomAttribute<SerializationConstructorAttribute>() is not null)
                ?? constructors.FirstOrDefault(static c => c.GetParameters().Length == 0)
                ?? constructors.OrderByDescending(static c => c.GetParameters().Length).FirstOrDefault();
            object instance;
            var bound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (constructor is null)
            {
                instance = Activator.CreateInstance(type) ?? throw new InvalidOperationException("no constructor");
            }
            else
            {
                var parameters = constructor.GetParameters();
                var arguments = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    arguments[i] = Build(parameters[i].ParameterType, depth + 1);
                    bound.Add(parameters[i].Name!);
                }
                instance = constructor.Invoke(arguments);
            }

            var mapMode = type.GetCustomAttribute<MessagePackObjectAttribute>()?.KeyAsPropertyName == true;
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.SetMethod is not { IsPublic: true } || property.GetIndexParameters().Length != 0 || bound.Contains(property.Name)
                    || property.GetCustomAttribute<IgnoreMemberAttribute>() is not null
                    || (property.GetCustomAttribute<KeyAttribute>() is null && !mapMode))
                {
                    continue;
                }
                property.SetValue(instance, Build(property.PropertyType, depth + 1));
            }
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.IsInitOnly || bound.Contains(field.Name) || field.GetCustomAttribute<IgnoreMemberAttribute>() is not null
                    || (field.GetCustomAttribute<KeyAttribute>() is null && !mapMode))
                {
                    continue;
                }
                field.SetValue(instance, Build(field.FieldType, depth + 1));
            }
            return instance;
        }
    }
}

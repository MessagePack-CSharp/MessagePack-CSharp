using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using MessagePack.Generated;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SerializerFoundation;
using Xunit;

namespace MessagePack.Tests;

// A generated factory that serves many types dispatches CreateFormatter through hash buckets instead of one chain of
// `if (type == typeof(X)) return new XFormatter();` (IL2CPP initializes every type a method references on its first
// call, so the chain made the first lookup of any type pay for every formatter of the assembly). The bucket key is
// computed twice: by the generator from the symbol, and by the generated code from the Type. A type missing from its
// bucket is still found by the walk over the others, so a disagreement between the two would not fail anything; it
// would only bring the cost back. These tests are what notices.
public class FactoryBucketDispatchTests
{
    // this assembly's own factory: hundreds of types of every shape the suite declares (nested types, closed generics,
    // arrays, tuples, Nullable, enums, surrogate targets, type-level formatters)
    [Fact]
    public void EveryTypeThisAssemblyRegisters_IsFoundInItsOwnBucket()
    {
        var factory = GeneratedMessagePackFormatterFactory.Instance;
        var factoryType = typeof(GeneratedMessagePackFormatterFactory);
        const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        var hash = factoryType.GetMethod("MessagePackTypeHash", PrivateStatic);
        Assert.NotNull(hash); // absent = this assembly's factory fell under the bucketing threshold, and the test covers nothing
        var bucketCount = (int)factoryType.GetField("MessagePackBucketCount", PrivateStatic)!.GetRawConstantValue()!;
        var createInBucket = factoryType.GetMethod("MessagePackCreateFormatterInBucket", PrivateStatic)!
            .MakeGenericMethod(typeof(CompatibleArrayPoolListWriteBuffer), typeof(CompatibleReadOnlySpanReadBuffer));

        var checkedTypes = 0;
        var strays = new List<string>();
        foreach (var type in RegisteredTypes(factory))
        {
            object? expected;
            try
            {
                expected = factory.CreateFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer>(type);
            }
            catch (Exception)
            {
                continue; // a type-level [MessagePackFormatter] factory of the suite that refuses this pair
            }
            if (expected is null)
            {
                continue;
            }
            var bucket = (int)((uint)hash.Invoke(null, [type])! % (uint)bucketCount);
            var found = createInBucket.Invoke(null, [bucket, type]);
            if (found is null)
            {
                strays.Add(type.ToString());
            }
            else
            {
                Assert.Equal(expected.GetType(), found.GetType());
            }
            checkedTypes++;
        }

        Assert.True(strays.Count == 0, "not in the bucket their runtime hash selects: " + string.Join(", ", strays));
        Assert.True(checkedTypes > 200, $"only {checkedTypes} types were checked");
    }

    [Fact]
    public void ATypeTheFactoryDoesNotServe_StaysNull()
    {
        // the miss path: every bucket is asked, none answers
        Assert.Null(GeneratedMessagePackFormatterFactory.Instance.CreateFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer>(typeof(FactoryBucketDispatchTests)));
        Assert.Null(GeneratedMessagePackFormatterFactory.Instance.CreateFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer>(typeof(List<FactoryBucketDispatchTests[]>)));
    }

    [Fact]
    public void SmallFactory_KeepsTheSingleChain()
    {
        var factory = GenerateFactory(Types(3));
        Assert.DoesNotContain("MessagePackTypeHash", factory, StringComparison.Ordinal);
        Assert.DoesNotContain("MessagePackBucket", factory, StringComparison.Ordinal);
        Assert.Contains("if (type == typeof(global::Bucketed.T0)) return new", factory, StringComparison.Ordinal);
    }

    [Fact]
    public void LargeFactory_SplitsTheChainIntoBuckets()
    {
        const int Count = 100;
        var factory = GenerateFactory(Types(Count));
        Assert.Contains("static uint MessagePackTypeHash(global::System.Type type)", factory, StringComparison.Ordinal);
        Assert.Contains("const int MessagePackBucketCount = ", factory, StringComparison.Ordinal);
        // every type is still constructed exactly once, in some bucket
        for (int i = 0; i < Count; i++)
        {
            var construction = $"if (type == typeof(global::Bucketed.T{i})) return new";
            var first = factory.IndexOf(construction, StringComparison.Ordinal);
            Assert.True(first >= 0, construction);
            Assert.Equal(-1, factory.IndexOf(construction, first + 1, StringComparison.Ordinal));
        }
    }

    static string Types(int count)
    {
        var source = new StringBuilder("using MessagePack;\nnamespace Bucketed {\n");
        for (int i = 0; i < count; i++)
        {
            source.Append("[MessagePackObject] public partial class T").Append(i).Append(" { [Key(0)] public int Value { get; set; } }\n");
        }
        return source.Append('}').ToString();
    }

    static string GenerateFactory(string source)
    {
        var compilation = AnalyzerTestHost.CreateCompilation(source, "BucketedApp");
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        var result = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _, CancellationToken.None).GetRunResult();
        Assert.Empty(result.Diagnostics);
        Assert.Empty(updated.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));
        return result.GeneratedTrees.Single(static t => t.FilePath.EndsWith("GeneratedMessagePackFormatterFactory.g.cs", StringComparison.Ordinal)).ToString();
    }

    // what this assembly's module initializer registered with its factory, read from the registry's table
    static List<Type> RegisteredTypes(MessagePackFormatterFactory factory)
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var registrations = typeof(SourceGeneratedFormatterFactory).GetField("registrations", Any)!.GetValue(SourceGeneratedFormatterFactory.Instance)!;
        var factories = (ConcurrentDictionary<Type, MessagePackFormatterFactory>)registrations.GetType().GetField("Factories", Any)!.GetValue(registrations)!;
        return [.. factories.Where(pair => ReferenceEquals(pair.Value, factory)).Select(static pair => pair.Key)];
    }
}

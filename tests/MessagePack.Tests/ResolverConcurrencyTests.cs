using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SerializerFoundation;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;

namespace MessagePack.Tests;

// The resolver builds a formatter graph under one lock, registers the whole graph when control returns to the root,
// and publishes the table copy-on-write (Volatile.Write) for lock-free reads. These tests put the first construction
// under contention: several threads released together by a Barrier onto a NEW resolver, asking for the same root,
// for overlapping roots (a cycle split across threads), through the serializer entry points, through the
// compatibility reroute (the graph is discarded and rebuilt over the Compatible pair; the event fires outside the
// lock), and through the registry's module-initializer miss path. Each run repeats on fresh resolvers because a
// race that shows once in a hundred is still a race.
public class ResolverConcurrencyTests
{
    const int Threads = 8;
    const int Rounds = 20;

    static readonly MessagePackFormatterFactory CompatOnlyChain = MessagePackFormatterFactory.Combine(
        new CompatiblePairOnlyFactory(), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance);

    // runs work(threadIndex) on Threads dedicated threads released by one Barrier; the first failure is rethrown with
    // its own stack
    static T[] Race<T>(Func<int, T> work)
    {
        using var barrier = new Barrier(Threads);
        var results = new T[Threads];
        var failures = new ConcurrentQueue<ExceptionDispatchInfo>();
        var threads = new Thread[Threads];
        for (int i = 0; i < Threads; i++)
        {
            var index = i;
            threads[i] = new Thread(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    results[index] = work(index);
                }
                catch (Exception ex)
                {
                    failures.Enqueue(ExceptionDispatchInfo.Capture(ex));
                }
            });
            threads[i].Start();
        }
        foreach (var thread in threads)
        {
            Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "a thread did not finish: the resolver deadlocked");
        }
        if (failures.TryDequeue(out var first))
        {
            first.Throw();
        }
        return results;
    }

    static IMessagePackFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T> Compat<T>(MessagePackFormatterResolver resolver)
        => resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T>();

    static void SameRoot<T>()
    {
        for (int round = 0; round < Rounds; round++)
        {
            var resolver = new MessagePackFormatterResolver(MessagePackFormatterFactory.Default);
            var formatters = Race(_ => (object)Compat<T>(resolver));
            var published = Compat<T>(resolver);
            Assert.All(formatters, f => Assert.Same(published, f));
        }
    }

    [Fact]
    public void SameRoot_EveryThreadGetsTheOnePublishedFormatter()
    {
        // one slot per instantiation: the losers of the race must take the winner's formatter from the table, never
        // keep a private copy of the graph they built and discarded
        SameRoot<CircularTree>();
        SameRoot<GenAnimal>();
        SameRoot<List<CircularPart>>();
        SameRoot<Dictionary<string, GenAnimal>>();
    }

    [Fact]
    public void OverlappingRoots_EveryMemberOfTheCycleIsPublished()
    {
        // CircularOwner <-> CircularPet, and CircularPart <-> CircularBranch/CircularLeafPart: threads enter the same
        // cycle from different roots at once, so each thread's graph contains the other's root as a nested node
        Type[] roots = [typeof(CircularOwner), typeof(CircularPet), typeof(CircularPart), typeof(CircularBranch), typeof(CircularLeafPart)];
        for (int round = 0; round < Rounds; round++)
        {
            var resolver = new MessagePackFormatterResolver(MessagePackFormatterFactory.Default);
            var formatters = Race(i => roots[i % roots.Length] switch
            {
                var t when t == typeof(CircularOwner) => (object)Compat<CircularOwner>(resolver),
                var t when t == typeof(CircularPet) => Compat<CircularPet>(resolver),
                var t when t == typeof(CircularPart) => Compat<CircularPart>(resolver),
                var t when t == typeof(CircularBranch) => Compat<CircularBranch>(resolver),
                _ => Compat<CircularLeafPart>(resolver),
            });
            for (int i = 0; i < Threads; i++)
            {
                object now = roots[i % roots.Length] switch
                {
                    var t when t == typeof(CircularOwner) => Compat<CircularOwner>(resolver),
                    var t when t == typeof(CircularPet) => Compat<CircularPet>(resolver),
                    var t when t == typeof(CircularPart) => Compat<CircularPart>(resolver),
                    var t when t == typeof(CircularBranch) => Compat<CircularBranch>(resolver),
                    _ => Compat<CircularLeafPart>(resolver),
                };
                Assert.Same(now, formatters[i]);
            }
        }
    }

    [Fact]
    public void EntryPoints_ConcurrentFirstUse_RoundTripsOnEveryThread()
    {
        // the serializer's own buffer pair, with nested formatters (List, Dictionary, union cases, cycles) all being
        // constructed for the first time by competing threads
        for (int round = 0; round < Rounds; round++)
        {
            var options = new V4Options(new MessagePackFormatterResolver(MessagePackFormatterFactory.Default));
            Race(i =>
            {
                switch (i % 3)
                {
                    case 0:
                    {
                        var owner = new CircularOwner { Name = "o" + i };
                        owner.Pet = new CircularPet { Name = "p" + i, Owner = owner };
                        var back = V4.Deserialize<CircularOwner>(V4.Serialize(owner, options), options)!;
                        Assert.Equal("o" + i, back.Name);
                        Assert.Same(back, back.Pet!.Owner);
                        return 0;
                    }
                    case 1:
                    {
                        GenAnimal[] animals = [new GenDog { Name = "d", BarkCount = i }, new GenCat { Name = "c", Lives = i }];
                        var back = V4.Deserialize<GenAnimal[]>(V4.Serialize(animals, options), options)!;
                        Assert.Equal(i, Assert.IsType<GenDog>(back[0]).BarkCount);
                        Assert.Equal(i, Assert.IsType<GenCat>(back[1]).Lives);
                        return 0;
                    }
                    default:
                    {
                        var branch = new CircularBranch { Name = "b" };
                        branch.Parts = [new CircularLeafPart { Name = "l" + i, Parent = branch }];
                        var value = new Dictionary<string, CircularPart> { ["root"] = branch };
                        var back = V4.Deserialize<Dictionary<string, CircularPart>>(V4.Serialize(value, options), options)!;
                        var backBranch = Assert.IsType<CircularBranch>(back["root"]);
                        Assert.Same(backBranch, Assert.IsType<CircularLeafPart>(backBranch.Parts![0]).Parent);
                        return 0;
                    }
                }
            });
        }
    }

    [Fact]
    public void CompatibilityReroute_UnderContention_RoutesEveryThreadAndRaisesTheEventOutsideTheLock()
    {
        // a downlevel-only formatter in the chain: each root graph that reaches it is discarded, the root marked
        // CompatiblePairRequired and rebuilt over the Compatible pair, then CompatibilityFallback fires after the lock
        // is released. The handler serializes on another thread (a logger would), which deadlocked when the event was
        // raised inside the lock (F37)
        for (int round = 0; round < Rounds; round++)
        {
            var resolver = new MessagePackFormatterResolver(CompatOnlyChain);
            var options = new V4Options(resolver);
            var rerouted = new ConcurrentBag<Type>();
            resolver.CompatibilityFallback += type =>
            {
                rerouted.Add(type);
                var worker = Task.Run(() => V4.Serialize(new RoutedValue { X = -1 }, options));
                Assert.True(worker.Wait(TimeSpan.FromSeconds(10)), "the handler's worker thread waited on the resolver's lock");
            };
            Race(i =>
            {
                if (i % 2 == 0)
                {
                    Assert.Equal(i, V4.Deserialize<RoutedValue>(V4.Serialize(new RoutedValue { X = i }, options), options).X);
                }
                else
                {
                    var back = V4.Deserialize<List<RoutedValue>>(V4.Serialize(new List<RoutedValue> { new() { X = i }, new() { X = i + 1 } }, options), options)!;
                    Assert.Equal([i, i + 1], back.Select(static v => v.X));
                }
                return 0;
            });
            Assert.Contains(typeof(RoutedValue), rerouted);
            Assert.Contains(typeof(List<RoutedValue>), rerouted);
        }
    }

    [Fact]
    public void CompatibilityReroute_ThrowOnLegacyFormatter_EveryThreadSeesTheThrow()
    {
        // no thread may observe a half-built graph as success while another thread's root is being refused
        for (int round = 0; round < Rounds; round++)
        {
            var options = new V4Options(new MessagePackFormatterResolver(CompatOnlyChain) { ThrowOnLegacyFormatter = true });
            var messages = Race(i =>
            {
                // the refused root resolves to the not-found formatter, whose message names the Compatible pair
                // (CompatibilityFallbackRoutingTests.ThrowOnLegacyFormatterRestoresTheThrow)
                var ex = Assert.Throws<InvalidOperationException>(() => i % 2 == 0
                    ? V4.Serialize(new RoutedValue { X = i }, options)
                    : V4.Serialize(new List<RoutedValue> { new() { X = i } }, options));
                return ex.Message;
            });
            Assert.All(messages, static m => Assert.Contains("Compatible (non-ref-struct) buffer pair", m));
        }
    }

    [Fact]
    public void RegistryMiss_ConcurrentTypeOnlyFirstContact_RunsTheModuleInitializerAndServesEveryThread()
    {
        // the type-only path (a Type from Type.GetType / assembly.GetType) does not run the type's module initializer,
        // so the registry runs it on a miss (LazyModuleInitializerTests). Here eight threads make that first contact
        // at once on a freshly loaded assembly: the CLR runs the module constructor once, every other thread must
        // find the registration afterwards instead of reporting the type as not found
        var compilation = AnalyzerTestHost.CreateCompilation("""
            using MessagePack;
            namespace Lazy
            {
                [MessagePackObject] public class Poco { [Key(0)] public int Value { get; set; } }
                [MessagePackObject] public class Holder { [Key(0)] public Poco? Inner { get; set; } [Key(1)] public Poco[]? Many { get; set; } }
            }
            """, "LazyRace" + Guid.NewGuid().ToString("N"));
        var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
        var driver = CSharpGeneratorDriver.Create([new MessagePack.SourceGenerator.Generators.MessagePackGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var generatorDiagnostics);
        Assert.Empty(generatorDiagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error));
        using var stream = new MemoryStream();
        var emitted = updated.Emit(stream);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        // loaded, module constructor NOT run (CompileAndLoad would run it)
        var assembly = System.Reflection.Assembly.Load(stream.ToArray());
        var poco = assembly.GetType("Lazy.Poco")!;
        var holder = assembly.GetType("Lazy.Holder")!;

        // registry only, no reflection tail: a lost registration is "not found", not a silent reflection fallback
        var options = new V4Options(new MessagePackFormatterResolver([SourceGeneratedFormatterFactory.Instance, BuiltInFormatterFactory.Instance]));
        byte[] pocoPayload = [0x91, 0x2A];                              // [42]
        byte[] holderPayload = [0x92, 0x91, 0x07, 0x92, 0x91, 0x01, 0x91, 0x02]; // [[7], [[1], [2]]]
        var values = Race(i => i % 2 == 0
            ? (int)poco.GetProperty("Value")!.GetValue(V4.Deserialize(poco, pocoPayload, options)!)!
            : ((Array)holder.GetProperty("Many")!.GetValue(V4.Deserialize(holder, holderPayload, options)!)!).Length);
        for (int i = 0; i < Threads; i++)
        {
            Assert.Equal(i % 2 == 0 ? 42 : 2, values[i]);
        }
    }
}

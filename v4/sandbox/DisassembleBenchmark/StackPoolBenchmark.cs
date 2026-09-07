using System.Buffers;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using SerializerFoundation;
using MessagePack;
using MessagePack.Formatters;

// StackFormatter.Deserialize must buffer the whole payload (stream is top-first, a stack
// rebuilds bottom-first) — today that temp is `new T[count]` per call, so even the
// populate path allocates. Candidate: ArrayPool<T>.Shared for the temp.
//
// Correctness constraint that shapes the pooled candidate: a rented slot may still hold
// a PREVIOUS renter's object, and our Deserialize(ref slot) contract treats a non-null
// incoming ref as a populate target — handing out rented slots by ref would resurrect
// pooled garbage into user data. So the pooled variant deserializes into a local and
// stores, and returns with clearArray for ref-containing T (don't retain user objects
// in the pool). The fresh path also loses the Stack<T>(T[]) memcpy ctor (a rented array
// is longer than count, the ctor would copy the garbage tail) and pays per-element Push
// into a Stack<T>(count) instead.
public sealed class PooledStackFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, Stack<T>?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        formatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, Stack<T>? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var f = formatter;
        state.Enter();
        buffer.WriteArrayHeader(value.Count);
        foreach (var item in value)
        {
            f.Serialize(ref buffer, ref state, item);
        }
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref Stack<T>? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadArrayHeader();

        var f = formatter;
        state.Enter();

        var temp = ArrayPool<T>.Shared.Rent(count);
        for (int i = 0; i < count; i++)
        {
            T item = default!;
            f.Deserialize(ref buffer, ref state, ref item);
            temp[count - 1 - i] = item;
        }

        Stack<T> result;
        if (value != null)
        {
            result = value;
            result.Clear();
            for (int i = 0; i < count; i++)
            {
                result.Push(temp[i]);
            }
        }
        else
        {
            // ArraySegment wrap recovers the ICollection<T>.CopyTo (memcpy) ctor path over
            // the rented prefix: one box + three interface calls total, instead of per-element Push
            result = new Stack<T>(new ArraySegment<T>(temp, 0, count));
        }

        // no try/finally: a malformed-payload throw just drops the rented array to the GC
        // (a lost pooling opportunity, not a leak)
        ArrayPool<T>.Shared.Return(temp, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());

        value = result;
        state.Exit();
    }
}

public sealed partial class PooledStackFormatterFactory<T> : MessagePackFormatterFactory
{
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        if (type == typeof(Stack<T>))
        {
            return new PooledStackFormatter<TWriteBuffer, TReadBuffer, T>();
        }
        return null;
    }
}

public class StackPoolIntBenchmark
{
    byte[] payload = default!;
    MessagePackSerializerOptions defaultOptions = default!;
    MessagePackSerializerOptions pooledOptions = default!;
    Stack<int>? populateTarget;

    [Params(16, 1000, 100_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        defaultOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        pooledOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([new PooledStackFormatterFactory<int>(), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        var rand = new Random(42);
        var stack = new Stack<int>();
        for (int i = 0; i < N; i++) stack.Push(rand.Next());
        payload = MessagePackSerializer.Serialize(stack, defaultOptions);
        populateTarget = new Stack<int>(N);
    }

    [Benchmark(Baseline = true)]
    public Stack<int>? Fresh_New() => MessagePackSerializer.Deserialize<Stack<int>?>(payload, defaultOptions);

    [Benchmark]
    public Stack<int>? Fresh_Pooled() => MessagePackSerializer.Deserialize<Stack<int>?>(payload, pooledOptions);

    [Benchmark]
    public Stack<int>? Populate_New()
    {
        var target = populateTarget;
        MessagePackSerializer.Deserialize(payload, ref target, defaultOptions);
        return target;
    }

    [Benchmark]
    public Stack<int>? Populate_Pooled()
    {
        var target = populateTarget;
        MessagePackSerializer.Deserialize(payload, ref target, pooledOptions);
        return target;
    }
}

// ref-type elements: the pooled Return pays an O(count) clear, and the per-element cost
// (string decode + string alloc) dilutes the temp-allocation delta
public class StackPoolStringBenchmark
{
    byte[] payload = default!;
    MessagePackSerializerOptions defaultOptions = default!;
    MessagePackSerializerOptions pooledOptions = default!;
    Stack<string>? populateTarget;

    [Params(16, 1000, 100_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        defaultOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        pooledOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver([new PooledStackFormatterFactory<string>(), BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        var stack = new Stack<string>();
        for (int i = 0; i < N; i++) stack.Push($"str{i}");
        payload = MessagePackSerializer.Serialize(stack, defaultOptions);
        populateTarget = new Stack<string>(N);
    }

    [Benchmark(Baseline = true)]
    public Stack<string>? Fresh_New() => MessagePackSerializer.Deserialize<Stack<string>?>(payload, defaultOptions);

    [Benchmark]
    public Stack<string>? Fresh_Pooled() => MessagePackSerializer.Deserialize<Stack<string>?>(payload, pooledOptions);

    [Benchmark]
    public Stack<string>? Populate_New()
    {
        var target = populateTarget;
        MessagePackSerializer.Deserialize(payload, ref target, defaultOptions);
        return target;
    }

    [Benchmark]
    public Stack<string>? Populate_Pooled()
    {
        var target = populateTarget;
        MessagePackSerializer.Deserialize(payload, ref target, pooledOptions);
        return target;
    }
}

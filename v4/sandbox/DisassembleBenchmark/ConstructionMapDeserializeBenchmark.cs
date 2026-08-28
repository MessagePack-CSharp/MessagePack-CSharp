using BenchmarkDotNet.Attributes;
using MessagePack;

// A/B for the ObjectEmitter dual-loop split: a UseConstruction reference type emits BOTH a populate
// read loop (`if (value is not null)`) and a construction read loop into one NoInlining Deserialize.
// The ArrayDeserializeShapeBenchmark round found that an unexecuted sibling block in the same method
// can slow the executed path (poisoning); the split moves each loop into its own NoInlining helper so
// the fresh path compiles without the populate automata in its body.
//
// Measure this class BEFORE and AFTER the generator change (2 runs). SettableOrder (plain setters,
// single-loop emission, unaffected by the change) is the drift control between the runs.
//
//   DeserializeFresh    - value=null entry: the construction loop, the hot case the split targets
//   DeserializePopulate - caller-supplied instance: the populate loop (init-only members skip)
//   DeserializeControl  - the settable twin, must not move between runs
[MessagePackObject(true)]
public class ConstructionOrder
{
    public string OrderId { get; }
    public int Quantity { get; }
    public double Price { get; }
    public string CustomerName { get; }
    public long Timestamp { get; }
    public bool Expedited { get; }
    public string Category { get; }
    public int Priority { get; }

    public ConstructionOrder(string orderId, int quantity, double price, string customerName, long timestamp, bool expedited, string category, int priority)
    {
        OrderId = orderId;
        Quantity = quantity;
        Price = price;
        CustomerName = customerName;
        Timestamp = timestamp;
        Expedited = expedited;
        Category = category;
        Priority = priority;
    }
}

[MessagePackObject(true)]
public class SettableOrder
{
    public string OrderId { get; set; } = "";
    public int Quantity { get; set; }
    public double Price { get; set; }
    public string CustomerName { get; set; } = "";
    public long Timestamp { get; set; }
    public bool Expedited { get; set; }
    public string Category { get; set; } = "";
    public int Priority { get; set; }
}

public class ConstructionMapDeserializeBenchmark
{
    byte[] payload = null!;
    byte[] controlPayload = null!;
    ConstructionOrder populateTarget = null!;

    [GlobalSetup]
    public void Setup()
    {
        var order = new ConstructionOrder("ORD-2026-08", 42, 129.95, "Yoshifumi Kawai", 638_600_000_000_000_000, true, "electronics", 3);
        payload = MessagePackSerializer.Serialize(order);
        controlPayload = MessagePackSerializer.Serialize(new SettableOrder
        {
            OrderId = order.OrderId, Quantity = order.Quantity, Price = order.Price, CustomerName = order.CustomerName,
            Timestamp = order.Timestamp, Expedited = order.Expedited, Category = order.Category, Priority = order.Priority,
        });
        populateTarget = new ConstructionOrder("", 0, 0, "", 0, false, "", 0);
        Verify();
    }

    public void Verify()
    {
        var fresh = MessagePackSerializer.Deserialize<ConstructionOrder>(payload)!;
        var expected = new ConstructionOrder("ORD-2026-08", 42, 129.95, "Yoshifumi Kawai", 638_600_000_000_000_000, true, "electronics", 3);
        if (fresh.OrderId != expected.OrderId || fresh.Quantity != expected.Quantity || fresh.Price != expected.Price
            || fresh.CustomerName != expected.CustomerName || fresh.Timestamp != expected.Timestamp
            || fresh.Expedited != expected.Expedited || fresh.Category != expected.Category || fresh.Priority != expected.Priority)
        {
            throw new InvalidOperationException("ConstructionOrder fresh roundtrip mismatch");
        }

        // populate semantics: a caller-supplied instance is kept by reference, and get-only
        // (construction-only) members are unreachable through setters, so they must survive unchanged
        var target = new ConstructionOrder("KEEP", -1, -2, "KEEP", -3, false, "KEEP", -4);
        var byRef = target;
        MessagePackSerializer.Deserialize(payload, ref byRef);
        if (!ReferenceEquals(byRef, target) || target.OrderId != "KEEP" || target.Quantity != -1)
        {
            throw new InvalidOperationException("ConstructionOrder populate semantics violated");
        }

        var control = MessagePackSerializer.Deserialize<SettableOrder>(controlPayload)!;
        if (control.OrderId != expected.OrderId || control.Priority != expected.Priority)
        {
            throw new InvalidOperationException("SettableOrder control roundtrip mismatch");
        }
    }

    [Benchmark(Baseline = true)]
    public ConstructionOrder? DeserializeFresh()
    {
        return MessagePackSerializer.Deserialize<ConstructionOrder>(payload);
    }

    [Benchmark]
    public ConstructionOrder? DeserializePopulate()
    {
        var target = populateTarget;
        MessagePackSerializer.Deserialize(payload, ref target);
        return target;
    }

    [Benchmark]
    public SettableOrder? DeserializeControl()
    {
        return MessagePackSerializer.Deserialize<SettableOrder>(controlPayload);
    }
}

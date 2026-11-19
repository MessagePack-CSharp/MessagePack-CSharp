// Round-trips generic shapes through the default chain in a trimmed (non-AOT) publish. GenericFormatterFactory and the
// other GenericFormatterFactoryBase tiers close an open factory definition with MakeGenericType and construct it with
// Activator.CreateInstance; if the trimmer removes the definition's constructors, the first use fails with
// MissingMethodException. Exits non-zero when any shape fails.
using MessagePack;
using MessagePack.TrimmedTest;

var options = MessagePackSerializerOptions.Default;
var item = new TrimmedItem { Id = 1, Name = "one" };
var items = new[] { item, new TrimmedItem { Id = 2, Name = "two" } };
var failures = 0;

// GenericFormatterFactory: collections, dictionaries, Nullable, enums and tuples closed over a user type
Check<IReadOnlyList<TrimmedItem>>(items);
Check<IReadOnlyCollection<TrimmedItem>>(items);
Check<IList<TrimmedItem>>(items);
Check<ICollection<TrimmedItem>>(items);
Check<IEnumerable<TrimmedItem>>(items);
Check<List<TrimmedItem>>([.. items]);
Check<TrimmedItem[]>(items);
Check<TrimmedItem[,]>(new[,] { { item } });
Check<HashSet<TrimmedItem>>([item]);
Check<ISet<TrimmedItem>>(new HashSet<TrimmedItem> { item });
Check<Dictionary<string, TrimmedItem>>(new() { ["a"] = item });
Check<IDictionary<string, TrimmedItem>>(new Dictionary<string, TrimmedItem> { ["a"] = item });
Check<IReadOnlyDictionary<string, TrimmedItem>>(new Dictionary<string, TrimmedItem> { ["a"] = item });
Check<TrimmedPoint?>(new TrimmedPoint { X = 1, Y = 2 });
Check<TrimmedColor>(TrimmedColor.Green);
Check<List<TrimmedColor>>([TrimmedColor.Red, TrimmedColor.Green]);
Check<(int, TrimmedItem)>((1, item));
Check<KeyValuePair<string, TrimmedItem>>(new("a", item));

// the Type-based entry points construct their per-type entry the same way
CheckNonGeneric(typeof(IReadOnlyList<TrimmedItem>), items);
CheckNonGeneric(typeof(List<TrimmedItem>), new List<TrimmedItem>(items));

if (failures != 0)
{
    Console.WriteLine($"{failures} shape(s) failed in the trimmed publish.");
    return 1;
}

Console.WriteLine("All shapes round-tripped in the trimmed publish.");
return 0;

void Check<T>(T value)
{
    try
    {
        var bytes = MessagePackSerializer.Serialize(value, options);
        var back = MessagePackSerializer.Deserialize<T>(bytes, options);
        var again = MessagePackSerializer.Serialize(back, options);
        if (!bytes.AsSpan().SequenceEqual(again))
        {
            throw new InvalidOperationException("the round-tripped value serialized to different bytes");
        }
        Console.WriteLine($"ok    {typeof(T)}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL  {typeof(T)}: {ex.GetBaseException().GetType().Name}: {ex.GetBaseException().Message}");
    }
}

void CheckNonGeneric(Type type, object value)
{
    try
    {
        var bytes = MessagePackSerializer.Serialize(type, value, options);
        var back = MessagePackSerializer.Deserialize(type, bytes, options);
        var again = MessagePackSerializer.Serialize(type, back, options);
        if (!bytes.AsSpan().SequenceEqual(again))
        {
            throw new InvalidOperationException("the round-tripped value serialized to different bytes");
        }
        Console.WriteLine($"ok    {type} (Type-based)");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL  {type} (Type-based): {ex.GetBaseException().GetType().Name}: {ex.GetBaseException().Message}");
    }
}

namespace MessagePack.TrimmedTest
{
    [MessagePackObject]
    public class TrimmedItem
    {
        [Key(0)] public int Id { get; set; }
        [Key(1)] public string? Name { get; set; }
    }

    [MessagePackObject]
    public struct TrimmedPoint
    {
        [Key(0)] public int X { get; set; }
        [Key(1)] public int Y { get; set; }
    }

    public enum TrimmedColor
    {
        Red,
        Green,
    }
}

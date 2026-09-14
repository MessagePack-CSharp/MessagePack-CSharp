namespace MessagePack.Tests;

// The serializer's Type-keyed cache: add-only, copy-on-write under a lock, lock-free reads. Coverage: hits and misses,
// growth past the half-full point, add semantics, and readers running through a burst of adds without ever seeing a
// wrong or torn entry.
public class TypeKeyHashTableTests
{
    static readonly Type[] ManyTypes =
    [
        typeof(int), typeof(string), typeof(double), typeof(List<int>), typeof(Dictionary<string, int>), typeof(Guid),
        typeof(DateTime), typeof(byte[]), typeof(TypeKeyHashTableTests), typeof(Box), typeof(long), typeof(bool),
        typeof(decimal), typeof(int[]), typeof(string[]), typeof(object), typeof(float), typeof(short), typeof(char),
        typeof(List<string>), typeof(List<Box>), typeof(Dictionary<int, Box>), typeof(TimeSpan), typeof(Uri),
        typeof(Version), typeof(byte), typeof(sbyte), typeof(uint), typeof(ulong), typeof(ushort), typeof(nint), typeof(Type),
        typeof(Task), typeof(Task<int>), typeof(ValueTask), typeof(Exception), typeof(Random), typeof(Array), typeof(Enum),
    ];

    [Fact]
    public void AddsGrowsAndFinds()
    {
        var table = new TypeKeyHashTable<Box>(capacity: 2); // starts at 8 slots, grows several times below
        for (var i = 0; i < ManyTypes.Length; i++)
        {
            Assert.True(table.TryAdd(ManyTypes[i], new Box(i)));
        }
        for (var i = 0; i < ManyTypes.Length; i++)
        {
            Assert.True(table.TryGetValue(ManyTypes[i], out var box));
            Assert.Equal(i, box.Value);
        }
        Assert.False(table.TryGetValue(typeof(TypeKeyHashTable<Box>), out _));
        Assert.False(table.TryGetValue(typeof(Dictionary<Box, Box>), out _));
    }

    [Fact]
    public void AddSemantics()
    {
        var table = new TypeKeyHashTable<Box>();
        var first = new Box(1);
        var second = new Box(2);
        Assert.True(table.TryAdd(typeof(int), first));
        Assert.False(table.TryAdd(typeof(int), second)); // never displaces
        Assert.Same(first, table.GetOrAdd(typeof(int), second));
        Assert.Same(second, table.GetOrAdd(typeof(string), second));
        Assert.True(table.TryGetValue(typeof(int), out var box));
        Assert.Same(first, box);
    }

    [Fact]
    public async Task ReadersNeverSeeATornOrWrongEntry()
    {
        var table = new TypeKeyHashTable<Box>(capacity: 2);
        using var stop = new CancellationTokenSource();
        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            var spins = 0L;
            while (!stop.IsCancellationRequested)
            {
                for (var i = 0; i < ManyTypes.Length; i++)
                {
                    if (table.TryGetValue(ManyTypes[i], out var box))
                    {
                        Assert.Equal(i, box.Value); // an entry is either absent or complete and correct
                        Assert.Same(ManyTypes[i], box.Type);
                    }
                }
                spins++;
            }
            return spins;
        })).ToArray();

        // adds arrive while the readers run, each one publishing a new array
        for (var i = 0; i < ManyTypes.Length; i++)
        {
            table.TryAdd(ManyTypes[i], new Box(i) { Type = ManyTypes[i] });
            await Task.Delay(1);
        }
        stop.Cancel();
        var totals = await Task.WhenAll(readers);
        Assert.All(totals, spins => Assert.True(spins > 0));
        for (var i = 0; i < ManyTypes.Length; i++)
        {
            Assert.True(table.TryGetValue(ManyTypes[i], out var box));
            Assert.Equal(i, box.Value);
        }
    }

    sealed class Box
    {
        public Box(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public Type? Type { get; init; }
    }
}

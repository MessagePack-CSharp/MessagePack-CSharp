using SerializerFoundation;

namespace UltraMessagePack;

static class FormatterTypeIdCounter
{
    internal static int Next;
}

// Global per-instantiation id for the resolvers' formatter tables — the id is only the
// slot INDEX; the tables (and the formatters in them) are per-resolver instance state, so
// different resolvers holding different formatters for the same T never cross paths.
// The id population is every (TWriteBuffer, TReadBuffer, T) instantiation resolved
// process-wide (nested graph types included, times the buffer pairs actually used). A
// table grows to the highest global id its resolver touches, so tables are sparse across
// resolvers (8B/slot, bounded by watermark x resolver count) — negligible while resolvers
// stay few and singleton-like; if mass-produced resolvers ever become a supported pattern,
// chunk the table (object?[][]) instead
static class FormatterTypeId<TWriteBuffer, TReadBuffer, T>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
{
    public static readonly int Value = Interlocked.Increment(ref FormatterTypeIdCounter.Next) - 1;
}

// One cache: formatterTable, indexed by the global per-instantiation id, is both the hot
// read path and the sole published store. The construction machinery (gate + constructing,
// also id-keyed) dedups recursive formatter graphs, and writes table slots only after the
// whole graph finished Initialize — lock-free readers can never observe a partially-
// initialized formatter by construction, not by a promotion rule.
// The resolver is CONCRETE and sealed: all resolution policy lives in the factory it
// wraps (compose factories, don't derive resolvers) — this class is only the cache.
public sealed class MessagePackFormatterResolver
{
    object?[] formatterTable = [];

    // reentrant: nested Initialize resolution re-enters GetFormatterSlow (Lock and
    // Monitor are both reentrant, so the downlevel object flavor keeps the semantics)
#if NET9_0_OR_GREATER
    readonly Lock gate = new Lock();
#else
    readonly object gate = new object();
#endif
    readonly Dictionary<int, object> constructing = new();
    readonly IMessagePackFormatterFactory factory;

    public MessagePackFormatterResolver(IMessagePackFormatterFactory factory)
    {
        this.factory = factory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> GetFormatter<TWriteBuffer, TReadBuffer, T>()
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        var id = FormatterTypeId<TWriteBuffer, TReadBuffer, T>.Value;
        var table = formatterTable;
        if ((uint)id < (uint)table.Length)
        {
            var f = table[id];
            if (f != null)
            {
                // slot id is unique to this instantiation, so the stored object is always
                // an IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
                return Unsafe.As<IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>>(f);
            }
        }
        return GetFormatterSlow<TWriteBuffer, TReadBuffer, T>(id);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> GetFormatterSlow<TWriteBuffer, TReadBuffer, T>(int id)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        lock (gate)
        {
            // re-check: another thread may have published while we waited on the gate
            var table = formatterTable;
            if ((uint)id < (uint)table.Length && table[id] is { } published)
            {
                return (IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>)published;
            }

            // recursion during graph construction: hand out the in-construction instance
            if (constructing.TryGetValue(id, out var cycle))
            {
                return (IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>)cycle;
            }

            var isRoot = constructing.Count == 0;

            object result;
#if NET9_0_OR_GREATER
            var created = factory.CreateFormatter<TWriteBuffer, TReadBuffer>(typeof(T));
#else
            // downlevel interface surface is Type-based only (see IMessagePackFormatterFactory)
            var created = factory.CreateFormatter(typeof(TWriteBuffer), typeof(TReadBuffer), typeof(T));
#endif
            if (created != null)
            {
                // the factory interface is non-generic, so the T link is enforced HERE:
                // whatever it created must be a formatter for exactly this instantiation
                if (created is not IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> formatter)
                {
                    throw new InvalidOperationException($"The factory asked for type '{typeof(T).FullName}' created '{created.GetType().FullName}', which is not an IMessagePackFormatter for that type.");
                }

                constructing[id] = formatter;
                try
                {
                    formatter.Initialize(this); // may re-enter GetFormatter recursively
                }
                catch
                {
                    constructing.Clear(); // rollback whole partially-constructed graph
                    throw;
                }
                result = formatter;
            }
            else
            {
                var servableByFallbackPair = false;
#if NET9_0_OR_GREATER
                // the whole chain declined this pair; if it CAN serve the type over a
                // fallback pair, the cause is a downlevel-compiled factory that cannot
                // close over ref struct buffers — recorded for the Missing message.
                // the probe instance is discarded without Initialize (fresh-per-call contract)
                if (typeof(TWriteBuffer).IsByRefLike || typeof(TReadBuffer).IsByRefLike)
                {
                    try
                    {
                        servableByFallbackPair = factory.CreateFormatter(
                            typeof(CompatibleArrayPoolListWriteBuffer),
                            typeof(UnsafeReadOnlySpanReadBuffer),
                            typeof(T)) != null;
                    }
                    catch
                    {
                        // generated factories return null here, but hand-written ones may
                        // throw on unknown pairs — treat that as "no"
                    }
                }
#endif
                result = new MissingMessagePackFormatter<TWriteBuffer, TReadBuffer, T>(factory.GetType(), servableByFallbackPair);
                constructing[id] = result;
            }

            if (isRoot)
            {
                PublishConstructed();
            }

            return (IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>)result;
        }
    }

    // called with the whole graph initialized: grow once, write every slot, then publish
    // the table reference for the lock-free readers
    void PublishConstructed()
    {
        var required = 0;
        foreach (var id in constructing.Keys)
        {
            required = Math.Max(required, id + 1);
        }

        var table = formatterTable;
        if (required > table.Length)
        {
            var grown = new object?[Math.Max(table.Length * 2, required)];
            Array.Copy(table, grown, table.Length);
            table = grown;
        }
        foreach (var kv in constructing)
        {
            table[kv.Key] = kv.Value;
        }
        Volatile.Write(ref formatterTable, table);
        constructing.Clear();
    }
}

internal sealed partial class MissingMessagePackFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T>
{
    string resolverType;
    bool servableByFallbackPair;

    public MissingMessagePackFormatter(Type resolverType, bool servableByFallbackPair)
    {
        this.resolverType = resolverType.FullName ?? "";
        this.servableByFallbackPair = servableByFallbackPair;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T value)
    {
        throw new InvalidOperationException(BuildMessage());
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T value)
    {
        throw new InvalidOperationException(BuildMessage());
    }

    string BuildMessage()
    {
        var message = $"Type '{typeof(T).FullName}' is not found in {resolverType} (TWriteBuffer: {typeof(TWriteBuffer).FullName}, TReadBuffer: {typeof(TReadBuffer).FullName}).";
        if (servableByFallbackPair)
        {
            message += " The factory can create this formatter for fallback (non-ref-struct) buffer pairs: it is likely provided by a library compiled against a netstandard build of MessagePack, which cannot serve ref struct buffer pairs. That library must also target net10.0.";
        }
        return message;
    }
}
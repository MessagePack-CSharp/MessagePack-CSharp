namespace MessagePack;

// The resolver hands out one shared formatter per type, so a nested formatter cannot differ per use site through it.
// An owner that creates a container formatter itself (the generated code of [MessagePackObject(EnumAsString = true)],
// which writes the enums inside its members by name) supplies the nested formatters through these interfaces first, then
// calls Initialize, which resolves only the slots left empty.

/// <summary>
/// Implemented by the built-in single-element container formatters (arrays, lists, sets, <see cref="Nullable{T}"/>, ...).
/// An owner that creates the formatter itself, rather than taking the resolver's shared instance, supplies the element
/// formatter here before calling <see cref="IMessagePackFormatter{TWriteBuffer, TReadBuffer, T}.Initialize"/>, which then
/// keeps it.
/// </summary>
public interface ICollectionFormatterInitializer<TWriteBuffer, TReadBuffer, TElement>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
{
    /// <summary>Supplies the formatter of the elements.</summary>
    void Initialize(IMessagePackFormatter<TWriteBuffer, TReadBuffer, TElement> elementFormatter);
}

/// <summary>
/// Implemented by the built-in two-slot container formatters (dictionaries, <see cref="KeyValuePair{TKey, TValue}"/>,
/// and <c>PriorityQueue&lt;TElement, TPriority&gt;</c> as element and priority).
/// Like <see cref="ICollectionFormatterInitializer{TWriteBuffer, TReadBuffer, TElement}"/>, it is called before
/// <see cref="IMessagePackFormatter{TWriteBuffer, TReadBuffer, T}.Initialize"/>, which resolves a slot passed as null.
/// </summary>
public interface IKeyValueFormatterInitializer<TWriteBuffer, TReadBuffer, TKey, TValue>
    where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
    where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
    , allows ref struct
#endif
{
    /// <summary>Supplies the formatters of the keys and of the values; null leaves that slot to the resolver.</summary>
    void Initialize(IMessagePackFormatter<TWriteBuffer, TReadBuffer, TKey>? keyFormatter, IMessagePackFormatter<TWriteBuffer, TReadBuffer, TValue>? valueFormatter);
}

/// <summary>
/// Implemented by the <see cref="IEnumerable{T}"/> formatter next to
/// <see cref="ICollectionFormatterInitializer{TWriteBuffer, TReadBuffer, TElement}"/>: a source without a count has its
/// elements staged over the Compatible buffer pair before the array header can be written, and that staging takes an
/// element formatter of its own.
/// </summary>
public interface IStagedCollectionFormatterInitializer<TElement>
{
    /// <summary>Supplies the element formatter of the staging buffer.</summary>
    void InitializeStaged(IMessagePackFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, TElement> elementFormatter);
}

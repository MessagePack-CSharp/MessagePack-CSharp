using System.ComponentModel;

namespace MessagePack;

// The resolver hands out one shared formatter per type, so a nested formatter cannot differ per use site through it.
// An owner that creates a container formatter itself (the generated code of [MessagePackObject(EnumAsString = true)],
// which writes the enums inside its members by name) supplies the nested formatters through these interfaces first, then
// calls Initialize, which resolves only the slots left empty.
//
// Infrastructure, not an extension point: public because generated code in user assemblies calls them, hidden from
// IntelliSense for that reason. Only the built-in container formatters are ever asked for them (the generator goes by
// the formatter's name, the reflection tier by what GenericFormatterFactory creates), so implementing them on a
// formatter of your own has no effect.

/// <summary>
/// Infrastructure for generated code. Implemented by the built-in single-element container formatters (arrays, lists,
/// sets, <see cref="Nullable{T}"/>, ...). An owner that creates the formatter itself supplies the element formatter
/// here before calling <see cref="IMessagePackFormatter{TWriteBuffer, TReadBuffer, T}.Initialize"/>, which then keeps it.
/// </summary>
/// <remarks>
/// Call it only on an instance you created, and only before its <c>Initialize</c>. A formatter obtained from a resolver
/// is shared by every use of its type: replacing its element formatter changes all of them, without synchronization.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
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
/// Infrastructure for generated code. Implemented by the built-in two-slot container formatters (dictionaries,
/// <see cref="KeyValuePair{TKey, TValue}"/>, and <c>PriorityQueue&lt;TElement, TPriority&gt;</c> as element and priority).
/// Like <see cref="ICollectionFormatterInitializer{TWriteBuffer, TReadBuffer, TElement}"/>, it is called on an instance
/// the caller created, before <see cref="IMessagePackFormatter{TWriteBuffer, TReadBuffer, T}.Initialize"/>, which
/// resolves a slot passed as null; the same restrictions apply.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
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
/// Infrastructure for generated code. Implemented by the <see cref="IEnumerable{T}"/> formatter next to
/// <see cref="ICollectionFormatterInitializer{TWriteBuffer, TReadBuffer, TElement}"/>: a source without a count has its
/// elements staged over the Compatible buffer pair before the array header can be written, and that staging takes an
/// element formatter of its own. The same restrictions apply.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IStagedCollectionFormatterInitializer<TElement>
{
    /// <summary>Supplies the element formatter of the staging buffer.</summary>
    void InitializeStaged(IMessagePackFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, TElement> elementFormatter);
}

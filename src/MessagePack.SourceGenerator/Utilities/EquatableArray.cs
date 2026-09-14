using System.Collections;

namespace MessagePack.SourceGenerator;

/// <summary>
/// Value-equatable array wrapper: incremental pipeline models must compare by content,
/// or every edit invalidates the cache and re-runs emission for every type.
/// </summary>
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    readonly T[]? array;

    public EquatableArray(T[] array)
    {
        this.array = array;
    }

    public T[] AsArray() => array ?? [];

    public int Length => array?.Length ?? 0;

    public T this[int index] => AsArray()[index];

    public bool Equals(EquatableArray<T> other)
    {
        var a = AsArray();
        var b = other.AsArray();
        if (a.Length != b.Length)
        {
            return false;
        }
        for (int i = 0; i < a.Length; i++)
        {
            if (!a[i].Equals(b[i]))
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var item in AsArray())
        {
            hash = (hash * 31) + (item?.GetHashCode() ?? 0);
        }
        return hash;
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)AsArray()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

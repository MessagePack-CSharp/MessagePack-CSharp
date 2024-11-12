// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1127 // Generic type constraints must be on own line
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using System.Collections;

namespace MessagePack.SourceGenerator;

/// <summary>
/// Provides an immutable list implementation which implements sequence equality.
/// </summary>
public sealed class ImmutableEquatableArray<T> : IEquatable<ImmutableEquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    public static ImmutableEquatableArray<T> Empty { get; } = new ImmutableEquatableArray<T>(Array.Empty<T>());

    private readonly T[] values;

    public T this[int index] => values[index];

    public int Count => values.Length;

    public ImmutableEquatableArray(IEnumerable<T> values)
        => this.values = values.ToArray();

    public bool Equals(ImmutableEquatableArray<T>? other)
        => other is not null && ((ReadOnlySpan<T>)values).SequenceEqual(other.values);

    public override bool Equals(object? obj)
        => obj is ImmutableEquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        int hash = 0;
        foreach (T value in values)
        {
            hash = Combine(hash, value is null ? 0 : value.GetHashCode());
        }

        return hash;

        static int Combine(int h1, int h2)
        {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }

    public Enumerator GetEnumerator() => new Enumerator(values);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();

    public struct Enumerator
    {
        private readonly T[] values;
        private int index;

        internal Enumerator(T[] values)
        {
            this.values = values;
            index = -1;
        }

        public bool MoveNext()
        {
            int newIndex = index + 1;

            if ((uint)newIndex < (uint)values.Length)
            {
                index = newIndex;
                return true;
            }

            return false;
        }

        public readonly T Current => values[index];
    }
}

public static class ImmutableEquatableArray
{
    public static ImmutableEquatableArray<T> Empty<T>() where T : IEquatable<T>
        => ImmutableEquatableArray<T>.Empty;

    public static ImmutableEquatableArray<T> ToImmutableEquatableArray<T>(this IEnumerable<T> values) where T : IEquatable<T>
        => new(values);

    public static ImmutableEquatableArray<T> Create<T>(params T[] values) where T : IEquatable<T>
        => values is { Length: > 0 } ? new(values) : ImmutableEquatableArray<T>.Empty;
}

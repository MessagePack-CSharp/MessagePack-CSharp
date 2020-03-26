// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using MessagePackCompiler.CodeAnalysis;

namespace MessagePackCompiler.Generator
{
    public struct StringKeyULongGroupEnumerable : IEnumerable<ReadOnlyMemory<ValueTuple<byte[], MemberSerializationInfo>>>
    {
        private readonly ReadOnlyMemory<ValueTuple<byte[], MemberSerializationInfo>> source;
        private readonly int startIndexOfUlong;
        public readonly bool HasSingleGroup;

        public struct Enumerator : IEnumerator<ReadOnlyMemory<ValueTuple<byte[], MemberSerializationInfo>>>
        {
            private ReadOnlyMemory<ValueTuple<byte[], MemberSerializationInfo>> rest;
            private readonly int start;

            public Enumerator(ReadOnlyMemory<ValueTuple<byte[], MemberSerializationInfo>> source, int start)
            {
                Current = ReadOnlyMemory<(byte[], MemberSerializationInfo)>.Empty;
                rest = source;
                this.start = start;
            }

            public bool MoveNext()
            {
                if (rest.IsEmpty)
                {
                    return false;
                }

                var restSpan = rest.Span;

                ReadOnlySpan<byte> GetSpan(ReadOnlySpan<(byte[], MemberSerializationInfo)> span, int startIndex, int index)
                {
                    return span[index].Item1.AsSpan(startIndex << 3, 8);
                }

                var first = GetSpan(restSpan, start, 0);
                if (first.IsEmpty)
                {
                    return false;
                }

                var value = BinaryPrimitives.ReadUInt64LittleEndian(first);
                for (var index = 1; index < restSpan.Length; index++)
                {
                    var current = BinaryPrimitives.ReadUInt64LittleEndian(GetSpan(restSpan, start, index));
                    if (current == value)
                    {
                        continue;
                    }

                    Current = rest.Slice(0, index);
                    rest = rest.Slice(index);
                    return true;
                }

                Current = rest;
                rest = ReadOnlyMemory<(byte[], MemberSerializationInfo)>.Empty;

                return true;
            }

            public void Reset()
            {
                throw new InvalidOperationException();
            }

            public ReadOnlyMemory<(byte[], MemberSerializationInfo)> Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        public StringKeyULongGroupEnumerable(ReadOnlyMemory<(byte[], MemberSerializationInfo)> source, int startIndexOfUlong)
        {
            this.source = source;
            this.startIndexOfUlong = startIndexOfUlong;
            var span = source.Span;
            if (span.Length == 1)
            {
                this.HasSingleGroup = true;
            }
            else
            {
                var ulong0 = BinaryPrimitives.ReadUInt64LittleEndian(span[0].Item1.AsSpan(startIndexOfUlong << 3, 8));
                var ulongLast = BinaryPrimitives.ReadUInt64LittleEndian(span[span.Length - 1].Item1.AsSpan(startIndexOfUlong << 3, 8));
                HasSingleGroup = ulong0 == ulongLast;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(source, startIndexOfUlong);
        }

        IEnumerator<ReadOnlyMemory<(byte[], MemberSerializationInfo)>> IEnumerable<ReadOnlyMemory<(byte[], MemberSerializationInfo)>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

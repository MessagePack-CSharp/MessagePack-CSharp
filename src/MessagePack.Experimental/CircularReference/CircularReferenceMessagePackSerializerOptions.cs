// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace MessagePack
{
    public sealed class CircularReferenceMessagePackSerializerOptions : MessagePackSerializerOptions, IDisposable
    {
        private object[] referenceArray;

        private int referenceCount;

        private bool isInUse;

        public Span<object> ReferenceSpan => referenceArray.AsSpan(0, referenceCount);

        private CircularReferenceMessagePackSerializerOptions(MessagePackSerializerOptions options)
            : base(options)
        {
            referenceCount = 0;
            referenceArray = ArrayPool<object>.Shared.Rent(16);
            isInUse = false;
        }

        public static CircularReferenceMessagePackSerializerOptions Rent(MessagePackSerializerOptions options)
        {
            var element = table.GetOrCreateValue(options);
            lock (element)
            {
                return element.GetCircularOption(options);
            }
        }

        private static readonly ConditionalWeakTable<MessagePackSerializerOptions, WeakTableElement> table = new ConditionalWeakTable<MessagePackSerializerOptions, WeakTableElement>();

        private sealed class WeakTableElement
        {
            public WeakTableElement()
            {
                array = ArrayPool<CircularReferenceMessagePackSerializerOptions?>.Shared.Rent(16);
                Array.Clear(array, 0, array.Length);
            }

            private CircularReferenceMessagePackSerializerOptions?[] array;

            public CircularReferenceMessagePackSerializerOptions GetCircularOption(MessagePackSerializerOptions options)
            {
                for (var i = 0; i < array.Length; i++)
                {
                    ref var element = ref array[i];
                    if (element is null)
                    {
                        return element = new CircularReferenceMessagePackSerializerOptions(options) { isInUse = true };
                    }

                    if (!element.isInUse)
                    {
                        element.isInUse = true;
                        return element;
                    }
                }

                var pool = ArrayPool<CircularReferenceMessagePackSerializerOptions?>.Shared;
                var arrayLength = array.Length;

                var newArray = pool.Rent(arrayLength << 1);
                Array.Clear(newArray, arrayLength, newArray.Length);
                Array.Copy(array, newArray, arrayLength);
                Array.Clear(array, 0, arrayLength);
                pool.Return(array);
                array = newArray;

                var answer = array[arrayLength] = new CircularReferenceMessagePackSerializerOptions(options) { isInUse = true };
                return answer;
            }

            ~WeakTableElement()
            {
                for (var i = 0; i < array.Length; i++)
                {
                    var element = array[i];
                    if (element is null)
                    {
                        continue;
                    }

                    element.Dispose();
                    array[i] = default;
                }

                ArrayPool<CircularReferenceMessagePackSerializerOptions?>.Shared.Return(array);
                array = default!;
            }
        }

        public void Add(object reference)
        {
            if (referenceCount == referenceArray.Length)
            {
                var pool = ArrayPool<object>.Shared;
                var newArray = pool.Rent(1 + (referenceCount << 1));
                if (referenceArray != Array.Empty<object>())
                {
                    Array.Copy(referenceArray, newArray, referenceArray.Length);
                    pool.Return(referenceArray);
                }

                referenceArray = newArray;
            }

            referenceArray[referenceCount++] = reference;
        }

        public int FindIndex(object reference)
        {
            for (var i = 0; i < ReferenceSpan.Length; i++)
            {
                if (ReferenceSpan[i] == reference)
                {
                    return i;
                }
            }

            return -1;
        }

        public void Clear()
        {
            if (referenceCount == 0)
            {
                return;
            }

            Array.Clear(referenceArray, 0, referenceCount);
            referenceCount = 0;
        }

        public void Dispose()
        {
            Clear();
            var empty = Array.Empty<object>();
            if (referenceArray != empty)
            {
                ArrayPool<object>.Shared.Return(referenceArray);
                referenceArray = empty;
            }

            isInUse = false;
        }
    }
}

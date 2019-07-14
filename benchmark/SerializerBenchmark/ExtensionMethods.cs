// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Benchmark
{
    public static class ExtensionMethods
    {
        public static bool TrueEqualsString(this IEnumerable<string> a, IEnumerable<string> b)
        {
            return a.SequenceEqual(b);
        }

        public static bool TrueEqualsString(this string a, string b)
        {
            return a == b;
        }

        public static bool TrueEquals<T>(this T? a, T? b)
            where T : struct
        {
            if (!a.HasValue && !b.HasValue)
            {
                return true;
            }

            if (!a.HasValue)
            {
                return false;
            }

            if (!b.HasValue)
            {
                return false;
            }

            return a.Value.Equals(b.Value);
        }

        public static bool TrueEquals<T>(this T a, T b)
            where T : class, IGenericEquality<T>
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
            {
                return true;
            }

            if (ReferenceEquals(a, null))
            {
                return false;
            }

            if (ReferenceEquals(b, null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool TrueEqualsList<T>(this IEnumerable<T> a, IEnumerable<T> b)
            where T : class, IGenericEquality<T>
        {
            return a.SequenceEqual(b, GenericEqualityComparer<T>.Default);
        }

        public static bool TrueEqualsListDynamic<T>(this IEnumerable<T> a, IEnumerable<dynamic> b)
            where T : class, IGenericEquality<T>
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
            {
                return true;
            }

            if (ReferenceEquals(a, null))
            {
                return false;
            }

            if (ReferenceEquals(b, null))
            {
                return false;
            }

            using (IEnumerator<T> e1 = a.GetEnumerator())
            using (IEnumerator<dynamic> e2 = b.GetEnumerator())
            {
                while (true)
                {
                    var e1Next = e1.MoveNext();
                    var e2Next = e2.MoveNext();
                    if (e1Next != e2Next)
                    {
                        return false;
                    }

                    if (!e1Next && !e2Next)
                    {
                        break;
                    }

                    T c1 = e1.Current;
                    dynamic c2 = e2.Current;

                    if (c1 == null && c2 != null)
                    {
                        return false;
                    }

                    if (c2 == null && c1 != null)
                    {
                        return false;
                    }

                    if (!c1.EqualsDynamic(c2))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool IsTypedList(this Type type)
        {
            return
                (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>)) ||
                type.GetTypeInfo().GetInterfaces().Any(i =>
                    i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
        }

        private class GenericEqualityComparer<T> : IEqualityComparer<T>
            where T : class, IGenericEquality<T>
        {
            public static readonly GenericEqualityComparer<T> Default = new GenericEqualityComparer<T>();

            public bool Equals(T x, T y)
            {
                return x.TrueEquals(y);
            }

            public int GetHashCode(T obj)
            {
                return 0; // not fast, but not really important here
            }
        }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;

namespace MessagePack.Formatters
{
    /// <summary>
    /// Provides general helpers for creating collections (including dictionaries).
    /// </summary>
    /// <typeparam name="TCollection">The concrete type of collection to create.</typeparam>
    /// <typeparam name="TEqualityComparer">The type of equality comparer that we would hope to pass into the collection's constructor.</typeparam>
    internal static class CollectionHelpers<TCollection, TEqualityComparer>
        where TCollection : new()
    {
        /// <summary>
        /// The delegate that will create the collection, if the typical (int count, IEqualityComparer{T} equalityComparer) constructor was found.
        /// </summary>
        private static Func<int, TEqualityComparer, TCollection> collectionCreator;

        /// <summary>
        /// Initializes static members of the <see cref="CollectionHelpers{TCollection, TEqualityComparer}"/> class.
        /// </summary>
        /// <remarks>
        /// Initializes a delegate that is optimized to create a collection of a given size and using the given equality comparer, if possible.
        /// </remarks>
        static CollectionHelpers()
        {
            var ctor = typeof(TCollection).GetConstructor(new Type[] { typeof(int), typeof(TEqualityComparer) });
            if (ctor != null)
            {
                ParameterExpression param1 = Expression.Parameter(typeof(int), "count");
                ParameterExpression param2 = Expression.Parameter(typeof(TEqualityComparer), "equalityComparer");
                NewExpression body = Expression.New(ctor, param1, param2);
                collectionCreator = Expression.Lambda<Func<int, TEqualityComparer, TCollection>>(body, param1, param2).Compile();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <typeparamref name="TCollection"/> collection.
        /// </summary>
        /// <param name="count">The number of elements the collection should be prepared to receive.</param>
        /// <param name="equalityComparer">The equality comparer to initialize the collection with.</param>
        /// <returns>The newly initialized collection.</returns>
        /// <remarks>
        /// Use of the <paramref name="count"/> and <paramref name="equalityComparer"/> are a best effort.
        /// If we can't find a constructor on the collection in the expected shape, we'll just instantiate the collection with its default constructor.
        /// </remarks>
        internal static TCollection CreateHashCollection(int count, TEqualityComparer equalityComparer) => collectionCreator != null ? collectionCreator.Invoke(count, equalityComparer) : new TCollection();
    }
}

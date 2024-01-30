// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace MessagePack
{
    /// <summary>
    /// When applied to a <c>partial class</c> with the MessagePackAnalyzer package referenced,
    /// this attribute triggers a source generator that fills in the class with a perf-optimized
    /// implementation of an <see cref="IFormatterResolver"/>.
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    [Conditional("NEVERDEFINED")] // We only need this attribute for source generation, so we don't want it to be included in the user's built assembly.
    public sealed class CompositeResolverAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeResolverAttribute"/> class
        /// that describes the composite resolver to generate.
        /// </summary>
        /// <param name="resolvers">The list of resolvers that this resolver aggregates together.</param>
        public CompositeResolverAttribute(params Type[] resolvers)
        {
        }
    }
}

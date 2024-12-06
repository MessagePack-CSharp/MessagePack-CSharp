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
        /// <param name="formattersAndResolvers">The list of formatters and resolvers that this resolver aggregates together.</param>
        public CompositeResolverAttribute(params Type[] formattersAndResolvers)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically include any formatters that are defined
        /// in the same assembly as the type to which this attribute is applied.
        /// </summary>
        /// <remarks>
        /// When <see langword="true"/>, the resolver will be a superset of the default source-generated resolver
        /// (which only includes formatters for data types defined in the same assembly as the type to which this attribute is applied)
        /// by adding any additional hand-written formatters declared in the same assembly for data types in other assemblies.
        /// </remarks>
        public bool IncludeLocalFormatters { get; set; }
    }
}

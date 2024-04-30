// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace MessagePack
{
    /// <summary>
    /// An attribute to apply to a <see langword="partial" /> <see langword="class" /> that will serve as the
    /// source-generated resolver for MessagePack.
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    [Conditional("NEVERDEFINED")] // We only need this attribute for source generation, so we don't want it to be included in the user's built assembly.
    public class GeneratedMessagePackResolverAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedMessagePackResolverAttribute"/> class.
        /// </summary>
        public GeneratedMessagePackResolverAttribute()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether types will be serialized with their property names as well as their
        /// values in a key=value dictionary, as opposed to an array of values.
        /// </summary>
        public bool UseMapMode { get; set; }
    }
}

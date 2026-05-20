// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace MessagePack.Internal
{
    /// <summary>
    /// An assembly-level attribute that identifies the source-generated resolver for MessagePack for all types in this assembly.
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public class GeneratedAssemblyMessagePackResolverAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedAssemblyMessagePackResolverAttribute"/> class.
        /// </summary>
        /// <param name="resolverType">The type that implements <see cref="IFormatterResolver"/>.</param>
        /// <param name="majorVersion">
        /// The <see cref="Version.Major"/> component of the version of the generator that produced the resolver and formatters.
        /// This may be used to determine whether the resolver and formatters are compatible with the current version of MessagePack.
        /// </param>
        /// <param name="minorVersion">
        /// The <see cref="Version.Minor"/> component of the version of the generator that produced the resolver and formatters.
        /// This may be used to determine whether the resolver and formatters are compatible with the current version of MessagePack.
        /// </param>
        public GeneratedAssemblyMessagePackResolverAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type resolverType, int majorVersion, int minorVersion)
        {
            ResolverType = resolverType;
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
        }

        public Type ResolverType { get; }

        public int MajorVersion { get; }

        public int MinorVersion { get; }
    }
}

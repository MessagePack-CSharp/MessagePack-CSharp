// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MessagePack
{
#if !DYNAMICCODEDUMPER
    /// <summary>
    /// An immutable description of options for running the <see cref="MessagePackSerializer"/>.
    /// </summary>
#endif
    public class MessagePackSerializerOptions
    {
        // see:http://msdn.microsoft.com/en-us/library/w3f99sx1.aspx
        internal static readonly Regex AssemblyNameVersionSelectorRegex = new Regex(@", Version=\d+.\d+.\d+.\d+, Culture=[\w-]+, PublicKeyToken=(?:null|[a-f0-9]{16})$", RegexOptions.Compiled);

        /// <summary>
        /// A collection of known dangerous types that are not expected in a typical MessagePack stream,
        /// and thus are rejected by the default implementation of <see cref="ThrowIfDeserializingTypeIsDisallowed(Type)"/>.
        /// </summary>
        private static readonly HashSet<string> BlacklistCheck = new HashSet<string>
        {
            "System.CodeDom.Compiler.TempFileCollection",
            "System.Management.IWbemClassObjectFreeThreaded",
        };

#if !DYNAMICCODEDUMPER
        /// <summary>
        /// Gets a good default set of options that uses the <see cref="Resolvers.StandardResolver"/> and no compression.
        /// </summary>
        public static MessagePackSerializerOptions Standard => MessagePackSerializerOptionsDefaultSettingsLazyInitializationHelper.Standard;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackSerializerOptions"/> class.
        /// </summary>
        protected internal MessagePackSerializerOptions(IFormatterResolver resolver)
        {
            this.Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackSerializerOptions"/> class
        /// with members initialized from an existing instance.
        /// </summary>
        /// <param name="copyFrom">The options to copy from.</param>
        protected MessagePackSerializerOptions(MessagePackSerializerOptions copyFrom)
        {
            if (copyFrom == null)
            {
                throw new ArgumentNullException(nameof(copyFrom));
            }

            this.Resolver = copyFrom.Resolver;
            this.Compression = copyFrom.Compression;
            this.OldSpec = copyFrom.OldSpec;
            this.OmitAssemblyVersion = copyFrom.OmitAssemblyVersion;
            this.AllowAssemblyVersionMismatch = copyFrom.AllowAssemblyVersionMismatch;
            this.Security = copyFrom.Security;
        }

        /// <summary>
        /// Gets the resolver to use for complex types.
        /// </summary>
        /// <value>An instance of <see cref="IFormatterResolver"/>. Never <c>null</c>.</value>
        /// <exception cref="ArgumentNullException">Thrown if an attempt is made to set this property to <c>null</c>.</exception>
        public IFormatterResolver Resolver { get; private set; }

        /// <summary>
        /// Gets the compression scheme to apply to serialized sequences.
        /// </summary>
        /// <remarks>
        /// When set to something other than <see cref="MessagePackCompression.None"/>,
        /// deserialization can still work on uncompressed sequences,
        /// and serialization may not compress if msgpack sequences are short enough that compression would not likely be advantageous.
        /// </remarks>
        public MessagePackCompression Compression { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to serialize with <see cref="MessagePackWriter.OldSpec"/> set to some value
        /// causing messagepack spec compliance to be explicitly set to the old or new format.
        /// </summary>
        /// <value>
        /// A null value means the <see cref="MessagePackWriter"/>'s default or previous setting will be used.
        /// A non-null value will be applied to the <see cref="MessagePackWriter.OldSpec"/> property for the duration of a
        /// serialization and then reverted to its prior setting.
        /// </value>
        /// <remarks>
        /// Reading always supports both new and old spec.
        /// </remarks>
        public bool? OldSpec { get; private set; }

        /// <summary>
        /// Gets a value indicating whether serialization should omit assembly version, culture and public key token metadata when using the typeless formatter.
        /// </summary>
        /// <value>The default value is <c>false</c>.</value>
        public bool OmitAssemblyVersion { get; private set; }

        /// <summary>
        /// Gets a value indicating whether deserialization may instantiate types from an assembly with a different version if a matching version cannot be found.
        /// </summary>
        /// <value>The default value is <c>false</c>.</value>
        public bool AllowAssemblyVersionMismatch { get; private set; }

        /// <summary>
        /// Gets the security-related options for deserializing messagepack sequences.
        /// </summary>
        /// <value>
        /// The default value is to use <see cref="MessagePackSecurity.TrustedData"/>.
        /// </value>
        public MessagePackSecurity Security { get; private set; } = MessagePackSecurity.TrustedData;

        /// <summary>
        /// Gets a type given a string representation of the type.
        /// </summary>
        /// <param name="typeName">The name of the type to load. This is typically the <see cref="Type.AssemblyQualifiedName"/> but may use the assembly's simple name.</param>
        /// <returns>The loaded type or <c>null</c> if no matching type could be found.</returns>
        public virtual Type LoadType(string typeName)
        {
            Type result = Type.GetType(typeName, false);
            if (result == null && this.AllowAssemblyVersionMismatch)
            {
                string shortenedName = AssemblyNameVersionSelectorRegex.Replace(typeName, string.Empty);
                if (shortenedName != typeName)
                {
                    result = Type.GetType(shortenedName, false);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks whether a given type may be deserialized.
        /// </summary>
        /// <param name="type">The type to be instantiated.</param>
        /// <exception cref="TypeAccessException">Thrown if the <paramref name="type"/> is not allowed to be deserialized.</exception>
        /// <remarks>
        /// This method provides a means for an important security mitigation when using the Typeless formatter to prevent untrusted messagepack from
        /// deserializing objects that may be harmful if instantiated, disposed or finalized.
        /// The default implementation throws for only a few known dangerous types.
        /// Applications that deserialize from untrusted sources should override this method and throw if the type is not among the expected set.
        /// </remarks>
        public virtual void ThrowIfDeserializingTypeIsDisallowed(Type type)
        {
            if (BlacklistCheck.Contains(type.FullName))
            {
                throw new MessagePackSerializationException("Deserialization attempted to create the type " + type.FullName + " which is not allowed.");
            }
        }

        /// <summary>
        /// Gets a copy of these options with the <see cref="Resolver"/> property set to a new value.
        /// </summary>
        /// <param name="resolver">The new value for the <see cref="Resolver"/>.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSerializerOptions WithResolver(IFormatterResolver resolver)
        {
            if (this.Resolver == resolver)
            {
                return this;
            }

            var result = this.Clone();
            result.Resolver = resolver;
            return result;
        }

        /// <summary>
        /// Gets a copy of these options with the <see cref="Compression"/> property set to a new value.
        /// </summary>
        /// <param name="compression">The new value for the <see cref="Compression"/> property.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSerializerOptions WithCompression(MessagePackCompression compression)
        {
            if (this.Compression == compression)
            {
                return this;
            }

            var result = this.Clone();
            result.Compression = compression;
            return result;
        }

        /// <summary>
        /// Gets a copy of these options with the <see cref="OldSpec"/> property set to a new value.
        /// </summary>
        /// <param name="oldSpec">The new value for the <see cref="OldSpec"/>.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSerializerOptions WithOldSpec(bool? oldSpec = true)
        {
            if (this.OldSpec == oldSpec)
            {
                return this;
            }

            var result = this.Clone();
            result.OldSpec = oldSpec;
            return result;
        }

        /// <summary>
        /// Gets a copy of these options with the <see cref="OmitAssemblyVersion"/> property set to a new value.
        /// </summary>
        /// <param name="omitAssemblyVersion">The new value for the <see cref="OmitAssemblyVersion"/> property.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSerializerOptions WithOmitAssemblyVersion(bool omitAssemblyVersion)
        {
            if (this.OmitAssemblyVersion == omitAssemblyVersion)
            {
                return this;
            }

            var result = this.Clone();
            result.OmitAssemblyVersion = omitAssemblyVersion;
            return result;
        }

        /// <summary>
        /// Gets a copy of these options with the <see cref="AllowAssemblyVersionMismatch"/> property set to a new value.
        /// </summary>
        /// <param name="allowAssemblyVersionMismatch">The new value for the <see cref="AllowAssemblyVersionMismatch"/> property.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSerializerOptions WithAllowAssemblyVersionMismatch(bool allowAssemblyVersionMismatch)
        {
            if (this.AllowAssemblyVersionMismatch == allowAssemblyVersionMismatch)
            {
                return this;
            }

            var result = this.Clone();
            result.AllowAssemblyVersionMismatch = allowAssemblyVersionMismatch;
            return result;
        }

        /// <summary>
        /// Gets a copy of these options with the <see cref="Security"/> property set to a new value.
        /// </summary>
        /// <param name="security">The new value for the <see cref="Security"/> property.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSerializerOptions WithSecurity(MessagePackSecurity security)
        {
            if (security is null)
            {
                throw new ArgumentNullException(nameof(security));
            }

            if (this.Security == security)
            {
                return this;
            }

            var result = this.Clone();
            result.Security = security;
            return result;
        }

        /// <summary>
        /// Creates a clone of this instance with the same properties set.
        /// </summary>
        /// <returns>The cloned instance. Guaranteed to be a new instance.</returns>
        /// <exception cref="NotSupportedException">Thrown if this instance is a derived type that doesn't override this method.</exception>
        protected virtual MessagePackSerializerOptions Clone()
        {
            if (this.GetType() != typeof(MessagePackSerializerOptions))
            {
                throw new NotSupportedException($"The derived type {this.GetType().FullName} did not override the {nameof(Clone)} method as required.");
            }

            return new MessagePackSerializerOptions(this);
        }

#if !DYNAMICCODEDUMPER
        private static class MessagePackSerializerOptionsDefaultSettingsLazyInitializationHelper
        {
            public static readonly MessagePackSerializerOptions Standard = new MessagePackSerializerOptions(Resolvers.StandardResolver.Instance);
        }
#endif
    }
}

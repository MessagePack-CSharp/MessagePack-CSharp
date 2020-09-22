// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MessagePack
{
    [AttributeUsage(AttributeTargets.Module, AllowMultiple = true)]
    public sealed class UnsafeUnmanagedStructAttribute : Attribute
    {
        public Type Type { get; }

        public UnsafeUnmanagedStructFormatterImplementationKind Kind { get; }

        public UnsafeUnmanagedStructAttribute(Type type, UnsafeUnmanagedStructFormatterImplementationKind kind)
        {
            Type = type;
            Kind = kind;
        }
    }

    [Flags]
    public enum UnsafeUnmanagedStructFormatterImplementationKind
    {
        Self = 1,
        Array = 2,
    }
}

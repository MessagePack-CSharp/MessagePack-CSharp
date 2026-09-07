// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET5_0_OR_GREATER

#pragma warning disable CA1812

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Used by C# 9 for property <c>init</c> accessors.
    /// </summary>
    internal sealed class IsExternalInit
    {
    }
}

#endif

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace MessagePack.Tests
{
    internal static class TestUtilities
    {
        /// <summary>
        /// Gets a value indicating whether the mono runtime is executing this code.
        /// </summary>
        internal static bool IsRunningOnMono => Type.GetType("Mono.Runtime") != null;
    }
}

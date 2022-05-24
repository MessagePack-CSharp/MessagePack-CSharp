// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Xunit.Abstractions;

namespace MessagePack.Tests
{
    internal static class TestUtilities
    {
        /// <summary>
        /// Gets a value indicating whether the mono runtime is executing this code.
        /// </summary>
        internal static bool IsRunningOnMono => Type.GetType("Mono.Runtime") != null;

        internal static string ToHex(byte[] buffer) => BitConverter.ToString(buffer).Replace("-", string.Empty).ToLowerInvariant();
    }

    public class NullTestOutputHelper : ITestOutputHelper
    {
        public void WriteLine(string message)
        {
        }

        public void WriteLine(string format, params object[] args)
        {
        }
    }
}

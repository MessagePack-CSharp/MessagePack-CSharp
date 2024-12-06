// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace MessagePack
{
    /// <summary>
    /// Special behavior for running on the mono runtime.
    /// </summary>
    internal struct MonoProtection
    {
        /// <summary>
        /// Gets a value indicating whether the mono runtime is executing this code.
        /// </summary>
        internal static bool IsRunningOnMono => Type.GetType("Mono.RuntimeStructs") != null;

        /// <summary>
        /// A lock that we enter on mono when generating dynamic types.
        /// </summary>
        private static readonly object RefEmitLock = new object();

        /// <summary>
        /// The method to call within the expression of a <c>using</c> statement whose block surrounds all Ref.Emit code.
        /// </summary>
        /// <returns>The value to be disposed of to exit the Ref.Emit lock.</returns>
        /// <remarks>
        /// This is a no-op except when running on Mono.
        /// <see href="https://github.com/mono/mono/issues/20369#issuecomment-690316456">Mono's implementation of Ref.Emit is not thread-safe</see> so we have to lock around all use of it
        /// when using that runtime.
        /// </remarks>
        internal static MonoProtectionDisposal EnterRefEmitLock() => IsRunningOnMono ? new MonoProtectionDisposal(RefEmitLock) : default;
    }

    internal struct MonoProtectionDisposal : IDisposable
    {
        private readonly object lockObject;

        internal MonoProtectionDisposal(object lockObject)
        {
            this.lockObject = lockObject;
            Monitor.Enter(lockObject);
        }

        public void Dispose()
        {
            if (this.lockObject is object)
            {
                Monitor.Exit(this.lockObject);
            }
        }
    }
}

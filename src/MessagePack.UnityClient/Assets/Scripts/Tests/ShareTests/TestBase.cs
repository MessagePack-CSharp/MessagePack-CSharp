// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace MessagePack.Tests
{
    public abstract class TestBase
    {
        protected readonly CancellationToken TimeoutToken = new CancellationTokenSource(TestTimeoutSpan).Token;

        private static readonly TimeSpan TestTimeoutSpan = Debugger.IsAttached ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(5);
    }
}

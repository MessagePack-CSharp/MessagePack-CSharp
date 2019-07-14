// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Internal.Test
{
    public class MessagePackInternalTest
    {
        [Fact]
        public void NoTypesAreExportedByMessagePackInternalAssembly()
        {
            System.Reflection.Assembly assembly = typeof(MessagePackReader).Assembly;

            Assert.Empty(assembly.ExportedTypes);
        }
    }
}

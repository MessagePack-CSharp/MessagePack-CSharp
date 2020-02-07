// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;

internal static class ReferencesHelper
{
    internal static ReferenceAssemblies DefaultReferences = ReferenceAssemblies.NetFramework.Net472.Default
        .WithPackages(ImmutableArray.Create(
            new PackageIdentity("MessagePack", "2.0.335")));
}

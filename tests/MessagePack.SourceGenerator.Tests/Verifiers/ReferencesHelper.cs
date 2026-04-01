// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

internal static class ReferencesHelper
{
    internal static ReferenceAssemblies DefaultTargetFrameworkReferences = ReferenceAssemblies.Net.Net90;

    internal static IEnumerable<MetadataReference> GetReferences(ReferencesSet references)
    {
        if (references.HasFlag(ReferencesSet.MessagePackAnnotations))
        {
            yield return MetadataReference.CreateFromFile(typeof(MessagePackObjectAttribute).Assembly.Location);
        }

        if (references.HasFlag(ReferencesSet.MessagePack))
        {
            yield return MetadataReference.CreateFromFile(typeof(MessagePackSerializer).Assembly.Location);
        }
    }

    internal static Compilation AddMessagePackReferences(Compilation compilation, ReferencesSet references)
        => compilation.AddReferences(GetReferences(references));
}

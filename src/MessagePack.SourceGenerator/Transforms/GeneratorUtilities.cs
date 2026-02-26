// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.Transforms;

internal static class GeneratorUtilities
{
    internal static bool ShouldUseFormatterResolver(MemberSerializationInfo[] infos)
    {
        _ = infos;
        return false;
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.Transforms;

internal static class GeneratorUtilities
{
    internal static bool ShouldUseFormatterResolver(MemberSerializationInfo[] infos)
    {
        foreach (var memberSerializationInfo in infos)
        {
            if (memberSerializationInfo.CustomFormatterTypeName == null && Array.IndexOf(AnalyzerUtilities.PrimitiveTypes, memberSerializationInfo.Type) == -1)
            {
                return true;
            }
        }

        return false;
    }
}

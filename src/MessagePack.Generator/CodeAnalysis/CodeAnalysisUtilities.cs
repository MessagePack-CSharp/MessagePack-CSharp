// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

internal static class CodeAnalysisUtilities
{
    internal static string QualifyWithOptionalNamespace(string leafTypeOrNamespace, string? baseNamespace)
    {
        return string.IsNullOrEmpty(baseNamespace) ? leafTypeOrNamespace : (baseNamespace!.EndsWith("::") ? $"{baseNamespace}{leafTypeOrNamespace}" : $"{baseNamespace}.{leafTypeOrNamespace}");
    }

    internal static string AppendNameToNamespace(string left, string? right)
    {
        return string.IsNullOrEmpty(right) ? left : $"{left}.{right}";
    }

    internal static string GetSanitizedFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }

        return fileName;
    }
}

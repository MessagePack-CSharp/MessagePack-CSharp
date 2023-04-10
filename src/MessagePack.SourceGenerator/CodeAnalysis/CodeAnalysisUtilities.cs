// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.CodeAnalysis;

internal static class CodeAnalysisUtilities
{
    private static readonly HashSet<char> InvalidFileNameChars = new(Path.GetInvalidFileNameChars());

    static CodeAnalysisUtilities()
    {
        // Roslyn really doesn't like angle brackets in file names, even on operating systems that allow them (e.g. linux).
        // See https://github.com/dotnet/roslyn/issues/67653
        InvalidFileNameChars.Add('<');
        InvalidFileNameChars.Add('>');
    }

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
        foreach (char c in InvalidFileNameChars)
        {
            fileName = fileName.Replace(c, '_');
        }

        return fileName;
    }
}

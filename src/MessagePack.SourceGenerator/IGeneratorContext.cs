// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator;

public interface IGeneratorContext
{
    CancellationToken CancellationToken { get; }

    void AddSource(string hintName, string source);

    void ReportDiagnostic(Diagnostic diagnostic);
}

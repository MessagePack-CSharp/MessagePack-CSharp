﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePack.Generator.CodeAnalysis;

public interface IResolverRegisterInfo
{
    string FullName { get; }

    string FormatterName { get; }

    IReadOnlyCollection<Diagnostic> Diagnostics { get; }
}
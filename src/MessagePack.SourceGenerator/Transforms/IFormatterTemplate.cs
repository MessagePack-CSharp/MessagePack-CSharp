﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.Transforms;

public interface IFormatterTemplate
{
    string FileName { get; }

    QualifiedNamedTypeName ResolverName { get; }

    ResolverRegisterInfo Info { get; }

    string TransformText();
}

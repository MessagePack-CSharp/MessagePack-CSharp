﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using MessagePackCompiler.CodeAnalysis;

namespace MessagePackCompiler.Generator
{
    public interface IFormatterTemplate : ITemplate
    {
        ObjectSerializationInfo[] ObjectSerializationInfos { get; }
    }

    public interface ITemplate
    {
        string Namespace { get; }

        void TransformAppend(StringBuilder builder);
    }
}

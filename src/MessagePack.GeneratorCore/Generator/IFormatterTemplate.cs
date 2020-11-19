// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePackCompiler.CodeAnalysis;

namespace MessagePackCompiler.Generator
{
    public interface IFormatterTemplate
    {
        string Namespace { get; }

        ObjectSerializationInfo[] ObjectSerializationInfos { get; }

        string TransformText();
    }
}

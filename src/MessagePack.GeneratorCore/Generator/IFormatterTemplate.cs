// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePackCompiler.CodeAnalysis;

namespace MessagePackCompiler.Generator
{
    public interface IFormatterTemplate : ITemplate
    {
        string Namespace { get; }

        ObjectSerializationInfo[] ObjectSerializationInfos { get; }
    }

    public interface ITemplate
    {
        void TransformAppend(ref Cysharp.Text.Utf8ValueStringBuilder builder);
    }
}

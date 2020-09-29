// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePackCompiler.CodeAnalysis;

namespace MessagePackCompiler.Generator
{
    public partial class FormatterTemplate : IFormatterTemplate
    {
        public string Namespace { get; set; }

        public ObjectSerializationInfo[] ObjectSerializationInfos { get; set; }
    }

    public partial class StringKeyFormatterTemplate : IFormatterTemplate
    {
        public string Namespace { get; set; }

        public ObjectSerializationInfo[] ObjectSerializationInfos { get; set; }
    }

    public partial class ResolverTemplate
    {
        public string Namespace { get; set; }

        public string FormatterNamespace { get; set; }

        public string ResolverName { get; set; } = "GeneratedResolver";

        public IResolverRegisterInfo[] RegisterInfos { get; set; }
    }

    public partial class EnumTemplate
    {
        public string Namespace { get; set; }

        public EnumSerializationInfo[] EnumSerializationInfos { get; set; }
    }

    public partial class UnionTemplate
    {
        public string Namespace { get; set; }

        public UnionSerializationInfo[] UnionSerializationInfos { get; set; }
    }
}

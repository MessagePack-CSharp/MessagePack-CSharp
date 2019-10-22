// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePack.CodeGenerator.Generator
{
    public partial class FormatterTemplate
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

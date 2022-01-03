// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using MessagePackCompiler.CodeAnalysis;

namespace MessagePackCompiler.Generator
{
    public partial struct FormatterTemplate : IFormatterTemplate
    {
        public FormatterTemplate(string @namespace, ObjectSerializationInfo[] objectSerializationInfos, CancellationToken cancellationToken)
        {
            Namespace = @namespace;
            ObjectSerializationInfos = objectSerializationInfos;
            CancellationToken = cancellationToken;
        }

        public string Namespace { get; }

        public ObjectSerializationInfo[] ObjectSerializationInfos { get; }

        public CancellationToken CancellationToken { get; }
    }

    public partial struct StringKeyFormatterTemplate : IFormatterTemplate
    {
        public StringKeyFormatterTemplate(string @namespace, ObjectSerializationInfo[] objectSerializationInfos, CancellationToken cancellationToken)
        {
            Namespace = @namespace;
            ObjectSerializationInfos = objectSerializationInfos;
            CancellationToken = cancellationToken;
        }

        public string Namespace { get; }

        public ObjectSerializationInfo[] ObjectSerializationInfos { get; }

        public CancellationToken CancellationToken { get; }
    }

    public partial struct ResolverTemplate : ITemplate
    {
        public ResolverTemplate(string @namespace, string formatterNamespace, string resolverName, IResolverRegisterInfo[] registerInfos, CancellationToken cancellationToken)
        {
            Namespace = @namespace;
            FormatterNamespace = formatterNamespace;
            ResolverName = resolverName;
            RegisterInfos = registerInfos;
            CancellationToken = cancellationToken;
        }

        public string Namespace { get; }

        public string FormatterNamespace { get; }

        public string ResolverName { get; }

        public IResolverRegisterInfo[] RegisterInfos { get; }

        public CancellationToken CancellationToken { get; }
    }

    public partial struct EnumTemplate : ITemplate
    {
        public EnumTemplate(string @namespace, EnumSerializationInfo[] enumSerializationInfos, CancellationToken cancellationToken)
        {
            Namespace = @namespace;
            EnumSerializationInfos = enumSerializationInfos;
            CancellationToken = cancellationToken;
        }

        public string Namespace { get; }

        public EnumSerializationInfo[] EnumSerializationInfos { get; }

        public CancellationToken CancellationToken { get; }
    }

    public partial struct UnionTemplate : ITemplate
    {
        public UnionTemplate(string @namespace, UnionSerializationInfo[] unionSerializationInfos, CancellationToken cancellationToken)
        {
            Namespace = @namespace;
            UnionSerializationInfos = unionSerializationInfos;
            CancellationToken = cancellationToken;
        }

        public string Namespace { get; }

        public UnionSerializationInfo[] UnionSerializationInfos { get; }

        public CancellationToken CancellationToken { get; }
    }
}

// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePackCompiler.CodeAnalysis;

namespace MessagePackCompiler.Generator
{
    public partial class FormatterTemplate : IFormatterTemplate
    {
        public FormatterTemplate(string @namespace, ObjectSerializationInfo[] objectSerializationInfos)
        {
            Namespace = @namespace;
            ObjectSerializationInfos = objectSerializationInfos;
        }

        public string Namespace { get; }

        public ObjectSerializationInfo[] ObjectSerializationInfos { get; }

        public string TransformText()
        {
            System.Text.StringBuilder builder = new();
            TransformAppend(builder);
            return builder.ToString();
        }
    }

    public partial class StringKeyFormatterTemplate : IFormatterTemplate
    {
        public StringKeyFormatterTemplate(string @namespace, ObjectSerializationInfo[] objectSerializationInfos)
        {
            Namespace = @namespace;
            ObjectSerializationInfos = objectSerializationInfos;
        }

        public string Namespace { get; }

        public ObjectSerializationInfo[] ObjectSerializationInfos { get; }

        public string TransformText()
        {
            System.Text.StringBuilder builder = new();
            TransformAppend(builder);
            return builder.ToString();
        }
    }

    public partial class ResolverTemplate
    {
        public ResolverTemplate(string @namespace, string formatterNamespace, string resolverName, IResolverRegisterInfo[] registerInfos)
        {
            Namespace = @namespace;
            FormatterNamespace = formatterNamespace;
            ResolverName = resolverName;
            RegisterInfos = registerInfos;
        }

        public string Namespace { get; }

        public string FormatterNamespace { get; }

        public string ResolverName { get; }

        public IResolverRegisterInfo[] RegisterInfos { get; }

        public string TransformText()
        {
            System.Text.StringBuilder builder = new();
            TransformAppend(builder);
            return builder.ToString();
        }
    }

    public partial class EnumTemplate
    {
        public EnumTemplate(string @namespace, EnumSerializationInfo[] enumSerializationInfos)
        {
            Namespace = @namespace;
            EnumSerializationInfos = enumSerializationInfos;
        }

        public string Namespace { get; }

        public EnumSerializationInfo[] EnumSerializationInfos { get; }

        public string TransformText()
        {
            System.Text.StringBuilder builder = new();
            TransformAppend(builder);
            return builder.ToString();
        }
    }

    public partial class UnionTemplate
    {
        public UnionTemplate(string @namespace, UnionSerializationInfo[] unionSerializationInfos)
        {
            Namespace = @namespace;
            UnionSerializationInfos = unionSerializationInfos;
        }

        public string Namespace { get; }

        public UnionSerializationInfo[] UnionSerializationInfos { get; }

        public string TransformText()
        {
            System.Text.StringBuilder builder = new();
            TransformAppend(builder);
            return builder.ToString();
        }
    }
}

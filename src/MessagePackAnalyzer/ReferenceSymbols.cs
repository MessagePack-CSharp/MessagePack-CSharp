// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePackAnalyzer
{
    public class ReferenceSymbols
    {
        internal INamedTypeSymbol Task { get; }

        internal INamedTypeSymbol TaskOfT { get; }

        internal INamedTypeSymbol MessagePackObjectAttribute { get; }

        internal INamedTypeSymbol UnionAttribute { get; }

        internal INamedTypeSymbol SerializationConstructorAttribute { get; }

        internal INamedTypeSymbol KeyAttribute { get; }

        internal INamedTypeSymbol IgnoreAttribute { get; }

        internal INamedTypeSymbol IgnoreDataMemberAttribute { get; }

        internal INamedTypeSymbol IMessagePackSerializationCallbackReceiver { get; }

        public ReferenceSymbols(Compilation compilation)
        {
            this.TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            this.Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            this.MessagePackObjectAttribute = compilation.GetTypeByMetadataName("MessagePack.MessagePackObjectAttribute");
            this.UnionAttribute = compilation.GetTypeByMetadataName("MessagePack.UnionAttribute");
            this.SerializationConstructorAttribute = compilation.GetTypeByMetadataName("MessagePack.SerializationConstructorAttribute");
            this.KeyAttribute = compilation.GetTypeByMetadataName("MessagePack.KeyAttribute");
            this.IgnoreAttribute = compilation.GetTypeByMetadataName("MessagePack.IgnoreMemberAttribute");
            this.IgnoreDataMemberAttribute = compilation.GetTypeByMetadataName("System.Runtime.Serialization.IgnoreDataMemberAttribute");
            this.IMessagePackSerializationCallbackReceiver = compilation.GetTypeByMetadataName("MessagePack.IMessagePackSerializationCallbackReceiver");
        }
    }
}

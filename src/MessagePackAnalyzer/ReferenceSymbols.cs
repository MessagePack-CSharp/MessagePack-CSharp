// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace MessagePackAnalyzer
{
    public class ReferenceSymbols
    {
        private ReferenceSymbols(
            INamedTypeSymbol messagePackObjectAttribute,
            INamedTypeSymbol unionAttribute,
            INamedTypeSymbol keyAttribute,
            INamedTypeSymbol ignoreAttribute,
            INamedTypeSymbol formatterAttribute,
            INamedTypeSymbol messagePackFormatter,
            INamedTypeSymbol ignoreDataMemberAttribute)
        {
            this.MessagePackObjectAttribute = messagePackObjectAttribute;
            this.UnionAttribute = unionAttribute;
            this.KeyAttribute = keyAttribute;
            this.IgnoreAttribute = ignoreAttribute;
            this.FormatterAttribute = formatterAttribute;
            this.MessagePackFormatter = messagePackFormatter;
            this.IgnoreDataMemberAttribute = ignoreDataMemberAttribute;
        }

        internal INamedTypeSymbol MessagePackObjectAttribute { get; }

        internal INamedTypeSymbol UnionAttribute { get; }

        internal INamedTypeSymbol KeyAttribute { get; }

        internal INamedTypeSymbol IgnoreAttribute { get; }

        internal INamedTypeSymbol FormatterAttribute { get; }

        internal INamedTypeSymbol MessagePackFormatter { get; }

        internal INamedTypeSymbol IgnoreDataMemberAttribute { get; }

        public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out ReferenceSymbols? instance)
        {
            instance = null;

            var messagePackObjectAttribute = compilation.GetTypeByMetadataName("MessagePack.MessagePackObjectAttribute");
            if (messagePackObjectAttribute is null)
            {
                return false;
            }

            var unionAttribute = compilation.GetTypeByMetadataName("MessagePack.UnionAttribute");
            if (unionAttribute is null)
            {
                return false;
            }

            var keyAttribute = compilation.GetTypeByMetadataName("MessagePack.KeyAttribute");
            if (keyAttribute is null)
            {
                return false;
            }

            var ignoreAttribute = compilation.GetTypeByMetadataName("MessagePack.IgnoreMemberAttribute");
            if (ignoreAttribute is null)
            {
                return false;
            }

            var formatterAttribute = compilation.GetTypeByMetadataName("MessagePack.MessagePackFormatterAttribute");
            if (formatterAttribute is null)
            {
                return false;
            }

            var messageFormatter = compilation.GetTypeByMetadataName("MessagePack.Formatters.IMessagePackFormatter");
            if (messageFormatter is null)
            {
                return false;
            }

            var ignoreDataMemberAttribute = compilation.GetTypeByMetadataName("System.Runtime.Serialization.IgnoreDataMemberAttribute");
            if (ignoreDataMemberAttribute is null)
            {
                return false;
            }

            instance = new ReferenceSymbols(
                messagePackObjectAttribute,
                unionAttribute,
                keyAttribute,
                ignoreAttribute,
                formatterAttribute,
                messageFormatter,
                ignoreDataMemberAttribute);
            return true;
        }
    }
}
